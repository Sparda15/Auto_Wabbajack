# Auto Wabbajack 1.0

Fork based on Wabbajack 4.2.3.0. Release tag: `1.0.0.0`.
The app/CLI retain version 4.2.3.0 for modlist compatibility. The fork launcher uses 1.0.0.0.

## Installation

Download **Wabbajack.exe** from this repository's release and place it in a new, separate folder.
As with upstream, this is a self-contained Windows x64 launcher. It downloads **1.0.0.0.zip**
and extracts the app and CLI into a version folder. For offline installation, extract that ZIP
into a folder named 1.0.0.0 beside the launcher. Do not overwrite an official installation.
These fork executables are unsigned; they do not carry upstream's signing certificate.

Both launcher and launcher updater use Sparda15/Auto_Wabbajack releases. The launcher does
not use official Nexus release files. The original modlist catalog and download pipeline are unchanged.

## Auto Download

- Starts OFF on every application launch, including when an older settings file saved ON.
- Once enabled, remains enabled between files for the current session.
- Clicks the identified Slow download button, or the exact Fast download button for a Premium account.
- Selects Standard download in the recognized large-file dialog; never selects Resumable.
- Does not bypass login, CAPTCHA, download countdowns, or account restrictions.

The browser displays **Esperando página**, **Esperando descarga**, or **Necesita intervención**.
CAPTCHA, login, navigation errors and unknown dialogs show a yellow notice and play one system
alert per reason per activation. OFF disables monitoring and alerts. A 45-second wait without
a download start raises an advisory notice; it is not proof of a network failure and does not retry
a click. The notice clears when the detected blocker disappears. Once Wabbajack takes the download,
its normal progress display handles transfer progress.

DOM detection relies on the known Nexus English markup and open component ShadowRoot.
Unrecognized layouts remain manual. It cannot diagnose every possible CAPTCHA or browser failure.

## Build and checks

Use the .NET 10 SDK, Node.js with jsdom 26.1.0 available, and 7-Zip on Windows.

```powershell
dotnet test Wabbajack.App.Wpf.Test/Wabbajack.App.Wpf.Test.csproj -c Release -p:VERSION=4.2.3.0
node --test Wabbajack.App.Wpf.Test/NexusAutoDownloadScript.test.cjs
./release-auto.ps1
```

The release script mirrors upstream's self-contained app, CLI and single-file launcher packaging.
It creates the full ZIP, launcher and SHA256SUMS.txt in a fresh artifacts directory.
It does not sign, publish, upload to Nexus, or post to Discord.

Manual validation: start OFF, enable and download a small and a >500MB file, check Standard
confirmation, test OFF, close/reopen, and check login/CAPTCHA/wait notices when encountered.
Automated tests cover click guards, DOM diagnosis, alert deduplication, recovery, timeout and OFF.
