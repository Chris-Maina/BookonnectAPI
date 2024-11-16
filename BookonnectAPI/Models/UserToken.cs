namespace BookonnectAPI.Models;

public class UserToken: UserDTO
{
    public string? Token { get; set; } = String.Empty;
    public DateTime? Expires { get; set; }
}

