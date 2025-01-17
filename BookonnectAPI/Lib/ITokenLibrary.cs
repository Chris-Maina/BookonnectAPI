using Microsoft.IdentityModel.Tokens;

namespace BookonnectAPI.Lib;

public interface ITokenLibrary
{
    public DateTime Expires { get; }
    public string? GetToken(int userId);
}

