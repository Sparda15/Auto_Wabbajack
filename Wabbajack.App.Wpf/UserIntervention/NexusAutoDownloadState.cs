using System;
using System.Web;
using System.Text.RegularExpressions;

namespace Wabbajack;

internal sealed class NexusAutoDownloadState
{
    private Uri? _requested;
    public bool Enabled { get; private set; }
    public bool Handled { get; private set; }
    public bool ChoiceHandled { get; private set; }
    public int Revision { get; private set; }

    public void Reset(Uri requested, bool enabled)
    {
        _requested = requested;
        Enabled = enabled;
        Handled = false;
        ChoiceHandled = false;
        Revision++;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Revision++;
    }

    public void Invalidate() => Revision++;

    public bool CanAct(Uri? current) => Enabled && !Handled && MatchesRequest(current);

    public bool MatchesRequest(Uri? current) =>
        IsEligibleUri(current) && IsEligibleUri(_requested) &&
        current!.AbsolutePath.TrimEnd('/') == _requested!.AbsolutePath.TrimEnd('/') &&
        HttpUtility.ParseQueryString(current.Query)["file_id"] ==
        HttpUtility.ParseQueryString(_requested.Query)["file_id"];

    public bool TryHandle(Uri? current, int revision, string action = "Slow")
    {
        if (revision != Revision || !CanAct(current)) return false;
        if (action == "Standard")
        {
            ChoiceHandled = true;
            Handled = true;
        }
        else if (action is "Slow" or "Fast" && !ChoiceHandled)
        {
            ChoiceHandled = true;
        }
        else return false;
        return true;
    }

    public void Complete()
    {
        Handled = true;
        Revision++;
    }

    public static bool IsEligibleUri(Uri? uri)
    {
        if (uri is not { IsAbsoluteUri: true } || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || uri.UserInfo.Length != 0 ||
            (uri.Host != "www.nexusmods.com" && uri.Host != "nexusmods.com"))
            return false;

        if (!Regex.IsMatch(uri.AbsolutePath, @"^/[a-z0-9_-]+/mods/[1-9][0-9]*/?$",
                RegexOptions.CultureInvariant))
            return false;

        var query = HttpUtility.ParseQueryString(uri.Query);
        return query["tab"] == "files" &&
               long.TryParse(query["file_id"], out var fileId) && fileId > 0;
    }
}
