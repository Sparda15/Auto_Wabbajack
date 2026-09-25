using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Threading;
using System.Windows.Input;
using Wabbajack.Configuration;

namespace Wabbajack;

public partial class ManualDownloadHandler
{
    private MainSettings _autoSettings = null!;
    private ILogger<ManualDownloadHandler> _autoLogger = null!;
    private NexusAutoDownload? _automation;

    [Reactive] public partial bool AutoDownloadAvailable { get; private set; }
    [Reactive] public partial bool AutoDownloadEnabled { get; private set; }
    [Reactive] public partial string AutoDownloadStatusText { get; private set; } = "Descarga manual · Auto Download OFF";
    [Reactive] public partial bool AutoDownloadNeedsAttention { get; private set; }
    public ICommand ToggleAutoDownloadCommand { get; private set; } = null!;

    private void InitializeAutoDownload(IServiceProvider provider)
    {
        _autoSettings = provider.GetRequiredService<MainSettings>();
        _autoLogger = provider.GetRequiredService<ILogger<ManualDownloadHandler>>();
        AutoDownloadEnabled = _autoSettings.AutoNexusDownload;
        ToggleAutoDownloadCommand = ReactiveCommand.Create(() =>
        {
            AutoDownloadEnabled = !AutoDownloadEnabled;
            _autoSettings.AutoNexusDownload = AutoDownloadEnabled;
            _automation?.SetEnabled(AutoDownloadEnabled);
            _autoLogger.LogInformation("Auto Nexus download {State}", AutoDownloadEnabled ? "enabled" : "disabled");
        });
    }

    private void StartAutoDownload(Uri requested, CancellationToken token)
    {
        if (!AutoDownloadAvailable) return;
        try
        {
            _automation = new NexusAutoDownload(Browser, requested, AutoDownloadEnabled, _autoLogger, token,
                (text, attention, notify) =>
                {
                    AutoDownloadStatusText = text;
                    AutoDownloadNeedsAttention = attention;
                    if (notify)
                    {
                        _autoLogger.LogWarning("Auto Nexus: {Status}", text);
                        try { System.Media.SystemSounds.Exclamation.Play(); }
                        catch (Exception ex) { _autoLogger.LogDebug(ex, "Could not play download alert"); }
                    }
                });
        }
        catch (Exception ex)
        {
            AutoDownloadStatusText = "Necesita intervención · continúa manualmente";
            AutoDownloadNeedsAttention = true;
            _autoLogger.LogWarning(ex, "Auto Nexus download unavailable, falling back to manual");
        }
    }
}
