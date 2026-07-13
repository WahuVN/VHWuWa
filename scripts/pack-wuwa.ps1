<#
.SYNOPSIS
  Đóng gói bản Việt hóa Wuthering Waves (loader DLL + pak + font) thành 1 file .vhwpack đã ký.

.DESCRIPTION
  Nhận thư mục "Win64" chứa sẵn theo layout CallMeDangDev/Wahu:
    <Win64Dir>\version.dll
    <Win64Dir>\verorg.dll
    <Win64Dir>\wuwaVietHoa.dll
    <Win64Dir>\wuwaVietHoa\*.pak        (pak dịch + pak font)
  (Ví dụ: ...\wuwavh_tool\BanVH_CuaToi\mod\Client\Binaries\Win64)

  Script tự sinh manifest.json với đích tương đối đúng (Client/Binaries/Win64/...),
  rồi gọi VHWuWa.PackageTool để tạo + KÝ SỐ gói.

  LƯU Ý BẢN QUYỀN: pak/DLL KHÔNG nằm trong repo. Script chỉ đọc từ thư mục bạn chỉ định.
  Gói .vhwpack thành phẩm phát hành riêng (Release/gửi trực tiếp), không commit.

.EXAMPLE
  ./pack-wuwa.ps1 -Win64Dir "D:\...\BanVH_CuaToi\mod\Client\Binaries\Win64" `
                  -PrivateKey "D:\...\VHWuWa\Samples\keys\private_key.pem" `
                  -Output "D:\out\wuwa-viethoa.vhwpack" -Version 1.0.0
#>
param(
    [Parameter(Mandatory)][string]$Win64Dir,
    [Parameter(Mandatory)][string]$PrivateKey,
    [string]$Output = "wuwa-viethoa.vhwpack",
    [string]$Version = "1.0.0",
    [string]$Name = "Wuthering Waves Việt hóa",
    [string]$Author = "WahuVN",
    [string]$Description = "Bản Việt hóa Wuthering Waves (pak dịch + loader chống crash + font)."
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot   # thư mục VHWuWa
$tool = Join-Path $root "src/VHWuWa.PackageTool/VHWuWa.PackageTool.csproj"

if (-not (Test-Path $Win64Dir)) { throw "Không thấy Win64Dir: $Win64Dir" }
if (-not (Test-Path $PrivateKey)) { throw "Không thấy private key: $PrivateKey" }

# --- Thu thập payload theo layout WuWa ---
$destBase = "Client/Binaries/Win64"
$files = @()
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("wuwapack_" + [guid]::NewGuid().ToString("N"))
$payloadDir = Join-Path $staging "payload"
New-Item -ItemType Directory -Force $payloadDir | Out-Null

function Add-PayloadFile([string]$absPath, [string]$relFromWin64, [string]$operation) {
    $rel = $relFromWin64.Replace('\', '/')
    $dstPayload = Join-Path $payloadDir $rel
    New-Item -ItemType Directory -Force (Split-Path -Parent $dstPayload) | Out-Null
    Copy-Item $absPath $dstPayload -Force
    $script:files += [ordered]@{
        source      = "payload/$rel"
        destination = "$destBase/$rel"
        operation   = $operation
    }
}

# DLL loader ở ngay Win64\ (version.dll ghi đè → Replace; còn lại là file mới → Copy)
foreach ($dll in Get-ChildItem $Win64Dir -File -Filter *.dll) {
    $op = if ($dll.Name -ieq "version.dll") { "replace" } else { "copy" }
    Add-PayloadFile $dll.FullName $dll.Name $op
}

# pak/font trong Win64\wuwaVietHoa\ (file mới → Copy)
$vhDir = Join-Path $Win64Dir "wuwaVietHoa"
if (Test-Path $vhDir) {
    foreach ($f in Get-ChildItem $vhDir -File -Recurse) {
        $rel = "wuwaVietHoa/" + $f.FullName.Substring($vhDir.Length).TrimStart('\', '/').Replace('\', '/')
        Add-PayloadFile $f.FullName $rel "copy"
    }
}

if ($files.Count -eq 0) { throw "Không tìm thấy file nào để đóng gói trong $Win64Dir" }

# --- Sinh manifest.json ---
$manifest = [ordered]@{
    schemaVersion         = 1
    packageId             = "wuwa-viethoa"
    packageName           = $Name
    packageType           = "translation"
    author                = $Author
    version               = $Version
    description           = $Description
    supportedGameVersions = @()
    files                 = $files
}
$manifestPath = Join-Path $staging "manifest.json"
$manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "== Đóng gói $($files.Count) file → $Output ==" -ForegroundColor Cyan
dotnet run --project $tool -c Release -- pack --input $staging --output $Output --key $PrivateKey

Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Xong. Kiểm tra: dotnet run --project `"$tool`" -- verify --file `"$Output`" --pub `"$root\Config\public_key.pem`"" -ForegroundColor Green
