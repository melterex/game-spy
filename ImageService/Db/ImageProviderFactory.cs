using Microsoft.Extensions.Logging;

namespace ImageService
{
    public class ImageProviderFactory
    {
        private readonly Dictionary<int, IProvider<ImageModel>> _providers;
        private ILogger<ImageProviderFactory> _logger;

        public ImageProviderFactory(IEnumerable<IProvider<ImageModel>> providers, 
            ILogger<ImageProviderFactory> logger){
            _logger =  logger;
            _providers = providers.ToDictionary(p => p.SourceType);
        }

        public IProvider<ImageModel> GetProvider(int sourceType)
        {
            if (!_providers.TryGetValue(sourceType, out var provider))
            {
                _logger.LogError("Image source type {SourceType} is not supported.", sourceType);
                throw new NotSupportedException($"Image source type {sourceType} is not supported.");
            }

            return provider;
        }
    }
}