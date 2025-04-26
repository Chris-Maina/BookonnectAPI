using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Controllers;

[ApiController]
[Authorize(Policy = "UserClaimPolicy")]
[Route("/api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ImagesController: ControllerBase
{
	private readonly Cloudinary _cloudinary;
	private readonly BookonnectContext _context;
    private readonly ILogger<ImagesController> _logger;
    public ImagesController(BookonnectContext context, IOptions<StorageOptions> options, ILogger<ImagesController> logger)
	{
		_context = context;
        _cloudinary = new Cloudinary(options.Value.CloudinaryURL);
        _cloudinary.Api.Secure = true;
        _logger = logger;
	}

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ImageDTO>>> GetImages()
    {
        _logger.LogInformation("Getting images");
        try
        {

            var images = await _context.Images
                .Select(img => Image.ImageToDTO(img))
                .ToListAsync();

            return Ok(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ImageDTO>> GetImage(int id)
    {
        _logger.LogInformation("Getting image with id {0}", id);
        var image = await _context.Images
            .Where(b => b.ID == id)
            .Include(b => b.Book)
            .FirstOrDefaultAsync();

        if (image == null)
        {
            return NotFound(new { Message = "Image not found." });
        }

        return Ok(Image.ImageToDTO(image));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ImageDTO>> Upload(ImageDTO imageDTO)
	{
        _logger.LogInformation("Uploading image");
		var imageExists = _context.Images.Any(img => img.BookID == imageDTO.BookID);
		if (imageExists)
		{
            _logger.LogError($"Image for the book with ID: {imageDTO.BookID} exists");
			return Conflict(new { Message = $"Image for the book with ID: {imageDTO.BookID} exists"});
		}

        var uploadParams = new ImageUploadParams()
		{
            File = new FileDescription(imageDTO.File),
			UseFilename = true,
            UniqueFilename = false
        };

		try
		{
            var result = _cloudinary.Upload(uploadParams);

			Image image = new Image
			{
				Url = result.Url.ToString(),
				PublicId = result.PublicId,
				BookID = imageDTO.BookID,
			};

            _context.Images.Add(image);
			await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Upload), new { id = image.ID }, Image.ImageToDTO(image));
        }
		catch (Exception ex)
		{
            _logger.LogError(ex.Message);
            return StatusCode(500);
		}
        
	}

	[HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImageDTO>> PatchImage(int id, [FromBody] JsonPatchDocument<Image> patchDoc)
    {
        _logger.LogInformation("Patching image");
        if (patchDoc == null)
        {
            return BadRequest(ModelState);
        }
        try
        {

            var image = await _context.Images
            .Where(img => img.ID == id)
            .Include(b => b.Book)
            .FirstOrDefaultAsync();

            if (image == null)
            {
                return NotFound(new { Message = "Image not found." });
            }

            patchDoc.ApplyTo(image, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Update(image);
        
            await _context.SaveChangesAsync();
            return Ok(Image.ImageToDTO(image));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ImageExists(id))
            {
                return NotFound(new { Message = "Image not found." });
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, ex.Message);
        }
    }


    [HttpDelete("{id}")]
	public async Task<ActionResult<ImageDTO>> DeleteImage(int id)
	{
		// Get image by ID. Use public id to delete image
		var image = await _context.Images.FindAsync(id);

        if (image == null)
        {
            return NotFound("Image not found");
        }

        var deletionParams = new DeletionParams(image.PublicId);
		try
		{
			var result = _cloudinary.Destroy(deletionParams);
			_context.Images.Remove(image);
			await _context.SaveChangesAsync();
            return Ok(Image.ImageToDTO(image));
        }
		catch(Exception ex)
		{
            _logger.LogError(ex.Message);
            return StatusCode(500);
		}
    }

    private bool ImageExists(int id)
    {
        return _context.Images.Any(img => img.ID == id);
    }
    // Create a worker to delete images after 24hrs
}

