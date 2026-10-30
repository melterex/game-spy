using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;

namespace ImageService
{
    public class LocalImageProvider(FileHasher hasher, ImageProcessor processor,
        ILogger<LocalImageProvider> logger) : IProvider<ImageModel>
    {
        public int SourceType => 1;

        private static string? ResolvePath(string relativePath)
        {
            var webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/', '\\')));
            var rootPrefix = webRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return null;
            return fullPath;
        }

        public async Task<IActionResult> ServeImageAsync(ImageModel image, ImageProcessingOptions options)
        {
            var fullPath = ResolvePath(image.Path);
            if (fullPath == null || !File.Exists(fullPath))
            {
                logger.LogWarning(
                    "Image file not found on disk at path {FullPath} for Image Hash: {Hash}",
                    fullPath, image.Hash
                    );
                return new NotFoundResult();
            }

            var contentType = processor.GetContentType(image.Extension);
            var originalBytes = await File.ReadAllBytesAsync(fullPath);
            var finalBytes = await processor.ProcessImageAsync(originalBytes, options);

            logger.LogDebug("Image with image hash {Hash} successfully served", image.Hash);
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
                var relativePath = $"/samples/{hash}{extension}";
                var fullPath = ResolvePath(relativePath);
                if (fullPath == null)
                    return null;

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                logger.LogDebug("Image with image hash {Hash} successfully created", hash);
                return new ImageModel
                {
                    Hash = hash,
                    Path = relativePath,
                    Extension = extension.TrimStart('.'),
                    Source = SourceType
                };
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Failed to process and create image model for file {FileName}", form.File?.FileName);
                return null;
            }
        }
    }
}