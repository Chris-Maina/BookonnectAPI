namespace BookonnectAPI.Models;

public class OrderVendor
{
	public int ID { get; set; }

	/**
	 * OrderVendor must be associated to an Order and Vendor
	 */
	public int OrderID { get; set; }
	public Order Order { get; set; } = null!;
	public int VendorID { get; set; }
	public User Vendor { get; set; } = null!;
}

