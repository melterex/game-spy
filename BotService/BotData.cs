using CardsService;

namespace BotService
{
    internal class BotData
    {
        public Dictionary<string, List<string>> ThemeResponses { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> WordResponses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public static BotData Load()
        {
            var data = new BotData();
            var themes = new ThemesDbRepository().GetAll();
            foreach (var theme in themes)
            {
                if (theme.ThemeResponses is { Count: > 0 })
                    data.ThemeResponses[theme.Theme] = theme.ThemeResponses;

                foreach (var word in theme.Words)
                {
                    if (!string.IsNullOrEmpty(word.Word) && word.Descriptions.Count > 0)
                        data.WordResponses[word.Word] = word.Descriptions;
                }
            }

            return data;
        }
    }
}
