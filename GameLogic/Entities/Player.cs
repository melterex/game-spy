using authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{
    internal class Player
    {
        public UserId Id { get; set; }
        public string Username { get; set; }    
        public Card Card { get; set; }
        public string CurrentComment { get; set; }
    }
}