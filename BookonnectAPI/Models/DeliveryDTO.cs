using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class DeliveryDTO
{
    public int ID { get; set; }
    public string Location { get; set; } = String.Empty;
    public string? Instructions { get; set; }
    public ICollection<Order>? Orders { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeliveryStatus Status { get; set; }
}

