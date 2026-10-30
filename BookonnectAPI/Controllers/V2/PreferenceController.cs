namespace BookonnectAPI.Controllers.V2
{
  using Microsoft.AspNetCore.Mvc;
  using BookonnectCore.Services.Interfaces;
  using BookonnectAPI.DTO;
  using BookonnectAPI.Mappings;
  using BookonnectAPI.Lib;
  using Microsoft.AspNetCore.Authorization;
  using System.Security.Claims;

  [ApiController]
  [Authorize(Policy = V2Authentication.UserPolicy)]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersion("2.0")]
  public class PreferenceController : ControllerBase
  {
    private readonly IPreferenceService _preferenceService;
    private readonly ILogger<PreferenceController> _logger;

    public PreferenceController(IPreferenceService preferenceService, ILogger<PreferenceController> logger)
    {
      _preferenceService = preferenceService;
      _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePreference([FromBody] PreferenceDTO preferenceDTO)
    {
      if (preferenceDTO == null)
      {
        return BadRequest();
      }

      var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
      if (preferenceDTO.UserId != userId)
      {
        return Forbid();
      }

      _logger.LogInformation("Creating a new preference for UserId: {UserId}, BookId: {BookId}",
        userId, preferenceDTO.BookId);
      var preference = preferenceDTO.ToEntity();
      preference.UserId = userId;
      // Book details are read-only; do not persist a client-supplied book graph.
      preference.Book = null!;
      await _preferenceService.AddPreferenceAsync(preference);

      return CreatedAtAction(nameof(GetPreference), new { id = preference.Id }, preference.ToDTO());
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PreferenceDTO>>> GetPreferences()
    {
      _logger.LogInformation("Fetching your preferences");
      var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userId))
      {
        return Unauthorized(new { Message = "Please sign in again." });
      }
      var preferences = await _preferenceService.GetPreferencesByUserIdAsync(int.Parse(userId));
      if (preferences == null || !preferences.Any())
      {
        return Ok(new List<PreferenceDTO>());
      }
      return Ok(preferences.Select(p => p.ToDTO()));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PreferenceDTO>> GetPreference(int id)
    {
      var preference = await _preferenceService.GetPreferenceByIdAsync(id);
      if (preference == null ||
          preference.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
      {
        return NotFound();
      }
      return Ok(preference.ToDTO());
    }
  }
} 