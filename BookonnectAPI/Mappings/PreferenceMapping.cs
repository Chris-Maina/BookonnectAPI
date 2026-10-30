using BookonnectAPI.DTO;
using BookonnectCore.Models;
namespace BookonnectAPI.Mappings
{
  public static class PreferenceMapping
  {
    public static PreferenceDTO ToDTO(this Preference preference)
    {
      return new PreferenceDTO
      {
        UserId = preference.UserId,
        BookId = preference.BookId,
        IsLiked = preference.IsLiked,
        Book = preference.Book?.ToDTO()
      };
    }

    public static Preference ToEntity(this PreferenceDTO preferenceDTO)
    {
      return new Preference
      {
        UserId = preferenceDTO.UserId,
        BookId = preferenceDTO.BookId,
        IsLiked = preferenceDTO.IsLiked,
        Book = preferenceDTO.Book != null ? preferenceDTO.Book.ToEntity() : null!
      };
    }
  }
}
