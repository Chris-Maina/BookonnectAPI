using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookonnectAPI.Services;
using BookonnectAPI.DTO;
using BookonnectAPI.Mappings;

namespace BookonnectAPI.Controllers.V2
{
  [ApiController]
  [Authorize(Policy = "UserClaimPolicy")]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersion("2.0")]
  public class BooksController : ControllerBase
  {
    private readonly IBookApiService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookApiService bookService, ILogger<BooksController> logger)
    {
      _bookService = bookService;
      _logger = logger;
    }

    [HttpGet("{ISBN13}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookDTO>> GetBookByIdAsync(string ISBN13)
    {
      try
      {
        _logger.LogInformation("Fetching book with ISBN13: {ISBN13}", ISBN13);
        var book = await _bookService.GetBookByIdAsync(ISBN13);
        if (book == null)
        {
          return NotFound();
        }
        return Ok(book.ToDTO());
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid ISBN13: {ISBN13}", ISBN13);
        return BadRequest(new { Message = "ISBN13 cannot be null or empty." });
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogError(ex, "Book not found: {Message}", ex.Message);
        return NotFound(new { Message = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while fetching book with ISBN13: {ISBN13}", ISBN13);
        return StatusCode(500, new { Message = "Failed to fetch book due to database operation error." });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while fetching book with ISBN13: {ISBN13}", ISBN13);
        return StatusCode(408, new { Message = "The request timed out. Please try again later." });
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "Request was canceled while fetching book with ISBN13: {ISBN13}", ISBN13);
        return StatusCode(499, new { Message = "The request was canceled. Please try again." });
      }

      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while fetching the book");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
     
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetAllBooksAsync([FromQuery] Models.QueryParameter queryParameters)
    {
      try
      {
        var books = await _bookService.GetAllBooksWithPaginationAsync(queryParameters);
        return Ok(books);
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid query parameters");
        return BadRequest(new { Message = "Invalid query parameters." });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Failed to retrieve books");
        return StatusCode(500, new { Message = "Failed to retrieve books due to a database error." });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Request timed out while retrieving books");
        return StatusCode(408, new { Message = "The request timed out. Please try again later." });
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "Request was canceled while retrieving books");
        return StatusCode(499, new { Message = "The request was canceled. Please try again." });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unexpected error occurred while retrieving books");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> SearchBooksAsync([FromQuery] Models.SearchQueryParameters queryParameters)
    {
      try
      {
        if (string.IsNullOrEmpty(queryParameters.SearchTerm))
        {

          return Ok();
        }

        var books = await _bookService.SearchBooksAsync(queryParameters.SearchTerm);
        if (books == null || !books.Any())
        {
          return NotFound(new { Message = "No books found matching the search criteria." });
        }
        // Include Google Books API search
        return Ok(books);
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid search query");
        return BadRequest(new { Message = "Invalid search query." });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Failed to search books");
        return StatusCode(500, new { Message = "Failed to search books due to a database error." });
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Request timed out while searching books");
        return StatusCode(408, new { Message = "The request timed out. Please try again later." });
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "Request was canceled while searching books");
        return StatusCode(499, new { Message = "The request was canceled. Please try again." });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unexpected error occurred while searching books");
        return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
      }
    }
  }
}