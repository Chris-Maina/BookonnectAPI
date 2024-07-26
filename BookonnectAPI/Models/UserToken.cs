namespace BookonnectAPI.Models;

public class UserToken
{
    public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string? Image { get; set; }
    public string? Token { get; set; } = String.Empty;
    public DateTime? Expires { get; set; }
}

