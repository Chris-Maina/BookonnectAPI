
namespace BookonnectAPI.Services
{
  using BookonnectAPI.DTO;
  using BookonnectAPI.Mappings;
  using BookonnectCore.Services.Interfaces;
  public class RecommendationApiService : IRecommendationApiService
  {
    private readonly IVectorOperationsService _vectorOperationsService;
    private readonly IBookService _bookService;
    private readonly ILogger<RecommendationApiService> _logger;

    public RecommendationApiService(IVectorOperationsService vectorOperationsService, IBookService bookService, ILogger<RecommendationApiService> logger)
    {
      _vectorOperationsService = vectorOperationsService;
      _bookService = bookService;
      _logger = logger;
    }

    public async Task<List<BookDTO>> GetPersonalizedRecommendationsAsync(int userId, int limit = 10)
    {
      var userProfileVector = await _vectorOperationsService.GetUserProfileVectorAsync(userId);
      if (userProfileVector?.Embeddings == null)
      {
        _logger.LogWarning("User profile vector or embeddings is null for userId {UserId}", userId);
        return new List<BookDTO>();
      }
      var embeddingsArray = userProfileVector.Embeddings.Value.ToArray();
      var recommendedBooks = await _vectorOperationsService.GetSimilarBooksAsync(embeddingsArray, limit);
      // Fetch books from database based on recommendedBooks IDs
      var bookIds = recommendedBooks.Select(b => b.Id).ToList();
      var books = await _bookService.GetBooksByIdsAsync(bookIds);

      return books.Select(b => b.ToDTO()).ToList();
    }

    public async Task<IEnumerable<BookDTO>> GetSimilarBooksAsync(string isbn13, int limit = 5)
    {
      var bookVector = await _vectorOperationsService.GetBookEmbeddingAsync(isbn13);
      if (bookVector?.Embeddings == null)
      {
        _logger.LogWarning("Book vector or embeddings is null for isbn13 {ISBN13}", isbn13);
        return Enumerable.Empty<BookDTO>();
      }
      var embeddingsArray = bookVector.Embeddings.Value.ToArray();
      var similarBooks = await _vectorOperationsService.GetSimilarBooksAsync(embeddingsArray, limit);
      // Fetch books from database based on similarBooks IDs
      var bookIds = similarBooks.Select(b => b.Id).ToList();
      var books = await _bookService.GetBooksByIdsAsync(bookIds);

      return books.Select(b => b.ToDTO()).ToList();
    }
  }
}