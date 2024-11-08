namespace BookonnectAPI.Models;

public class Signature
{
	public int ID { get; set; }
	public DateTime DateTime { get; set; }
	public bool Signed { get; set; }
	/*
	 * A signature must be associated with an OrderVendor
	 * A signature must be associated/signed off with a User
	 */
	public int OrderVendorID { get; set; }
	public OrderVendor OrderVendor { get; set; } = null!;
	public int UserID { get; set; }
	public User User { get; set; } = null!;
}

