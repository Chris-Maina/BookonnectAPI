using Newtonsoft.Json;

namespace BookonnectAPI.Models;

public class MpesaAuthToken
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = String.Empty;
    [JsonProperty("expires_in")]
    public Double ExpiresInSeconds { get; set; }
}

