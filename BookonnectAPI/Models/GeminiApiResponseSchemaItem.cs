using System;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiResponseSchemaItem
{
  [JsonPropertyName("type")]
  public string? Type { get; set; }
  [JsonPropertyName("properties")]
  public object? Properties { get; set; } // Use object to allow for flexible property types
  [JsonPropertyName("propertyOrdering")]
  public string[]? PropertyOrdering { get; set; }
}
