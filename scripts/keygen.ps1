<#
  Tạo cặp khóa ký gói .vhwpack. Public key -> Config/public_key.pem. Private key GIỮ NGOÀI repo.
  Dùng: powershell -ExecutionPolicy Bypass -File scripts/keygen.ps1 -Out .\mykeys
#>
param([string]$Out = "./mykeys")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet run --project "$root/src/VHWuWa.PackageTool/VHWuWa.PackageTool.csproj" -- keygen --out $Out

Write-Host ""
Write-Host "→ Copy public key vào ứng dụng:" -ForegroundColor Cyan
Write-Host "   copy `"$Out\public_key.pem`" `"$root\Config\public_key.pem`""
Write-Host "⚠ KHÔNG commit private_key.pem. Lưu vào GitHub Actions Secrets (VHWUWA_SIGNING_KEY)." -ForegroundColor Yellow
