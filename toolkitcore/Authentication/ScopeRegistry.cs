using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using ConcurrentStringHashSet = System.Collections.Concurrent.ConcurrentDictionary<string, object>;

namespace ToolkitCore.Authentication;

/// <summary>Manages OAuth scopes and triggers reauthorization when scopes change.</summary>
[SuppressMessage(category: "ReSharper", checkId: "ChangeFieldTypeToSystemThreadingLock")]
public sealed class ScopeRegistry : IDisposable
{
    private readonly DeviceCodeAuthService _authService;
    private readonly object _lock = new();
    private readonly TimeSpan _reauthDebounceDelay;
    private readonly ConcurrentDictionary<string, ScopeRequest> _scopeRequests;
    private TokenResponse _currentToken;

    private CancellationTokenSource _debounceCts;
    private ConcurrentStringHashSet _lastAuthenticatedScopes;
    private Task _pendingReauthTask; // Used to prevent multiple reauths from being triggered at once
    private int _reauthsInProgress;

    private ScopeRegistry(DeviceCodeAuthService authService, ScopeRequest[] coreScopes = null, TimeSpan? reauthDebounceDelay = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _scopeRequests = new ConcurrentDictionary<string, ScopeRequest>();
        _lastAuthenticatedScopes = new ConcurrentStringHashSet();
        _reauthDebounceDelay = reauthDebounceDelay ?? TimeSpan.FromSeconds(5);

        if (coreScopes == null) return;

        foreach (ScopeRequest scopeRequest in coreScopes)
        {
            _scopeRequests[scopeRequest.Scope] = scopeRequest;
        }
    }

    /// <summary>Convenience constructor for core scopes without reasons.</summary>
    public ScopeRegistry(DeviceCodeAuthService authService, string[] coreScopes, TimeSpan? reauthDebounceDelay = null) : this(authService,
        coreScopes?.Select(s => new ScopeRequest(s, requestedBy: "Core", reason: null)).ToArray(), reauthDebounceDelay)
    {
    }

    /// <summary>Gets the current access token, or null if not authenticated.</summary>
    public TokenResponse CurrentToken
    {
        get => _currentToken;
        internal set => _currentToken = value;
    }

    /// <summary>Returns true if there are any outbound requests (e.g. reauth) in progress.</summary>
    public bool HasOutboundRequests => _reauthsInProgress > 0;

