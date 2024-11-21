namespace BookonnectAPI.Models;

public class PaymentDTO
{
    public string ID { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public float Amount { get; set; }
    public int OrderID { get; set; }
    public int FromID { get; set; }
    public User? From { get; set; }
    public int ToID { get; set; }
    public User? To { get; set; }
}

