namespace RoomService
{
    public interface IParser
    {
        IReadOnlyDictionary<string, List<string>> Parse();
    }
}