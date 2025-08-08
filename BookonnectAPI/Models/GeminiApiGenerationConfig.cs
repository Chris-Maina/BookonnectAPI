using System;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiGenerationConfig
{
  [JsonPropertyName("temperature")]
  public double? Temperature { get; set; }
  [JsonPropertyName("response_mime_type")]
  public string? ResponseMimeType { get; set; }
  [JsonPropertyName("response_schema")]
  public GeminiApiResponseSchema? ResponseSchema { get; set; }
}
