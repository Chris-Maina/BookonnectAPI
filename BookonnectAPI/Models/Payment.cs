using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public enum PaymentStatus
{
    Verified,
    Unverified
};

public class Payment
{
    [Column(TypeName = "VARCHAR(10)")]
    public string ID { get; set; } = string.Empty;// will store the MPESA transaction code
    public PaymentStatus Status { get; set; }  = PaymentStatus.Unverified;
    public DateTime DateTime { get; set; }
    public float Amount { get; set; }
    /**
     * Payment must be associated with From, To user and an Order
     */
    public int FromID { get; set; } // Tracks payment made by a customer for an order
    public User? From { get; set; }
    public int ToID { get; set; } // Tracks individual amounts paid to book owners
    public User? To { get; set; }
    public int OrderID { get; set; }
    [JsonIgnore]
    public Order? Order { get; set; } // Optional reference navigation.

    public static PaymentDTO PaymentToDTO(Payment payment)
    {
        return new PaymentDTO
        {
            ID = payment.ID,
            DateTime = payment.DateTime,
            Amount = payment.Amount,
            FromID = payment.FromID,
            From = payment.From,
            ToID = payment.ToID,
            To = payment.To,
            OrderID = payment.OrderID,
            Status = payment.Status,
        };
    }

}

