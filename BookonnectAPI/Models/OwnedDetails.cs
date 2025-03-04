using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OwnedDetails
{
    public int ID { get; set; }

    /**
     * OwnedDetails must be associated with a user/owner/vendor and a book.
     * 
     */
    public int VendorID { get; set; }
    [JsonIgnore]
    public User Vendor { get; set; } = null!;
    public int BookID { get; set; } 
    public Book Book { get; set; } = null!;
}

