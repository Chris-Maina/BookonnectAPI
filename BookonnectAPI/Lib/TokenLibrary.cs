using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
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

	public string GetToken(int userId, bool isV2User = false)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var issuer = _jwtOptions.Issuer;
		var audience = _jwtOptions.Audience;
		var claims = new List<Claim>
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
		};
		if (isV2User)
		{
			claims.Add(new Claim(V2Authentication.IdentityStoreClaim, V2Authentication.IdentityStore));
		}

		var token = new JwtSecurityToken(issuer, audience, claims, expires: Expires, signingCredentials: credentials);

		return tokenHandler.WriteToken(token);
	}
}
