using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookonnectAPI.Lib;

public class TokenLibrary: ITokenLibrary
{

    private readonly JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
    private readonly BookonnectContext _context;
	public DateTime Expires { get; } = DateTime.UtcNow.AddDays(1);
	private readonly JWTOptions _jwtOptions;

    public TokenLibrary(BookonnectContext context, IOptionsSnapshot<JWTOptions> jwtOptions)
	{
		_context = context;
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

	public async Task<bool> ValidateToken(string authToken)
	{
		var validationParameters = GetTokenValidationParameters();
		SecurityToken validatedToken;
		try
		{
            tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
        }
		catch (Exception)
		{
			throw;
		}

		if (validatedToken == null)
		{
			return false;
		}

		// Check if claim has a valid user id
		var jwtToken = (JwtSecurityToken)validatedToken;
		int userId = int.Parse(jwtToken.Claims.First(claim => claim.Type == "sub").Value);
        var user = await _context.Users.FindAsync(userId);
		if (user == null)
		{
			return false;
		}

		return true;
	}

    public TokenValidationParameters GetTokenValidationParameters()
	{
		return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey))
        };
    }
}

