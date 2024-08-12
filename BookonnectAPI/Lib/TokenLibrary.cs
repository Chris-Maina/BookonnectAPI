using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookonnectAPI.Data;
using Microsoft.IdentityModel.Tokens;

namespace BookonnectAPI.Lib;

public class TokenLibrary: ITokenLibrary
{
    const string issuerKeyIdentifier = "Authentication:JWT:Issuer";
    const string audienceKeyIdentifier = "Authentication:JWT:Audience";
    const string secretKeyIdentifier = "Authentication:JWT:SecretKey";

    private readonly JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
    private readonly IConfiguration _configuration;
    private readonly BookonnectContext _context;
	public DateTime Expires { get; } = DateTime.UtcNow.AddDays(1);

    public TokenLibrary(BookonnectContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}

	public string GetToken(int userId)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration[secretKeyIdentifier]!));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var issuer = _configuration[issuerKeyIdentifier];
		var audience = _configuration[audienceKeyIdentifier];
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
            ValidIssuer = _configuration[issuerKeyIdentifier],
            ValidAudience = _configuration[audienceKeyIdentifier],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration[secretKeyIdentifier]!))
        };
    }
}

