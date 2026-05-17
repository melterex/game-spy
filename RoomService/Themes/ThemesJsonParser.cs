using System.Text.Json;

namespace RoomService
{
    internal class ThemesJsonParser() : IParser
    {

        public IReadOnlyDictionary<string, List<string>> Parse()
        {
            string[] files;
            Dictionary<string, List<string>> themes = new();

            if (Directory.Exists("Data"))
            {
                string dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
                files = Directory.GetFiles(dataPath, "*.json");
            }
            else
            {
                Logger.Log($"Directory Data doesn`t exist");
                throw new FileNotFoundException("Directory Data doesn`t exist");
            }

            foreach (var jsonFile in files)
            {
                string jsonText = File.ReadAllText(jsonFile);
                var jsonParsed = JsonSerializer.Deserialize<ThemeModel>(jsonText);

                if (jsonParsed != null && jsonParsed.Words != null)
                    themes[jsonParsed.Theme] = jsonParsed.Words;

                else
                {
                    Logger.Log("Failed to parse JSON");
                    throw new JsonException("Failed to parse JSON");
                }
            }

            return themes.AsReadOnly();
        }
    }
}