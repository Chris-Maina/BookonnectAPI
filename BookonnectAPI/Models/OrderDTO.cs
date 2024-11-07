using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OrderDTO
{
	public int ID { get; set; }
	public float Total { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }
    public string DeliveryLocation { get; set; } = String.Empty;
    public string? DeliveryInstructions { get; set; }
    public ICollection<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
    public int CustomerID { get; set; }
    public User? Customer { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

