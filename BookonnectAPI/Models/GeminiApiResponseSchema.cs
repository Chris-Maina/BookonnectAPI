using System;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiResponseSchema
{
  [JsonPropertyName("type")]
  public string? Type { get; set; }
  [JsonPropertyName("items")]
  public GeminiApiResponseSchemaItem? Items { get; set; }
}
