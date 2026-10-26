<#
.SYNOPSIS
  Builds SoundSwap for Windows x64 (self-contained + ReadyToRun) and, optionally, the installer and a portable zip.
.EXAMPLE
  .\scripts\publish.ps1                                  # artifacts\publish
  .\scripts\publish.ps1 -Version 1.2.0 -Installer -Portable
.NOTES
  -Installer needs Inno Setup 6 (https://jrsoftware.org/isinfo.php, or: choco install innosetup).
  Self-contained means users do not need to install .NET; ReadyToRun makes startup faster (useful at sign-in).
#>
param(
    [string]$Version = "",
    [switch]$Installer,
    [switch]$Portable
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\publish"
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

$publishArgs = @("publish", (Join-Path $root "src\SoundSwap.App\SoundSwap.App.csproj"),
    "-c", "Release", "-r", "win-x64", "--self-contained", "true", "-p:PublishReadyToRun=true", "-o", $out)
if ($Version) { $publishArgs += "-p:Version=$Version" }
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$v = if ($Version) { $Version } else { "0.0.0" }

if ($Installer) {
    # Inno Setup can be installed for all users (Program Files) or for the current user (winget may do that).
    $iscc = @(
        (Get-Command iscc -ErrorAction SilentlyContinue).Source,
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $iscc) { throw "Inno Setup 6 not found. Install it (winget install JRSoftware.InnoSetup), or run without -Installer." }
    $env:SOUNDSWAP_VERSION = $v
    & $iscc (Join-Path $root "installer\SoundSwap.iss")
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
}

if ($Portable) {
    $zip = Join-Path $root "artifacts\SoundSwap-$v-win-x64-portable.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip
}
Write-Host "Done. Output in $($root)\artifacts"
