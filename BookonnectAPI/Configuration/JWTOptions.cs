namespace BookonnectAPI.Configuration;

public class JWTOptions
{
    public const string SectionName = "Authentication:JWT";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
