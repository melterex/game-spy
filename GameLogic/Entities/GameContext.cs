using authorization;
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
    }
}
