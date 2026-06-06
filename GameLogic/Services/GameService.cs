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
        public List<UserId> GeneratePlayerOrder(List<UserId> playersIDs)
        {
            return playersIDs.OrderBy(_ => Guid.NewGuid()).ToList();
        }
        public GameService(IVotingService votingService, IThemesService themesService)
        {
            _votingService = votingService;
            _themesService = themesService;
        }
        public Guid CreateGameSession(List<UserId> playersIDs, GameSettings settings)
        {
            var session = new SpyGameSession
            {
                GameId = Guid.NewGuid(),
                PlayersIDs = playersIDs,
                GameSettings = settings,
                CurrentRound = 1,
                CurrentPlayerIndex = 0,
                CurrentPlayerOrder = GeneratePlayerOrder(playersIDs),
                CurrentStage = GameStage.Round,
                MessagesList = new List<Message>(),
                PlayerCards = new Dictionary<UserId, Card>(),
                CurrentTurnStartTime = DateTime.Now,
                CurrentTurnNumber = 0,
            };
            AssignCards(session);
            sessions[session.GameId] = session;
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
    }
}