using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Models;

[Index(nameof(BookID), IsUnique = false)]
public class OrderItem
{
	public int ID { get; set; }
	public int Quantity { get; set; }

	/**
	 * OrderItem must be associated with a Book, Order
	 * The reference navigations for each of the above relations are optional
	 */
    public int BookID { get; set; }
	[JsonIgnore]
	public Book? Book { get; set; }

    public int OrderID { get; set; }
	public Order? Order { get; set; }

    [JsonIgnore]
    public ICollection<Confirmation>? Confirmations { get; } // Optional collection navigation


    public static OrderItemDTO OrderItemToDTO(OrderItem orderItem)
	{
		return new OrderItemDTO
		{
			ID = orderItem.ID,
			Quantity = orderItem.Quantity,
			OrderID = orderItem.OrderID,
			BookID = orderItem.BookID,
			Book = orderItem.Book != null ? Book.BookToDTO(orderItem.Book) : null,
			Confirmations = orderItem.Confirmations != null ? orderItem.Confirmations.Select(Confirmation.ConfirmationToDTO).ToList() : null,
		};
	}
}

