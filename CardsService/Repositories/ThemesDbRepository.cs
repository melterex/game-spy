using Microsoft.EntityFrameworkCore;
using DbConnection;

namespace CardsService
{
    public class ThemesDbRepository : IRepository<ThemeModel>
    {
        private ThemeContext db;
        public ThemesDbRepository() => db = new ThemeContext();

        public IEnumerable<ThemeModel> GetAll() => db.Themes.ToList();
        public ThemeModel? Get(string theme) => db.Themes.Find(theme);
        public void Create(ThemeModel theme) => db.Themes.Add(theme);
        public void Update(ThemeModel theme) => db.Update(theme);
        public void Save() => db.SaveChanges();
        private bool disposed = false;

        public void Delete(string theme)
        {
            var t = db.Themes.Find(theme);
            if (t != null)
            {
                db.Themes.Remove(t);
                Save();
            }
        }

        public virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
                if (disposing)
                    db.Dispose();

            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}