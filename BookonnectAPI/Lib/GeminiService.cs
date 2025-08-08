using BookonnectAPI.Configuration;
using BookonnectAPI.DTO;
using BookonnectAPI.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BookonnectAPI.Lib;

public class GeminiService : IGeminiService
{
  private readonly HttpClient _httpClient;
  private readonly GoogleOptions _googleOptions;
  private readonly ILogger<GeminiService> _logger;

  public static readonly int EmbeddingDimension = 768; // Default gemini-embedding-001 embedding dimension is 3072. Scaling down to 768 for compatibility with ChromaDB storage.

  public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger, IOptions<GoogleOptions> options)
  {
    _httpClient = httpClient;
    _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    _logger = logger;
    _googleOptions = options.Value;
  }

  public async Task<GeminiApiResponse?> GenerateContent(string prompt)
  {
    _logger.LogInformation("Sending generate content request to Gemini LLM");
    var payload = new
    {
      systemInstruction = new
      {
        Parts = new[]
            {
                    new { Text = "You are a book recommendations assistant or agent.The user will provide their email and books they like and dislike.Respond with books the user they would enjoy reading.Consider responding with one of either print, audio or ebook. Share the full book blurb as description."}
                }
      },
      contents = new[]
        {
               new
               {
                    role = "user",
                    Parts = new[]
                    {
                        new { Text = prompt },
                    },
               }
            },
      generationConfig = new
      {
        temperature = 1.0,
        response_mime_type = "application/json",
        response_schema = new
        {
          type = "ARRAY",
          items = new
          {
            type = "OBJECT",
            properties = new
            {
              // Use BookSearchDTO properties. Rename to ExternalBookDTO
              id = new { type = "STRING" },
              title = new { type = "STRING" },
              isbn = new { type = "STRING" },
              description = new { type = "STRING" },
              authors = new { type = "ARRAY", items = new { type = "STRING" } },
            },
            propertyOrdering = new[] { "id", "title", "isbn", "authors", "description" }
          }
        }
      }
    };

    JsonContent content = JsonContent.Create(payload);
    var httpResponseMessage = await _httpClient.PostAsync($"/v1beta/models/gemini-2.0-flash:generateContent?key={_googleOptions.GeminiApiKey}", content);
    if (httpResponseMessage.IsSuccessStatusCode)
    {
      var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };

      return await JsonSerializer.DeserializeAsync<GeminiApiResponse>(responseStream, options);
    }
    // throw error returned in httpResponseMessage
    return null;
  }
  public string GetRecommendationsPrompt(string email, IEnumerable<Review> reviews)
  {
    List<string> likes = new List<string>();
    List<string> disLikes = new List<string>();
    foreach (var review in reviews)
    {
      if (review.Status == ReviewStatus.Like)
      {
        likes.Add($"{review.Book?.Title} by {review.Book?.Author}");
      }
      if (review.Status == ReviewStatus.Dislike)
      {
        disLikes.Add($"{review.Book?.Title} by {review.Book?.Author}");
      }

    }
    string likesString = string.Join(", ", likes);
    string disLikesString = string.Join(", ", disLikes);

    return $@"Give 4 book recommendations to {email}. Consider responding with one of either print, audio or ebook. They like {likesString} and dislike {disLikesString}.Include id, title, ISBN, author and description information in the response. Strictly adhere to the JSON schema.";
  }

  public BookSearchDTO[]? DeserializeGeminiResponse(GeminiApiResponse? response)
  {
    if (
           response == null ||
           response.Candidates == null ||
           response.Candidates?.Length == 0 ||
           response.Candidates?[0].Content == null ||
           response.Candidates[0].Content?.Parts == null ||
           response.Candidates[0].Content?.Parts?.Length == 0 ||
           response.Candidates[0].Content?.Parts?[0].Text == null)
    {
      return null;
    }
    var options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };
    return JsonSerializer.Deserialize<BookSearchDTO[]>(response.Candidates[0].Content?.Parts?[0].Text!, options);
  }
    
  public async Task<float[]?> GetBookEmbeddingAsync(string bookTitle, string bookDescription, string bookAuthor, string? bookGenre)
  {
    try
    {
      _logger.LogInformation("Requesting embedding for book: {Title}", bookTitle);
      var payload = new
      {
        content = new[]
        {
          new
          {
            text = $"Generate a dense vector embedding that captures the core themes, plot elements, and overall tone of the following book description. Focus on aspects that would help identify similar books for a reader: Title:{bookTitle} Author:{bookAuthor} Genre:{bookGenre} Synopsis:{bookDescription}"
          }
        },
        model = "gemini-embedding-001",
        generationConfig = new
        {
          responseMimeType = "application/json",
          responseSchema = new
          {
            type = "ARRAY",
            items = new
            {
              type = "FLOAT",
              format = "float32"
            }
          }
        }
      };

      JsonContent content = JsonContent.Create(payload);
      var httpResponseMessage = await _httpClient.PostAsync($"/v1beta/models/gemini-embedding-001:embed?key={_googleOptions.GeminiApiKey}", content);
      if (!httpResponseMessage.IsSuccessStatusCode)
      {
        _logger.LogError("Failed to get embedding for book: {Title}. Status code: {StatusCode}", bookTitle, httpResponseMessage.StatusCode);
        return Array.Empty<float>();
      }

      var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync<GeminiEmbeddingResponse>(responseStream);

      if (result?.Embeddings == null || result.Embeddings.Length == 0)
      {
        _logger.LogError("No embeddings found for book: {Title}", bookTitle);
        return null;
      }

      var embedding = result.Embeddings[0];
      _logger.LogInformation("Successfully retrieved embedding for book: {Title}", bookTitle);
      
      return embedding.ToArray();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to get embedding for book: {Title}", bookTitle);
      throw;
    }
  }
}

