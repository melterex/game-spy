namespace DbConnection
{
    public interface IRepository<T> : IDisposable
    {
        IEnumerable<T> GetAll();
        T? Get(string obj);
        void Create(T item);
        void Update(T item);
        void Delete(string theme);
        void Save();
    }
}