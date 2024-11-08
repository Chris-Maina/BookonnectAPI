namespace BookonnectAPI.Models;

public enum ConfirmationType
{
	Dispatch,
	Receipt,
	Cancel
}
public class Confirmation
{
	public int ID { get; set; }
	public DateTime DateTime { get; set; }
	public ConfirmationType Type { get; set; }
    /*
	 * A confirmation must be associated with an OrderItem
	 * A confirmation must be associated/signed off with a Vendor and Customer
	 */
    public int OrderItemID { get; set; }
	public OrderItem OrderItem { get; set; } = null!;
	public int UserID { get; set; }
	public User User { get; set; } = null!;
}