    /// <summary>Gets all currently registered scopes.</summary>
    public string[] RegisteredScopes => _scopeRequests.Keys.ToArray();

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }

    /// <summary>Fired when reauthorization is needed. Provides detailed information about the request.</summary>
    public event Action<ReauthInfo> ReauthRequired;

    /// <summary>Fired when reauthorization completes successfully.</summary>
    public event Action<TokenResponse> ReauthCompleted;

    /// <summary>Fired when reauthorization fails.</summary>
    public event Action<string> ReauthFailed;

    /// <summary>Gets all scope requests with their reasoning.</summary>
    public ScopeRequest[] GetScopeRequests()
    {
        return _scopeRequests.Values.ToArray();
    }

    /// <summary>Gets scope requests from a specific plugin.</summary>
    public ScopeRequest[] GetScopeRequestsBy(string pluginName)
    {
        return _scopeRequests.Values.Where(sr => sr.RequestedBy == pluginName).ToArray();
    }

    /// <summary>Performs initial authentication with core scopes.</summary>
    internal async Task InitialAuthenticateAsync(CancellationToken cancellationToken = default)
    {
        ScopeRequest[] scopeRequests = _scopeRequests.Values.ToArray();
        await PerformReauthAsync(scopeRequests, scopeRequests, cancellationToken);
    }

    /// <summary>Registers a single scope with reasoning.</summary>
    public void RegisterScope(string pluginName, string scope, string reason = null)
    {
        RegisterScopes(pluginName, (scope, reason));
    }

    /// <summary>Registers multiple scopes with optional reasoning.</summary>
    /// <param name="pluginName">Name of the plugin requesting scopes</param>
    /// <param name="scopes">Tuples of (scope, reason). Reason can be null.</param>
    public void RegisterScopes(string pluginName, params (string scope, string reason)[] scopes)
    {
        if (scopes == null || scopes.Length == 0) return;

        var scopesChanged = false;
        var newScopes = new List<ScopeRequest>();

        foreach ((string scope, string reason) in scopes)
        {
            if (string.IsNullOrWhiteSpace(scope)) continue;
            if (_scopeRequests.ContainsKey(scope)) continue;

            var scopeRequest = new ScopeRequest(scope, pluginName, reason);
            _scopeRequests.AddOrUpdate(scope, scopeRequest, (_, _) => scopeRequest);
            newScopes.Add(scopeRequest);
            scopesChanged = true;
        }

        ScopeRequest[] newScopeRequests = newScopes.ToArray();
        ScopeRequest[] allScopeRequests = _scopeRequests.Values.ToArray();

        if (!scopesChanged) return;

        Log.Message($"Plugin '{pluginName}' registered {newScopeRequests.Length} new scope(s). Total scopes: {allScopeRequests.Length}");

        foreach (ScopeRequest scopeRequest in newScopeRequests)
        {
            if (!string.IsNullOrWhiteSpace(scopeRequest.Reason)) Log.Message($"  {scopeRequest.Scope}: {scopeRequest.Reason}");
        }

        TriggerDebouncedReauth(newScopeRequests, allScopeRequests);
    }

    /// <summary>Convenience method for registering scopes without reasons.</summary>
    public void RegisterScopes(string pluginName, params string[] scopes)
    {
        if (scopes == null || scopes.Length == 0) return;

        RegisterScopes(pluginName, scopes.Select(s => (s, (string)null)).ToArray());
    }

    /// <summary>Unregisters scopes. Note: This does NOT trigger reauth (you can't remove granted permissions).</summary>
    public void UnregisterScopes(string pluginName, params string[] scopes)
    {
        if (scopes == null || scopes.Length == 0) return;

        foreach (string scope in scopes)
        {
            if (_scopeRequests.TryGetValue(scope, out ScopeRequest request) && request.RequestedBy == pluginName) _scopeRequests.Remove(scope, out _);
        }

        Log.Message($"Plugin '{pluginName}' unregistered {scopes.Length} scope(s)");
    }

    /// <summary>Checks if the current token has all registered scopes.</summary>
    public bool HasAllRequiredScopes()
    {
        if (_currentToken == null) return false;

        var grantedScopes = new HashSet<string>(_currentToken.Scopes ?? []);
        return _scopeRequests.Keys.All(scope => grantedScopes.Contains(scope));
    }

    private void TriggerDebouncedReauth(ScopeRequest[] newScopes, ScopeRequest[] allScopes)
    {
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            CancellationTokenSource cts = _debounceCts;

            _pendingReauthTask = Task.Run(async () =>
            {
                Interlocked.Increment(ref _reauthsInProgress);

                try
                {
                    await Task.Delay(_reauthDebounceDelay, cts.Token).ConfigureAwait(false);

                    if (!cts.Token.IsCancellationRequested) await PerformReauthAsync(newScopes, allScopes, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Log.Message("Reauth debounce cancelled - will restart timer");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error during debounced reauth: {ex}");
                    ReauthFailed?.Invoke($"Reauth error: {ex.Message}");
                }
                finally
                {
                    // Discard the task if it's still the same one that was triggered,
                    // thus signaling that this class can trigger another reauth.
                    Task copy = _pendingReauthTask;
                    await Interlocked.CompareExchange(ref _pendingReauthTask, value: null, copy);

                    Interlocked.Decrement(ref _reauthsInProgress);
                }
            }, cts.Token);
        }
    }

    private async Task PerformReauthAsync(ScopeRequest[] newScopes, ScopeRequest[] allScopes, CancellationToken cancellationToken)
    {
        if (_reauthsInProgress > 0)
        {
            Log.Error("Reauth already in progress - will not trigger another one");

            return;
        }

        Interlocked.Increment(ref _reauthsInProgress);
        string[] scopeStrings = allScopes.Select(sr => sr.Scope).ToArray();

        Log.Message($"Performing reauthorization with {scopeStrings.Length} scopes ({newScopes.Length} new)...");

        Result<DeviceAuthSession> sessionResult = await _authService.InitiateAuthFlowAsync(scopeStrings, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!sessionResult.Success)
        {
            ReauthFailed?.Invoke(sessionResult.Error);
            Interlocked.Decrement(ref _reauthsInProgress);

            return;
        }

        DeviceAuthSession session = sessionResult.Value;

        var reauthInfo = new ReauthInfo(session!.UserCode, session.VerificationUri, newScopes, allScopes, cancellationToken);

        ReauthRequired?.Invoke(reauthInfo);

        Result<TokenResponse> tokenResult = await _authService.PollForAuthorizationAsync(session, cancellationToken).ConfigureAwait(false);

        if (tokenResult.Success)
        {
            TokenResponse currentToken = _currentToken;
            Interlocked.CompareExchange(ref _currentToken, tokenResult.Value, currentToken);
            
            // For backwards compatibility, update the field in the settings class.
            string snapshot = ToolkitCoreSettings.oauth_token;
            Interlocked.CompareExchange(ref ToolkitCoreSettings.oauth_token, tokenResult.Value!.AccessToken, snapshot);

            ConcurrentStringHashSet lastAuthenticatedScopes = _lastAuthenticatedScopes;
            ConcurrentStringHashSet currentAuthenticatedScopes = new(scopeStrings.ToDictionary(s => s, _ => new object()));
            Interlocked.CompareExchange(ref _lastAuthenticatedScopes, currentAuthenticatedScopes, lastAuthenticatedScopes);

            Log.Message("Reauthorization successful!");
            ReauthCompleted?.Invoke(tokenResult.Value);
        }
        else
        {
            Log.Error($"Reauthorization failed: {tokenResult.Error}");
            ReauthFailed?.Invoke(tokenResult.Error);
        }

        Interlocked.Decrement(ref _reauthsInProgress);
    }
}