namespace BookonnectAPI.Controllers.V2
{
  using Microsoft.AspNetCore.Mvc;
  using BookonnectCore.Services.Interfaces;
  using BookonnectAPI.DTO;
  using BookonnectAPI.Mappings;
  using BookonnectAPI.Filters;
  using Microsoft.AspNetCore.Authorization;
  using System.Security.Claims;

  [ApiController]
  [Authorize(Policy = "UserClaimPolicy")]
  [Route("api/v2/[controller]")]
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePreference([FromBody] PreferenceDTO preferenceDTO)
    {
      try
      {
        _logger.LogInformation("Creating a new preference for UserId: {UserId}, BookId: {BookId}",
          preferenceDTO.UserId, preferenceDTO.BookId);
        if (preferenceDTO == null)
        {
          return BadRequest();
        }

        var preference = preferenceDTO.ToEntity();
        await _preferenceService.AddPreferenceAsync(preference);

        return CreatedAtAction(nameof(GetPreference), new { id = preference.Id }, preference.ToDTO());
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid Preference data provided");
        return BadRequest(new { Message = "Invalid preference data.",  Error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while creating preference");
        return StatusCode(500, new { Message = "Failed to create preference due to database operation error.", Error = ex.Message });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while creating preference");
        return StatusCode(408, new { Message = "The request timed out. Please try again later.", Error = ex.Message });
      }
      catch (Exception ex) when (!CoreExceptionFilter.CanHandle(ex))
      {
        _logger.LogError(ex, "An unexpected error occurred while creating preference");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PreferenceDTO>>> GetPreferences()
    {
      try
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
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid Preference request check user ID");
        return BadRequest(new { Message = "Preference request is invalid.", Error = ex.Message });
      }
      catch(InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while fetching preferences");
        return StatusCode(500, new { Message = "Failed to fetch preferences due to database operation error.", Error = ex.Message });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while fetching preferences");
        return StatusCode(408, new { Message = "The request timed out. Please try again later.", Error = ex.Message });
      }
      catch (Exception ex) when (!CoreExceptionFilter.CanHandle(ex))
      {
        _logger.LogError(ex, "An unexpected error occurred while fetching preferences");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PreferenceDTO>> GetPreference(int id)
    {
      try
      {
        var preference = await _preferenceService.GetPreferenceByIdAsync(id);
        if (preference == null)
        {
          return NotFound();
        }
        return Ok(preference.ToDTO());
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid Preference: {Id}", id);
        return BadRequest(new { Message = "Preference ID missing.", Error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while fetching preference with ID: {Id}", id);
        return StatusCode(500, new { Message = "Failed to fetch preference due to database operation error.", Error = ex.Message });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while fetching preference with ID: {Id}", id);
        return StatusCode(408, new { Message = "The request timed out. Please try again later.", Error = ex.Message });
      }
      catch (Exception ex) when (!CoreExceptionFilter.CanHandle(ex))
      {
        _logger.LogError(ex, "An unexpected error occurred while fetching preference");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
    }
  }
} 