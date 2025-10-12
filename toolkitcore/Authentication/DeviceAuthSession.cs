using System;

namespace ToolkitCore.Authentication;

/// <summary>Represents an active device authorization session.</summary>
public sealed class DeviceAuthSession(
    string clientId,
    string deviceCode,
    string userCode,
    string verificationUri,
    int expiresInSeconds,
    int pollingIntervalSeconds,
    string[] scopes
)
{
    public string ClientId { get; } = clientId ?? throw new ArgumentNullException(nameof(clientId));
    public string DeviceCode { get; } = deviceCode ?? throw new ArgumentNullException(nameof(deviceCode));
    public string UserCode { get; } = userCode ?? throw new ArgumentNullException(nameof(userCode));
    public string VerificationUri { get; } = verificationUri ?? throw new ArgumentNullException(nameof(verificationUri));
    public int ExpiresInSeconds { get; } = expiresInSeconds;
    public int PollingIntervalSeconds { get; } = pollingIntervalSeconds;
    public string[] Scopes { get; } = scopes ?? throw new ArgumentNullException(nameof(scopes));
    public DateTime ExpiresAt { get; } = DateTime.UtcNow.AddSeconds(expiresInSeconds);

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}