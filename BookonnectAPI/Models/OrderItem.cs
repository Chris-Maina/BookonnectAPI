using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OrderItem
{
	public int ID { get; set; }
	public int Quantity { get; set; }

    /**
	 * OrderItem must be associated with a Book, Order and Customer
	 * We've added Customer to enable both Vendor and Customer to confirm dispatch and receipt
	 * The reference navigations for each of the above relations can be options
	 */
    public int BookID { get; set; }
    [JsonIgnore]
    public Book? Book { get; set; }

    public int OrderID { get; set; }
	public Order? Order { get; set; }

    public int CustomerID { get; set; }
    public User? Customer { get; set; }


    public static OrderItemDTO OrderItemToDTO(OrderItem orderItem)
	{
		return new OrderItemDTO
		{
			ID = orderItem.ID,
			Quantity = orderItem.Quantity,
			BookID = orderItem.BookID,
			Book = orderItem.Book != null ? Book.BookToDTO(orderItem.Book) : null,
		};
	}
}

