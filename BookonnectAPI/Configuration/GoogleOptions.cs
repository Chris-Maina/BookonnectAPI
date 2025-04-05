namespace BookonnectAPI.Configuration;

public class GoogleOptions
{
	public const string SectionName = "Authentication:Google";
	public string ClientId { get; set;} = string.Empty;
	public string BooksApiKey { get; set; } = string.Empty;
	public string GeminiApiKey { get; set; } = string.Empty;
}

