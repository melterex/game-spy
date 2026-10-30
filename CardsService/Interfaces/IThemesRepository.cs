namespace CardsService;

public interface IThemesRepository
{
    IReadOnlyList<ThemeModel> GetAll();
}
