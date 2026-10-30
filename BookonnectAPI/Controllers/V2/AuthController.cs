using System.Globalization;
using System.Security.Claims;
using BookonnectAPI.DTO;
using BookonnectAPI.Lib;
using BookonnectAPI.Mappings;
using BookonnectAPI.Models;
using BookonnectCore.DTO;
using BookonnectCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookonnectAPI.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ITokenLibrary _tokenLibrary;

    public AuthController(
        IUserAccountService userAccountService,
        IGoogleTokenValidator googleTokenValidator,
        ITokenLibrary tokenLibrary)
    {
        _userAccountService = userAccountService;
        _googleTokenValidator = googleTokenValidator;
        _tokenLibrary = tokenLibrary;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserToken), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UserToken), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserToken>> Authenticate(
        [FromBody] ExternalAuthDTO auth, CancellationToken cancellationToken)
    {
        var payload = await _googleTokenValidator.ValidateAsync(auth.IdToken, cancellationToken);
        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email))
        {
            return Unauthorized(new { Message = "A verified Google identity and email are required." });
        }

        var identity = new ExternalIdentity(
            "google", payload.Subject, payload.Email,
            string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name, payload.Picture);
        var result = await _userAccountService.FindOrCreateExternalUserAsync(identity, cancellationToken);
        var token = _tokenLibrary.GetToken(result.User.ID, isV2User: true)
            ?? throw new InvalidOperationException("Failed to issue an access token.");
        var response = result.User.ToTokenDTO(token, _tokenLibrary.Expires);

        return result.IsNewUser
            ? CreatedAtAction(nameof(GetAuthenticatedUser), response)
            : Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = V2Authentication.UserPolicy)]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDTO>> GetAuthenticatedUser(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
            NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
        {
            return Unauthorized(new { Message = "Please sign in again." });
        }

        var user = await _userAccountService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Unauthorized(new { Message = "Please sign in again." });
        }

        return Ok(user.ToDTO());
    }
}
