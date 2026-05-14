namespace RoomService
{
    public interface IThemesService
    {
        IReadOnlyList<string> GetThemes();
        string GetRandomTheme();
        string? GetRandomWordByTheme(string theme);
    }
}