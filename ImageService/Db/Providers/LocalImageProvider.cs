using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageService
{
    public class LocalImageProvider : IProvider<ImageModel>
    {
        public int SourceType => 1;

        public async Task<IActionResult> ServeImageAsync(ImageModel image, HttpRequest request)
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRoot, image.Path.TrimStart('/'));

            if (!File.Exists(fullPath))
                return new NotFoundResult();

            var bytes = await File.ReadAllBytesAsync(fullPath);
            var contentType = GetContentType(image.Extension);

            return new FileContentResult(bytes, contentType);
        }

        public async Task<ImageModel?> SaveImageAsync(ImageUploadForm form)
        {
            try
            {
                if (form.File == null) return null;

                var fileName = Path.GetFileName(form.File.FileName);
                var extension = Path.GetExtension(fileName).ToLower();
                var hash = Guid.NewGuid().ToString("N");

                var relativePath = $"/samples/{fileName}";

                return new ImageModel
                {
                    Hash = hash,
                    Path = relativePath,
                    Extension = extension.TrimStart('.'),
                    Source = SourceType
                };
            }
            catch
            {
                return null;
            }
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower().TrimStart('.') switch
            {
                "png" => "image/png",
                "jpg" => "image/jpeg",
                "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "webp" => "image/webp",
                "bmp" => "image/bmp",
                "svg" => "image/svg+xml",
                "ico" => "image/x-icon",
                "avif" => "image/avif",
                _ => "application/octet-stream"
            };
        }
    }
}