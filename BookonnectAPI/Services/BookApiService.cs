using BookonnectAPI.Data;
using BookonnectAPI.DTO;
using BookonnectAPI.Mappings;
using BookonnectCore.Models;
using BookonnectCore.Services;
using BookonnectCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Services
{
  public class BookApiService : BookService, IBookApiService
  {
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BookApiService> _logger;

    public BookApiService(IServiceScopeFactory serviceScopeFactory, ILogger<BookApiService> logger, IRelationalOperationsService relationalOperationsService)
    : base(serviceScopeFactory, logger, relationalOperationsService)
    {
      _serviceScopeFactory = serviceScopeFactory;
      _logger = logger;
    }

    public async Task<IEnumerable<BookDTO>> GetAllBooksWithPaginationAsync(Models.QueryParameter queryParameters)
    {
      try
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

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var books = await dbContext.Books
          .AsNoTracking()
          .Skip((queryParameters.Page - 1) * queryParameters.Size)
          .Take(queryParameters.Size)
          .Select(b => b.ToDTO())
          .ToListAsync();

        _logger.LogInformation("Fetched {Count} books", books.Count);
        return books;
      }
      catch (ArgumentNullException ex)
      {
        _logger.LogError(ex, "Query parameters validation failed");
        throw;
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid query parameters: {Message}", ex.Message);
        throw;
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed: {Message}", ex.Message);
        throw new InvalidOperationException("Failed to fetch books due to database operation error", ex);
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database query timeout while fetching books");
        throw new TimeoutException("The request took too long to complete. Please try again.", ex);
      }
      catch (DbUpdateException ex)
      {
        _logger.LogError(ex, "Database update exception while fetching books");
        throw new InvalidOperationException("Database error occurred while fetching books", ex);
      }
      catch (TaskCanceledException ex)
      {
        _logger.LogError(ex, "Request was cancelled while fetching books");
        throw new OperationCanceledException("The operation was cancelled", ex);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while fetching books");
        throw;
      }
    }

    public async Task<IEnumerable<BookDTO>> SearchBooksAsync(string query)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(query))
        {
          _logger.LogWarning("Search query is null or empty");
          throw new ArgumentException("Search query cannot be null or empty", nameof(query));
        }
        // Set a reasonable safety limit to prevent memory issues
        const int MAX_RESULTS = 5000;

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var searchTermLower = query.ToLower();

        var books = await dbContext.Books
          .AsNoTracking() // Improves performance by disabling change tracking. We only need to read data.
          .Where(b =>
            EF.Functions.Like(b.Title.ToLower(), $"%{searchTermLower}%") ||
            EF.Functions.Like(b.Authors.ToLower(), $"%{searchTermLower}%") ||
            EF.Functions.Like(b.Categories.ToLower(), $"%{searchTermLower}%"))
          .OrderBy(b => b.Title) // Add ordering for consistent pagination
          .Take(MAX_RESULTS) // Limit results to prevent overload
          .Select(b => b.ToDTO()) // Project to DTO in the database query
          .ToListAsync();

        return books;
      }
      catch (ArgumentException ex)
      {
        _logger.LogError(ex, "Invalid search query: {Query}", query);
        throw;
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogError(ex, "Database operation failed while searching for books");
        throw new InvalidOperationException("Failed to search books due to database operation error", ex);
      }
      catch (DbUpdateException ex)
      {
        _logger.LogError(ex, "Database update exception while searching for books");
        throw new InvalidOperationException("Database error occurred while searching for books", ex);
      }
      catch (TimeoutException ex)
      {
        _logger.LogError(ex, "Database operation timeout while searching for books");
        throw new TimeoutException("The request took too long to complete. Please try again.", ex);
      }
      catch (TaskCanceledException ex)
      {
        _logger.LogError(ex, "Request was cancelled while searching for books");
        throw new OperationCanceledException("The operation was cancelled", ex);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unexpected error occurred while searching for books");
        throw;
      }
    }
  }
}