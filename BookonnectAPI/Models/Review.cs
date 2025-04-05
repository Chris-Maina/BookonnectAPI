using System.ComponentModel;

namespace BookonnectAPI.Models;

public enum ReviewStatus
{
	[Description("Like")]
	Like,
    [Description("Dislike")]
    Dislike,
    [Description("Neutral")]
    Neutral
};
public class Review
{
	public int ID { get; set; }
	public ReviewStatus Status { get; set; }
	public string? Text { get; set; }
    public DateTime DateTime { get; set; }
    /**
	 * Required foreign key and reference navigation
	 * Each review is written by only one user.
	 */
    public int UserID { get; set; }
    public User? User { get; set; }
    /**
	 * Required foreign key and reference navigation
	 * Each review is for only one book
	 */
    public int BookID { get; set; }
    public Book? Book { get; set; }

    public static ReviewDTO ReviewToDTO(Review review) =>
        new ReviewDTO
        {
            ID = review.ID,
            Status = review.Status,
            Text = review.Text,
            DateTime = review.DateTime,
            BookID = review.BookID,
            Book = review.Book != null ? Book.BookToDTO(review.Book) : null,
        };
}

