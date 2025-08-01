using System;
using BookonnectAPI.Configuration;
using ChromaDB.Client;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Lib;

public class ChromaService: IChromaService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<ChromaService> _logger;
  private readonly ChromaClient _chromaClient;
  private readonly string _collectionName = "books";
  private ChromaCollectionClient? _collectionClient; // Client for a specific collection

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
      var collection = await _chromaClient.GetOrCreateCollection(_collectionName);
      _collectionClient = new ChromaCollectionClient(collection, new ChromaConfigurationOptions(uri: _chromaOptions.Uri), _httpClient);
      _logger.LogInformation("Chroma collection initialized: {CollectionName}", _collectionName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to initialize Chroma collection: {CollectionName}", _collectionName);
      throw; // Re-throw to let caller handle
    }
  }

  public async Task UpsertBookEmbeddingAsync(int bookId, float[] embedding, Dictionary<string, object>? metadata = null)
  {
    try
    {

      if (_collectionClient == null)
      {
        await InitializeAsync();
      }

      // Chroma expects IDs as strings and embeddings as List<Float>
      var ids = new List<string> { bookId.ToString() };
      var embeddings = new List<ReadOnlyMemory<float>> { embedding };
      var metadatas = new List<Dictionary<string, object>> { metadata ?? new Dictionary<string, object>() };

      // Chroma's Add method handles upserts if ID already exists
      await _collectionClient!.Add(ids, embeddings, metadatas);
      _logger.LogInformation("Upserted embedding for book ID: {BookId}", bookId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upsert embedding for book ID: {BookId}", bookId);
      throw; // Re-throw to let caller handle
    }
  }

  public async Task<List<string>> QuerySimilarBooksAsync(float[] embedding, int limit = 5, List<int>? excludeBookIds = null)
  {
    try
    {
      if (_collectionClient == null)
      {
        await InitializeAsync();
      }

      // Convert to ReadOnlyMemory<float> for single query
      ReadOnlyMemory<float> queryEmbeddings = embedding;

      // construct the filter for excluded IDs
      ChromaWhereOperator? filter = null;
      if (excludeBookIds != null && excludeBookIds.Any())
     {
        var excludeBookIdsAsStrings = excludeBookIds.Select(id => id.ToString()).ToList();
        filter = ChromaWhereOperator.NotIn("book_id", excludeBookIdsAsStrings);
      }

      // Query similar books based on the embedding
      _logger.LogInformation("Querying similar books for embedding: {Embedding}", string.Join(", ", embedding));
     var queryResult = await _collectionClient!.Query(queryEmbeddings, nResults: limit, where: filter);

      // Extract IDs from the query result
      var similarBookIds = queryResult.Select(result => result.Id).ToList();
      _logger.LogInformation("Queried similar books for embedding: {Embedding}, found {Count} results", string.Join(", ", embedding), similarBookIds.Count);

      return similarBookIds;
   }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to query similar books for embedding: {Embedding}", string.Join(", ", embedding));
      throw;
    }
  }

  public async Task<float[]?> GetBookEmbeddingByIdAsync(int bookId)
  {
    try
    {
      if (_collectionClient == null)
      {
        await InitializeAsync(); // Ensure the collection is initialized
      }

      // Get the embedding for the specified book ID
      var result = await _collectionClient!.Get(bookId.ToString());
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
}
