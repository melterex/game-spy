using authorization;
using GameLogic.Entities;
using GameLogic.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BotService
{
    internal class OpenRouterBot : IDecisionMaker
    {
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _httpClient = new HttpClient();

        public OpenRouterBot(string apiKey, string model = "openrouter/free")
        {
            _apiKey = apiKey;
            _model = model;
        }

        public string MakeMessage(GameContext context)
        {
            var systemPrompt = BuildSystemPrompt(context);
            var chatHistory = BuildChatHistory(context);
            var userPrompt = $"{chatHistory}\nТвоя очередь писать сообщение в чат. Напиши ОДНУ короткую реплику (вопрос или ответ). Не пиши ничего кроме самой реплики.";

            return SendToOpenRouter(systemPrompt, userPrompt);
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

            var userPrompt = $"{chatHistory}\nВремя голосовать! Кто по твоему мнению шпион? (Если ты сам шпион — голосуй за любого другого игрока, чтобы отвести подозрения).\n" +
                             $"Список кандидатов:\n{candidatesText}\n\n" +
                             $"В ответ напиши ТОЛЬКО ID выбранного игрока. Никаких других слов или символов.";

            string response = SendToOpenRouter(systemPrompt, userPrompt);

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
            sb.AppendLine("Ты играешь в текстовую игру 'Шпион' (Spyfall) вместе с другими игроками.");

            if (context.IsSpy)
            {
                sb.AppendLine("ТВОЯ РОЛЬ: ШПИОН.");
                sb.AppendLine("Ты НЕ ЗНАЕШЬ загаданное слово. Твоя задача — не выдать себя и попытаться понять, что за слово, анализируя сообщения других.");
                sb.AppendLine("Отвечай уклончиво, задавай общие, но логичные вопросы.");
            }
            else
            {
                sb.AppendLine("ТВОЯ РОЛЬ: МИРНЫЙ ИГРОК.");
                sb.AppendLine($"ЗАГАДАННОЕ СЛОВО: {context.Word}");
                sb.AppendLine("Твоя задача — доказать другим, что ты знаешь слово, но при этом НЕ НАЗЫВАТЬ его напрямую, чтобы шпион не догадался.");
            }

            sb.AppendLine("Будь краток, веди себя как реальный человек в чате.");
            return sb.ToString();
        }

        private string BuildChatHistory(GameContext context)
        {
            if (context.Messages == null || context.Messages.Count == 0)
                return "Чат пока пуст. Ты начинаешь первым.";

            var sb = new StringBuilder();
            sb.AppendLine("История чата:");

            foreach (var msg in context.Messages)
            {
                var senderName = context.Players.FirstOrDefault(p => p.Id.Equals(msg.Id))?.Username ?? $"Игрок {msg.Id.ToString()}";

                sb.AppendLine($"{senderName}: {msg.MessageBody}");
            }
            return sb.ToString();
        }
        private string SendToOpenRouter(string systemPrompt, string userPrompt)
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

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
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

            return reply?.Trim() ?? "Я пропущу ход.";
        }
    }
}