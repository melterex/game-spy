using System.Text.Json;

namespace CardsService
{
    public class ThemesJsonParser() : IParser
    {

        public IReadOnlyDictionary<string, List<string>> Parse()
        {
            string[] files;
            string dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
            Dictionary<string, List<string>> themes = new();


            if (Directory.Exists(dataPath))
            {
                files = Directory.GetFiles(dataPath, "*.json");
            }
            else
            {
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
                    throw new JsonException("Failed to parse JSON");
                }
            }

            return themes.AsReadOnly();
        }
    }
}