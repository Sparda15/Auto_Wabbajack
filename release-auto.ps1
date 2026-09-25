param(
    [string]$Dotnet = "dotnet",
    [string]$SevenZip = "C:\Program Files\7-Zip\7z.exe",
    [string]$ReleaseVersion = "1.0.0.0"
)
$ErrorActionPreference = "Stop"
if ($ReleaseVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "Use a four-part release version" }
$publishDir = Join-Path $PSScriptRoot "artifacts\release-$ReleaseVersion"
# Refuse reuse so old runtime files or user data cannot enter the package.
if (Test-Path -LiteralPath $publishDir) { throw "Output already exists: $publishDir" }
New-Item -ItemType Directory -Path $publishDir | Out-Null
Push-Location $PSScriptRoot
try {
    $common = @("-c", "Release", "-r", "win-x64", "--self-contained", "true",
        "-p:IncludeNativeLibrariesForSelfExtract=true", "-p:DebugType=embedded")
    & $Dotnet publish Wabbajack.App.Wpf/Wabbajack.App.Wpf.csproj @common -p:VERSION=4.2.3.0 -o "$publishDir\app"
    if ($LASTEXITCODE) { throw "App publish failed" }
    & $Dotnet publish Wabbajack.CLI/Wabbajack.CLI.csproj @common -p:VERSION=4.2.3.0 -o "$publishDir\app\cli"
    if ($LASTEXITCODE) { throw "CLI publish failed" }
    & $Dotnet publish Wabbajack.Launcher/Wabbajack.Launcher.csproj @common "-p:VERSION=$ReleaseVersion" -p:PublishSingleFile=true -o "$publishDir\launcher"
    if ($LASTEXITCODE) { throw "Launcher publish failed" }
    & $SevenZip a -tzip "$publishDir\$ReleaseVersion.zip" "$publishDir\app\*" -bso0
    if ($LASTEXITCODE) { throw "Archive creation failed" }
    & $SevenZip t "$publishDir\$ReleaseVersion.zip" -bso0
    if ($LASTEXITCODE) { throw "Archive verification failed" }
    Copy-Item -LiteralPath "$publishDir\launcher\Wabbajack.exe" -Destination "$publishDir\Wabbajack.exe"
    $hashes = @("$ReleaseVersion.zip", "Wabbajack.exe") | ForEach-Object {
        $hash = Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $publishDir $_)
        "$($hash.Hash.ToLowerInvariant())  $_"
    }
    $hashes | Set-Content -LiteralPath "$publishDir\SHA256SUMS.txt" -Encoding ascii
    Write-Host "Release files: $publishDir"
} finally { Pop-Location }
