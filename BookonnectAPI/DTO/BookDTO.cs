namespace BookonnectAPI.DTO
{
  public class BookDTO
  {
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string ISBN13 { get; set; } = string.Empty;
    public DateTime PublishedYear { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
  }
}