﻿using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OrderItem
{
	public int ID { get; set; }
	public int Quantity { get; set; }

    /**
	 * OrderItem must be associated with a Book, Order
	 * The reference navigations for each of the above relations can be options
	 */
    public int BookID { get; set; }
	[JsonIgnore]
	public Book Book { get; set; } = null!;

    public int OrderID { get; set; }
	public Order? Order { get; set; }

    public ICollection<Confirmation>? Confirmations { get; } // Optional collection navigation


    public static OrderItemDTO OrderItemToDTO(OrderItem orderItem)
	{
		return new OrderItemDTO
		{
			ID = orderItem.ID,
			Quantity = orderItem.Quantity,
			BookID = orderItem.BookID,
			Book = Book.BookToDTO(orderItem.Book),
			Confirmations = orderItem.Confirmations,
		};
	}
}

