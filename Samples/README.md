# Samples

- `PackageSource/` — thư mục nguồn mẫu để đóng gói (`manifest.json` + `payload/`). **Không** chứa tài sản game có bản quyền.
- `demo-translation.vhwpack` — gói demo đã ký, dùng để kiểm thử luồng cài/gỡ.
- `keys/` — cặp khóa mẫu. **`private_key.pem` KHÔNG được commit** (đã nằm trong `.gitignore`).

## Tạo khóa của riêng bạn

```bash
dotnet run --project src/VHWuWa.PackageTool -- keygen --out ./mykeys
copy .\mykeys\public_key.pem .\Config\public_key.pem
```

## Đóng gói & ký

```bash
dotnet run --project src/VHWuWa.PackageTool -- pack --input ./PackageSource --output ./demo.vhwpack --key ./mykeys/private_key.pem
dotnet run --project src/VHWuWa.PackageTool -- verify --file ./demo.vhwpack --pub ./Config/public_key.pem
```

> Public key nằm trong `Config/public_key.pem` để app xác minh chữ ký. Private key giữ ngoài repo / trong GitHub Actions Secrets.
