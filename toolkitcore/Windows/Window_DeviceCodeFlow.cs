using System.Threading;
using ToolkitCore.Authentication;
using UnityEngine;
using Verse;

namespace ToolkitCore.Windows;

internal sealed class Window_DeviceCodeFlow : Window
{
    private Page _currentPage = Page.Welcome;
    private ReauthInfo _info;
    private bool _mocking;
    private string _statusMessage = "ToolkitCore_DeviceCodeFlowInitializing".TranslateSimple();

    private Window_DeviceCodeFlow()
    {
    }

    internal CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <inheritdoc />
    public override void DoWindowContents(Rect inRect)
    {
        var contentRegion = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - UiConstants.LineHeight);
        var navigationButtonRegion = new Rect(inRect.x, inRect.height - UiConstants.LineHeight, inRect.width, UiConstants.LineHeight);

        GUI.BeginGroup(inRect);

        GUI.BeginGroup(contentRegion);

        switch (_currentPage)
        {
            case Page.Welcome:
                DrawWelcomeScreen(inRect);
                break;
            case Page.Finished:
                DrawFinishedScreen(inRect);
                break;
            case Page.WaitingRoom:
                DrawWaitingRoomScreen(inRect);
                break;
            case Page.Pairing:
                DrawPairingScreen(inRect);
                break;
            case Page.Failed:
                DrawFailedScreen(inRect);
                break;
            default:
                DrawWaywardTravelerMessage(inRect);
                break;
        }

        GUI.EndGroup();

        GUI.BeginGroup(navigationButtonRegion);
        DrawNavigationButtons(navigationButtonRegion.AtZero());
        GUI.EndGroup();

