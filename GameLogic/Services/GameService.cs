using authorization;
using GameLogic.Enums;
using GameLogic.Interfaces;
using CardsService;
using System;
using System.Collections.Generic;
using System.Text;
using GameLogic.Entities;
using System.Runtime.Serialization.Formatters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace GameLogic.Services
{
    public class GameService : IGameService
    {
        private readonly Dictionary<Guid, GameSession> sessions = new();
        private readonly IVotingService _votingService;
        private readonly IThemesService _themesService;
        private readonly IGameWorker _gameWorker;
        private readonly IBotFactory _botFactory;
        private readonly ILogger<GameService> _logger;
        public List<SlotID> GeneratePlayerOrder(List<SlotID> playersIDs)
        {
            return playersIDs.OrderBy(_ => Guid.NewGuid()).ToList();
        }
        public GameService(IVotingService votingService, IThemesService themesService, IBotFactory botFactory, IGameWorker gameWorker, ILogger<GameService> logger)
        {
            _votingService = votingService;
            _themesService = themesService;
            _botFactory = botFactory;
            _gameWorker = gameWorker;
            _logger = logger;
        }
        public Guid CreateGameSession(List<UserId> playersIDs, GameSettings settings)
        {
            List<PlayerSlot> PlayerSlots = new List<PlayerSlot>();
            Dictionary<UserId, SlotID> PlayerIDs = new Dictionary<UserId, SlotID>();
            Dictionary<SlotID, IDecisionMaker> Bots = new Dictionary<SlotID, IDecisionMaker>();

            int currentSlotCounter = 0;
            foreach (UserId userId in playersIDs)
            {
                PlayerSlot pSlot = new PlayerSlot
                {
                    Id = new SlotID { Id = currentSlotCounter },
                    Username = userId.ToString(),
                    IsBot = false
                };
                PlayerSlots.Add(pSlot);
                PlayerIDs[userId] = pSlot.Id;
                currentSlotCounter++;
            }

            for (int i = 0; i < settings.BotCount; i++)
            {
                PlayerSlot bSlot = new PlayerSlot
                {
                    Id = new SlotID { Id = currentSlotCounter },
                    Username = $"Бот_{i}",
                    IsBot = true
                };
                PlayerSlots.Add(bSlot);
                Bots[bSlot.Id] = _botFactory.CreateBot();
                currentSlotCounter++;
            }
            var session = new SpyGameSession
            {
                GameId = Guid.NewGuid(),
                PlayerSlots = PlayerSlots,
                PlayerIDs = PlayerIDs,
                Bots = Bots,
                GameSettings = settings,
                CurrentRound = 1,
                CurrentPlayerIndex = 0,
                CurrentPlayerOrder = GeneratePlayerOrder(PlayerSlots.Select(s => s.Id).ToList()),
                CurrentStage = GameStage.Round,
                MessagesList = new List<Message>(),
                PlayerCards = new Dictionary<SlotID, Card>(),
                CurrentTurnStartTime = DateTime.Now,
                CurrentTurnNumber = 0,
                PlayerComments = PlayerSlots.ToDictionary(Slot => Slot.Id, Slot => string.Empty),
            };

            AssignCards(session);
            sessions[session.GameId] = session;

            _logger.LogInformation("New GameSession {GameId} created", session.GameId);

            return session.GameId;
        }
        public Dictionary<SlotID, Card> AssignCards(GameSession session)
        {
            var random = new Random();
            int spyIndex = random.Next(session.PlayerSlots.Count);

            string word = _themesService.GetRandomWordByTheme(session.GameSettings.Theme);

            session.CurrentWord = word;

            var cards = new Dictionary<SlotID, Card>();

            for (int i = 0; i < session.PlayerSlots.Count; i++)
            {
                var playerId = session.PlayerSlots[i].Id;
                bool isSpy = (i == spyIndex);
                var card = new Card
                {
                    IsSpy = isSpy,
                    Word = isSpy ? session.GameSettings.Theme : word
                };
                cards[playerId] = card;
            }

            session.PlayerCards = cards;

            _logger.LogInformation("Cards assigned");

            return cards;
        }
        public GameSession GetGameSessionById(Guid GameSessionId)
        {
            if (!sessions.TryGetValue(GameSessionId, out var session))
                _logger.LogWarning("Game session with ID {GameSessionId} not found", GameSessionId);

            return session;
        }
        public List<SlotID> GetPlayerOrder(GameSession session)
        {
            return session.CurrentPlayerOrder;
        }
        public IVotingService GetVoteService(GameSession session)
        {
            return _votingService;
        }
        public SlotID WhoseTurn(GameSession session)
        {
            if (session.CurrentPlayerIndex == -1)
                return null;

            return session.CurrentPlayerOrder[session.CurrentPlayerIndex];
        }
        public void MessageReceived(GameSession session, string message)
        {
            if (session.CurrentStage != GameStage.Round)
                throw new InvalidOperationException("Сейчас не этап ходов");

            var currentPlayerId = WhoseTurn(session);
            if (currentPlayerId == null){
                _logger.LogCritical("No active player");
                throw new InvalidOperationException("Нет активного игрока");
            }

            session.MessagesList.Add(new Message(currentPlayerId, message));
            AdvanceTurn(session);
        }

        private void AdvanceTurn(GameSession session)
        {
            session.CurrentPlayerIndex++;
            session.CurrentTurnNumber++;
            session.CurrentTurnStartTime = DateTime.Now;

            if (session.CurrentPlayerIndex >= session.PlayerSlots.Count)
            {
                if (session.CurrentRound == session.GameSettings.TotalRounds)
                {
                    session.CurrentPlayerIndex = -1;
                    session.CurrentStage = GameStage.Voting;

                    _logger.LogInformation(
                        "All rounds finished for session {GameId}. Moving to Voting stage", session.GameId
                        );
                }
                else
                {
                    session.CurrentPlayerIndex = 0;
                    session.CurrentRound++;

                    _logger.LogInformation(
                        "Round {CurrentRound} started for session {GameId}", session.CurrentRound, session.GameId
                        );
                }
            }
        }

        public async Task StartVotingAsync(Guid roomId, GameSession session)
        {
            if (session.VotingStartTime != default)
                return;

            session.CurrentPlayerIndex = -1;
            session.CurrentStage = GameStage.Voting;
            session.Votes = new Dictionary<SlotID, SlotID>();
            session.VotingEnded = false;
            session.VotingStartTime = DateTime.Now;
            session.IsUsingExtraTime = false;
            session.ExtraTime = DateTime.MinValue;
            session.IsPlayerReadyToEndVotingDict = new Dictionary<SlotID, bool>();

            foreach (PlayerSlot slot in session.PlayerSlots)
            {
                session.IsPlayerReadyToEndVotingDict[slot.Id] = false;
            }

            foreach (var botPair in session.Bots)
            {
                SlotID botId = botPair.Key;
                IDecisionMaker bot = botPair.Value;

                var context = BuildBotContext(session, botId);

                SlotID targetId = bot.MakeVote(context);

                _votingService.Vote(session, botId, targetId);
                var report = _votingService.GetVotingReport(session);
                await _gameWorker.OnVoteMadeAsync(roomId, report);
                await _gameWorker.MakeReadyEndVoteAsync(roomId, session, botId, true);
            }

            _logger.LogInformation("Voting phase started for game session {GameId}", session.GameId);
        }

        public DateTime GetCurrentTurnStartTime(GameSession session)
        {
            return session.CurrentTurnStartTime;
        }

        public DateTime GetVotingStartTime(GameSession session)
        {
            return session.VotingStartTime;
        }

        public int GetCurrentTurnNumnber(GameSession session)
        {
            return session.CurrentTurnNumber;
        }
        public Card GetPlayerCardBySlotID(GameSession session, SlotID slotId)
        {
            return session.PlayerCards[slotId];
        }
        public SlotID GetSlotIDByUserID(GameSession session, UserId userId)
        {
            return session.PlayerIDs[userId];
        }

        public void SetExtraTime(GameSession session, DateTime time)
        {
            session.ExtraTime = time;
        }

        public void SetIsUsingExtraTime(GameSession session, bool isUsing)
        {
            session.IsUsingExtraTime = isUsing;
        }

        public DateTime GetExtraTime(GameSession session)
        {
            return session.ExtraTime;
        }

        public bool GetIsUdingExtraTime(GameSession session)
        {
            return session.IsUsingExtraTime;
        }

        private GameContext BuildBotContext(GameSession session, SlotID botId)
        {
            var botCard = session.PlayerCards[botId];

            var safePlayersList = session.PlayerSlots
                .Select(slot => new PlayerPublicInfo(slot.Id, "Игрок_" + slot.Id.ToString(), session.PlayerComments[slot.Id]))
                .ToList();

            return new GameContext(
                botId,
                botCard.IsSpy,
                botCard.Word,
                safePlayersList,
                session.MessagesList.ToList(),
                session.GameSettings.Theme
            );
        }
        public async Task ProcessBotActionsAsync(Guid roomId, GameSession session)
        {
            while (session.CurrentStage == GameStage.Round)
            {
                var nextPlayerId = WhoseTurn(session);

                if (nextPlayerId == null || !session.Bots.TryGetValue(nextPlayerId, out var bot))
                {
                    break;
                }

                var context = BuildBotContext(session, nextPlayerId);
                string botMessage = bot.MakeMessage(context);

                session.MessagesList.Add(new Message(nextPlayerId, botMessage));

                AdvanceTurn(session);
                var playerAfterBot = WhoseTurn(session);
                await _gameWorker.OnTurnMadeAsync(roomId, nextPlayerId, botMessage, playerAfterBot);
            }

            if (session.CurrentStage == GameStage.Voting)
                await StartVotingAsync(roomId, session);
        }

        public UserId? GetUserIDBySlotID(GameSession session, SlotID slotID)
        {
            foreach (UserId key in session.PlayerIDs.Keys)
            {
                if (session.PlayerIDs[key] == slotID)
                {
                    return key;
                }
            }
            return null;
        }
    }
}
