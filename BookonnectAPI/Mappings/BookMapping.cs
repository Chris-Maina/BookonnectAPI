using BookonnectCore.Models;
using BookonnectAPI.DTO;

namespace BookonnectAPI.Mappings
{
  public static class BookMappings
  {
    public static BookDTO ToDTO(this Book book)
    {
      return new BookDTO
      {
        Title = book.Title,
        Authors = book.Authors,
        ISBN13 = book.ISBN13,
        PublishedYear = book.PublishedYear,
        Description = book.Description,
        ThumbnailUrl = book.ThumbnailUrl
      };
    }

    public static Book ToEntity(this BookDTO bookDTO)
    {
      return new Book
      {
        Title = bookDTO.Title,
        Authors = bookDTO.Authors,
        ISBN13 = bookDTO.ISBN13,
        PublishedYear = bookDTO.PublishedYear,
        Description = bookDTO.Description,
        ThumbnailUrl = bookDTO.ThumbnailUrl
      };
    }
  }
}