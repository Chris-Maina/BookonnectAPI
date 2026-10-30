using BookonnectAPI.Models;
using User = BookonnectCore.Models.User;

namespace BookonnectAPI.Mappings;

public static class UserMapping
{
    public static UserDTO ToDTO(this User user) => new()
    {
        ID = user.ID,
        Name = user.Name,
        Email = user.Email,
        Image = user.Image,
        Phone = user.Phone,
        Location = user.Location
    };

    public static UserToken ToTokenDTO(this User user, string token, DateTime expires) => new()
    {
        ID = user.ID,
        Name = user.Name,
        Email = user.Email,
        Image = user.Image,
        Phone = user.Phone,
        Location = user.Location,
        Token = token,
        Expires = expires
    };
}
