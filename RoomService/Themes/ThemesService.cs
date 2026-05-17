namespace RoomService
{
    internal class ThemesService : IThemesService
    {
        private IReadOnlyDictionary<string, List<string>> _themes;

        public ThemesService(IParser parser) => _themes = parser.Parse();

        public string? GetRandomWordByTheme(string theme)
        {
            if (_themes.TryGetValue(theme, out var words))
                return words.ElementAt(Random.Shared.Next(words.Count));

            Logger.Log($"Data have no theme {theme}");
            return null;
        }
        public string GetRandomTheme()
        {
            if (_themes.Count == 0)
            {
                Logger.Log("Couldn`t open Data files");
                throw new InvalidOperationException("Data files are empty");
            }

            return _themes.Keys.ElementAt(Random.Shared.Next(_themes.Count));
        }

        public IReadOnlyList<string> GetThemes() => _themes.Keys.ToList();
    }
}