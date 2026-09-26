using DbConnection;

namespace ImageService
{
    public class ImageDbRepository : IRepository<ImageModel>
    {
        private ImageContext db;
        public ImageDbRepository() => db = new ImageContext();

        public IEnumerable<ImageModel> GetAll() => db.Images.ToList();
        public ImageModel? Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (!int.TryParse(id, out int numericId))
                return null;

            return db.Images.Find(numericId);
        }
        public void Create(ImageModel image) => db.Images.Add(image);
        public void Update(ImageModel image) => db.Update(image);
        public void Save() => db.SaveChanges();
        private bool disposed = false;

        public void Delete(string id)
        {
            if (!int.TryParse(id, out int numericId)) return;
            
            var image = db.Images.Find(numericId);
            if (image != null)
            {
                db.Images.Remove(image);
                Save();
            }
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposed)
                if (disposing)
                    db.Dispose();

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
