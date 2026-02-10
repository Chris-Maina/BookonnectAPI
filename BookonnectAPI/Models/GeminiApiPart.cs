using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiPart
{
  [JsonPropertyName("text")]
	public string? Text { get; set; }
}
