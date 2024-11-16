namespace BookonnectAPI.Models;

public class BookDTO
{
	public int ID { get; set; }
    public string Title { get; set; } = String.Empty;
    public string Author { get; set; } = String.Empty;
    public string ISBN { get; set; } = string.Empty;
    public float Price { get; set; }
    public string? Description { get; set; }
    public int VendorID { get; set; }
    public UserDTO? Vendor { get; set; }
    public ImageDTO? Image { get; set; }
}

