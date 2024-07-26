namespace BookonnectAPI.Models;

public class User
{
    public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string? Image { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }

    public ICollection<Book>? Books { get; } // Optional collection navigation. A user does not need to be associated with books
}
