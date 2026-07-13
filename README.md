# VHWuWa

**VHWuWa** là ứng dụng Windows (WPF / .NET 8) giúp người dùng cài đặt và quản lý **bản Việt hóa game**, mod, font và cấu hình đồ họa — giao diện tối giản kiểu Windows 11, có chế độ sáng/tối.

> ⚠️ Repository công khai này **chỉ chứa mã nguồn + cấu hình mẫu + dữ liệu demo**. Không đưa file Việt hóa thật, PAK bản quyền, khóa ký hay bất kỳ tài sản game có bản quyền nào lên đây.

## ✨ Tính năng

- Chọn / tự dò / kéo-thả / kiểm tra thư mục game (theo `Config/game.json`, không chỉ dựa vào tên thư mục).
- Cài / gỡ **bản Việt hóa** có kiểm tra chữ ký + SHA-256, sao lưu và **rollback** khi lỗi.
- Quản lý **mod** (cài/gỡ/bật-tắt, phát hiện xung đột).
- Đổi **font**, chỉnh **đồ họa** (preset + tùy chỉnh, có backup).
- **Sao lưu / khôi phục** file gốc theo từng thao tác.
- **Hướng dẫn** tiếng Việt (Markdown), **Nhật ký** (Serilog, ẩn dữ liệu nhạy cảm).
- **Tự cập nhật** qua GitHub Releases (updater riêng, có rollback).

## 🖥️ Yêu cầu hệ thống

- Windows 10 / 11 (x64).
- Bản phát hành self-contained: **không cần cài .NET**.

## ⬇️ Tải bản phát hành

Vào **[Releases](https://github.com/WahuVN/VHWuWa/releases)** → tải `VHWuWa-x.y.z-win-x64.zip` → giải nén → chạy `VHWuWa.exe`.

## 🚀 Sử dụng

1. Mở app → **Trang chủ** → chọn/tự dò thư mục game.
2. **Cài Việt hóa** → chọn gói `.vhwpack` → xem trước → cài.
3. Xem **Hướng dẫn** trong app để biết chi tiết mod/font/đồ họa/sao lưu.

## 🔧 Build từ mã nguồn

```bash
dotnet restore VHWuWa.sln
dotnet build VHWuWa.sln -c Release
dotnet test  VHWuWa.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts/build-release.ps1 -Version 1.0.0
```

## 📦 Tạo gói `.vhwpack`

```bash
dotnet run --project src/VHWuWa.PackageTool -- keygen --out ./mykeys
copy .\mykeys\public_key.pem .\Config\public_key.pem
dotnet run --project src/VHWuWa.PackageTool -- pack   --input ./PackageSource --output ./vietnamese.vhwpack --key ./mykeys/private_key.pem
dotnet run --project src/VHWuWa.PackageTool -- verify --file ./vietnamese.vhwpack --pub ./Config/public_key.pem
```

`.vhwpack` = ZIP gồm `manifest.json` + `payload/` + `signature.sig`.

## ⚙️ Cấu hình

| Việc | File |
|---|---|
| Đổi tên/nhận diện game | `Config/game.json` (`gameName`, `executable`, `requiredFiles`, `possibleRegistryKeys`, `steamAppId`) |
| Thêm bản Việt hóa | Đóng gói `.vhwpack` với `packageType: "translation"` |
| Thêm mod | `.vhwpack` với `packageType: "mod"` |
| Thêm font | `.vhwpack` với `packageType: "font"` |
| Thêm preset đồ họa | `Config/graphics.json` (`options`, `presets`) |
| Đổi public key | Thay `Config/public_key.pem` bằng public key mới |
| Bảo quản private key | Giữ NGOÀI repo / trong GitHub Actions Secrets (`VHWUWA_SIGNING_KEY`) |

## 🔄 Phát hành bản cập nhật

1. Tăng `Version` trong `Directory.Build.props`.
2. `git tag v1.1.0 && git push origin v1.1.0`.
3. GitHub Actions tự build, tạo ZIP + `checksums.txt` + `update.json` và tạo Release.
4. App người dùng kiểm tra `releases/latest`, tải, verify SHA-256/chữ ký, chạy updater.

## 🧱 Cấu trúc

```
src/VHWuWa.App           WPF (MVVM, Wpf.Ui, DI)
src/VHWuWa.Core          Model + service thuần .NET (hash, chữ ký, .vhwpack, path-safety)
src/VHWuWa.Infrastructure Cài/gỡ, backup, mod, font, đồ họa, cập nhật, log
src/VHWuWa.Updater       Trình cập nhật riêng (swap an toàn + rollback)
src/VHWuWa.PackageTool   CLI tạo/ký/xác minh .vhwpack
tests/                   Unit test (xUnit)
```

## 📜 Giấy phép

MIT (chỉ cho mã nguồn) — xem [LICENSE](LICENSE) và [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
Không đưa dữ liệu Việt hóa / tài sản game có bản quyền vào repository.
