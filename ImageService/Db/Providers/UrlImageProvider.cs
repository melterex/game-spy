using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageService
{
    public class UrlImageProvider : IProvider<ImageModel>
    {
        public int SourceType => 2;

        public Task<IActionResult> ServeImageAsync(ImageModel image, HttpRequest request)
        {
            var externalUrl = image.Path.StartsWith("http")
                ? image.Path
                : $"https://your-cdn.com{image.Path}";

            return Task.FromResult<IActionResult>(new RedirectResult(externalUrl, permanent: false));
        }

        public async Task<ImageModel?> SaveImageAsync(ImageUploadForm form)
        {
            try
            {
                if (form.Url == null) return null;

                var hash = Guid.NewGuid().ToString("N");
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