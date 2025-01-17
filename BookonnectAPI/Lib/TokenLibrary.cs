using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookonnectAPI.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookonnectAPI.Lib;

public class TokenLibrary: ITokenLibrary
{

    private readonly JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
	public DateTime Expires { get; } = DateTime.UtcNow.AddDays(1);
	private readonly JWTOptions _jwtOptions;

    public TokenLibrary(IOptionsSnapshot<JWTOptions> jwtOptions)
	{
		_jwtOptions = jwtOptions.Value;
	}

	public string GetToken(int userId)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var issuer = _jwtOptions.Issuer;
		var audience = _jwtOptions.Audience;
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
		};

		var token = new JwtSecurityToken(issuer, audience, claims, expires: Expires, signingCredentials: credentials);

		try
		{
            return tokenHandler.WriteToken(token);
        }
		catch (Exception)
		{
			throw;
		}	
	}
}

