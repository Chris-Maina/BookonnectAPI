using BookonnectAPI.Configuration;
using BookonnectAPI.DTO;
using BookonnectAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BookonnectAPI.Lib;

public class GeminiService: IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOptions _googleOptions;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger, IOptions<GoogleOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
        _logger = logger;
        _googleOptions = options.Value;
    }

    public async Task<GeminiApiResponse?> GenerateContent(string prompt) {
        _logger.LogInformation("Sending generate content request to Gemini LLM");
        var payload = new
        {
            systemInstruction = new
            {
                Parts = new[]
                {
                    new { Text = "You are a book recommendations assistant or agent.The user will provide their email and books they like and dislike.Respond with books the user they would enjoy reading."}
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
                likes.Add($"{review.Book?.Title} by {review.Book?.Author}" );
            }
            if (review.Status == ReviewStatus.Dislike)
            {
                disLikes.Add($"{review.Book?.Title} by {review.Book?.Author}");
            }

        }
        string likesString = string.Join(", ", likes);
        string disLikesString = string.Join(", ", disLikes);

        return $@"Give 4 book recommendations to {email}.They like {likesString} and dislike {disLikesString}.Include id, title, ISBN, author and description information in the response.Strictly adhere to the JSON schema.";
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
}

