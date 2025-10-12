using Newtonsoft.Json;

namespace ToolkitCore.Helix;

public sealed class HelixErrorResponse
{
    [JsonProperty("status")] public int Error { get; set; }
    [JsonProperty("message")] public string Message { get; set; }
}