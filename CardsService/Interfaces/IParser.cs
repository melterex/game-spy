namespace CardsService
{
    public interface IParser
    {
        IReadOnlyDictionary<string, List<string>> Parse();
    }
}