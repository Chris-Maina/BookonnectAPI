using System;
namespace BookonnectAPI.Models;

public class PaymentOwnerBody: PaymentDTO
{
	public int OrderItemID { get; set; }
}
