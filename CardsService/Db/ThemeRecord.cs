namespace CardsService;

public class ThemeRecord
{
    public string Name { get; set; } = string.Empty;
    public string[] ThemeResponses { get; set; } = [];
    public List<WordRecord> Words { get; set; } = [];
}
