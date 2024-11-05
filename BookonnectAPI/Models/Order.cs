using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public enum OrderStatus
{
    OrderPlaced, // checkout started and order registered
	OrderConfirmed, // payment made
	OrderShipped, // Owner of the book has shipped/sent the book
    OrderCompleted, // Recipient has received the book and transaction completed successful
    Canceled, // Order does not go through due to reasons like stock issues or payment problems
};
public class Order
{
	public int ID { get; set; }
	public float Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.OrderPlaced;

    /**
     * Order can have multiple OrderItems i.e. Optional collection navigation.
     * Order can have a delivery (principal) i.e. Optional reference navigation
     * Order can have a multiple payments (dependant) i.e Optional collection navigation
     * Adding JsonIgnore attribute on OrderItems and Delivery to avoid cycles
    */
    [JsonIgnore]
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public int? DeliveryID { get; set; }
    [JsonIgnore]
    public Delivery? Delivery { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();


    /**
     * Required foreign key properties and reference navigations 
     * An order must be associated with a customer/user
     * 
     */
    public int UserID { get; set; }
	public User User { get; set; } = null!;

    public static OrderDTO OrderToDTO(Order order) => new OrderDTO
    {
        ID = order.ID,
        Total = order.Total,
        Status = order.Status,
        UserID = order.UserID,
        User = order.User,
        Delivery = order.Delivery ?? null,
        OrderItems = order.OrderItems.Select(OrderItem.OrderItemToDTO).ToList(),
        Payments = order.Payments
    };
}

