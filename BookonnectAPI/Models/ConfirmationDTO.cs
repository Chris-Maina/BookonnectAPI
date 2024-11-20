namespace BookonnectAPI.Models;

public class ConfirmationDTO
{
    public int ID { get; set; }
    public DateTime DateTime { get; set; }
    public ConfirmationType Type { get; set; }
    public int OrderItemID { get; set; }
    public int? UserID { get; set; }
}
