
namespace BookonnectAPI.Controllers.V2
{
  using BookonnectAPI.Services;
  using Microsoft.AspNetCore.Mvc;
  using BookonnectAPI.DTO;
  using Microsoft.AspNetCore.Authorization;
  using System.Security.Claims;

  [ApiController]
  [Authorize(Policy = "UserClaimPolicy")]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersion("2.0")]
  public class RecommendationsController : ControllerBase
  {
    private readonly IRecommendationApiService _recommendationApiService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(IRecommendationApiService recommendationApiService, ILogger<RecommendationsController> logger)
    {
      _recommendationApiService = recommendationApiService;
      _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetPersonalizedRecommendations()
    {
      try
      {
        _logger.LogInformation("Fetching personalized recommendations");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
          return Unauthorized(new { Message = "Please sign in again." });
        }

        var books = await _recommendationApiService.GetPersonalizedRecommendationsAsync(userId: int.Parse(userId));
        return Ok(books);
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid request check your user parameters: {Message}", ex.Message);
        return BadRequest(new { Message = "Invalid request check your user parameters.", Error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while fetching recommended books");
        return StatusCode(500, new { Message = "Failed to fetch recommended books due to database operation error.", Error = ex.Message });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while fetching recommended books");
        return StatusCode(408, new { Message = "The request timed out. Please try again later.", Error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unexpected error while fetching recommended books");
        return StatusCode(500, new { Message = "An unexpected error occurred while fetching recommended books." });
      }
    }

    [HttpGet("{isbn13}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetSimilarBooks(string isbn13)
    {
      try
      {
        ArgumentException.ThrowIfNullOrEmpty(isbn13, nameof(isbn13));
        _logger.LogInformation("Fetching similar books for ISBN13: {ISBN13}", isbn13);
        var books = await _recommendationApiService.GetSimilarBooksAsync(isbn13);
        return Ok(books);
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid ISBN13: {ISBN13}", isbn13);
        return BadRequest(new { Message = "Invalid ISBN13.", Error = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while fetching similar books for ISBN13: {ISBN13}", isbn13);
        return StatusCode(500, new { Message = "Failed to fetch similar books due to database operation error.", Error = ex.Message });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while fetching similar books for ISBN13: {ISBN13}", isbn13);
        return StatusCode(408, new { Message = "The request timed out. Please try again later.", Error = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unexpected error while fetching similar books for ISBN13: {ISBN13}", isbn13);
        return StatusCode(500, new { Message = "An unexpected error occurred while fetching similar books." });
      }
    }
  }
}