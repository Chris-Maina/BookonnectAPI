namespace BookonnectAPI.Models;

public enum Status
{
    Placed, // checkout started and order registered
    Shipped, // Owner of the book has shipped/sent the book
    Delivered, // Recipient has received the book and transaction completed successful
    Canceled, // Order does not go through due to reasons like stock issues or payment problems
};
public class OrderStatus
{
	public int ID { get; set; }
	public Status Status { get; set; }
    public DateTime DateTime { get; set; }
    /**
	 * An order status must be associated to an OrderVendor
	 */
    public int OrderVendorID { get; set; }
	public OrderVendor? OrderVendor { get; set; }

}

