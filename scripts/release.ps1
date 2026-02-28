<#
.SYNOPSIS
    Full release pipeline: test → publish → portable ZIP → installer.

.DESCRIPTION
    1. Runs dotnet test  — aborts on any failure.
    2. Reads <Version>  from ClipboardManager.csproj.
    3. Runs dotnet publish → publish\win-x64\ClipboardManager.exe (self-contained, single file).
    4. Creates         release\ClipboardManager-portable-v{VERSION}.zip
    5. (Optional) Compiles installer if Inno Setup (iscc.exe) is on PATH.
    6. Lists final artifacts in release\.

.PARAMETER SkipTests
    Skip the dotnet test step (useful for iterating on packaging).

.PARAMETER SkipInstaller
    Skip the Inno Setup compilation step.

.EXAMPLE
    .\scripts\release.ps1
    .\scripts\release.ps1 -SkipTests
    .\scripts\release.ps1 -SkipInstaller
#>

param(
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'

# ── Paths ─────────────────────────────────────────────────────────────────────
$root       = Split-Path $PSScriptRoot -Parent
$csproj     = Join-Path $root 'src\ClipboardManager\ClipboardManager.csproj'
$solution   = Join-Path $root 'ClipboardManager.sln'
$publishDir = Join-Path $root 'publish\win-x64'
$releaseDir = Join-Path $root 'release'
$issFile    = Join-Path $root 'installer\ClipboardManager.iss'

# ── Helper ────────────────────────────────────────────────────────────────────
function Step([string]$msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function OK  ([string]$msg) { Write-Host "[OK] $msg"    -ForegroundColor Green }
function Fail([string]$msg) { Write-Host "[FAIL] $msg"  -ForegroundColor Red; exit 1 }

# ── 0. Read version ───────────────────────────────────────────────────────────
[xml]$proj  = Get-Content $csproj
$version    = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { Fail "Could not read <Version> from $csproj" }
Write-Host "Release version: v$version"

# ── 1. Tests ──────────────────────────────────────────────────────────────────
if (-not $SkipTests) {
    Step "Running tests"
    dotnet test $solution --configuration Release --no-restore --logger "console;verbosity=minimal"
    if ($LASTEXITCODE -ne 0) { Fail "Tests failed — aborting release." }
    OK "All tests passed."
} else {
    Write-Host "  (tests skipped)"
}

# ── 2. Publish ────────────────────────────────────────────────────────────────
Step "Publishing win-x64 self-contained single file"
dotnet publish $csproj `
    --configuration Release `
    /p:PublishProfile=win-x64 `
    --nologo

if ($LASTEXITCODE -ne 0) { Fail "dotnet publish failed." }

$exe = Join-Path $publishDir 'ClipboardManager.exe'
if (-not (Test-Path $exe)) { Fail "EXE not found at '$exe' after publish." }
$exeMB = [math]::Round((Get-Item $exe).Length / 1MB, 2)
OK "Published: $exe ($exeMB MB)"

# ── 3. Portable ZIP ───────────────────────────────────────────────────────────
Step "Creating portable ZIP"
& (Join-Path $PSScriptRoot 'build-portable.ps1')
if ($LASTEXITCODE -ne 0) { Fail "build-portable.ps1 failed." }

# ── 4. Inno Setup installer ───────────────────────────────────────────────────
if (-not $SkipInstaller) {
    Step "Compiling Inno Setup installer"
    $iscc = Get-Command 'iscc' -ErrorAction SilentlyContinue
    if (-not $iscc) {
        # Try a common install path
        $iscc = Get-Item 'C:\Program Files (x86)\Inno Setup 6\iscc.exe' -ErrorAction SilentlyContinue
    }

    if (-not $iscc) {
        Write-Host "  [WARN] iscc.exe not found on PATH - skipping installer compilation." -ForegroundColor Yellow
        Write-Host "         Install Inno Setup 6 from https://jrsoftware.org/isdl.php and re-run." -ForegroundColor Yellow
    } else {
        $isccExe = if ($iscc -is [System.IO.FileInfo]) { $iscc.FullName } else { $iscc.Source }
        & $isccExe $issFile
        if ($LASTEXITCODE -ne 0) { Fail "iscc.exe returned exit code $LASTEXITCODE." }
        OK "Installer compiled."
    }
} else {
    Write-Host "  (installer skipped)"
}

# ── 5. Summary ────────────────────────────────────────────────────────────────
Step "Release artifacts"
if (Test-Path $releaseDir) {
    Get-ChildItem $releaseDir | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 2)
        Write-Host ("  {0,-55} {1,8} MB" -f $_.Name, $sizeMB)
    }
} else {
    Write-Host "  (release folder empty)"
}

Write-Host "`nDone. Release v$version ready in: $releaseDir" -ForegroundColor Green
