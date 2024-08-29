using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace BookonnectAPI.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthController: ControllerBase
{
	private readonly BookonnectContext _context;
	private readonly IConfiguration _configuration;
	private readonly ITokenLibrary _tokenLibrary;
	private readonly ILogger<AuthController> _logger;
	public AuthController(BookonnectContext context, IConfiguration configuration, ITokenLibrary tokenLibrary, ILogger<AuthController> logger)
	{
		_context = context;
		_context.Database.EnsureCreated();
        _configuration = configuration;
		_tokenLibrary = tokenLibrary;
		_logger = logger;
	}


	[HttpPost]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<UserToken>> Authenticate(Auth auth)
	{
        Payload payload = new();
        try
		{
			_logger.LogInformation("Validating Google token");
			payload = await ValidateAsync(auth.IdToken, new ValidationSettings
            {
				Audience = new[] { _configuration["Authentication:Google:ClientId"] }
			});
		}
        catch (Exception ex)
        {
			_logger.LogError(ex, "Error validating google token");
			return StatusCode(500, ex.Message);

        }

		_logger.LogInformation("Fetching user with the provided email");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
		UserToken userToken;
        var token = string.Empty;
        if (user != null)
        {
			_logger.LogInformation("User with the provided email exists");
			// Get and set token in session
			token = _tokenLibrary.GetToken(user.ID);
            userToken = new UserToken
            {
                Email = user.Email,
                Image = user.Image,
                Name = user.Name,
                Token = token,
                Expires = _tokenLibrary.Expires
            };

            return Ok(userToken);

        }
        _logger.LogInformation("Creating new user");
        user = new User
		{
			Name = payload.Name,
			Email = payload.Email,
			Image = payload.Picture
		};
		
        _context.Users.Add(user);
		try
		{
            await _context.SaveChangesAsync();
        } catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding user to DB");
            return StatusCode(500, ex.Message);
        }

        _logger.LogInformation("Creating an account with the provider");
        // Get and store token
        token = _tokenLibrary.GetToken(user.ID);

        Account account = new Account
		{
			UserID = user.ID,
			Provider = auth.Provider,
		};
		_context.Accounts.Add(account);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding account to DB");
            return StatusCode(500, ex.Message);
        }

		userToken = new UserToken
		{
			Email = user.Email,
			Image = user.Image,
			Name = user.Name,
			Token = token,
			Expires = _tokenLibrary.Expires
		};

        return CreatedAtAction(nameof(Authenticate), new { id = user.ID }, userToken);
    }

	[HttpGet]
	[Authorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<User>> GetAuthenticatedUser()
	{
		var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
		{
			return Unauthorized(new { Message = "Please sign in again." });
		}
		var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == int.Parse(userId));

		if (user == null)
		{
			return NotFound(new { Message = "User not found. Sign in again." });
		}
		return Ok(user);
    }
}

