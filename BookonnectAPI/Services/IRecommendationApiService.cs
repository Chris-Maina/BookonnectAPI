using BookonnectAPI.DTO;

namespace BookonnectAPI.Services
{
  public interface IRecommendationApiService
  {
    Task<List<BookDTO>> GetPersonalizedRecommendationsAsync(int userId, int limit = 10);
    Task<IEnumerable<BookDTO>> GetSimilarBooksAsync(string isbn13, int limit = 5);
  }
}