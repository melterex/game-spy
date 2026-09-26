using DbConnection;
using Microsoft.Extensions.Logging;

namespace CardsService
{
    public class ThemesService : IThemesService
    {
        private IEnumerable<ThemeModel> _themes;
        private ILogger<ThemesService> _logger;

        public ThemesService(IRepository<ThemeModel> repo, ILogger<ThemesService> logger)
        {
            _themes = repo.GetAll();
            _logger = logger;
        }

        public string? GetRandomWordByTheme(string theme)
        {
            var t = _themes.FirstOrDefault(t => t.Theme == theme);
            if (t != null && t.Words.Any())
            {
                var word = t.Words[Random.Shared.Next(t.Words.Count)];

                _logger.LogInformation("Word {word} was given by theme {theme}", word, theme);
                return word;
            }

            _logger.LogWarning("Theme {theme} not found or theme contains no words", theme);
            return null;
        }
        public string GetRandomTheme()
        {
            if (!_themes.Any())
            {
                _logger.LogError("No themes available");
                throw new InvalidOperationException("No themes available");
            }

            var randomIndex = Random.Shared.Next(_themes.Count());
            var theme = _themes.ElementAt(randomIndex).Theme;

            _logger.LogInformation("Theme {theme} was randomly given", theme);
            return _themes.ElementAt(randomIndex).Theme;
        }

        public IReadOnlyList<string> GetThemes() => _themes.Select(t => t.Theme).ToList();
    }
}