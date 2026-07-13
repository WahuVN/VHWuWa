# Changelog

Tất cả thay đổi đáng chú ý sẽ được ghi ở đây. Theo [SemVer](https://semver.org/lang/vi/).

## [1.0.0] - 2026-07-14
### Thêm mới
- Ứng dụng WPF (.NET 8, MVVM, DI, Wpf.Ui) với 9 trang: Trang chủ, Cài Việt hóa, Quản lý mod, Font, Đồ họa, Sao lưu, Hướng dẫn, Cài đặt, Nhật ký.
- Hệ thống gói `.vhwpack` (manifest + SHA-256 + chữ ký RSA), chống path traversal / zip-slip.
- Cài / gỡ có backup + rollback; quản lý mod (bật/tắt, xung đột); đổi font; chỉnh đồ họa theo preset.
- Sao lưu/khôi phục theo từng thao tác; nhật ký Serilog (ẩn dữ liệu nhạy cảm).
- Tự cập nhật qua GitHub Releases + trình cập nhật riêng (`VHWuWa.Updater`).
- Công cụ CLI `VHWuWa.PackageTool` (keygen/pack/verify/list).
- GitHub Actions build + test + publish + release; script build-release.
