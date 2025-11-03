# PowerShell script to build and package the Web application for Azure Zip Deploy
# Usage: .\scripts\Publish-ZipDeploy.ps1

param(
    [string]$ProjectPath = "LibraryApp.Web\LibraryApp.Web.csproj",
    [string]$Configuration = "Release",
    [string]$PublishDir = "artifacts\publish",
    [string]$ZipPath = "artifacts\deploy\LibraryApp.Web.zip"
)

$ErrorActionPreference = 'Stop'

# Get the script's directory and project root (parent of scripts folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

# Change to project root directory
Set-Location $projectRoot
Write-Host "Working directory: $projectRoot" -ForegroundColor Gray

Write-Host "=== Building and Packaging for Azure Zip Deploy ===" -ForegroundColor Cyan

# Ensure output folders exist (relative to project root)
$publishDirPath = Join-Path $projectRoot $PublishDir
$zipDirPath = Split-Path -Path (Join-Path $projectRoot $ZipPath) -Parent

if (-not (Test-Path $publishDirPath)) {
    New-Item -ItemType Directory -Force -Path $publishDirPath | Out-Null
    Write-Host "Created directory: $publishDirPath" -ForegroundColor Gray
}

if (-not (Test-Path $zipDirPath)) {
    New-Item -ItemType Directory -Force -Path $zipDirPath | Out-Null
    Write-Host "Created directory: $zipDirPath" -ForegroundColor Gray
}

# Restore and publish
Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing application ($Configuration)..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    -c $Configuration `
    -o $publishDirPath `
    --nologo `
    /p:PublishReadyToRun=false `
    /p:PublishSingleFile=false `
    /p:IncludeSymbols=false `
    /p:SelfContained=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to publish application" -ForegroundColor Red
    exit 1
}

# Remove previous zip if exists
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
    Write-Host "Removed previous ZIP file" -ForegroundColor Gray
}

# Create ZIP from PUBLISHED CONTENTS (not the folder itself)
# Azure Zip Deploy requires files at the root of the ZIP archive
Write-Host "`nCreating ZIP package..." -ForegroundColor Yellow

# Verify publish directory has files
if (-not (Test-Path $publishDirPath)) {
    Write-Host "Error: Publish directory does not exist: $publishDirPath" -ForegroundColor Red
    exit 1
}

$items = Get-ChildItem -Path $publishDirPath -Force
if ($items.Count -eq 0) {
    Write-Host "Error: Publish directory is empty: $publishDirPath" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($items.Count) items in publish directory" -ForegroundColor Gray
$zipFullPath = Join-Path $projectRoot $ZipPath

# Create ZIP with files at root
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDirPath, $zipFullPath)

Write-Host "`n[SUCCESS] ZIP created successfully!" -ForegroundColor Green
Write-Host "  Location: $zipFullPath" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host '  1. Navigate to Azure Portal to Your App Service to Deployment Center to Zip Deploy' -ForegroundColor White
Write-Host "  2. Upload this ZIP file: $ZipPath" -ForegroundColor White
Write-Host "  3. Wait for deployment to complete" -ForegroundColor White

