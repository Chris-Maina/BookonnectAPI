using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiRequest
{
  [JsonPropertyName("system_instruction")]
  public GeminiApiContent SystemInstruction { get; set; } = new GeminiApiContent();
  [JsonPropertyName("contents")]
  public GeminiApiContent[] Contents { get; set; } = Array.Empty<GeminiApiContent>();

  public GeminiApiGenerationConfig GenerationConfig { get; set; } = new GeminiApiGenerationConfig();
}
