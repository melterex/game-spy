using DbConnection;
using Microsoft.AspNetCore.Mvc;
using ImageService;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.API.V1;

[ApiController]
[Route("api/images")]
[Authorize]
public class ImageController(
    IRepository<ImageModel> repo, ImageProviderFactory providerFactory
    ) : ControllerBase
{
    [HttpGet("{uid}/file")]
    public async Task<IActionResult> GetImageAsync(string uid, [FromQuery] ImageProcessingOptions options)
    {
        var image = repo.Get(uid);
        if (image == null)
            return NotFound();

        var provider = providerFactory.GetProvider(image.Source);
        return await provider.ServeImageAsync(image, options);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(ImageUploadForm form, int source)
    {
        if (form == null)
            return BadRequest("Form cannot be empty");

        var provider = providerFactory.GetProvider(source);
        var savedImage = await provider.CreateImageModelAsync(form);

        if (savedImage == null)
            return BadRequest("Failed to save the image.");

        if (repo.Get(savedImage.Hash) == null)
        {
            repo.Create(savedImage);
            repo.Save();
        }

        return Ok(new
        {
            success = true,
            hash = savedImage.Hash,
            url = $"/api/images/{savedImage.Hash}",
            path = savedImage.Path
        });
    }
}