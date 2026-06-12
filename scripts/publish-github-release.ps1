[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [string]$Repository = "anythingituk/ai-investment-app",
    [string]$ReleaseTitle = "",
    [string]$Notes = "",
    [switch]$Draft,
    [switch]$Prerelease,
    [switch]$SkipInstallerBuild,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$buildScript = Join-Path $repoRoot "scripts\build-installer.ps1"
$outputDir = Join-Path $repoRoot "dist\installer"
$versionedSetupPath = Join-Path $outputDir "AlphaTray-Setup-$Version.exe"
$stableSetupPath = Join-Path $outputDir "setup.exe"
$tag = "v$Version"

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI 'gh' is required to publish releases. Install it, run 'gh auth login', then retry this script."
}

if (-not $SkipInstallerBuild) {
    $buildArgs = @("-ExecutionPolicy", "Bypass", "-File", $buildScript, "-Version", $Version)
    if ($NoRestore) {
        $buildArgs += "-NoRestore"
    }

    Invoke-Checked powershell @buildArgs
}

if (-not (Test-Path $versionedSetupPath)) {
    throw "Missing versioned installer: $versionedSetupPath"
}

if (-not (Test-Path $stableSetupPath)) {
    Copy-Item -Force $versionedSetupPath $stableSetupPath
}

if ([string]::IsNullOrWhiteSpace($ReleaseTitle)) {
    $ReleaseTitle = "AlphaTray $Version"
}

if ([string]::IsNullOrWhiteSpace($Notes)) {
    $Notes = "AlphaTray Windows installer release $Version."
}

$releaseExists = $false
& gh release view $tag --repo $Repository *> $null
if ($LASTEXITCODE -eq 0) {
    $releaseExists = $true
}

if (-not $releaseExists) {
    $createArgs = @(
        "release", "create", $tag,
        "--repo", $Repository,
        "--title", $ReleaseTitle,
        "--notes", $Notes
    )

    if ($Draft) {
        $createArgs += "--draft"
    }

    if ($Prerelease) {
        $createArgs += "--prerelease"
    }

    Invoke-Checked gh @createArgs
}

Invoke-Checked gh release upload $tag $versionedSetupPath $stableSetupPath --repo $Repository --clobber

Write-Host "Published GitHub release assets:"
Write-Host " - $versionedSetupPath"
Write-Host " - $stableSetupPath"
Write-Host "Release: https://github.com/$Repository/releases/tag/$tag"
