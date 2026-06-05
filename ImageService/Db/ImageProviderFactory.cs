namespace ImageService
{
    public class ImageProviderFactory
    {
        private readonly Dictionary<int, IProvider<ImageModel>> _providers;

        public ImageProviderFactory(IEnumerable<IProvider<ImageModel>> providers) =>
            _providers = providers.ToDictionary(p => p.SourceType);

        public IProvider<ImageModel> GetProvider(int sourceType)
        {
            return _providers.TryGetValue(sourceType, out var provider)
                ? provider
                : throw new NotSupportedException($"Image source type {sourceType} is not supported.");
        }
    }
}