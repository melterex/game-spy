using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ImageService
{
    public class ImageUploadForm
    {
        public IFormFile? File { get; set; }
        public string? Url { get; set; }
    }
}