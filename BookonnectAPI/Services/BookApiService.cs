using BookonnectAPI.DTO;
using BookonnectAPI.Mappings;
using BookonnectCore.Services;
using BookonnectCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Services
{
  public class BookApiService : BookService, IBookApiService
  {
    private readonly ILogger<BookApiService> _logger;
    private readonly IRelationalOperationsService _relationalOperationsService;

    public BookApiService(ILogger<BookApiService> logger, IRelationalOperationsService relationalOperationsService)
    : base(logger, relationalOperationsService)
    {
      _logger = logger;
      _relationalOperationsService = relationalOperationsService;
    }

    public async Task<IEnumerable<BookDTO>> GetAllBooksWithPaginationAsync(Models.QueryParameter queryParameters)
    {
      _logger.LogInformation("Fetching all books with query parameters: {@QueryParameters}", queryParameters);
      if (queryParameters == null)
      {
        _logger.LogWarning("Query parameters are null");
        throw new ArgumentNullException(nameof(queryParameters), "Query parameters cannot be null");
      }

      if (queryParameters.Page <= 0 || queryParameters.Size <= 0)
      {
        _logger.LogWarning("Invalid pagination parameters: Page={Page}, Size={Size}",
          queryParameters.Page, queryParameters.Size);
        throw new ArgumentException("Page and Size must be greater than 0", nameof(queryParameters));
      }

      if (queryParameters.Size > 1000) // Prevent excessively large page sizes
      {
        _logger.LogWarning("Page size too large: {Size}", queryParameters.Size);
        throw new ArgumentException("Page size cannot exceed 1000", nameof(queryParameters));
      }

      var books = await _relationalOperationsService.SafeDatabaseReadAsync
      (
        async dbContext =>
        {
          var books = await dbContext.Books
          .AsNoTracking()
          .Skip((queryParameters.Page - 1) * queryParameters.Size)
          .Take(queryParameters.Size)
          .Select(b => b.ToDTO())
          .ToListAsync();

          _logger.LogInformation("Fetched {Count} books", books.Count);
          return books;
        },
        nameof(GetAllBooksWithPaginationAsync)
      );
      _logger.LogInformation("Successfully fetched {Count} books", books.Count());

      return books;
    }

    public async Task<IEnumerable<BookDTO>> SearchBooksAsync(string query)
    {
      _logger.LogInformation("Searching books with query: {Query}", query);
      if (string.IsNullOrWhiteSpace(query))
      {
        _logger.LogWarning("Search query is null or empty");
        throw new ArgumentException("Search query cannot be null or empty", nameof(query));
      }
      // Set a reasonable safety limit to prevent memory issues
      const int MAX_RESULTS = 5000;
      var searchTermLower = query.ToLower();
      var books = await _relationalOperationsService.SafeDatabaseReadAsync
      (
        async dbContext => await dbContext.Books
          .AsNoTracking() // Improves performance by disabling change tracking. We only need to read data.
          .Where(b =>
            EF.Functions.Like(b.Title.ToLower(), $"%{searchTermLower}%") ||
            EF.Functions.Like(b.Authors.ToLower(), $"%{searchTermLower}%") ||
            EF.Functions.Like(b.Categories.ToLower(), $"%{searchTermLower}%"))
          .OrderBy(b => b.Title) // Add ordering for consistent pagination
          .Take(MAX_RESULTS) // Limit results to prevent overload
          .Select(b => b.ToDTO()) // Project to DTO in the database query
          .ToListAsync(),
          nameof(SearchBooksAsync)
      );

      _logger.LogInformation("Found {Count} books matching query: {Query}", books.Count(), query);

      return books;
    }
  }
}