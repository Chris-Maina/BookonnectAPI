using BookonnectCore.Services.Interfaces;
using BookonnectAPI.DTO;

namespace BookonnectAPI.Services
{
  public interface IBookApiService: IBookService
  {
    // Define methods for API book-related operations
    Task<IEnumerable<BookDTO>> GetAllBooksWithPaginationAsync(Models.QueryParameter queryParameters);
    Task<IEnumerable<BookDTO>> SearchBooksAsync(string query);
  }
}