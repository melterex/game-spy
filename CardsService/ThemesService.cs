namespace CardsService
{
    public class ThemesService : IThemesService
    {
        private readonly IReadOnlyDictionary<string, List<string>> _themes;

        public ThemesService(IThemesRepository repository) => _themes = repository.GetAll()
            .ToDictionary(theme => theme.Theme, theme => theme.Words.Select(word => word.Word).ToList());

        public string? GetRandomWordByTheme(string theme)
        {
            if (_themes.TryGetValue(theme, out var words) && words.Count > 0)
                return words.ElementAt(Random.Shared.Next(words.Count));

            return null;
        }
        public string GetRandomTheme()
        {
            if (_themes.Count == 0)
                throw new InvalidOperationException("No themes are available in the database.");

            return _themes.Keys.ElementAt(Random.Shared.Next(_themes.Count));
        }

        public IReadOnlyList<string> GetThemes() => _themes.Keys.ToList();
    }
}
