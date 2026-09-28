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

namespace GameLogic.Services
{
    public class GameService : IGameService
    {
        private readonly Dictionary<Guid, GameSession> sessions = new();
        private readonly IVotingService _votingService;
        private readonly IThemesService _themesService;
        private readonly IGameWorker _gameWorker;
        private readonly IBotFactory _botFactory;
        public List<SlotID> GeneratePlayerOrder(List<SlotID> playersIDs)
        {
            return playersIDs.OrderBy(_ => Guid.NewGuid()).ToList();
        }
        public GameService(IVotingService votingService, IThemesService themesService, IBotFactory botFactory, IGameWorker gameWorker)
        {
            _votingService = votingService;
            _themesService = themesService;
            _botFactory = botFactory;
            _gameWorker = gameWorker;
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
            ProcessBotTurns(session);
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
            return cards;
        }
        public GameSession GetGameSessionById(Guid GameSessionId)
        {
            sessions.TryGetValue(GameSessionId, out var session);
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
            var currentPlayerId = WhoseTurn(session);
            if (currentPlayerId == null)
                throw new InvalidOperationException("Нет активного игрока");

            session.MessagesList.Add(new Message(currentPlayerId, message));
            session.CurrentPlayerIndex++;
            session.CurrentTurnNumber++;
            session.CurrentTurnStartTime = DateTime.Now;

            if (session.CurrentPlayerIndex >= session.PlayerSlots.Count)
            {
                if (session.CurrentRound == session.GameSettings.TotalRounds)
                {
                    session.CurrentPlayerIndex = -1;
                    session.CurrentStage = GameStage.Voting;
                }
                else
                {
                    session.CurrentPlayerIndex = 0;
                    session.CurrentRound++;
                }
            }
            var nextPlayerId = WhoseTurn(session);
            _ = Task.Run(() => _gameWorker.OnTurnMadeAsync(session.GameId, currentPlayerId, message, nextPlayerId));
            ProcessBotTurns(session);
        }

        public void StartVoting(GameSession session)
        {
            session.CurrentStage = GameStage.Voting;
            session.Votes = new Dictionary<SlotID, SlotID>();
            session.VotingEnded = false;
            session.VotingStartTime = DateTime.Now;
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
                _ = Task.Run(() => _gameWorker.OnVoteMadeAsync(session.GameId, report));
                _votingService.SetPlayerReadyToEndVoting(session, botId, true);
            }
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
                session.MessagesList.ToList()
            );
        }
        private void ProcessBotTurns(GameSession session)
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

                session.CurrentPlayerIndex++;
                session.CurrentTurnNumber++;
                session.CurrentTurnStartTime = DateTime.Now;

                if (session.CurrentPlayerIndex >= session.PlayerSlots.Count)
                {
                    if (session.CurrentRound == session.GameSettings.TotalRounds)
                    {
                        session.CurrentPlayerIndex = -1;
                        StartVoting(session);
                        break;
                    }
                    else
                    {
                        session.CurrentPlayerIndex = 0;
                        session.CurrentRound++;
                    }
                }
                var playerAfterBot = WhoseTurn(session);
                _ = Task.Run(() => _gameWorker.OnTurnMadeAsync(session.GameId, nextPlayerId, botMessage, playerAfterBot));
            }
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
