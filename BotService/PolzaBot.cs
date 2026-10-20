using authorization;
using GameLogic.Entities;
using GameLogic.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BotService
{
    internal class PolzaBot : IDecisionMaker
    {
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _httpClient = new HttpClient();

        public PolzaBot(string apiKey, string model = "deepseek/deepseek-v4-flash@provider=streamlake/fp8")
        {
            _apiKey = apiKey;
            _model = model;
        }

        public string MakeMessage(GameContext context)
        {
            var systemPrompt = BuildSystemPrompt(context);
            var chatHistory = BuildChatHistory(context);
            var userPrompt = $"{chatHistory}\nТвоя очередь писать в чат. Напиши ОДНУ короткую реплику (можешь задать вопрос или просто ответить). Пиши как обычный чел в телеге: без точек в конце, можно с мелкими опечатками, без заумных слов. Выдай ТОЛЬКО сам текст сообщения, без кавычек и прочего мусора.";

            return SendToPolza(systemPrompt, userPrompt);
        }

        public SlotID MakeVote(GameContext context)
        {
            var candidates = context.Players
                .Where(p => p.Id != context.MyId)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("Нет игроков, за кого можно голосовать");

            var systemPrompt = BuildSystemPrompt(context);
            var chatHistory = BuildChatHistory(context);
            var candidatesText = string.Join("\n", candidates.Select(c => $"ID: {c.Id.ToString()}, Имя: {c.Username}"));

            var userPrompt = $"{chatHistory}\nВремя голосовать! Кто по твоему шпик? (Если ты сам шпион — просто слей голос на кого-то другого, чтоб не палиться).\n" +
                             $"Кого подозреваешь из этих:\n{candidatesText}\n\n" +
                             $"В ответ напиши ТОЛЬКО ID этого чела. Вообще больше ни одного символа, только цифры.";

            string response = SendToPolza(systemPrompt, userPrompt);

            foreach (var candidate in candidates)
            {
                if (response.Contains(candidate.Id.ToString()))
                {
                    return candidate.Id;
                }
            }

            return candidates[Random.Shared.Next(candidates.Count)].Id;
        }

        private string BuildSystemPrompt(GameContext context)
        {
            var sb = new StringBuilder();
            var myName = context.Players.FirstOrDefault(p => p.Id == context.MyId)?.Username ?? $"Игрок {context.MyId}";

            sb.AppendLine($"Ты играешь в шпиона (Spyfall) в чатике. Твой ник {myName}.");
            sb.AppendLine("ТВОЙ СТИЛЬ ОБЩЕНИЯ: ты обычный пользователь интернета. Пиши неформально, сленгом (типа 'хз', 'чел', 'лол', 'всм'), забей на пунктуацию, можешь делать мелкие опечатки. Не используй длинные сложные предложения. Будь на чилле, общайся как с кентами.");

            if (context.IsSpy)
            {
                sb.AppendLine("ТВОЯ РОЛЬ: ШПИОН.");
                sb.AppendLine($"Ты НЕ ЗНАЕШЬ загаданное слово, но знаешь, что тема: {context.Theme}. Твоя цель — не спалиться и попробовать выкупить слово по базару остальных.");
                sb.AppendLine("Отвечай обтекаемо, лей воду, но чтоб звучало правдоподобно. Соглашайся с другими, задавай встречные вопросы типа 'а ты сам как думаешь?'.");
            }
            else
            {
                sb.AppendLine("ТВОЯ РОЛЬ: МИРНЫЙ.");
                sb.AppendLine($"ЗАГАДАННОЕ СЛОВО: {context.Word}");
                sb.AppendLine("Твоя цель — показать своим, что ты в теме, но НЕ НАЗЫВАТЬ слово прямо, чтоб шпик не догадался. Описывай какими-то абстрактными ассоциациями, но понятно для тех, кто знает слово.");
            }

            sb.AppendLine("Короче, пиши коротко, читай че пишут другие и подстраивайся под их вайб. НЕ ПОВТОРЯЙ то, что уже писали.");

            if (context.Messages.Count >= context.Players.Count * 2)
            {
                sb.AppendLine("ВНИМАНИЕ: Игра уже идет давно.");
                sb.AppendLine("В этом сообщении тебе надо прям наехать на кого-то. Выбери ник из чата и скажи, что он мутный и походу он шпион. Объясни свою предъяву, но по-простому.");
            }

            return sb.ToString();
        }

        private string BuildChatHistory(GameContext context)
        {
            if (context.Messages == null || context.Messages.Count == 0)
                return "Чат пока пуст. Ты пишешь первым, го.";

            var sb = new StringBuilder();
            sb.AppendLine("Вот че писали в чате:");

            foreach (var msg in context.Messages)
            {
                var senderName = context.Players.FirstOrDefault(p => p.Id.Equals(msg.Id))?.Username ?? $"Игрок {msg.Id.ToString()}";
                sb.AppendLine($"{senderName}: {msg.MessageBody}");
            }
            return sb.ToString();
        }

        private string SendToPolza(string systemPrompt, string userPrompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.polza.ai/v1/chat/completions")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = _httpClient.Send(request);
                response.EnsureSuccessStatusCode();

                var responseJson = response.Content.ReadAsStringAsync().Result;

                using var doc = JsonDocument.Parse(responseJson);
                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return reply?.Trim() ?? "хз че сказать, пасс";
            }
            catch (Exception ex)
            {
                return "чет лагает, пропуск";
            }
        }
    }
}