using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Wabbajack;

/// <summary>One manual request; all calls and timer events stay on the WebView UI thread.</summary>
internal sealed class NexusAutoDownload : IDisposable
{
    private readonly WebView2 _browser;
    private readonly ILogger _logger;
    private readonly CoreWebView2 _core;
    private readonly CancellationToken _token;
    private ulong _navigationId;
    private readonly DispatcherTimer _timer;
    private readonly NexusAutoDownloadState _state = new();
    private readonly string _request;
    private readonly string _script;
    private bool _active = true;
    private bool _ready;
    private bool _busy;
    private bool _reportedUnavailable;
    private bool _reportedEligible;

    public NexusAutoDownload(WebView2 browser, Uri requested, bool enabled, ILogger logger, CancellationToken token)
    {
        _browser = browser;
        _core = browser.CoreWebView2;
        _logger = logger;
        _token = token;
        _state.Reset(requested, enabled);
        _request = JsonSerializer.Serialize(new
        {
            session = Guid.NewGuid().ToString("N"),
            path = requested.AbsolutePath.TrimEnd('/'),
            game = "/" + requested.AbsolutePath.Split('/')[1],
            fileId = System.Web.HttpUtility.ParseQueryString(requested.Query)["file_id"]
        });
        using var stream = typeof(NexusAutoDownload).Assembly.GetManifestResourceStream(
            "Wabbajack.NexusAutoDownload.js") ?? throw new InvalidOperationException("Missing Nexus automation script");
        using var reader = new StreamReader(stream);
        _script = reader.ReadToEnd();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background,
            OnTick, browser.Dispatcher);
        _timer.Stop();
        _core.DownloadStarting += OnDownloadStarting;
        _browser.NavigationStarting += OnStarting;
        _browser.NavigationCompleted += OnCompleted;
        if (enabled) _timer.Start();
    }

    public void SetEnabled(bool enabled)
    {
        _state.SetEnabled(enabled);
        if (enabled && _active && !_state.Handled) _timer.Start();
        else _timer.Stop();
        // CheckAsync contains all failures; no unobserved failing task.
        _ = CheckAsync();
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs args)
    {
        _ready = false;
        // Observes only; the existing handler still owns URI capture/cancellation.
        _state.Complete();
        _timer.Stop();
    }

    private void OnStarting(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        _ready = false;
        _navigationId = args.NavigationId;
        _state.Invalidate();
    }

    private void OnCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.NavigationId != _navigationId) return;
        _ready = args.IsSuccess;
        _ = CheckAsync();
    }

    private async void OnTick(object? sender, EventArgs args) => await CheckAsync();

    private async Task CheckAsync()
    {
        if (!_active || _token.IsCancellationRequested || !_ready || _busy) return;
        try
        {
            if (!_state.CanAct(_browser.Source)) return;
            _busy = true;
            var revision = _state.Revision;
            if (!_reportedEligible)
            {
                _reportedEligible = true;
                _logger.LogInformation("Eligible Nexus download page detected");
            }
            using var probe = JsonDocument.Parse(await _browser.ExecuteScriptAsync(
                "(" + _script + ")(" + _request + ", null)"));
            if (!_active || _token.IsCancellationRequested || !_ready || revision != _state.Revision ||
                !_state.CanAct(_browser.Source)) return;

            if (!probe.RootElement.TryGetProperty("status", out var status) ||
                status.GetString() != "ready")
            {
                var probeStatus = status.ValueKind == JsonValueKind.String ? status.GetString() : null;
                if (probeStatus == "error")
                {
                    _state.Complete();
                    _timer.Stop();
                }
                if (probeStatus is not ("waiting" or "handled")) ReportUnavailable();
                return;
            }

            var document = probe.RootElement.GetProperty("document").GetString();
            var action = probe.RootElement.GetProperty("action").GetString();
            // Reserve before dispatch: uncertain script results must never cause a second click.
            if (!_state.TryHandle(_browser.Source, revision, action ?? "")) return;
            if (_state.Handled) _timer.Stop();
            _logger.LogInformation("Attempting automatic {Action} Download", action);
            using var result = JsonDocument.Parse(await _browser.ExecuteScriptAsync(
                "(" + _script + ")(" + _request + ", " + JsonSerializer.Serialize(document) + ")"));
            if (!result.RootElement.TryGetProperty("status", out var outcome) ||
                outcome.GetString() != "clicked")
            {
                _state.Complete();
                _timer.Stop();
                ReportUnavailable();
            }
        }
        catch (Exception ex)
        {
            _state.Complete();
            _timer.Stop();
            // WebView/script failures are isolated from the existing download task.
            ReportUnavailable(ex);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ReportUnavailable(Exception? exception = null)
    {
        if (_reportedUnavailable || !_active) return;
        _reportedUnavailable = true;
        _logger.LogWarning(exception, "Auto Nexus download unavailable, falling back to manual");
    }

    public void Dispose()
    {
        _active = false;
        _state.Complete();
        _timer.Stop();
        _timer.Tick -= OnTick;
        try
        {
            _core.DownloadStarting -= OnDownloadStarting;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Nexus automation browser already unavailable during cleanup");
        }
        _browser.NavigationStarting -= OnStarting;
        _browser.NavigationCompleted -= OnCompleted;
    }
}
