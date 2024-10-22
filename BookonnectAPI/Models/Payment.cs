using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class Payment
{
    public string ID { get; set; } = string.Empty;// will store the MPESA transaction code
    public DateTime DateTime { get; set; }

    public int UserID { get; set; } // Required foreign key
    public User? User { get; set; } // Optional reference navigation
    public int OrderID { get; set; }
    [JsonIgnore]
    public Order? Order { get; set; } // Optional reference navigation.

    public static PaymentDTO PaymentToDTO(Payment payment)
    {
        return new PaymentDTO
        {
            ID = payment.ID,
            User = payment.User,
            DateTime = payment.DateTime,
            OrderID = payment.OrderID,
        };
    }

}

