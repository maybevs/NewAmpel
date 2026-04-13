<#
.SYNOPSIS
    Deploy ampel-display Python code to the Raspberry Pi.

.DESCRIPTION
    Syncs source files, requirements, run script and service file to the Pi
    via SCP, then optionally installs dependencies and restarts the service.

.PARAMETER PiHost
    Pi hostname or IP. Default: ampel-display.local

.PARAMETER User
    SSH user on the Pi. Default: pi

.PARAMETER RemoteDir
    Target directory on the Pi. Default: /home/pi/ampel-display

.PARAMETER Venv
    Path to the Python venv on the Pi. Default: /home/pi/ampel-venv

.PARAMETER SkipDeps
    Skip pip install of requirements on the Pi.

.PARAMETER SkipRestart
    Skip restarting the systemd service after deploy.

.PARAMETER DryRun
    Show what would be done without executing.

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -PiHost 192.168.1.50 -SkipDeps
    .\deploy.ps1 -DryRun
#>

param(
    [string]$PiHost = "ampel-display.local",
    [string]$User = "pi",
    [string]$RemoteDir = "/home/pi/ampel-display",
    [string]$Venv = "/home/pi/ampel-venv",
    [switch]$SkipDeps,
    [switch]$SkipRestart,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$LocalDir = $PSScriptRoot
$Target = "${User}@${PiHost}"

function Log($msg) { Write-Host "[deploy] $msg" -ForegroundColor Cyan }
function Warn($msg) { Write-Host "[deploy] $msg" -ForegroundColor Yellow }

function Invoke-Remote([string]$cmd) {
    if ($DryRun) {
        Warn "DRY-RUN ssh: $cmd"
    } else {
        Log "ssh: $cmd"
        ssh $Target $cmd
        if ($LASTEXITCODE -ne 0) { throw "Remote command failed (exit $LASTEXITCODE): $cmd" }
    }
}

function Invoke-Scp([string]$local, [string]$remote) {
    if ($DryRun) {
        Warn "DRY-RUN scp: $local -> ${Target}:${remote}"
    } else {
        Log "scp: $local -> ${remote}"
        scp -r $local "${Target}:${remote}"
        if ($LASTEXITCODE -ne 0) { throw "SCP failed for $local" }
    }
}

# ── 1. Verify connectivity ──────────────────────────────────────────────────
Log "Testing SSH connection to ${Target}..."
if (-not $DryRun) {
    ssh -o ConnectTimeout=5 $Target "echo ok" 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot reach ${Target} via SSH. Check hostname/IP and that SSH is enabled."
    }
}
Log "Connected."

# ── 2. Stop service before deploying ────────────────────────────────────────
Log "Stopping ampel-display service (if running)..."
Invoke-Remote "sudo systemctl stop ampel-display.service 2>/dev/null || true"

# ── 3. Clean stale bytecache on Pi ──────────────────────────────────────────
Log "Cleaning __pycache__ on Pi..."
Invoke-Remote "sudo find $RemoteDir -type d -name __pycache__ -exec rm -rf {} + 2>/dev/null || true"

# ── 4. Ensure remote directories exist ─────────────────────────────────────
Log "Creating remote directory structure..."
Invoke-Remote "mkdir -p $RemoteDir/src $RemoteDir/fonts"

# ── 5. Sync files ──────────────────────────────────────────────────────────
Log "Deploying source code..."
# scp -r on Windows nests dir/ inside existing dir — delete + copy to parent to avoid
Invoke-Remote "rm -rf $RemoteDir/src/ampel_display"
Invoke-Scp "$LocalDir/src/ampel_display" "$RemoteDir/src/"

Log "Deploying config files..."
foreach ($file in @("run.sh", "setup.py", "requirements.txt", "ampel-display.service")) {
    $localPath = Join-Path $LocalDir $file
    if (Test-Path $localPath) {
        Invoke-Scp $localPath "$RemoteDir/$file"
    } else {
        Warn "Skipping $file (not found locally)"
    }
}

# Fonts directory (if BDF fonts exist)
$fontsDir = Join-Path $LocalDir "fonts"
$fontFiles = Get-ChildItem -Path $fontsDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in ".bdf", ".ttf", ".otf" }
if ($fontFiles) {
    Log "Deploying fonts ($($fontFiles.Count) files)..."
    foreach ($f in $fontFiles) {
        Invoke-Scp $f.FullName "$RemoteDir/fonts/$($f.Name)"
    }
}

# ── 6. Fix permissions ────────────────────────────────────────────────────
Log "Setting permissions..."
Invoke-Remote "chmod +x $RemoteDir/run.sh"

# ── 7. Install/update dependencies ─────────────────────────────────────────
if (-not $SkipDeps) {
    Log "Installing Python dependencies in venv..."
    Invoke-Remote "sudo bash -c 'source $Venv/bin/activate && pip install -r $RemoteDir/requirements.txt --quiet'"
} else {
    Warn "Skipping dependency install (-SkipDeps)"
}

# ── 8. Update systemd service ─────────────────────────────────────────────
Log "Installing systemd service..."
Invoke-Remote "sudo cp $RemoteDir/ampel-display.service /etc/systemd/system/ampel-display.service && sudo systemctl daemon-reload"

# ── 9. Restart service ────────────────────────────────────────────────────
if (-not $SkipRestart) {
    Log "Starting ampel-display service..."
    Invoke-Remote "sudo systemctl start ampel-display.service"
    Log "Checking service status..."
    Invoke-Remote "sudo systemctl status ampel-display.service --no-pager -l 2>&1 | head -15"
} else {
    Warn "Skipping restart (-SkipRestart). Start manually: sudo systemctl start ampel-display"
}

Log "Deploy complete!"
