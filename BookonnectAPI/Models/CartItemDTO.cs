namespace BookonnectAPI.Models;

public class CartItemDTO
{
    public int ID { get; set; }
    public int Quantity { get; set; }
	public int BookID { get; set; }
	public BookDTO? Book { get; set; }
}
