param(
    [string]$VersionLabel = "Beta-0.10.1",
    [string]$GameInstallRoot = "C:\Program Files (x86)\Steam\steamapps\common\SubnauticaRanked"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$launcherRoot = Join-Path $repoRoot "Launcher"
$srcRoot = Join-Path $launcherRoot "src"
$runtimeProject = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Runtime\SubnauticaSpeedrunningMod.Runtime.csproj"
$bootstrapProject = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Bootstrap\SubnauticaSpeedrunningMod.Bootstrap.csproj"
$launcherProject = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Launcher\SubnauticaSpeedrunningMod.Launcher.csproj"
$updaterProject = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Updater\SubnauticaSpeedrunningMod.Updater.csproj"
$bridgeProject = Join-Path $srcRoot "SubnauticaSpeedrunningMod.NetworkBridge\SubnauticaSpeedrunningMod.NetworkBridge.csproj"

$installModRoot = Join-Path $GameInstallRoot "SubnauticaSpeedrunningMod"
$releaseFolderName = "SubnauticaSpeedrunningMod-$VersionLabel"
$releaseRoot = Join-Path $PSScriptRoot $releaseFolderName
$zipPath = Join-Path $PSScriptRoot ($releaseFolderName + ".zip")
$latestJsonPath = Join-Path $PSScriptRoot "latest.json"
$buildTempRoot = Join-Path $PSScriptRoot ".build-temp"
$launcherPublishRoot = Join-Path $buildTempRoot "launcher"
$updaterPublishRoot = Join-Path $buildTempRoot "updater"
$bridgePublishRoot = Join-Path $buildTempRoot "bridge"

$practiceSourceRoot = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Runtime\PracticeSaveFiles"
$languageSourceRoot = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Runtime\LanguageFiles"
$seedSourceRoot = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Runtime\Seeds\RankedSurvival"

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Reset-Directory {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Copy-DirectoryContents {
    param(
        [string]$Source,
        [string]$Destination
    )

    Ensure-Directory -Path $Destination
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
}

function Remove-FilesByPattern {
    param(
        [string]$Root,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue |
            Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

function Write-LatestManifest {
    param(
        [string]$ManifestPath,
        [string]$Version,
        [string]$ZipFileName
    )

    $manifest = @{
        version = $Version
        zipFileName = $ZipFileName
        zipUrl = "https://github.com/ItsFrostyYo/Subnautica-Speedrunning-Mod/releases/download/$Version/$ZipFileName"
    }

    $manifest | ConvertTo-Json | Set-Content -LiteralPath $ManifestPath -Encoding UTF8
}

Write-Host "Building Subnautica Speedrunning Mod release $VersionLabel..."

Reset-Directory -Path $buildTempRoot

dotnet build $runtimeProject -c Release | Out-Host
dotnet build $bootstrapProject -c Release | Out-Host
dotnet publish $launcherProject -c Release -o $launcherPublishRoot | Out-Host
dotnet publish $updaterProject -c Release -o $updaterPublishRoot | Out-Host
dotnet publish $bridgeProject -c Release -o $bridgePublishRoot | Out-Host

$runtimeOutputRoot = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Runtime\bin\Release\net35"
$bootstrapOutputRoot = Join-Path $srcRoot "SubnauticaSpeedrunningMod.Bootstrap\bin\Release\net35"

Write-Host "Updating local install at $GameInstallRoot..."

Ensure-Directory -Path $installModRoot
Ensure-Directory -Path (Join-Path $installModRoot "Runtime")
Ensure-Directory -Path (Join-Path $installModRoot "Bootstrap")
Ensure-Directory -Path (Join-Path $installModRoot "Bridge")
Ensure-Directory -Path (Join-Path $installModRoot "Updater")
Ensure-Directory -Path (Join-Path $installModRoot "LanguageFiles")
Ensure-Directory -Path (Join-Path $installModRoot "SaveFiles")
Ensure-Directory -Path (Join-Path $installModRoot "Seeds")
Ensure-Directory -Path (Join-Path $installModRoot "Seeds\RankedSurvival")

Copy-DirectoryContents -Source $launcherPublishRoot -Destination $installModRoot
Copy-Item -LiteralPath (Join-Path $bootstrapOutputRoot "SubnauticaSpeedrunningMod.Bootstrap.dll") -Destination (Join-Path $installModRoot "Bootstrap\SubnauticaSpeedrunningMod.Bootstrap.dll") -Force
Copy-Item -LiteralPath (Join-Path $runtimeOutputRoot "SubnauticaSpeedrunningMod.Runtime.dll") -Destination (Join-Path $installModRoot "Runtime\SubnauticaSpeedrunningMod.Runtime.dll") -Force
Copy-DirectoryContents -Source $updaterPublishRoot -Destination (Join-Path $installModRoot "Updater")
Copy-DirectoryContents -Source $bridgePublishRoot -Destination (Join-Path $installModRoot "Bridge")

Reset-Directory -Path (Join-Path $installModRoot "LanguageFiles")
Copy-DirectoryContents -Source $languageSourceRoot -Destination (Join-Path $installModRoot "LanguageFiles")

Reset-Directory -Path (Join-Path $installModRoot "SaveFiles")
Copy-DirectoryContents -Source $practiceSourceRoot -Destination (Join-Path $installModRoot "SaveFiles")

$installRankedSurvivalRoot = Join-Path $installModRoot "Seeds\RankedSurvival"
Get-ChildItem -LiteralPath $installRankedSurvivalRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne "BS1" } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
Reset-Directory -Path (Join-Path $installRankedSurvivalRoot "BS1")
Copy-DirectoryContents -Source (Join-Path $seedSourceRoot "BS1") -Destination (Join-Path $installRankedSurvivalRoot "BS1")

Remove-FilesByPattern -Root (Join-Path $installModRoot "Bridge") -Patterns @("*.pdb")

Write-Host "Creating release staging folder..."

Reset-Directory -Path $releaseRoot
Copy-Item -LiteralPath (Join-Path $GameInstallRoot ".doorstop_version") -Destination (Join-Path $releaseRoot ".doorstop_version") -Force
Copy-Item -LiteralPath (Join-Path $GameInstallRoot "doorstop_config.ini") -Destination (Join-Path $releaseRoot "doorstop_config.ini") -Force
Copy-Item -LiteralPath (Join-Path $GameInstallRoot "INSTALL.txt") -Destination (Join-Path $releaseRoot "INSTALL.txt") -Force
Copy-Item -LiteralPath (Join-Path $GameInstallRoot "Launch Mod.cmd") -Destination (Join-Path $releaseRoot "Launch Mod.cmd") -Force
Copy-Item -LiteralPath (Join-Path $GameInstallRoot "winhttp.dll") -Destination (Join-Path $releaseRoot "winhttp.dll") -Force

$releaseModRoot = Join-Path $releaseRoot "SubnauticaSpeedrunningMod"
Reset-Directory -Path $releaseModRoot

Copy-DirectoryContents -Source $installModRoot -Destination $releaseModRoot

Reset-Directory -Path (Join-Path $releaseModRoot "Logs")
Ensure-Directory -Path (Join-Path $releaseModRoot "Logs\Bootstrap")
Ensure-Directory -Path (Join-Path $releaseModRoot "Logs\CrashReports")
Ensure-Directory -Path (Join-Path $releaseModRoot "Logs\Launcher")
Ensure-Directory -Path (Join-Path $releaseModRoot "Logs\Runtime")

Reset-Directory -Path (Join-Path $releaseModRoot "Client")
if (Test-Path -LiteralPath (Join-Path $releaseModRoot "Seeds\State")) {
    Remove-Item -LiteralPath (Join-Path $releaseModRoot "Seeds\State") -Recurse -Force
}

$releaseRankedSurvivalRoot = Join-Path $releaseModRoot "Seeds\RankedSurvival"
Get-ChildItem -LiteralPath $releaseRankedSurvivalRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne "BS1" } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

Remove-FilesByPattern -Root $releaseModRoot -Patterns @("*.pdb", "*.cs")

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

# Keep package files at the ZIP root for compatibility with every updater since Beta-0.8.0.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $releaseRoot,
    $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)
Write-LatestManifest -ManifestPath $latestJsonPath -Version $VersionLabel -ZipFileName ([System.IO.Path]::GetFileName($zipPath))

Write-Host "Release ready:"
Write-Host "  Folder: $releaseRoot"
Write-Host "  Zip:    $zipPath"
Write-Host "  latest: $latestJsonPath"
