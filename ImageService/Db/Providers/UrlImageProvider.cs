using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ImageService
{
    public class UrlImageProvider(FileHasher hasher, ImageProcessor processor, 
        ILogger<UrlImageProvider> logger) : IProvider<ImageModel>
    {
        public int SourceType => 2;

        public async Task<IActionResult> ServeImageAsync(ImageModel image, ImageProcessingOptions options)
        {
            var externalUrl = image.Path;

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                var originalBytes = await httpClient.GetByteArrayAsync(externalUrl);
                var finalBytes = await processor.ProcessImageAsync(originalBytes, options);
                var contentType = processor.GetContentType(image.Extension);

                logger.LogDebug("Image with image hash {Hash} successfully served", image.Hash);
                return new FileContentResult(finalBytes, contentType);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to serve image with url {ExternalUrl}", externalUrl);
                return new RedirectResult(externalUrl, permanent: false);
            }
        }

        public async Task<ImageModel?> CreateImageModelAsync(ImageUploadForm form)
        {
            try
            {
                if (form.Url == null) return null;

                var hash = hasher.ComputeSha256Hash(form.Url);
                var extension = Path.GetExtension(form.Url).ToLower();

                logger.LogDebug("Image with image hash {Hash} successfully created", hash);
                return new ImageModel
                {
                    Hash = hash,
                    Path = form.Url,
                    Extension = extension.TrimStart('.'),
                    Source = SourceType
                };
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Failed to process and create image model for url {Url}", form.Url);
                return null;
            }
        }
    }
}