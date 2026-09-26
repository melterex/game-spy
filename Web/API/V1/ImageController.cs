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
    [HttpGet("{id}/file")]
    public async Task<IActionResult> GetImageAsync(string id)
    {
        var image = repo.Get(id);
        if (image == null)
            return NotFound();

        var provider = providerFactory.GetProvider(image.Source);
        return await provider.ServeImageAsync(image, Request);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(ImageUploadForm form, int source = 1)
    {
        if (form == null)
            return BadRequest("Form cannot be empty");

        var provider = providerFactory.GetProvider(source);

        var savedImage = await provider.SaveImageAsync(form);

        if (savedImage == null)
            return BadRequest("Failed to save the image.");

        repo.Create(savedImage);
        repo.Save();

        return Ok(new
        {
            success = true,
            hash = savedImage.Hash,
            url = $"/api/images/{savedImage.Hash}",
            path = savedImage.Path
        });
    }
}