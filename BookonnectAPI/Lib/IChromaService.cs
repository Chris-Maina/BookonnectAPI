using System;
using System.Numerics;
using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IChromaService
{
  public Task InitializeAsync();
  public Task<List<string>> QuerySimilarBooksAsync(float[] userProfileVectorEmbedding, int limit = 5, List<int>? excludeBookIds = null);
  public Task<float[]?> GetBookEmbeddingByIdAsync(int bookId);
  public Task<List<float[]?>> GetBookEmbeddingsAsync(List<int> bookIds);
  public Task UpsertBookEmbeddingAsync(int bookId, float[] embedding, Dictionary<string, object>? metadata = null);
  public Task UpsertUserProfileEmbeddingAsync(int userId, float[] embedding, Dictionary<string, object>? metadata = null);
  public Task<float[]> CalculateUserProfileVectorEmbeddingAsync(List<ReviewDTO> userReviews);
}