        GUI.EndGroup();
    }

    private void DrawWaitingRoomScreen(Rect region)
    {
        string initializingMessage = "ToolkitCore_DeviceCodeFlowInitializing".TranslateSimple();
        string ellipses = GenText.MarchingEllipsis();
        
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(region.TopHalf(), initializingMessage + ellipses);
        if (_statusMessage != initializingMessage) Widgets.Label(region.BottomHalf(), _statusMessage);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawWaywardTravelerMessage(Rect region)
    {
        var preambleRegion = new Rect(region.x, region.y, region.width, UiConstants.LineHeight * 2f);
        var restartRegion = new Rect(region.x, region.y + UiConstants.LineHeight * 2f, region.width, UiConstants.LineHeight * 2f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(preambleRegion, "ToolkitCore_DeviceCodeFlowWaywardTraveller".TranslateSimple());
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonText(restartRegion, "ToolkitCore_DeviceCodeFlowRestart".TranslateSimple())) _currentPage = Page.Welcome;
    }

    private static void DrawWelcomeScreen(Rect region)
    {
        var preambleRegion = new Rect(region.x, region.y, region.width, UiConstants.LineHeight * 5f);
        var noticeRegion = new Rect(region.x, region.y + UiConstants.LineHeight * 5f, region.width, UiConstants.LineHeight * 2f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(preambleRegion, "ToolkitCore_DeviceCodeFlowWelcomePreamble".TranslateSimple());

        GUI.color = ColorLibrary.RedReadable;
        Widgets.Label(noticeRegion, "ToolkitCore_DeviceCodeFlowWarning".TranslateSimple());
        GUI.color = Color.white;

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawPairingScreen(Rect region)
    {
        var preambleRegion = new Rect(region.x, region.y, region.width, UiConstants.LineHeight * 2f);
        var urlRegion = new Rect(region.x, region.y + UiConstants.LineHeight * 2f, region.width, UiConstants.LineHeight);
        var deviceCodeRegion = new Rect(region.x, region.y + UiConstants.LineHeight * 3f, region.width, UiConstants.LineHeight * 2f);
        var statusMessageRegion = new Rect(region.x, region.y + UiConstants.LineHeight * 4f, region.width, UiConstants.LineHeight * 5f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(preambleRegion, "ToolkitCore_DeviceCodeFlowNavigateToUrl".TranslateSimple());
        Widgets.Label(deviceCodeRegion, "ToolkitCore_DeviceCodeFlowCode".Translate(_info.UserCode));
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonText(urlRegion, _info.VerificationUri)) Application.OpenURL(_info.VerificationUri);
        if (string.IsNullOrWhiteSpace(_statusMessage)) return;

        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = ColorLibrary.Teal;
        Widgets.LabelEllipses(statusMessageRegion, _statusMessage);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void DrawFinishedScreen(Rect region)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(region, "ToolkitCore_DeviceCodeFlowDone".TranslateSimple());
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawFailedScreen(Rect region)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(region.TopHalf(), "ToolkitCore_DeviceCodeFlowFailed".TranslateSimple());
        if (!string.IsNullOrWhiteSpace(_statusMessage)) Widgets.Label(region.BottomHalf(), "ToolkitCore_DeviceCodeFlowErrorMessage".Translate(_statusMessage));
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawNavigationButtons(Rect region)
    {
        if (_currentPage is Page.Finished)
        {
            if (Widgets.ButtonText(region, "CloseButton".TranslateSimple())) Close();
            return;
        }

        if (_currentPage is Page.Pairing) return;

        var nextButtonRegion = new Rect(region.width - 150, y: 0, width: 150, region.height);

        if (HasNextPage())
        {
            if (Widgets.ButtonText(nextButtonRegion, "Next".TranslateSimple())) NavigateToNextPage();
            nextButtonRegion.x -= 155;
        }

        if (HasPreviousPage() && Widgets.ButtonText(nextButtonRegion, "Back".TranslateSimple())) NavigateToPreviousPage();
    }

    /// <inheritdoc />
    public override void PreClose()
    {
        base.PreClose();

        GlobalResources.ScopeRegistry.ReauthFailed -= UpdateStatusMessage;
        GlobalResources.ScopeRegistry.ReauthCompleted -= UpdateOAuthToken;

        if (!GlobalResources.ScopeRegistry.HasOutboundRequests) return;

        Log.Message("Cancelling outstanding reauth requests...");
        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();
    }

    private void UpdateStatusMessage(string message)
    {
        string current = _statusMessage;
        string statusMessage = ConvertStatusMessage(message);

        Interlocked.CompareExchange(ref _statusMessage, statusMessage, current);
        _currentPage = Page.Finished;
    }

    private void UpdateOAuthToken(TokenResponse token)
    {
        string current = ToolkitCoreSettings.oauth_token;

        // Probably unnecessary, but just in case.
        Interlocked.CompareExchange(ref ToolkitCoreSettings.oauth_token, token.AccessToken, current);
        _currentPage = Page.Finished;
    }

    private static string ConvertStatusMessage(string message)
    {
        return message switch
        {
            "authentication_pending" => "ToolkitCore_DeviceCodeFlowPending".TranslateSimple() + GenText.MarchingEllipsis(),
            "invalid device code"    => "ToolkitCore_DeviceCodeFlowInvalid".TranslateSimple(),
            _                        => string.Empty
        };
    }

    /// <summary>Creates a new instance of <see cref="Window_DeviceCodeFlow" /> with mock data.</summary>
    /// <remarks>This method is intended for testing purposes only.</remarks>
    public static Window_DeviceCodeFlow CreateMockInstance()
    {
        return new Window_DeviceCodeFlow
        {
            _mocking = true
        };
    }

    /// <summary>
    ///     Returns a new instance of <see cref="Window_DeviceCodeFlow" />, bound to the events from the global scope
    ///     registry.
    /// </summary>
    public static Window_DeviceCodeFlow CreateInstance()
    {
        var window = new Window_DeviceCodeFlow();
        GlobalResources.ScopeRegistry.ReauthCompleted += window.UpdateOAuthToken;
        GlobalResources.ScopeRegistry.ReauthRequired += info =>
        {
            ReauthInfo copy = window._info;
            Interlocked.CompareExchange(ref window._info, info, copy);
        };
        GlobalResources.ScopeRegistry.ReauthFailed += window.UpdateStatusMessage;

        return window;
    }

    private void NavigateToNextPage()
    {
        if (_mocking)
        {
            _currentPage = (Page)Mathf.Clamp((int)_currentPage + 1, min: 0, (int)Page.Finished);

            return;
        }

        _currentPage = _currentPage switch
        {
            Page.Welcome => _info == null ? Page.WaitingRoom : Page.Pairing,
            Page.Pairing => Page.Finished,
            _            => _currentPage
        };
    }

    private void NavigateToPreviousPage()
    {
        if (_mocking)
        {
            _currentPage = (Page)Mathf.Clamp((int)_currentPage - 1, min: 0, (int)Page.Finished);

            return;
        }

        _currentPage = _currentPage switch
        {
            Page.Pairing  => Page.Welcome,
            Page.Finished => Page.Pairing,
            _             => _currentPage
        };
    }

    private bool HasNextPage()
    {
        if (_mocking) return _currentPage is not Page.Finished;

        return _currentPage switch
        {
            Page.Welcome     => true,
            Page.WaitingRoom => _info != null,
            Page.Pairing     => true,
            _                => false
        };
    }

    private bool HasPreviousPage()
    {
        if (_mocking) return _currentPage is not Page.Welcome;

        return _currentPage switch
        {
            Page.Pairing  => true,
            Page.Finished => true,
            _             => false
        };
    }

    private enum Page
    {
        Welcome,
        WaitingRoom,
        Pairing,
        Failed,
        Finished
    }
}