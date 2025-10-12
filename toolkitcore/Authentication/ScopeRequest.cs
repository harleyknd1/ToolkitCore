using System;

namespace ToolkitCore.Authentication;

/// <summary>Represents a scope with optional reasoning for why it's necessary.</summary>
public sealed class ScopeRequest(string scope, string requestedBy, string reason = null)
{
    public string Scope { get; } = scope ?? throw new ArgumentNullException(nameof(scope));
    public string RequestedBy { get; } = requestedBy ?? throw new ArgumentNullException(nameof(requestedBy));
    public string Reason { get; } = reason;

    public override bool Equals(object obj)
    {
        return obj is ScopeRequest other && Scope == other.Scope;
    }

    public override int GetHashCode()
    {
        return Scope.GetHashCode();
    }
}