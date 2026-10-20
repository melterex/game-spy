using authorization;
using GameLogic.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{

    public class PlayerPublicInfo
    {
        public PlayerPublicInfo(SlotID id, string username, string currentComment)
        {
            Id = id;
            Username = username;
            CurrentComment = currentComment;
        }

        public SlotID Id { get; set; }
        public string Username { get; set; }
        public string CurrentComment { get; set; }
    }
    public class GameContext
    {
        public GameContext(SlotID myId, bool isSpy, string word, List<PlayerPublicInfo> players, List<Message> messages, string theme)
        {
            MyId = myId;
            IsSpy = isSpy;
            Word = word;
            Players = players;
            Messages = messages;
            Theme = theme;
        }

        public SlotID MyId { get; set; }
        public bool IsSpy { get; set; }
        public string Word { get; set; }
        public List<PlayerPublicInfo> Players { get; set; }
        public List<Message> Messages { get; set; }
        public string Theme { get; set; }

        // Явное состояние сессии: история может содержать пропуски ходов и не заменяет счётчики игры.
        public GameStage Stage { get; set; } = GameStage.Round;
        public int? CurrentRound { get; set; }
        public int? TotalRounds { get; set; }
        // Номер текущего хода и позиция в раунде считаются с 1. На голосовании равны null.
        public int? TurnNumber { get; set; }
        public int? PositionInRound { get; set; }
        public List<SlotID> PlayerOrder { get; set; } = new();
    }
}
