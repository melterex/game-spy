using Microsoft.EntityFrameworkCore;

namespace CardsService;

public static class ThemeStorage
{
    public static void Initialize()
    {
        using var db = new ThemeDbContext();
        db.Database.Migrate();
    }
}
