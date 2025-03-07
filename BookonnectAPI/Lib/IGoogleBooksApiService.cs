using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IGoogleBooksApiService
{
    public Task<GoogleBookApiResponse?> SearchBook(string searchTerm);
}
