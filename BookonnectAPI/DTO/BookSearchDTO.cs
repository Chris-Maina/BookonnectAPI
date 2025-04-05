namespace BookonnectAPI.DTO;

public class BookSearchDTO
{
    public string ID { get; set; } = string.Empty;
	public int? BookId { get; set; } // Book id in local DB. Missing for external sources
	public string? Title { get; set; }
    public string[]? Authors { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? ISBN { get; set; }
}
