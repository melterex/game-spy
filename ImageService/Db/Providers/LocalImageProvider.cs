using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageService
{
    public class LocalImageProvider(FileHasher hasher, ImageProcessor processor) : IProvider<ImageModel>
    {
        public int SourceType => 1;

        public async Task<IActionResult> ServeImageAsync(ImageModel image, ImageProcessingOptions options)
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRoot, image.Path.TrimStart('/'));

            if (!File.Exists(fullPath))
                return new NotFoundResult();

            var contentType = processor.GetContentType(image.Extension);
            var originalBytes = await File.ReadAllBytesAsync(fullPath);
            var finalBytes = await processor.ProcessImageAsync(originalBytes, options);

            return new FileContentResult(finalBytes, contentType);
        }

        public async Task<ImageModel?> CreateImageModelAsync(ImageUploadForm form)
        {
            try
            {
                if (form.File == null) return null;

                var fileName = Path.GetFileName(form.File.FileName);
                var extension = Path.GetExtension(fileName).ToLower();

                using var memoryStream = new MemoryStream();

                await form.File.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                var hash = hasher.ComputeSha256Hash(fileBytes);
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
    }
}