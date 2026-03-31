<#
.SYNOPSIS
    Installs the Ampel Steuerung Stream Deck plugin.
.DESCRIPTION
    Builds the plugin, generates icons, and copies everything to the Stream Deck
    plugins directory. Restarts Stream Deck to detect the new plugin.
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipRestart
)

$ErrorActionPreference = "Stop"
$PluginId = "com.ampelsteuerung.sdPlugin"
$StreamDeckPluginsDir = "$env:APPDATA\Elgato\StreamDeck\Plugins\$PluginId"
$ProjectDir = $PSScriptRoot
$SrcDir = Join-Path $ProjectDir "src\AmpelSteuerung.StreamDeck"

Write-Host "=== Ampel Steuerung Stream Deck Plugin Installer ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build
Write-Host "[1/4] Building plugin ($Configuration)..." -ForegroundColor Yellow
Push-Location $ProjectDir
dotnet publish "$SrcDir\AmpelSteuerung.StreamDeck.csproj" -c $Configuration -o "$SrcDir\publish" --self-contained false
if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Build failed" }
Pop-Location
Write-Host "  Build succeeded." -ForegroundColor Green

# Step 2: Generate icons
Write-Host "[2/4] Generating icons..." -ForegroundColor Yellow
$ExePath = Join-Path $SrcDir "publish\AmpelSteuerung.StreamDeck.exe"
& $ExePath --generate-icons
if ($LASTEXITCODE -ne 0) { throw "Icon generation failed" }
Write-Host "  Icons generated." -ForegroundColor Green

# Step 3: Copy to Stream Deck plugins directory
Write-Host "[3/4] Installing to Stream Deck plugins directory..." -ForegroundColor Yellow
if (Test-Path $StreamDeckPluginsDir) {
    Remove-Item $StreamDeckPluginsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $StreamDeckPluginsDir -Force | Out-Null

# Copy compiled files
Copy-Item "$SrcDir\publish\*" $StreamDeckPluginsDir -Recurse -Force

# Copy plugin assets (manifest, property inspector, icons)
$SdPluginDir = Join-Path $SrcDir "sdplugin"
Copy-Item "$SdPluginDir\manifest.json" $StreamDeckPluginsDir -Force
if (Test-Path "$SdPluginDir\property-inspector") {
    Copy-Item "$SdPluginDir\property-inspector" $StreamDeckPluginsDir -Recurse -Force
}
# Icons were generated into sdplugin/imgs, copy those too
if (Test-Path "$SdPluginDir\imgs") {
    Copy-Item "$SdPluginDir\imgs" $StreamDeckPluginsDir -Recurse -Force
}

Write-Host "  Installed to: $StreamDeckPluginsDir" -ForegroundColor Green

# Step 4: Restart Stream Deck
if (-not $SkipRestart) {
    Write-Host "[4/4] Restarting Stream Deck..." -ForegroundColor Yellow
    $sdProcess = Get-Process "StreamDeck" -ErrorAction SilentlyContinue
    if ($sdProcess) {
        $sdPath = $sdProcess.Path
        Stop-Process -Name "StreamDeck" -Force
        Start-Sleep -Seconds 2
        Start-Process $sdPath
        Write-Host "  Stream Deck restarted." -ForegroundColor Green
    } else {
        Write-Host "  Stream Deck is not running. Start it manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "[4/4] Skipping Stream Deck restart (use -SkipRestart to prevent)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installation complete! Open Stream Deck and find 'Ampel Steuerung' in the action list." -ForegroundColor Cyan
