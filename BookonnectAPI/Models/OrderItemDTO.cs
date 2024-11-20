namespace BookonnectAPI.Models;

public class OrderItemDTO
{
	public int ID { get; set; }
	public int Quantity { get; set; }
    public int OrderID { get; set; }
    public int BookID { get; set; }
    public BookDTO? Book { get; set; }
    public ICollection<ConfirmationDTO>? Confirmations { get; set; }
}

