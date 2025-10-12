using System;
using System.IO;
using Newtonsoft.Json;
using ToolkitCore.Authentication;
using Verse;

namespace ToolkitCore;

[StaticConstructorOnStartup]
internal static class AuthTokenScribe
{
    private static readonly string TokenPath = Path.Combine(CoreFilePaths.ConfigDir, path2: "auth.json");

    /// <summary>Loads the auth token from the config directory if one exists.</summary>
    public static void LoadAuthToken()
    {
        if (!File.Exists(TokenPath)) return;

        try
        {
            string response = File.ReadAllText(TokenPath);

            GlobalResources.ScopeRegistry.CurrentToken = JsonConvert.DeserializeObject<TokenResponse>(response);
            ToolkitCoreSettings.oauth_token = GlobalResources.ScopeRegistry.CurrentToken.AccessToken;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not load auth token\n\n{ex}");
        }
    }

    /// <summary>Saves the current auth token to the config directory.</summary>
    public static void SaveAuthToken()
    {
        if (GlobalResources.ScopeRegistry.CurrentToken == null)
        {
            Log.Warning("Auth token is empty, not saving");
            return;
        }

        try
        {
            File.WriteAllText(TokenPath, JsonConvert.SerializeObject(GlobalResources.ScopeRegistry.CurrentToken, Formatting.None));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save auth token\n\n{ex}");
        }
    }
}