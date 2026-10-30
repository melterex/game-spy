using Microsoft.EntityFrameworkCore;

namespace CardsService;

public class ThemesDbRepository : IThemesRepository
{
    public IReadOnlyList<ThemeModel> GetAll()
    {
        using var db = new ThemeDbContext();
        return db.Themes.AsNoTracking().Include(theme => theme.Words)
            .OrderBy(theme => theme.Name).ToList()
            .Select(theme => new ThemeModel(theme.Name,
                theme.Words.OrderBy(word => word.Position)
                    .Select(word => new WordModel(word.Word, word.Descriptions.ToList())).ToList(),
                theme.ThemeResponses.ToList())).ToList();
    }
}
