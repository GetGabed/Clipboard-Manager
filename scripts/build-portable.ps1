<#
.SYNOPSIS
    Packages the published win-x64 binary into a portable ZIP archive.
.DESCRIPTION
    Reads <Version> from ClipboardManager.csproj, then zips:
      publish\win-x64\ClipboardManager.exe
      README.md
      LICENSE
    Output: release\ClipboardManager-portable-v{VERSION}.zip
.EXAMPLE
    .\scripts\build-portable.ps1
#>

$ErrorActionPreference = 'Stop'

# --- Paths ---
$root       = Split-Path $PSScriptRoot -Parent
$csproj     = Join-Path $root 'src\ClipboardManager\ClipboardManager.csproj'
$publishDir = Join-Path $root 'publish\win-x64'
$releaseDir = Join-Path $root 'release'

# --- Read version ---
[xml]$proj = Get-Content $csproj
$version   = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "Could not read <Version> from $csproj" }
Write-Host "Building portable ZIP for v$version ..."

# --- Verify publish output ---
$exe = Join-Path $publishDir 'ClipboardManager.exe'
if (-not (Test-Path $exe)) {
    throw "Published EXE not found at '$exe'. Run dotnet publish first."
}

# --- Stage files ---
$staging = Join-Path $env:TEMP "ClipboardManager-portable-v$version"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item $staging -ItemType Directory | Out-Null

Copy-Item $exe                          (Join-Path $staging 'ClipboardManager.exe')
Copy-Item (Join-Path $root 'README.md') (Join-Path $staging 'README.md')
Copy-Item (Join-Path $root 'LICENSE')   (Join-Path $staging 'LICENSE')

# --- Create ZIP ---
if (-not (Test-Path $releaseDir)) { New-Item $releaseDir -ItemType Directory | Out-Null }
$zipPath = Join-Path $releaseDir "ClipboardManager-portable-v$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$staging\*" -DestinationPath $zipPath

# --- Cleanup ---
Remove-Item $staging -Recurse -Force
$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "[OK] Portable ZIP created: $zipPath ($sizeMB MB)"
