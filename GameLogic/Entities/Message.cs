using authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{
    public class Message
    {
        public SlotID Id { get; set; }

        public String MessageBody { get; set; }

        public Message(SlotID id, String messageBody)
        {
            Id = id;
            MessageBody = messageBody;
        }
    }
}
