namespace BookonnectAPI.Models;

public class BookDTO
{
	public int ID { get; set; }
    public string Title { get; set; } = String.Empty;
    public string Author { get; set; } = String.Empty;
    public string ISBN { get; set; } = string.Empty;
    public float Price { get; set; }
    public string? Description { get; set; }
    public ImageDTO? Image { get; set; }
    public bool Visible { get; set; }
    public BookCondition Condition { get; set; }
    public int Quantity { get; set; }
    public int? VendorID { get; set; }
    public UserDTO? Vendor { get; set; }
    public string? AffiliateLink { get; set; } = string.Empty;
    public string? AffiliateSource { get; set; } = string.Empty;
    public string? AffiliateSourceID { get; set; } = string.Empty;
}
