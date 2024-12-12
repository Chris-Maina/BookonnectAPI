using System.ComponentModel.DataAnnotations.Schema;

namespace BookonnectAPI.Models;

public class User
{
    public int ID { get; set; }
    [Column(TypeName = "VARCHAR(255)")]
    public string Name { get; set; } = String.Empty;
    [Column(TypeName = "VARCHAR(255)")]
    public string Email { get; set; } = String.Empty;
    public string? Image { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }

    public ICollection<Book>? Books { get; } // Optional collection navigation. A user does not need to be associated with books

    public static UserDTO UserToDTO(User user)
    {
        return new UserDTO
        {
            ID = user.ID,
            Name = user.Name,
            Email = user.Email,
            Image = user.Image,
            Phone = user.Phone,
            Location = user.Location
        };
    }
}

