using Microsoft.EntityFrameworkCore;

namespace ImageService;

public static class ImageStorage
{
    public static void Initialize()
    {
        using var db = new ImageContext();
        db.Database.Migrate();
    }
}
