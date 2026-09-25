using System;
using System.Collections.Generic;
using Xunit;

namespace Wabbajack.App.Wpf.Test;

public class NexusAutoDownloadStatusTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch;

    [Fact]
    public void OffNeverNotifiesAndClearsAttention()
    {
        var events = new List<(string Text, bool Attention, bool Notify)>();
        var status = new NexusAutoDownloadStatus((t, a, n) => events.Add((t, a, n)));
        status.Enable(false, Start);
        status.Observe("captcha", Start);
        status.Tick(Start.AddMinutes(2));
        Assert.Single(events);
        Assert.False(events[0].Attention);
        Assert.False(events[0].Notify);
    }

    [Fact]
    public void TimeoutIsOnlyAnAlertAndFiresOnce()
    {
        var events = new List<(string Text, bool Attention, bool Notify)>();
        var status = new NexusAutoDownloadStatus((t, a, n) => events.Add((t, a, n)));
        status.Enable(true, Start);
        status.Tick(Start.AddSeconds(44));
        Assert.Single(events);
        status.Tick(Start.AddSeconds(45));
        status.Tick(Start.AddSeconds(90));
        Assert.Equal(2, events.Count);
        Assert.True(events[1].Notify);
        Assert.Contains("45 s", events[1].Text);
    }

    [Theory]
    [InlineData("captcha", "CAPTCHA")]
    [InlineData("login", "inicia sesión")]
    [InlineData("navigation", "cargar")]
    [InlineData("dialog", "diálogo")]
    public void BlockerNotifiesOnceAndRecoveryClearsBanner(string reason, string expected)
    {
        var events = new List<(string Text, bool Attention, bool Notify)>();
        var status = new NexusAutoDownloadStatus((t, a, n) => events.Add((t, a, n)));
        status.Enable(true, Start);
        status.Observe(reason, Start);
        status.Observe(reason, Start.AddSeconds(1));
        Assert.Equal(2, events.Count);
        Assert.Contains(expected, events[1].Text);
        Assert.True(events[1].Notify);
        status.Observe(null, Start.AddSeconds(2));
        Assert.False(events[^1].Attention);
        status.Observe(reason, Start.AddSeconds(3));
        Assert.False(events[^1].Notify);
    }

    [Fact]
    public void DownloadWaitResetsClockAndCompletionStopsAlerts()
    {
        var events = new List<(string Text, bool Attention, bool Notify)>();
        var status = new NexusAutoDownloadStatus((t, a, n) => events.Add((t, a, n)));
        status.Enable(true, Start);
        status.Clicked(Start.AddSeconds(30));
        status.Tick(Start.AddSeconds(50));
        Assert.Equal("Esperando descarga", events[^1].Text);
        status.DownloadStarted();
        status.Tick(Start.AddMinutes(3));
        Assert.Equal("Descarga iniciada", events[^1].Text);
        Assert.DoesNotContain(events, e => e.Notify);
    }
}
