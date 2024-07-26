using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class DeliveryDTO
{
    public string Name { get; set; } = String.Empty;
    public string Location { get; set; } = String.Empty;
    public string Phone { get; set; } = String.Empty;
    public string? Instructions { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeliveryStatus Status { get; set; }
    public int OrderID { get; set; }
}

