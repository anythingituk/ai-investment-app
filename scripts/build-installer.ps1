[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $repoRoot "publish\AlphaTray"
$outputDir = Join-Path $repoRoot "dist\installer"
$installerScript = Join-Path $repoRoot "installer\AlphaTray.iss"

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

New-Item -ItemType Directory -Force -Path $publishDir, $outputDir | Out-Null

$publishArgs = @(
    "publish",
    (Join-Path $repoRoot "src\AlphaTray.App\AlphaTray.App.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:Version=$Version",
    "-o", $publishDir
)

if ($NoRestore) {
    $publishArgs += "--no-restore"
}

Invoke-Checked dotnet @publishArgs

$assetOutputDir = Join-Path $publishDir "Assets"
New-Item -ItemType Directory -Force -Path $assetOutputDir | Out-Null
Copy-Item -Force (Join-Path $repoRoot "assets\default_purple\default_purple.ico") (Join-Path $assetOutputDir "default_purple.ico")
Copy-Item -Force (Join-Path $repoRoot "assets\paused_grey\paused_grey.ico") (Join-Path $assetOutputDir "paused_grey.ico")
Copy-Item -Force (Join-Path $repoRoot "assets\researching_orange\researching_orange.ico") (Join-Path $assetOutputDir "researching_orange.ico")

$env:ALPHATRAY_VERSION = $Version
$env:ALPHATRAY_PUBLISH_DIR = $publishDir
$env:ALPHATRAY_OUTPUT_DIR = $outputDir

Invoke-Checked dotnet tool restore
Invoke-Checked dotnet iscc $installerScript

$setupPath = Join-Path $outputDir "AlphaTray-Setup-$Version.exe"
if (-not (Test-Path $setupPath)) {
    throw "Expected installer was not created: $setupPath"
}

$stableSetupPath = Join-Path $outputDir "setup.exe"
Copy-Item -Force $setupPath $stableSetupPath

Write-Host "Installer created: $setupPath"
Write-Host "Stable setup alias created: $stableSetupPath"
