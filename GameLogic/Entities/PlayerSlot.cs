using authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{
    public class PlayerSlot
    {
        public SlotID Id { get; set; }
        public string Username { get; set; }    
        public bool IsBot { get; set; }
    }
}