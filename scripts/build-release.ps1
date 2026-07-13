#requires -Version 5
<#
  Build bản phát hành VHWuWa (win-x64) vào thư mục dist/, kèm checksums + update.json.
  Dùng: powershell -ExecutionPolicy Bypass -File scripts/build-release.ps1 [-Version 1.0.0]
#>
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force $dist | Out-Null

Write-Host "== Test ==" -ForegroundColor Cyan
dotnet test "$root/VHWuWa.sln" -c Release

Write-Host "== Publish App + Updater (1 file .exe duy nhất) ==" -ForegroundColor Cyan
$pub = @("-c","Release","-r","win-x64","--self-contained","true",
         "-p:PublishSingleFile=true","-p:IncludeNativeLibrariesForSelfExtract=true",
         "-p:EnableCompressionInSingleFile=true")
dotnet publish "$root/src/VHWuWa.App/VHWuWa.App.csproj" @pub -o $dist
dotnet publish "$root/src/VHWuWa.Updater/VHWuWa.Updater.csproj" @pub -o $dist
Remove-Item (Join-Path $dist "*.pdb") -Force -ErrorAction SilentlyContinue

# Tài liệu kèm theo
Copy-Item "$root/LICENSE" (Join-Path $dist "LICENSE.txt") -Force -ErrorAction SilentlyContinue
Copy-Item "$root/README.md" (Join-Path $dist "README.txt") -Force -ErrorAction SilentlyContinue

if (-not (Test-Path (Join-Path $dist "VHWuWa.exe"))) { throw "Thiếu VHWuWa.exe sau publish." }

$zip = Join-Path $root "VHWuWa-$Version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -Force
$sha = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
"$sha  $(Split-Path $zip -Leaf)" | Out-File (Join-Path $root "checksums.txt") -Encoding utf8

@{ version = $Version; minimumVersion = "1.0.0"; releaseNotes = "Bản phát hành $Version"; downloadUrl = "https://github.com/WahuVN/VHWuWa/releases/download/v$Version/$(Split-Path $zip -Leaf)"; sha256 = $sha; signature = ""; mandatory = $false } |
  ConvertTo-Json | Out-File (Join-Path $root "update.json") -Encoding utf8

Write-Host "XONG. Thư mục: $dist" -ForegroundColor Green
Write-Host "ZIP: $zip"
Write-Host "SHA-256: $sha"
