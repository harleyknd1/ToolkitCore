using Newtonsoft.Json;

namespace ToolkitCore.Authentication;

internal sealed class DeviceCodeResponse
{
    [JsonProperty("device_code")] public string DeviceCode { get; set; }
    [JsonProperty("user_code")] public string UserCode { get; set; }
    [JsonProperty("verification_uri")] public string VerificationUri { get; set; }
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
    [JsonProperty("interval")] public int Interval { get; set; }
}