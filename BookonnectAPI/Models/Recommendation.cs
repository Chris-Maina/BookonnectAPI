using BookonnectAPI.DTO;

namespace BookonnectAPI.Models;

public class Recommendation
{
	public int ID { get; set; }
	public int UserID { get; set; }
    public User? User { get; set; }
    /**
    * Required foreign key and optional reference navigation
    * Each recommendation belongs to a book
    */
    public int BookID { get; set; }
    public Book? Book { get; set; }

    public static RecommendationDTO RecommendationToDTO(Recommendation recommendation) =>
        new RecommendationDTO
        {
            ID = recommendation.ID,
            BookID = recommendation.BookID,
            Book = recommendation.Book != null ? Book.BookToDTO(recommendation.Book) : null
        };
}

