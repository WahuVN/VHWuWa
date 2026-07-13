# Đóng góp (CONTRIBUTING)

Cảm ơn bạn đã đóng góp cho VHWuWa!

## Quy tắc

- **Không** commit dữ liệu Việt hóa / tài sản game có bản quyền / khóa riêng / token.
- Tên class & method dùng **tiếng Anh**; giao diện & thông báo dùng **tiếng Việt**.
- Bật `nullable`; xử lý ngoại lệ; không chặn UI thread; không hard-code đường dẫn máy dev.
- Thêm unit test cho logic mới (path-safety, manifest, hash, chữ ký, backup, cài/gỡ…).

## Quy trình

```bash
dotnet restore VHWuWa.sln
dotnet build   VHWuWa.sln -c Release
dotnet test    VHWuWa.sln -c Release
```

1. Fork & tạo nhánh từ `main`.
2. Commit nhỏ, rõ ràng.
3. Mở Pull Request; CI phải xanh (build + test).
