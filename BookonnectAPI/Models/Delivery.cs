namespace BookonnectAPI.Models;

public enum DeliveryStatus
{
    OrderPlaced, // user has completed checkout and an order is created
    OrderConfirmed, // owner of books has received payment and is preparing to ship
    InTransit, // package is moving between locations or on its way to the recipient
    Delivered, // customer has successfully received the package
    Delayed // External factors like weather or operational challenges have postponed the delivery
}

public class Delivery
{
	public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public string Location { get; set; } = String.Empty;
    public string Phone { get; set; } = String.Empty;
    public string? Instructions { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.OrderPlaced;

    /**
     * Delivery needs to be associated with an owner
     */
    public int UserID { get; set; } // Required foreign key
    public User? user { get; } // Optional reference navigation

    /**
     * A delivery can have multiple orders
     */
    public ICollection<Order>? Orders { get; } // Optional collection navigation.

    public static DeliveryDTO DeliveryToDTO(Delivery delivery) =>
        new DeliveryDTO
        {
            ID = delivery.ID,
            Name = delivery.Name,
            Location = delivery.Location,
            Phone = delivery.Phone,
            Status = delivery.Status,
            Instructions = delivery.Instructions
        };
}

