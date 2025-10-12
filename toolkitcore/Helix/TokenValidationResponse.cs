using Newtonsoft.Json;

namespace ToolkitCore.Helix;

public sealed class TokenValidationResponse
{
    [JsonProperty("client_id")] public string ClientId { get; set; }
    [JsonProperty("login")] public string Login { get; set; }
    [JsonProperty("scopes")] public string[] Scopes { get; set; }
    [JsonProperty("user_id")] public string UserId { get; set; }
    [JsonProperty("expires_at")] public string ExpiresAt { get; set; }
}