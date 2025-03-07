using System.Text.Json;
using BookonnectAPI.Configuration;
using BookonnectAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BookonnectAPI.Lib;

public class GoogleBooksApiService: IGoogleBooksApiService
{
	private readonly HttpClient _httpClient;
    private readonly GoogleOptions _googleOptions;
    private readonly ILogger<GoogleBooksApiService> _logger;

    public GoogleBooksApiService(HttpClient httpClient, IOptions<GoogleOptions> googleOptions, ILogger<GoogleBooksApiService> logger)
	{
		_httpClient = httpClient;
		_httpClient.BaseAddress = new Uri("https://books.googleapis.com");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
        _googleOptions = googleOptions.Value;
        _logger = logger;
    }

    public async Task<GoogleBookApiResponse?> SearchBook(string searchTerm)
	{

        _logger.LogInformation("Sending search request to Google Books API");
        var httpResponseMessage = await _httpClient.GetAsync($"/books/v1/volumes?q={searchTerm}&orderBy=newest&printType=BOOKS&projection=FULL&key={_googleOptions.BooksApiKey}");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return await JsonSerializer.DeserializeAsync
                <GoogleBookApiResponse?>(contentStream, options);

        }
        return null;
    }
}

