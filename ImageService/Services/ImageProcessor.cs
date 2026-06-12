using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageService
{
    public class ImageProcessor
    {
        public async Task<byte[]> ProcessImageAsync(byte[] originalBytes, ImageProcessingOptions options)
        {
            if (options.Size == null)
                return originalBytes;

            try
            {
                var parts = options.Size.Split("x");
                if
                (
                    parts.Length != 2 ||
                    !int.TryParse(parts[0], out var width) ||
                    !int.TryParse(parts[1], out var height)
                )
                    return originalBytes;

                using var img = Image.Load(originalBytes);
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = options.GetResizeMode(),
                    Position = options.GetAnchorPosition()
                };

                img.Mutate(x => x.Resize(resizeOptions));

                using var outStream = new MemoryStream();
                await img.SaveAsync(outStream, PngFormat.Instance);

                return outStream.ToArray();
            }
            catch
            {
                return originalBytes;
            }
        }

        public string GetContentType(string extension)
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