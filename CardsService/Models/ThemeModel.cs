namespace CardsService
{
    public record ThemeModel(string Theme, List<string> Words)
    {
        public int Id { get; init; } 
    }
}