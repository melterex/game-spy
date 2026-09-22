using authorization;
using GameLogic.Enums;
using GameLogic.Interfaces;
using CardsService;
using System;
using System.Collections.Generic;
using System.Text;
using GameLogic.Entities;

namespace GameLogic.Services
{
    public class GameService : IGameService
    {
        private readonly Dictionary<Guid, GameSession> sessions = new();
        private readonly IVotingService _votingService;
        private readonly IThemesService _themesService;

        private readonly IBotFactory _botFactory;
        public List<UserId> GeneratePlayerOrder(List<UserId> playersIDs)
        {
            return playersIDs.OrderBy(_ => Guid.NewGuid()).ToList();
        }
        public GameService(IVotingService votingService, IThemesService themesService, IBotFactory botFactory)
        {
            _votingService = votingService;
            _themesService = themesService;
            _botFactory = botFactory;
        }
        public Guid CreateGameSession(List<UserId> playersIDs, GameSettings settings)
        {

            var botsDict = new Dictionary<UserId, IDecisionMaker>();

            for (int i = 0; i < settings.BotCount; i++)
            {
                var botId = new UserId(-1 * (i + 1));
                playersIDs.Add(botId);
                botsDict[botId] = _botFactory.CreateBot();
            }
            var session = new SpyGameSession
            {
                GameId = Guid.NewGuid(),
                PlayersIDs = playersIDs,
                Bots = botsDict,
                GameSettings = settings,
                CurrentRound = 1,
                CurrentPlayerIndex = 0,
                CurrentPlayerOrder = GeneratePlayerOrder(playersIDs),
                CurrentStage = GameStage.Round,
                MessagesList = new List<Message>(),
                PlayerCards = new Dictionary<UserId, Card>(),
                CurrentTurnStartTime = DateTime.Now,
                CurrentTurnNumber = 0,
                PlayerComments = playersIDs.ToDictionary(id => id, id => string.Empty),
            };

            AssignCards(session);
            sessions[session.GameId] = session;
            ProcessBotTurns(session);
            return session.GameId;
        }
        public Dictionary<UserId, Card> AssignCards(GameSession session)
        {
            var random = new Random();
            int spyIndex = random.Next(session.PlayersIDs.Count);

            string word = _themesService.GetRandomWordByTheme(session.GameSettings.Theme);

            session.CurrentWord = word;

            var cards = new Dictionary<UserId, Card>();

            for (int i = 0; i < session.PlayersIDs.Count; i++)
            {
                var playerId = session.PlayersIDs[i];
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
        public List<UserId> GetPlayerOrder(GameSession session)
        {
            return session.CurrentPlayerOrder;
        }
        public IVotingService GetVoteService(GameSession session)
        {
            return _votingService;
        }
        public UserId WhoseTurn(GameSession session)
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

            if (session.CurrentPlayerIndex >= session.PlayersIDs.Count())
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

            ProcessBotTurns(session);
        }

        public void StartVoting(GameSession session)
        {
            session.CurrentStage = GameStage.Voting;
            session.Votes = new Dictionary<UserId, UserId>();
            session.VotingEnded = false;
            session.VotingStartTime = DateTime.Now;
            session.IsPlayerReadyToEndVotingDict = new Dictionary<UserId, bool>();

            foreach (UserId userID in session.PlayersIDs)
            {
                session.IsPlayerReadyToEndVotingDict[userID] = false;
            }

            foreach (var botPair in session.Bots)
            {
                UserId botId = botPair.Key;
                IDecisionMaker bot = botPair.Value;

                var context = BuildBotContext(session, botId);

                UserId targetId = bot.MakeVote(context);

                    _votingService.Vote(session, botId, targetId);
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
        public Card GetPlayerCardByID(GameSession session, UserId userID)
        {
            return session.PlayerCards[userID];
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

        private GameContext BuildBotContext(GameSession session, UserId botId)
        {
            var botCard = session.PlayerCards[botId];

            var safePlayersList = session.PlayersIDs
                .Select(id => new PlayerPublicInfo(id, "Игрок_" + id.ToString(), session.PlayerComments[id]))
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

                if (session.CurrentPlayerIndex >= session.PlayersIDs.Count)
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
            }
        }
    }
}