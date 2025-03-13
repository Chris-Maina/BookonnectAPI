using System.Text;
using System.Text.Json;
using BookonnectAPI.Configuration;
using BookonnectAPI.Models;
using BookonnectAPI.DTO;
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
        var httpResponseMessage = await _httpClient
            .GetAsync($"/books/v1/volumes?q={searchTerm}&orderBy=newest&printType=BOOKS&projection=FULL&fields=items(id,volumeInfo(title,authors,description,industryIdentifiers,imageLinks/smallThumbnail))&key={_googleOptions.BooksApiKey}");

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

    public static IEnumerable<BookSearchDTO>? ConvertResponseToSearchDTO(GoogleBookApiResponse? response)
    {

        if (response == null)
        {
            return null;
        }

        if (response.Items == null || (response.Items != null && response.Items.Count() == 0))
        {
            return null;
        }

        return response.Items?
            .Select(bookItem => new BookSearchDTO
            {
                ID = bookItem.Id,
                BookId = null,
                Title = bookItem.VolumeInfo == null ? null : bookItem.VolumeInfo.Title,
                Authors = bookItem.VolumeInfo?.Authors == null ? null : bookItem.VolumeInfo?.Authors,
                Description = bookItem.VolumeInfo?.Description == null ? null : bookItem.VolumeInfo.Description,
                ImageUrl = bookItem.VolumeInfo?.ImageLinks?.smallThumbnail == null ? null : bookItem.VolumeInfo?.ImageLinks?.smallThumbnail,
                ISBN = GetISBN(bookItem.VolumeInfo?.IndustryIdentifiers)
            })
            .ToArray();

    }

    private static string? GetISBN (GoogleBookIdentifiers[]? identifiers)
    {
        if (identifiers == null)
        {
            return null;
        }
        if (identifiers.Count() == 0)
        {
            return null;
        }

        return identifiers[0].Identifier;
    }
}

