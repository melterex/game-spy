using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageService
{
    public class UrlImageProvider(FileHasher hasher, ImageProcessor processor) : IProvider<ImageModel>
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

                return new FileContentResult(finalBytes, contentType);
            }
            catch
            {
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

                return new ImageModel
                {
                    Hash = hash,
                    Path = form.Url,
                    Extension = extension.TrimStart('.'),
                    Source = SourceType
                };
            }
            catch
            {
                return null;
            }
        }
    }
}