using System;
using System.Collections.Generic;

namespace Wabbajack;

// UI state and notification policy, independent of WebView and the wall clock.
internal sealed class NexusAutoDownloadStatus
{
    private readonly Action<string, bool, bool> _publish;
    private readonly HashSet<string> _notified = new();
    private DateTimeOffset _since;
    private bool _enabled;
    private bool _waitingDownload;
    private string? _reason;
    private string? _lastText;

    public NexusAutoDownloadStatus(Action<string, bool, bool> publish) => _publish = publish;

    public void Enable(bool enabled, DateTimeOffset now)
    {
        _enabled = enabled;
        _since = now;
        _reason = null;
        if (!enabled) _notified.Clear();
        Show(enabled ? (_waitingDownload ? "Esperando descarga" : "Esperando página") : "Descarga manual · Auto Download OFF", false);
    }

    public void Page(DateTimeOffset now)
    {
        _since = now;
        _reason = null;
        if (_enabled) Show(_waitingDownload ? "Esperando descarga" : "Esperando página", false);
    }

    public void Clicked(DateTimeOffset now)
    {
        _waitingDownload = true;
        Page(now);
    }

    public void Observe(string? reason, DateTimeOffset now)
    {
        if (!_enabled) return;
        if (reason != null)
        {
            _reason = reason;
            Attention(reason);
        }
        else
        {
            if (_reason != null) Page(now);
            Tick(now);
        }
    }

    public void Tick(DateTimeOffset now)
    {
        if (_enabled && now - _since >= TimeSpan.FromSeconds(45) && _reason == null)
            Attention("timeout");
    }

    public void DownloadStarted()
    {
        _enabled = false;
        Show("Descarga iniciada", false);
    }

    private void Attention(string reason)
    {
        var detail = reason switch
        {
            "captcha" => "resuelve el CAPTCHA en el navegador",
            "login" => "inicia sesión en Nexus",
            "navigation" => "no se pudo cargar la página",
            "error" => "la automatización no está disponible; continúa manualmente",
            "dialog" => "revisa el diálogo del navegador",
            _ => "sin inicio de descarga durante 45 s; revisa la página"
        };
        Show("Necesita intervención · " + detail, true, _notified.Add(reason));
    }

    private void Show(string text, bool attention, bool notify = false)
    {
        if (_lastText == text && !notify) return;
        _lastText = text;
        _publish(text, attention, notify);
    }
}