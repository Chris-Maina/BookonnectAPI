using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class Order
{
	public int ID { get; set; }
	public float Total { get; set; }
    public string DeliveryLocation { get; set; } = String.Empty;
    public string? DeliveryInstructions { get; set; }
    public DateTime DateTime { get; set; }  

    /**
     * Order can have multiple OrderItems i.e. Optional collection navigation.
     * Order can have a multiple payments (dependant) i.e Optional collection navigation
     * Adding JsonIgnore attribute on OrderItems and Delivery to avoid cycles
    */
    [JsonIgnore]
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();


    /**
     * Required foreign key properties and reference navigations 
     * An order must be associated with a customer/user
     * 
     */
    public int CustomerID { get; set; }
	public User? Customer { get; set; }

    public static OrderDTO OrderToDTO(Order order) => new OrderDTO
    {
        ID = order.ID,
        Total = order.Total,
        DateTime = order.DateTime,
        DeliveryLocation = order.DeliveryLocation,
        DeliveryInstructions = order.DeliveryInstructions,
        CustomerID = order.CustomerID,
        Customer = order.Customer,
        OrderItems = order.OrderItems.Select(OrderItem.OrderItemToDTO).ToList(),
        Payments = order.Payments
    };
}

