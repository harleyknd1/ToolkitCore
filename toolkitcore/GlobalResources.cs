using System.Net.Http;
using ToolkitCore.Authentication;
using ToolkitCore.Windows;
using Verse;

namespace ToolkitCore;

/// <summary>
///     Houses the global resources used by the mod. Developers should use the singletons where possible to ensure
///     long-term stability.
/// </summary>
public static class GlobalResources
{
    /// <summary>The shared HTTP client used by the mod.</summary>
    public static readonly HttpClient Client = new();

    /// <summary>The shared device code auth service used by the mod.</summary>
    /// <remarks>This shared service should be used unless you have a specific reason to use your own instance.</remarks>
    public static readonly DeviceCodeAuthService Service = DeviceCodeAuthService.CreateDefault();

    /// <summary>The shared scope registry used by the mod.</summary>
    public static readonly ScopeRegistry ScopeRegistry = new(Service, ["chat:read", "chat:edit"]);

    static GlobalResources()
    {
        ScopeRegistry.ReauthCompleted += info =>
        {
            ToolkitCoreSettings.oauth_token = info.AccessToken;
        };
    }
}