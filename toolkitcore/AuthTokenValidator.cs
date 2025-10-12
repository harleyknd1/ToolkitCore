using System.Threading.Tasks;
using ToolkitCore.Authentication;
using ToolkitCore.Helix;
using Verse;

namespace ToolkitCore;

/// <summary>Loads the auth token from the file and validates it. If it's invalid, it will attempt to refresh it.</summary>
internal static class AuthTokenValidator
{
    internal static async Task ValidateTokenAsync()
    {
        Result<TokenValidationResponse> response = await HelixApi.ValidateTokenAsync(GlobalResources.ScopeRegistry.CurrentToken.AccessToken);

        if (!response.Success)
        {
            Log.Error("Token isn't valid; refreshing...");
            await ExchangeToken();
        }
        else
        {
            Log.Message("Token validation succeeded");
            
            ToolkitCoreSettings.bot_username = response.Value!.Login;
        }
    }

    private static async Task ExchangeToken()
    {
        if (string.IsNullOrWhiteSpace(ToolkitCoreSettings.client_id))
        {
            Log.Error("Client ID is not set");
            return;
        }

        Result<RefreshTokenResponse> response = await HelixApi.RefreshTokenAsync(GlobalResources.ScopeRegistry.CurrentToken.RefreshToken);

        if (response.Failure)
        {
            Log.Error("Failed to refresh token; reauthenticate through the ToolkitCore settings menu");

            return;
        }

        // Since exchanging the refresh token doesn't return a new expiration time, we'll just set it to the old one.
        // Generally, you're not supposed to rely on the expiration time since it's not guaranteed to be accurate, so
        // the actual value doesn't matter.
        GlobalResources.ScopeRegistry.CurrentToken = new TokenResponse
        {
            AccessToken = response.Value!.AccessToken,
            RefreshToken = response.Value.RefreshToken,
            Scopes = response.Value.Scopes,
            TokenType = response.Value.TokenType,
            ExpiresIn = GlobalResources.ScopeRegistry.CurrentToken.ExpiresIn
        };
    }
}