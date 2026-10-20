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
    internal class PolzaBot : IDecisionMaker
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly BotWritingStyle _writingStyle;
        private static readonly HttpClient _httpClient = new HttpClient();

        public PolzaBot(string apiKey, string model = "deepseek/deepseek-v4-flash@provider=streamlake/fp8",
            BotWritingStyle? writingStyle = null)
        {
            _apiKey = apiKey;
            _model = model;
            _writingStyle = writingStyle ?? BotWritingStyle.Presets[Random.Shared.Next(BotWritingStyle.Presets.Count)];
        }

        public string MakeMessage(GameContext context)
        {
            var prompt = BotPromptBuilder.BuildMessage(context, _writingStyle);
            return _writingStyle.FormatMessage(SendToPolza(prompt.System, prompt.User));
        }

        public SlotID MakeVote(GameContext context)
        {
            var candidates = context.Players
                .Where(p => p.Id != context.MyId)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("Нет игроков, за кого можно голосовать");

            var prompt = BotPromptBuilder.BuildVote(context, candidates);
            string response = SendToPolza(prompt.System, prompt.User);

            foreach (var candidate in candidates)
            {
                if (string.Equals(response, candidate.Id.ToString(), StringComparison.Ordinal))
                {
                    return candidate.Id;
                }
            }

            return candidates[Random.Shared.Next(candidates.Count)].Id;
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
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.polza.ai/v1/chat/completions")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var response = _httpClient.Send(request);
                response.EnsureSuccessStatusCode();

                var responseJson = response.Content.ReadAsStringAsync().Result;

                using var doc = JsonDocument.Parse(responseJson);
                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return string.IsNullOrWhiteSpace(reply) ? "Пропущу этот ход." : reply.Trim();
            }
            catch (Exception)
            {
                return "Пропущу этот ход.";
            }
        }
    }
}
