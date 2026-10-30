namespace BookonnectAPI.DTO
{
  public class PreferenceDTO
  {
    public int UserId { get; set; }
    public string BookId { get; set; } = string.Empty;
    public bool IsLiked { get; set; }
    public BookDTO? Book { get; set; }
  }
}