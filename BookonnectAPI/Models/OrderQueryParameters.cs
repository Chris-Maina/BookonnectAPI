namespace BookonnectAPI.Models;

public class OrderQueryParameters: QueryParameter
{
    public float? Total { get; set; }
    public int[]? BookID { get; set; }
}

