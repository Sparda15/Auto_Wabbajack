using System;
using System.Text.Json;
using Wabbajack.Configuration;
using Xunit;

namespace Wabbajack.App.Wpf.Test;

public class NexusAutoDownloadTests
{
    private static readonly Uri Requested = new("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290");

    [Theory]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290", true)]
    [InlineData("https://nexusmods.com/vampirebloodlines/mods/176/?tab=files&file_id=1290", true)]
    [InlineData("https://nexusmods.com.example.org/vampirebloodlines/mods/176?tab=files&file_id=1290", false)]
    [InlineData("https://www.nexusmods.com.example.com/", false)]
    [InlineData("https://evilnexusmods.com/", false)]
    [InlineData("https://example.org/nexusmods.com/", false)]
    [InlineData("https://users.nexusmods.com/oauth", false)]
    [InlineData("https://www.nexusmods.com/login", false)]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files", false)]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=0", false)]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290&file_id=1291", false)]
    [InlineData("https://user@www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290", false)]
    [InlineData("https://www.nexusmods.com:444/vampirebloodlines/mods/176?tab=files&file_id=1290", false)]
    [InlineData("http://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1290", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("file:///C:/test.html", false)]
    [InlineData("about:blank", false)]
    public void OnlyRecognizedNexusFileUrisAreEligible(string url, bool eligible) =>
        Assert.Equal(eligible, NexusAutoDownloadState.IsEligibleUri(new Uri(url)));

    [Fact]
    public void MissingAndRelativeUrisAreRejected()
    {
        Assert.False(NexusAutoDownloadState.IsEligibleUri(null));
        Assert.False(NexusAutoDownloadState.IsEligibleUri(new Uri("/mods/1", UriKind.Relative)));
    }

    [Fact]
    public void OffDoesNotAct()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, false);
        Assert.False(state.CanAct(Requested));
        Assert.False(state.TryHandle(Requested, state.Revision));
    }

    [Fact]
    public void OnActsOnceEvenAfterOffOnOrNavigation()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision));
        Assert.False(state.TryHandle(Requested, state.Revision));
        state.SetEnabled(false);
        state.SetEnabled(true);
        state.Invalidate();
        Assert.False(state.TryHandle(Requested, state.Revision));
    }

    [Fact]
    public void NextArchiveResetsState()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision));
        var next = new Uri("https://www.nexusmods.com/vampirebloodlines/mods/370?tab=files&file_id=1335");
        state.Reset(next, true);
        Assert.False(state.CanAct(Requested));
        Assert.True(state.TryHandle(next, state.Revision));
    }

    [Fact]
    public void ToggleOffAndNavigationInvalidatePendingProbes()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        var probeRevision = state.Revision;
        state.SetEnabled(false);
        Assert.False(state.TryHandle(Requested, probeRevision));
        state.SetEnabled(true);
        Assert.False(state.TryHandle(Requested, probeRevision));
        probeRevision = state.Revision;
        state.Invalidate();
        Assert.False(state.TryHandle(Requested, probeRevision));
        Assert.True(state.TryHandle(Requested, state.Revision));
    }

    [Theory]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/176?tab=files&file_id=1291")]
    [InlineData("https://www.nexusmods.com/vampirebloodlines/mods/177?tab=files&file_id=1290")]
    [InlineData("https://www.nexusmods.com/skyrim/mods/176?tab=files&file_id=1290")]
    [InlineData("https://example.org/")]
    public void OtherFilesAndSitesCannotAct(string url)
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.False(state.TryHandle(new Uri(url), state.Revision));
    }

    [Fact]
    public void ExistingSettingsDefaultToOff()
    {
        Assert.False(new MainSettings().AutoNexusDownload);
        Assert.False(JsonSerializer.Deserialize<MainSettings>("{\"CurrentSettingsVersion\":1}")!.AutoNexusDownload);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreferenceResetsBetweenSessions(bool enabled)
    {
        Assert.False(JsonSerializer.Deserialize<MainSettings>("{\"AutoNexusDownload\":true}")!.AutoNexusDownload);
        var settings = new MainSettings { AutoNexusDownload = enabled };
        Assert.False(JsonSerializer.Deserialize<MainSettings>(
            JsonSerializer.Serialize(settings))!.AutoNexusDownload);
    }

    [Fact]
    public void LargeFileAllowsOneChoiceAndOneStandardConfirmation()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision, "Slow"));
        Assert.True(state.CanAct(Requested));
        Assert.False(state.TryHandle(Requested, state.Revision, "Fast"));
        Assert.True(state.TryHandle(Requested, state.Revision, "Standard"));
        Assert.False(state.CanAct(Requested));
        Assert.False(state.TryHandle(Requested, state.Revision, "Standard"));
    }

    [Fact]
    public void AlreadyOpenDialogCanBeConfirmedWithoutAnotherChoiceClick()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision, "Standard"));
        Assert.False(state.TryHandle(Requested, state.Revision, "Slow"));
    }

    [Fact]
    public void OffStopsPendingStandardAndOnPreservesTheFirstStep()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision, "Slow"));
        var revision = state.Revision;
        state.SetEnabled(false);
        Assert.False(state.TryHandle(Requested, revision, "Standard"));
        state.SetEnabled(true);
        Assert.False(state.TryHandle(Requested, state.Revision, "Slow"));
        Assert.False(state.TryHandle(Requested, revision, "Standard"));
        Assert.True(state.TryHandle(Requested, state.Revision, "Standard"));
    }

    [Fact]
    public void DownloadStartingOrFailureTerminatesBothSteps()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        state.Complete();
        state.SetEnabled(true);
        Assert.False(state.TryHandle(Requested, state.Revision, "Slow"));
        Assert.False(state.TryHandle(Requested, state.Revision, "Standard"));
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision, "Slow"));
        Assert.True(state.TryHandle(Requested, state.Revision, "Standard"));
    }

    [Theory]
    [InlineData("Resumable")]
    [InlineData("Premium trial")]
    [InlineData("")]
    public void UnrecognizedActionsNeverConsumeOrExecuteAStep(string action)
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.False(state.TryHandle(Requested, state.Revision, action));
        Assert.False(state.ChoiceHandled);
        Assert.False(state.Handled);
    }

    [Fact]
    public void StaleOrWrongFileStandardConfirmationIsRejected()
    {
        var state = new NexusAutoDownloadState();
        state.Reset(Requested, true);
        Assert.True(state.TryHandle(Requested, state.Revision, "Slow"));
        var revision = state.Revision;
        state.Invalidate();
        Assert.False(state.TryHandle(Requested, revision, "Standard"));
        var other = new Uri(Requested.ToString().Replace("1290", "1291"));
        Assert.False(state.TryHandle(other, state.Revision, "Standard"));
    }}
