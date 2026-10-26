<#
.SYNOPSIS
Builds Overmix and packs an installer into .\releases.

.DESCRIPTION
Every run makes a newer version. Running the new Setup.exe installs over the old version, and an
Overmix that's already installed and running notices the new build and updates itself within a few
seconds, so after the first install you only need to run this script.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\build\pack.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\build\pack.ps1 -Version 0.2.0
#>
param(
    # Defaults to one made from the current time, so each build is newer than the last.
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$releases = Join-Path $root 'releases'
$publish = Join-Path $root 'artifacts\publish'

# Every build adds a full package; keep this many, plus every small update between versions.
$fullPackagesToKeep = 2

if (-not $Version) {
    # 0.<days since 2026>.<minute of the day>. Windows version numbers can't go past 65535 in any part.
    $now = [DateTime]::UtcNow
    $days = [int][Math]::Floor(($now - [DateTime]::new(2026, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)).TotalDays)
    $Version = "0.$days.$($now.Hour * 60 + $now.Minute)"
}

Push-Location $root
try {
    Write-Host "Building Overmix $Version" -ForegroundColor Cyan
    if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
    dotnet publish src\Overmix\Overmix.csproj -c Release -r win-x64 --self-contained -o $publish `
        -p:Version=$Version -p:PublishReadyToRun=true "-p:OvermixUpdateSource=$releases"
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

    # The drawing libraries bring 100 MB of debug symbols nobody needs on the PC; Overmix's own stay for the log.
    Get-ChildItem $publish -Filter *.pdb | Where-Object Name -NotLike 'Overmix*' | Remove-Item

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed' }

    Write-Host 'Packing' -ForegroundColor Cyan
    dotnet vpk pack --packId Overmix --packVersion $Version --packTitle Overmix --packDir $publish `
        --mainExe Overmix.exe --runtime win-x64 --icon src\Overmix\Assets\overmix.ico --shortcuts StartMenuRoot `
        --outputDir $releases
    if ($LASTEXITCODE -ne 0) { throw 'vpk pack failed' }

    Get-ChildItem $releases -Filter 'Overmix-*-full.nupkg' |
        Sort-Object { [version]($_.Name -replace '^Overmix-(.+)-full\.nupkg$', '$1') } -Descending |
        Select-Object -Skip $fullPackagesToKeep |
        Remove-Item
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host "Packed Overmix $Version into $releases" -ForegroundColor Green
Write-Host "First time on this PC: run $releases\Overmix-win-Setup.exe"
Write-Host 'After that, a running Overmix installs new builds by itself within a few seconds.'
