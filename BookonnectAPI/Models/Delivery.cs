namespace BookonnectAPI.Models;

public enum DeliveryStatus
{
    OrderPlaced, // delivery created for an order
    OrderConfirmed, // user has completed checkout, owner of books has received payment and is preparing to ship
    InTransit, // package is moving between locations or on its way to the recipient
    Delivered, // customer has successfully received the package
    Delayed // External factors like weather or operational challenges have postponed the delivery
}

public class Delivery
{
	public int ID { get; set; }
    public string Location { get; set; } = String.Empty;
    public string? Instructions { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.OrderPlaced;

    /**
     * Delivery needs to be associated with an owner
     * A delivery can have multiple orders
     */
    public int UserID { get; set; } // Required foreign key
    public User? User { get; set; } // Optional reference navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>(); // Collection navigation.

    public static DeliveryDTO DeliveryToDTO(Delivery delivery) =>
        new DeliveryDTO
        {
            ID = delivery.ID,
            Location = delivery.Location,
            Status = delivery.Status,
            Instructions = delivery.Instructions
        };
}
