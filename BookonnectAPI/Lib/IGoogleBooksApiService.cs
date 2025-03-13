using BookonnectAPI.DTO;
using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IGoogleBooksApiService
{
    public Task<GoogleBookApiResponse?> SearchBook(string searchTerm);
    public static IEnumerable<BookSearchDTO>? ConvertResponseToSearchDTO(GoogleBookApiResponse? response) => throw new NotImplementedException();
}
