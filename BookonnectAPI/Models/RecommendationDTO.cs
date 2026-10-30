namespace BookonnectAPI.Models;

public class RecommendationDTO
{
	public int ID { get; set; }
	public int BookID { get; set; }
	public BookDTO? Book { get; set; } 
}

