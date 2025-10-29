using System;
using MathNet.Numerics.LinearAlgebra;
using BookonnectAPI.Configuration;
using BookonnectAPI.Models;
using ChromaDB.Client;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Lib;

public class ChromaService : IChromaService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<ChromaService> _logger;
  private readonly ChromaClient _chromaClient;

  // separate collection clients for different entity types
  private ChromaCollectionClient? _booksCollectionClient;
  private ChromaCollectionClient? _usersCollectionClient;
  private readonly string _booksCollectionName = "books";
  private readonly string _usersCollectionName = "users_profiles";
  private readonly ChromaOptions _chromaOptions;


  public ChromaService(HttpClient httpClient, ILogger<ChromaService> logger, IOptions<ChromaOptions> options)
  {
    _httpClient = httpClient;
    _chromaOptions = options.Value;
    _logger = logger;
    _chromaClient = new ChromaClient(new ChromaConfigurationOptions(uri: _chromaOptions.Uri), _httpClient);
  }

  public async Task InitializeAsync()
  {
    try
    {
      // Get or create a collection (like a table in a database)
      // initialize books collection
      var booksCollection = await _chromaClient.GetOrCreateCollection(_booksCollectionName);
      _booksCollectionClient = new ChromaCollectionClient(booksCollection, new ChromaConfigurationOptions(uri: _chromaOptions.Uri), _httpClient);

      // initialize users collection
      var usersCollection = await _chromaClient.GetOrCreateCollection(_usersCollectionName);
      _usersCollectionClient = new ChromaCollectionClient(usersCollection, new ChromaConfigurationOptions(uri: _chromaOptions.Uri), _httpClient);
      _logger.LogInformation("Chroma collections initialized: {BooksCollectionName}, {UsersCollectionName}", _booksCollectionName, _usersCollectionName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to initialize Chroma collections");
      throw; // Re-throw to let caller handle
    }
  }

  public async Task UpsertBookEmbeddingAsync(int bookId, float[] embedding, Dictionary<string, object>? metadata = null)
  {
    try
    {

      if (_booksCollectionClient == null)
      {
        await InitializeAsync();
      }

      // Chroma expects IDs as strings and embeddings as List<Float>
      string bookIdString = bookId.ToString();
      var ids = new List<string> { bookIdString };
      var embeddings = new List<ReadOnlyMemory<float>> { embedding };
      var defaultMetadata = new Dictionary<string, object>
      {
        { "book_id", bookIdString }
      };
      var metadatas = new List<Dictionary<string, object>> { metadata ?? defaultMetadata };

      // Chroma's Add method ignores and ID if it already exists
      await _booksCollectionClient!.Add(ids, embeddings, metadatas);
      _logger.LogInformation("Upserted embedding for book ID: {BookId}", bookId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upsert embedding for book ID: {BookId}", bookId);
      throw; // Re-throw to let caller handle
    }
  }

  public async Task UpsertUserProfileEmbeddingAsync(int userId, float[] embedding, Dictionary<string, object>? metadata = null)
  {
    try
    {
      if (_usersCollectionClient == null)
      {
        await InitializeAsync();
      }

      // Chroma expects IDs as strings and embeddings as List<Float>
      string userIdString = userId.ToString();
      var ids = new List<string> { userIdString };
      var embeddings = new List<ReadOnlyMemory<float>> { embedding };
      var defaultMetadata = new Dictionary<string, object>
      {
        { "user_id", userIdString }
      };
      var metadatas = new List<Dictionary<string, object>> { metadata ?? defaultMetadata };

      await _usersCollectionClient!.Add(ids, embeddings, metadatas);
      _logger.LogInformation("Upserted user profile embedding for user ID: {UserId}", userId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upsert user profile embedding for user ID: {UserId}", userId);
      throw;
    }
  }

  public async Task<List<int>> QuerySimilarBooksAsync(float[] userProfileVectorEmbedding, int limit = 5, List<int>? excludeBookIds = null)
  {
    try
    {
      if (_booksCollectionClient == null)
      {
        await InitializeAsync();
      }

      // Convert to ReadOnlyMemory<float> for single query
      ReadOnlyMemory<float> queryEmbeddings = userProfileVectorEmbedding;

      // construct the filter for excluded IDs
      ChromaWhereOperator? filter = null;
      if (excludeBookIds != null && excludeBookIds.Any())
      {
        var excludeBookIdsAsStrings = excludeBookIds.Select(id => id.ToString()).ToList();
        filter = ChromaWhereOperator.NotIn("book_id", excludeBookIdsAsStrings);
      }

      // Query similar books based on the embedding
      var queryResult = await _booksCollectionClient!.Query(queryEmbeddings, nResults: limit, where: filter);

      // Extract IDs from the query result
      var similarBookIds = queryResult.Select(result => int.Parse(result.Id)).ToList();
      _logger.LogInformation("Queried similar books for user profile vector embedding: {Embedding}, found {Count} results", string.Join(", ", userProfileVectorEmbedding), similarBookIds.Count);

      return similarBookIds;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to query similar books for user profile vector embedding: {Embedding}", string.Join(", ", userProfileVectorEmbedding));
      throw;
    }
  }

  public async Task<float[]?> GetBookEmbeddingByIdAsync(int bookId)
  {
    try
    {
      if (_booksCollectionClient == null)
      {
        await InitializeAsync(); // Ensure the collection is initialized
      }

      // Get the embedding for the specified book ID
      var result = await _booksCollectionClient!.Get(bookId.ToString());
      if (result == null)
      {
        _logger.LogWarning("No embedding found for book ID: {BookId}", bookId);
        return null; // Return null if no embedding is found
      }

      _logger.LogInformation("Retrieved embedding for book ID: {BookId}", bookId);
      return result.Embeddings?.ToArray();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get book embedding by ID: {BookId}", bookId);
      throw;
    }
  }

  public async Task<List<float[]?>> GetBookEmbeddingsAsync(List<int> bookIds)
  {
    try
    {
      if (_booksCollectionClient == null)
      {
        await InitializeAsync();
      }

      // Convert book IDs to string for ChromaDB
      var bookIdStrings = bookIds.Select(id => id.ToString()).ToList();

      // Retrieve embeddings for the specified book IDs.
      // The 'include' parameter is crucial here to ensure embeddings are returned.
      var results = await _booksCollectionClient!.Get(ids: bookIdStrings, include: ChromaGetInclude.Embeddings);

      // Extract embeddings from the results
      var embeddings = results.Select(r => r.Embeddings?.ToArray()).Where(e => e != null).ToList();

      if (embeddings.Count == 0)
      {
        _logger.LogWarning("No embeddings found for the provided book IDs.");
        return new List<float[]?>();
      }

      // Assuming all embeddings are of the same dimension, return the first one as a sample
      return embeddings;
    }

    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get book embeddings for IDs: {BookIds}", string.Join(", ", bookIds));
      throw;
    }
  }

  public async Task<float[]> CalculateUserProfileVectorEmbeddingAsync(List<ReviewDTO> userReviews)
  {
    try
    {
      if (_usersCollectionClient == null)
      {
        await InitializeAsync();
      }

      var likedBookIds = userReviews
        .Where(r => r.Status == ReviewStatus.Like)
        .Select(r => r.BookID)
        .Distinct()
        .ToList();

      var dislikedBookIds = userReviews
        .Where(r => r.Status == ReviewStatus.Dislike)
        .Select(r => r.BookID)
        .Distinct()
        .ToList();

      // Retrieve embeddings for liked books
      var likedEmbeddings = new List<Vector<float>>();
      var likedBookEmbeddings = await GetBookEmbeddingsAsync(likedBookIds);
      if (likedBookEmbeddings == null)
      {
        _logger.LogWarning("No embeddings found for liked books.");
        return Vector<float>.Build.Dense(GeminiService.EmbeddingDimension).ToArray();
      }
      foreach (var embedding in likedBookEmbeddings)
      {
        if (embedding != null)
        {
          likedEmbeddings.Add(Vector<float>.Build.Dense(embedding));
        }
      }

      // Retrieve embeddings for disliked books
      var dislikedEmbeddings = new List<Vector<float>>();
      var dislikedBookEmbeddings = await GetBookEmbeddingsAsync(dislikedBookIds);
      if (dislikedBookEmbeddings == null)
      {
        _logger.LogWarning("No embeddings found for disliked books.");
        return Vector<float>.Build.Dense(GeminiService.EmbeddingDimension).ToArray();
      }
      foreach (var embedding in dislikedBookEmbeddings)
      {
        if (embedding != null)
        {
          dislikedEmbeddings.Add(Vector<float>.Build.Dense(embedding));
        } 
      }

      Vector<float> vLiked = Vector<float>.Build.Dense(GeminiService.EmbeddingDimension);
      if (likedEmbeddings.Count > 0)
      {
        vLiked = likedEmbeddings.Aggregate((current, next) => current + next) / likedEmbeddings.Count;
      }
      
      Vector<float> vDisliked = Vector<float>.Build.Dense(GeminiService.EmbeddingDimension);
      if (dislikedEmbeddings.Count > 0)
      {
        vDisliked = dislikedEmbeddings.Aggregate((current, next) => current + next) / dislikedEmbeddings.Count;
      }

      Vector<float> userProfileVector = vLiked - vDisliked;

      // Normalize the vector
      if (userProfileVector.Norm(2) == 0)
      {
        return Vector<float>.Build.Dense(GeminiService.EmbeddingDimension).ToArray();
      }

      return userProfileVector.Normalize(2).ToArray();

    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to calculate user profile vector embedding from reviews");
      throw;
    }
  }

}
