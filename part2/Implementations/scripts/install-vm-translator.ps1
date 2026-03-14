param(
    [string]$InstallDir = "$HOME\.local\bin",
    [string]$AppDir = "$HOME\.local\share\vm-translator"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "..\VMTranslator\VMTranslator.csproj"))

try {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    New-Item -ItemType Directory -Path $AppDir -Force | Out-Null

    dotnet publish $projectFile `
        -c Release `
        --self-contained false `
        -o $AppDir

    $commandPath = Join-Path $InstallDir "vm-translator.cmd"
    @"
@echo off
"$AppDir\vm-translator.exe" %*
"@ | Set-Content -Path $commandPath -NoNewline

    Write-Host "Installed vm-translator to $commandPath" -ForegroundColor Green
    Write-Host "Application files published to $AppDir" -ForegroundColor Green

    $pathEntries = $env:PATH -split ';'
    if (-not ($pathEntries | Where-Object { $_ -eq $InstallDir })) {
        Write-Host "Add $InstallDir to your PATH to run `vm-translator` directly." -ForegroundColor Yellow
    }
}
finally {
}
