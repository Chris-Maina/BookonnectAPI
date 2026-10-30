
namespace BookonnectAPI.Controllers.V2
{
  using BookonnectAPI.Services;
  using Microsoft.AspNetCore.Mvc;
  using BookonnectAPI.DTO;
  using BookonnectAPI.Lib;
  using Microsoft.AspNetCore.Authorization;
  using System.Security.Claims;

  [ApiController]
  [Authorize(Policy = V2Authentication.UserPolicy)]
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
      _logger.LogInformation("Fetching personalized recommendations");
      var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userId))
      {
        return Unauthorized(new { Message = "Please sign in again." });
      }

      var books = await _recommendationApiService.GetPersonalizedRecommendationsAsync(userId: int.Parse(userId));
      return Ok(books);
    }

    [HttpGet("{isbn13}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetSimilarBooks(string isbn13)
    {
      ArgumentException.ThrowIfNullOrEmpty(isbn13, nameof(isbn13));
      _logger.LogInformation("Fetching similar books for ISBN13: {ISBN13}", isbn13);
      var books = await _recommendationApiService.GetSimilarBooksAsync(isbn13);
      return Ok(books);
    }
  }
}