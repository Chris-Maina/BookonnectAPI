using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public enum ConfirmationType
{
    Dispatched,
    Received,
    Canceled
}
public class Confirmation
{
	public int ID { get; set; }
	public DateTime DateTime { get; set; }
	public ConfirmationType Type { get; set; }
    /*
	 * A confirmation must be associated with an OrderItem
	 * A confirmation must be associated/signed off with a Vendor and/or Customer
	 */
    public int OrderItemID { get; set; }
    [JsonIgnore]
    public OrderItem OrderItem { get; set; } = null!;
	public int UserID { get; set; }
	public User User { get; set; } = null!;

	public static ConfirmationDTO ConfirmationToDTO(Confirmation confirmation)
	{
		return new ConfirmationDTO
		{
			ID = confirmation.ID,
			DateTime = confirmation.DateTime,
			Type = confirmation.Type,
			UserID = confirmation.UserID,
			OrderItemID = confirmation.OrderItemID,
		};
	}
}

