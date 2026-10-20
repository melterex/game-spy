using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotService
{
    internal class BotData
    {
        public Dictionary<string, List<string>> ThemeResponses { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> WordResponses { get; } = new(StringComparer.OrdinalIgnoreCase);

        private sealed class ThemeFile
        {
            [JsonPropertyName("theme")]
            public string Theme { get; set; } = string.Empty;

            [JsonPropertyName("themeResponses")]
            public List<string> ThemeResponses { get; set; } = new();

            [JsonPropertyName("words")]
            public List<WordEntry> Words { get; set; } = new();
        }

        private sealed class WordEntry
        {
            [JsonPropertyName("word")]
            public string Word { get; set; } = string.Empty;

            [JsonPropertyName("descriptions")]
            public List<string> Descriptions { get; set; } = new();
        }

        public static BotData Load()
        {
            var data = new BotData();
            string dataPath = Path.Combine(AppContext.BaseDirectory, "Data");

            if (!Directory.Exists(dataPath))
                return data;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var jsonFile in Directory.GetFiles(dataPath, "*.json"))
            {
                ThemeFile? themeFile;
                try
                {
                    themeFile = JsonSerializer.Deserialize<ThemeFile>(File.ReadAllText(jsonFile), options);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (themeFile == null)
                    continue;

                if (!string.IsNullOrEmpty(themeFile.Theme) && themeFile.ThemeResponses.Count > 0)
                    data.ThemeResponses[themeFile.Theme] = themeFile.ThemeResponses;

                foreach (var word in themeFile.Words)
                {
                    if (!string.IsNullOrEmpty(word.Word) && word.Descriptions.Count > 0)
                        data.WordResponses[word.Word] = word.Descriptions;
                }
            }

            return data;
        }
    }
}
