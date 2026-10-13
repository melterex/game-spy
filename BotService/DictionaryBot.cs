using authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameLogic.Entities;
using GameLogic.Interfaces;

namespace BotService
{
    internal class DictionaryBot : IDecisionMaker
    {
        private static int _globalBotCounter = 0;
        private readonly int _botOffset;
        private int _turnCounter = 0;

        private static readonly Dictionary<string, List<string>> _themeResponses;
        private static readonly Dictionary<string, List<string>> _wordResponses;

        static DictionaryBot()
        {
            var data = BotData.Load();
            _themeResponses = new Dictionary<string, List<string>>(data.ThemeResponses, StringComparer.OrdinalIgnoreCase);
            _wordResponses = new Dictionary<string, List<string>>(data.WordResponses, StringComparer.OrdinalIgnoreCase);
        }

        public DictionaryBot()
        {
            _botOffset = Interlocked.Increment(ref _globalBotCounter);
        }

        private readonly List<string> _spyFallbackResponses = new List<string>
        {
            "Даже не знаю, какую деталь выделить, чтобы не сказать лишнего.",
            "Это довольно обыденная вещь, если так подумать.",
            "Согласен с предыдущими ораторами, добавить особо нечего.",
            "Интересная штука, у каждого с ней свои ассоциации.",
            "Мы все с этим так или иначе сталкиваемся.",
            "Я бы сказал, что в этом есть свои плюсы и минусы.",
            "Главное — правильно этим распоряжаться.",
            "Я бы сказал, что отношение к этому у всех разное."
        };

        public string MakeMessage(GameContext context)
        {
            string message;
            Thread.Sleep(2000);

            if (context.IsSpy)
            {
                if (!string.IsNullOrEmpty(context.Theme) && _themeResponses.TryGetValue(context.Theme, out var themeList))
                {
                    int index = (_botOffset + _turnCounter) % themeList.Count;
                    message = themeList[index];
                }
                else
                {
                    int index = (_botOffset + _turnCounter) % _spyFallbackResponses.Count;
                    message = _spyFallbackResponses[index];
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(context.Word) && _wordResponses.TryGetValue(context.Word, out var wordList))
                {
                    int index = (_botOffset + _turnCounter) % wordList.Count;
                    message = wordList[index];
                }
                else
                {
                    message = "Неплохое слово, но оставлю свою мысль при себе.";
                }
            }

            _turnCounter++;
            return message;
        }

        public SlotID MakeVote(GameContext context)
        {
            Thread.Sleep(1000);
            var candidates = context.Players
                .Where(p => p.Id != context.MyId)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("Нет игроков, за кого можно голосовать");

            return candidates[Random.Shared.Next(candidates.Count)].Id;
        }
    }
}