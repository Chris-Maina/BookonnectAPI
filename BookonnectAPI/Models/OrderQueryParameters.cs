namespace BookonnectAPI.Models;

public class OrderQueryParameters: QueryParameter
{
    public float? Total { get; set; }
	public OrderStatus? Status { get; set; }
}

