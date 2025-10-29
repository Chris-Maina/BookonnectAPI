using System;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Lib;

public class GeminiApiEmbeddingResponse
{
  [JsonPropertyName("embeddings")]
  public List<float>? Embeddings { get; set; }
}
