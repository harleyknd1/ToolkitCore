using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ToolkitCore.Helix;
using Verse;

namespace ToolkitCore.Authentication;

/// <summary>Orchestrates the device code flow for Twitch authentication.</summary>
[PublicAPI]
public sealed class DeviceCodeAuthService(HttpClient httpClient, string defaultClientId)
{
    private const string DeviceCodeEndpoint = "https://id.twitch.tv/oauth2/device";
    private const string TokenEndpoint = "https://id.twitch.tv/oauth2/token";
    private const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";
    private const string AuthorizationPendingError = "authorization_pending";
    private const string SlowDownError = "slow_down";
    private const int MillisecondsPerSecond = 1000;
    private readonly string _defaultClientId = defaultClientId ?? throw new ArgumentNullException(nameof(defaultClientId));
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>Returns a new default instance of the <see cref="DeviceCodeAuthService" /> class.</summary>
    /// <remarks>
    ///     This method defaults to using the <see cref="ToolkitCoreSettings.client_id" /> value as the default client ID,
    ///     and the global shared HTTP client.
    /// </remarks>
    public static DeviceCodeAuthService CreateDefault()
    {
        return new DeviceCodeAuthService(GlobalResources.Client, ToolkitCoreSettings.client_id);
    }

    /// <summary>Initiates the device code flow and returns the authorization session.</summary>
    public async Task<Result<DeviceAuthSession>> InitiateAuthFlowAsync(string[] scopes, [CanBeNull] string clientId = null, CancellationToken cancellationToken = default)
    {
        if (scopes == null || scopes.Length == 0) return Result.Fail<DeviceAuthSession>("Scopes cannot be null or empty");

        string effectiveClientId = clientId ?? _defaultClientId;
        FormUrlEncodedContent payload = CreateFormPayload(("client_id", effectiveClientId), ("scopes", string.Join(separator: " ", scopes)));
        
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(DeviceCodeEndpoint, payload, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string errorMessage = await ExtractErrorMessageAsync(response).ConfigureAwait(false) ?? $"HTTP {response.StatusCode}";
                Log.Error($"Failed to initiate device code flow: {errorMessage}");
                return Result.Fail<DeviceAuthSession>(errorMessage);
            }

            DeviceCodeResponse deviceCodeResponse = await DeserializeResponseAsync<DeviceCodeResponse>(response).ConfigureAwait(false);

            if (deviceCodeResponse == null) return Result.Fail<DeviceAuthSession>("Failed to deserialize response");

            var session = new DeviceAuthSession(effectiveClientId, deviceCodeResponse.DeviceCode, deviceCodeResponse.UserCode, deviceCodeResponse.VerificationUri,
                deviceCodeResponse.ExpiresIn, deviceCodeResponse.Interval, scopes);

            return Result.Ok(session);
        }
        catch (Exception ex)
        {
            Log.Error($"Exception during device code flow initiation: {ex}");
            return Result.Fail<DeviceAuthSession>($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>Polls for user authorization completion.</summary>
    public async Task<Result<TokenResponse>> PollForAuthorizationAsync(DeviceAuthSession session, CancellationToken cancellationToken = default)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));

        int intervalMs = session.PollingIntervalSeconds * MillisecondsPerSecond;

        while (!session.IsExpired)
        {
            if (cancellationToken.IsCancellationRequested) return Result.Fail<TokenResponse>("Operation canceled by the user");

            await Task.Delay(intervalMs, cancellationToken).ConfigureAwait(false);

            Result<TokenResponse> result = await PollForTokenAsync(session, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                return Result.Ok(result.Value);
            }

            switch (result.Error)
            {
                case AuthorizationPendingError:
                    continue;
                case SlowDownError:
                    intervalMs += 5 * MillisecondsPerSecond;
                    continue;
                default:
                    Log.Error($"Authorization polling failed: {result.Error}");
                    return Result.Fail<TokenResponse>(result.Error);
            }
        }

        return Result.Fail<TokenResponse>("Authorization timed out - the device code has expired");
    }

    /// <summary>Convenience method that initiates and waits for the complete auth flow.</summary>
    public async Task<Result<TokenResponse>> AuthenticateAsync(string[] scopes, [CanBeNull] string clientId = null, CancellationToken cancellationToken = default)
    {
        Result<DeviceAuthSession> sessionResult = await InitiateAuthFlowAsync(scopes, clientId, cancellationToken).ConfigureAwait(false);

        if (!sessionResult.Success) return Result.Fail<TokenResponse>(sessionResult.Error);

        return await PollForAuthorizationAsync(sessionResult.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<TokenResponse>> PollForTokenAsync(DeviceAuthSession session, CancellationToken cancellationToken)
    {
        FormUrlEncodedContent payload = CreateFormPayload(("client_id", session.ClientId), ("device_code", session.DeviceCode), ("grant_type", DeviceCodeGrantType),
            ("scopes", string.Join(separator: " ", session.Scopes)));

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(TokenEndpoint, payload, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                TokenResponse tokenResponse = await DeserializeResponseAsync<TokenResponse>(response).ConfigureAwait(false);
                return tokenResponse != null ? Result.Ok(tokenResponse) : Result.Fail<TokenResponse>("Failed to deserialize token response");
            }

            HelixErrorResponse errorResponse = await DeserializeResponseAsync<HelixErrorResponse>(response).ConfigureAwait(false);

            if (errorResponse != null) Log.Message($"Polling for token failed: {errorResponse.Message}");
            return errorResponse != null
                ? Result.Fail<TokenResponse>(errorResponse.Message, errorResponse.Message)
                : Result.Fail<TokenResponse>($"HTTP {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in PollForTokenAsync: {ex}");
            return Result.Fail<TokenResponse>($"Request failed: {ex.Message}");
        }
    }

    private static FormUrlEncodedContent CreateFormPayload(params (string Key, string Value)[] parameters)
    {
        IEnumerable<KeyValuePair<string, string>> pairs = parameters.Select(p => new KeyValuePair<string, string>(p.Key, p.Value));
        return new FormUrlEncodedContent(pairs);
    }

    private static async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response) where T : class
    {
        try
        {
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception ex)
        {
            Log.Error($"Deserialization error: {ex}");
            return null;
        }
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        HelixErrorResponse errorResponse = await DeserializeResponseAsync<HelixErrorResponse>(response).ConfigureAwait(false);
        return errorResponse?.Message;
    }
}