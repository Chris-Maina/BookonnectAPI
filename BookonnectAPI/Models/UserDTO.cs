namespace BookonnectAPI.Models;

public class UserDTO
{
    public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string? Image { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
}

