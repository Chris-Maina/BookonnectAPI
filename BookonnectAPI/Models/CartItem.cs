namespace BookonnectAPI.Models;

public class CartItem
{
	public int ID { get; set; }
	public int Quantity { get; set; }
	/**
	 * Required foreign key and reference navigation
	 * A cart must be associated with a customer/user
	 * A cart can be associated with one/more product(s)
	 */
	public int UserID { get; set; }
	public User? User { get; set; }
	public int BookID { get; set; }
	public Book? Book { get; set; }

	public static CartItemDTO CartItemToDTO(CartItem cartItem) => new CartItemDTO
	{
		ID = cartItem.ID,
		Quantity = cartItem.Quantity,
		BookID = cartItem.BookID,
        Book = cartItem.Book != null ? Book.BookToDTO(cartItem.Book) : null,
    };
}

