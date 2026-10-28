namespace BookonnectAPI.Controllers.V2
{
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using BookonnectAPI.Services;
  using BookonnectAPI.DTO;
  using BookonnectAPI.Mappings;
  
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookDTO>> GetBookByIdAsync(string ISBN13)
    {
      _logger.LogInformation("Fetching book with ISBN13: {ISBN13}", ISBN13);
      var book = await _bookService.GetBookByIdAsync(ISBN13);
      if (book == null)
      {
        return NotFound();
      }
      return Ok(book.ToDTO());
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetAllBooksAsync([FromQuery] Models.QueryParameter queryParameters)
    {
      var books = await _bookService.GetAllBooksWithPaginationAsync(queryParameters);
      return Ok(books);
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> SearchBooksAsync([FromQuery] Models.SearchQueryParameters queryParameters)
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
  }
}