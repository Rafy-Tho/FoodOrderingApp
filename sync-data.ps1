<#
.SYNOPSIS
    Copies the FoodExpress SQLite database between this project and the Android app.

.DESCRIPTION
    The app stores everything (accounts, menu, carts, orders) in one file:

      Windows :  <project>\Data\FoodExpressDb\FoodExpress.db3
      Android :  /data/user/0/<package>/files/FoodExpressDb/FoodExpress.db3

    A PC cannot open the phone's sandbox, so this script moves the file with
    adb. The app must be a Debug build, because adb run-as only works on a
    debuggable package.

    The app is stopped before the file is replaced and started again afterwards,
    so it always loads the new data.

.EXAMPLE
    .\sync-data.ps1 -Pull      # Android -> project (project copy is backed up)

.EXAMPLE
    .\sync-data.ps1 -Push      # project -> Android

.EXAMPLE
    .\sync-data.ps1 -Status    # show both copies and compare them
#>
[CmdletBinding(DefaultParameterSetName = 'Status')]
param(
    [Parameter(ParameterSetName = 'Pull')]
    [switch]$Pull,

    [Parameter(ParameterSetName = 'Push')]
    [switch]$Push,

    [Parameter(ParameterSetName = 'Status')]
    [switch]$Status
)

$ErrorActionPreference = 'Stop'

$Package     = 'com.companyname.foodorderingapp'
$Activity    = 'crc64cdf54960f53d0bee.MainActivity'
$RemoteDir   = 'files/FoodExpressDb'
$RemoteDb    = "$RemoteDir/FoodExpress.db3"
$ProjectRoot = $PSScriptRoot
$LocalDir    = Join-Path $ProjectRoot 'Data\FoodExpressDb'
$LocalDb     = Join-Path $LocalDir 'FoodExpress.db3'
$TempDb      = Join-Path $env:TEMP 'foodexpress-pull.db3'

function Find-Adb {
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
        'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe',
        'C:\Android\Sdk\platform-tools\adb.exe'
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw 'adb.exe not found. Install Android platform-tools or add adb to PATH.'
}

$Adb = Find-Adb

function Assert-Device {
    $state = (& $Adb get-state 2>$null)
    if ($state -ne 'device') {
        throw "No Android device is ready (state: $state). Start the emulator, then run 'adb devices'."
    }
}

function Stop-AndroidApp {
    & $Adb shell am force-stop $Package | Out-Null
    Start-Sleep -Milliseconds 800
}

function Start-AndroidApp {
    & $Adb shell am start -n "$Package/$Activity" | Out-Null
}

function Stop-WindowsApp {
    Get-Process -Name 'FoodOrderingApp' -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Get-RemoteHash {
    $line = & $Adb shell run-as $Package sha256sum $RemoteDb 2>$null
    if ($line) { return ($line -split '\s+')[0].ToLower() }
    return $null
}

# The Windows app keeps the database open while it runs, so a plain
# Get-FileHash fails. Read it through a shared handle instead.
function Get-LocalHash {
    if (-not (Test-Path $LocalDb)) { return $null }

    try {
        $stream = [System.IO.File]::Open(
            $LocalDb,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite)

        try {
            $sha = [System.Security.Cryptography.SHA256]::Create()
            try {
                return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLower()
            }
            finally { $sha.Dispose() }
        }
        finally { $stream.Dispose() }
    }
    catch {
        return $null
    }
}

function Invoke-Pull {
    Assert-Device
    Stop-AndroidApp
    Stop-WindowsApp

    Write-Host 'Pulling the Android database into the project...' -ForegroundColor Yellow

    if (Test-Path $TempDb) { Remove-Item $TempDb -Force }

    # run-as reads the app sandbox; exec-out keeps the file binary-safe.
    cmd /c "`"$Adb`" exec-out run-as $Package cat $RemoteDb > `"$TempDb`""

    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $TempDb) -or (Get-Item $TempDb).Length -eq 0) {
        Remove-Item $TempDb -Force -ErrorAction SilentlyContinue
        throw 'Could not read the app database. Is the app installed, opened once, and a Debug build?'
    }

    New-Item -ItemType Directory -Force -Path $LocalDir | Out-Null

    if (Test-Path $LocalDb) {
        Copy-Item $LocalDb "$LocalDb.bak" -Force
    }

    Move-Item $TempDb $LocalDb -Force

    Start-AndroidApp

    Write-Host 'Done. Project database updated:' -ForegroundColor Green
    Write-Host "  $LocalDb"
    if (Test-Path "$LocalDb.bak") {
        Write-Host "  (previous copy saved as FoodExpress.db3.bak)" -ForegroundColor DarkGray
    }
}

function Invoke-Push {
    Assert-Device

    if (-not (Test-Path $LocalDb)) {
        throw "Project database not found: $LocalDb"
    }

    Stop-AndroidApp

    Write-Host 'Pushing the project database to Android...' -ForegroundColor Yellow

    & $Adb push $LocalDb /data/local/tmp/FoodExpress.db3 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'adb push failed.' }

    # Copy into the sandbox as the app user, then remove any stale journal files
    # left over from the old database.
    & $Adb shell run-as $Package cp /data/local/tmp/FoodExpress.db3 $RemoteDb
    & $Adb shell run-as $Package chmod 600 $RemoteDb
    & $Adb shell run-as $Package rm -f "$RemoteDb-wal" "$RemoteDb-shm" "$RemoteDb-journal"
    & $Adb shell rm -f /data/local/tmp/FoodExpress.db3 | Out-Null

    Start-AndroidApp

    Write-Host 'Done. Android database updated:' -ForegroundColor Green
    Write-Host "  /data/user/0/$Package/$RemoteDb"
}

function Invoke-Status {
    Assert-Device

    Write-Host 'Project (Windows)' -ForegroundColor Cyan
    if (Test-Path $LocalDb) {
        $local = Get-Item $LocalDb
        '  {0,8:N0} bytes   {1:yyyy-MM-dd HH:mm:ss}' -f $local.Length, $local.LastWriteTime
        "  $LocalDb"
    }
    else {
        Write-Host "  (missing) $LocalDb" -ForegroundColor DarkGray
    }

    Write-Host ''
    Write-Host 'Android (app sandbox)' -ForegroundColor Cyan
    $remoteLine = & $Adb shell run-as $Package ls -l $RemoteDb 2>$null
    if ($remoteLine) {
        Write-Host "  $remoteLine"
    }
    else {
        Write-Host '  (missing - open the app once)' -ForegroundColor DarkGray
    }

    Write-Host ''
    Write-Host 'SHA-256' -ForegroundColor Cyan

    $localHash = Get-LocalHash
    if (-not $localHash) { $localHash = '(locked - close the Windows app)' }

    $remoteHash = Get-RemoteHash
    if (-not $remoteHash) { $remoteHash = '(missing)' }

    "  project: $localHash"
    "  android: $remoteHash"

    if ($localHash -eq $remoteHash) {
        Write-Host '  identical' -ForegroundColor Green
    }
    else {
        Write-Host '  different - use -Pull or -Push to copy one side over the other' -ForegroundColor Yellow
    }
}

switch ($PSCmdlet.ParameterSetName) {
    'Pull'  { Invoke-Pull }
    'Push'  { Invoke-Push }
    default { Invoke-Status }
}
