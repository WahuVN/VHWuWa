# Bảo mật (SECURITY)

## Nguyên tắc

- Repository công khai — **không** commit: file Việt hóa thật, PAK bản quyền, `private*.pem`, `*.pfx`, token, mật khẩu.
- App chỉ chứa **public key** để xác minh chữ ký gói `.vhwpack`. Private key giữ ngoài repo / trong GitHub Actions Secrets.
- Mỗi gói có: SHA-256 từng file + chữ ký số (RSA). App **từ chối cài** nếu chữ ký không hợp lệ (khi đã cấu hình public key).
- Chống path traversal / zip-slip: mọi đích ghi đều được kiểm chứng nằm trong thư mục game.
- Không chạy `.exe/.bat/.cmd/.ps1` lấy từ gói mod nếu chưa có cấu hình tin cậy.
- Không tắt antivirus, không tiêm tiến trình, không dùng driver/rootkit, không kỹ thuật anti-debug gây báo nhầm malware.
- Log **ẩn** token/khóa/mật khẩu.

> Mã hóa phía client KHÔNG thể ngăn trích xuất tuyệt đối. Mục tiêu chính là chống sửa file, giả mạo gói và sao chép đơn giản — ưu tiên chữ ký số + kiểm tra toàn vẹn.

## Báo lỗi bảo mật

Vui lòng mở một *private security advisory* trên GitHub thay vì issue công khai.
