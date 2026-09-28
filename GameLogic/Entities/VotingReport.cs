using authorization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace GameLogic.Entities
{
    public class VotingReport
    {
        public Dictionary<SlotID, Int32> Votes { get; set; }

        public VotingReport(Dictionary<SlotID, Int32> votes)
        {
            Votes = votes;
        }
    }
}
