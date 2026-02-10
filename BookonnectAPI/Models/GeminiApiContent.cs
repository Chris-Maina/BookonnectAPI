using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiContent
{

  [JsonPropertyName("parts")]
	public GeminiApiPart[]? Parts { get; set; }
}

