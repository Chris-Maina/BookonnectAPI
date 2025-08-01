using System;

namespace BookonnectAPI.Lib;

public interface IChromaService
{
  public Task InitializeAsync();
  public Task UpsertBookEmbeddingAsync(int bookId, float[] embedding, Dictionary<string, object>? metadata = null);
  public Task<List<string>> QuerySimilarBooksAsync(float[] embedding, int limit = 5, List<int>? excludeBookIds = null);

  public Task<float[]?> GetBookEmbeddingByIdAsync(int bookId);

}
