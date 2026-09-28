using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CardsService
{
    public class ThemesJsonParser : IParser
    {
        public ILogger<ThemesJsonParser> logger { get; set; } = NullLogger<ThemesJsonParser>.Instance;

        public IReadOnlyDictionary<string, List<string>> Parse()
        {
            string[] files;
            string dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
            Dictionary<string, List<string>> themes = new();


            if (Directory.Exists(dataPath))
            {
                logger.LogDebug("Successfully found directory {dataPath}", dataPath);
                files = Directory.GetFiles(dataPath, "*.json");
            }
            else
            {
                logger.LogError("Directory {dataPath} doesn`t exist", dataPath);
                throw new FileNotFoundException($"Directory {dataPath} doesn`t exist");
            }

            foreach (var jsonFile in files)
            {
                string jsonText = File.ReadAllText(jsonFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var jsonParsed = JsonSerializer.Deserialize<ThemeModel>(jsonText, options);

                if (jsonParsed != null && jsonParsed.Words != null)
                    themes[jsonParsed.Theme] = jsonParsed.Words;

                else
                {
                    logger.LogError("Failed to parse JSON {jsonFile}", jsonFile);
                    throw new JsonException("Failed to parse JSON");
                }
            }

            return themes.AsReadOnly();
        }
    }
}