﻿using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public ImagesController(BookonnectContext context, IOptions<StorageOptions> options)
	{
		_context = context;
        _cloudinary = new Cloudinary(options.Value.CloudinaryURL);
        _cloudinary.Api.Secure = true;
	}

	[HttpPost]
	public async Task<ActionResult<Image>> Upload(ImageDTO imageDTO)
	{
		var bookExists = _context.Books.Any(bk => bk.ID == imageDTO.BookID);
		if (!bookExists)
		{
			return NotFound("Book not found");
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
		catch (Exception)
		{
			return StatusCode(500);
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
		catch(Exception)
		{
			return StatusCode(500);
		}
    }

	// Create a worker to delete images after 24hrs
}

