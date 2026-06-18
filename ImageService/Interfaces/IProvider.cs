using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageService
{
    public interface IProvider<T> 
    {
        int SourceType { get; }
        Task<IActionResult> ServeImageAsync(T image, ImageProcessingOptions options);
        Task<T?> CreateImageModelAsync(ImageUploadForm form);
    }
}