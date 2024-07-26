
namespace BookonnectAPI.Models;

public class ImageDTO
{
	public int ID { get; set; }
    public string Url { get; set; } = String.Empty;
    public string? File { get; set; } = String.Empty;
    public string PublicId { get; set; } = String.Empty;
    public int BookID { get; set; }
}

