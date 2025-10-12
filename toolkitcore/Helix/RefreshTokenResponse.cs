using Newtonsoft.Json;

namespace ToolkitCore.Helix;

public sealed class RefreshTokenResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
    [JsonProperty("scope")] public string[] Scopes { get; set; }
    [JsonProperty("token_type")] public string TokenType { get; set; }
}