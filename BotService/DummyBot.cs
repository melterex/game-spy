using authorization;
using System;
using System.Collections.Generic;
using System.Text;
using GameLogic.Entities;
using GameLogic.Interfaces;

namespace BotService
{
    internal class DummyBot : IDecisionMaker
    {
        public string MakeMessage(GameContext context)
        {
            return "Я ботик";
        }

        public UserId MakeVote(GameContext context)
        {
            var candidates = context.Players
                .Where(p => p.Id != context.MyId)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("Нет игроков, за кого можно голосовать");

            return candidates[Random.Shared.Next(candidates.Count)].Id;
        }
    }
}
