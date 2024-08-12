namespace BookonnectAPI.Models;

public enum OrderStatus
{
    OrderPlaced, // checkout started and order registered
	OrderConfirmed, // payment made
	OrderProcessing, // order is being readied for shipment.Owner of the book is preparing to ship having received a notification
    Canceled, // Order does not go through due to reasons like stock issues or payment problems
};
public class Order
{
	public int ID { get; set; }
	public float Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.OrderPlaced;

    // Order can have multiple OrderItems
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // Optional collection navigation.

    /**
     * Required foreign key properties and reference navigations 
     * An order must be associated with a customer/user
     * An order must be associated with a delivery
     * An order must be associated with Payment
     * 
     */
    public int UserID { get; set; }
	public User User { get; set; } = null!;
    public int DeliveryID { get; set; }
    public Delivery Delivery { get; set; } = null!;
    public string PaymentID { get; set; } = String.Empty;
    public Payment? Payment { get; set; }

    public static OrderDTO OrderToDTO(Order order) => new OrderDTO
    {
        ID = order.ID,
        Total = order.Total,
        Status = order.Status,
        PaymentID = order.PaymentID,
        OrderItems = order.OrderItems.Select(OrderItem.OrderItemToDTO).ToList(),
    };
}

