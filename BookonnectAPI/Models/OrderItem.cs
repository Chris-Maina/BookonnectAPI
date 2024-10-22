using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OrderItem
{
	public int ID { get; set; }
	public int Quantity { get; set; }

    public int BookID { get; set; } // Required foreign key reference. An order item is related to one product.
    [JsonIgnore]
    public Book? Book { get; set; }

    public int OrderID { get; set; } // Required foreign key reference. An order item cannot exist without an order
	public Order? Order { get; set; } // Optional reference navigation

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

