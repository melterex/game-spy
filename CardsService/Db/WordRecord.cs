namespace CardsService;

public class WordRecord
{
    public string ThemeName { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public int Position { get; set; }
    public string[] Descriptions { get; set; } = [];
}
