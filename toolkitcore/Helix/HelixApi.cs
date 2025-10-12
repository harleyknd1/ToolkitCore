using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ToolkitCore.Helix;

public static class HelixApi
{
    /// <summary>Validates a Twitch OAuth token.</summary>
    /// <param name="token">The Twitch OAuth token to validate.</param>
    /// <param name="cancellationToken">The cancellation token to use for the request.</param>
    /// <returns>A result containing the token validation response, indicating the success or failure of token validation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the token is null or empty.</exception>
    public static async Task<Result<TokenValidationResponse>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

        using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri: "https://id.twitch.tv/oauth2/validate"))
        {
            request.Headers.Add(name: "Authorization", $"OAuth {token}");

            HttpResponseMessage response = await GlobalResources.Client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: return Result.Fail<TokenValidationResponse>("Token is invalid or expired");
                case HttpStatusCode.OK:
                {
                    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Result.Ok(JsonConvert.DeserializeObject<TokenValidationResponse>(content));
                }
                default: return Result.Fail<TokenValidationResponse>("Failed to validate oauth token");
            }
        }
    }

    /// <summary>Refreshes a Twitch OAuth token using the provided refresh token.</summary>
    /// <param name="refreshToken">The refresh token to use for generating a new access token.</param>
    /// <param name="cancellationToken">The cancellation token to use for the request.</param>
    /// <returns>
    ///     A result containing the refreshed token response, indicating the success or failure of the token refresh
    ///     operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the refresh token is null or empty.</exception>
    public static async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken)) throw new ArgumentNullException(nameof(refreshToken));

        var formContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>(key: "client_id", ToolkitCoreSettings.client_id), new KeyValuePair<string, string>(key: "grant_type", value: "refresh_token"),
            new KeyValuePair<string, string>(key: "refresh_token", refreshToken)
        ]);

        HttpResponseMessage response = await GlobalResources.Client.PostAsync(requestUri: "https://id.twitch.tv/oauth2/token", formContent, cancellationToken);

        if (!response.IsSuccessStatusCode) return Result.Fail<RefreshTokenResponse>("Invalid refresh token");
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return Result.Ok(JsonConvert.DeserializeObject<RefreshTokenResponse>(content));
    }
}