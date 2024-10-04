namespace BookonnectAPI.Models;

public class PaymentDTO
{
    public string ID { get; set; } = string.Empty;
    public User? User { get; set; }
    public DateTime DateTime { get; set; }
    public int OrderID { get; set; }
}

