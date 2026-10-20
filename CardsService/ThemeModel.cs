namespace CardsService
{
    public record WordModel(string Word, List<string> Descriptions)
    {
        public override string ToString() => Word;
    }

    public record ThemeModel(string Theme, List<WordModel> Words, List<string>? ThemeResponses);
}