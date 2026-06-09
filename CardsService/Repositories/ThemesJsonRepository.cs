using System.Text.Json;
using DbConnection;

namespace CardsService
{
    public class ThemesJsonRepository : IRepository<ThemeModel>
    {
        private readonly IParser _parser;
        private readonly string _directoryPath;
        private Lazy<List<ThemeModel>> _cache;

        public ThemesJsonRepository(IParser parser, string directoryPath = "Data")
        {
            _parser = parser;
            _directoryPath = directoryPath;
            _cache = new Lazy<List<ThemeModel>>(LoadAllFromDirectory);
        }

        private List<ThemeModel> LoadAllFromDirectory() =>
            _parser.Parse().Select(entry => new ThemeModel(entry.Key, entry.Value)).ToList();

        public IEnumerable<ThemeModel> GetAll() => _cache.Value;
        public ThemeModel? Get(string theme) => _cache.Value.Find(t => t.Theme == theme);

        public void Save()
        {
            foreach (var theme in _cache.Value)
            {
                string fileName = Path.Combine(_directoryPath, $"{theme.Theme}.json");
                var dataToSave = new Dictionary<string, List<string>>
                {
                    { theme.Theme, theme.Words }
                };
                var json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fileName, json);
            }
        }
        public void Create(ThemeModel item) => _cache.Value.Add(item);
        public void Update(ThemeModel item)
        {
            var existing = _cache.Value.FirstOrDefault(t => t.Theme == item.Theme);
            
            if (existing != null)
            {
                int index = _cache.Value.IndexOf(existing);
                _cache.Value[index] = item;
            }
        }

        public void Delete(string themeName)
        {
            var themeToDelete = _cache.Value.FirstOrDefault(t => t.Theme == themeName);
            if (themeToDelete != null)
            {
                _cache.Value.Remove(themeToDelete);

                string fileName = Path.Combine(_directoryPath, $"{themeName}.json");
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
        }
        public void ClearAllData() => _cache.Value.Clear();
        public void Dispose() { }
    }

}