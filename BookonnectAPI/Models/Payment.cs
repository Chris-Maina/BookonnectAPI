namespace BookonnectAPI.Models;

public class Payment
{

	public string ID { get; set; } = string.Empty;// will store the MPESA transaction code
    public string? Phone { get; set; } // phone number that made payment
    public DateTime DateTime { get; set; }

    public int UserID { get; set; } // Required foreign key
    public User? User { get; set; } = null!; // Optional reference navigation
    public int OrderID { get; set; } // Required foreign key
    public Order Order { get; set; } = null!; // Optional reference navigation.

}

