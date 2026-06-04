using DbConnection;

namespace CardsService
{
    public class ThemesService : IThemesService
    {
        private IEnumerable<ThemeModel> _themes;

        public ThemesService(IRepository<ThemeModel> repo) => _themes = repo.GetAll();

        public string? GetRandomWordByTheme(string theme)
        {
            var t = _themes.FirstOrDefault(t => t.Theme == theme);
            if (t != null && t.Words.Any())
                return t.Words[Random.Shared.Next(t.Words.Count)];

            return null;
        }
        public string GetRandomTheme()
        {
            if (!_themes.Any())
                throw new InvalidOperationException("No themes available");

            var randomIndex = Random.Shared.Next(_themes.Count());
            return _themes.ElementAt(randomIndex).Theme;
        }
        
        public IReadOnlyList<string> GetThemes() => _themes.Select(t => t.Theme).ToList();
    }
}