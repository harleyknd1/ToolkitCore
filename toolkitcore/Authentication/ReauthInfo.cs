using System.Threading;

namespace ToolkitCore.Authentication;

/// <summary>Information about a pending reauthorization request.</summary>
public sealed class ReauthInfo(string userCode, string verificationUri, ScopeRequest[] newScopes, ScopeRequest[] allScopes, CancellationToken cancellationToken)
{
    public CancellationToken CancellationToken { get; } = default;
    public string UserCode { get; } = userCode;
    public string VerificationUri { get; } = verificationUri;
    public ScopeRequest[] NewScopes { get; } = newScopes ?? [];
    public ScopeRequest[] AllScopes { get; } = allScopes ?? [];
}