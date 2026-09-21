using authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{

    public class PlayerPublicInfo
    {
        public PlayerPublicInfo(UserId id, string username, string currentComment)
        {
            Id = id;
            Username = username;
            CurrentComment = currentComment;
        }

        public UserId Id { get; set; }
        public string Username { get; set; }
        public string CurrentComment { get; set; }
    }
    public class GameContext
    {
        public GameContext(UserId myId, bool isSpy, string word, List<PlayerPublicInfo> players, List<Message> messages)
        {
            MyId = myId;
            IsSpy = isSpy;
            Word = word;
            Players = players;
            Messages = messages;
        }

        public UserId MyId { get; set; }
        public bool IsSpy { get; set; }
        public string Word { get; set; }
        public List<PlayerPublicInfo> Players { get; set; }
        public List<Message> Messages { get; set; }
    }
}
