namespace BookonnectAPI.Models;

public class Review
{
	public int ID { get; set; }
	public string Text { get; set; } = string.Empty;
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
}

