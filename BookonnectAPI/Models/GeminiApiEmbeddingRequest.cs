using System;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class GeminiApiEmbeddingRequest
{

  [JsonPropertyName("content")]
  public GeminiApiContent? Content { get; set; }
}
