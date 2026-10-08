# Copy the built mod into a BepInEx plugins folder.
#
#   ./deploy.ps1 -PluginsDir "D:\Steam\steamapps\common\Valheim\BepInEx\plugins"
#
# The target has to be named on purpose: this will not guess at your live game folder while other
# mods (and stale duplicates) are sitting in it.
param(
    [Parameter(Mandatory = $true)][string]$PluginsDir,
    [string]$Configuration = "Release"   # ship what you publish; pass -Configuration Debug for a local ear-test
)

$ErrorActionPreference = "Stop"

$src = Join-Path $PSScriptRoot "bin\$Configuration\net472\FishCues.dll"
if (-not (Test-Path $src)) {
    throw "Not built yet - run 'dotnet build -c $Configuration' first (missing $src)."
}
if (-not (Test-Path $PluginsDir)) {
    throw "No such folder: $PluginsDir"
}
if (-not (Test-Path (Join-Path (Split-Path $PluginsDir -Parent) "core\BepInEx.dll"))) {
    throw "$PluginsDir does not look like a BepInEx plugins folder (no ..\core\BepInEx.dll)."
}

# Only the DLL. The other files in bin\ are build tooling, not runtime dependencies.
Copy-Item $src -Destination $PluginsDir -Force
Write-Host "Installed $((Get-Item $src).Length) bytes -> $(Join-Path $PluginsDir 'FishCues.dll')"
Write-Host "Start the game; the config appears as online.buddycloud.fishcues.cfg next time."
