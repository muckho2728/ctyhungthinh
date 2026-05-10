# Hướng Dẫn Chạy Nhanh (Quick Start)

## Các bước để chạy hệ thống:

### Bước 1: Khởi động Docker Desktop ⚠️ QUAN TRỌNG

1. Mở **Docker Desktop** từ Start Menu
2. Đợi icon Docker ở taskbar chuyển sang màu xanh (running)
3. Kiểm tra Docker hoạt động:
```bash
docker --version
```

**Nếu Docker không chạy, hệ thống sẽ không hoạt động!**

---

### Bước 2: Cấu hình Environment Variables

Chạy script này để mở file .env:

```bash
.\setup-env.bat
```

Script sẽ:
- Tạo file .env nếu chưa có
- Mở notepad để bạn chỉnh sửa

**Chỉnh sửa file .env**:
```
TWELVEDATA_API_KEY=c5795c825e5447c8a05a7cfe6c5da761
JWT_SECRET=your_random_32_character_secret_here
```

**Generate JWT Secret** (một trong các cách):
- Online: https://www.uuidgenerator.net/api/version4
- PowerShell: `[guid]::NewGuid()`
- Hoặc dùng string random 32 ký tự bất kỳ

Lưu file và đóng notepad.

---

### Bước 3: Test TwelveData API (Optional)

Kiểm tra API key có hoạt động không:

```bash
.\test-twelvedata.bat
```

Nếu thấy JSON responses = API hoạt động tốt
Nếu thấy error messages = Check API key

---

### Bước 4: Chạy hệ thống với Docker Compose

```bash
docker compose up -d
```

Đợi 30-60 giây để tất cả containers khởi động.

---

### Bước 5: Kiểm tra hệ thống

```bash
# Xem tất cả containers đang chạy
docker compose ps

# Xem logs (nếu có lỗi)
docker compose logs -f
```

---

### Bước 6: Truy cập ứng dụng

- **Frontend**: http://localhost
- **Backend API**: http://localhost:5000/api
- **Swagger Docs**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

---

## Các lỗi thường gặp

### Lỗi 1: "unable to get image 'nginx:alpine'"
**Nguyên nhân**: Docker Desktop không chạy
**Fix**: Mở Docker Desktop và đợi nó khởi động hoàn toàn

### Lỗi 2: Port đã được sử dụng
```
Bind for 0.0.0.0:5432 failed: port is already allocated
```
**Fix**: Đóng service khác đang dùng port đó, hoặc đổi port trong docker-compose.yml

### Lỗi 3: Backend không kết nối được database
**Nguyên nhân**: Database chưa khởi động xong
**Fix**: Đợi thêm 10-20 giây, backend sẽ tự động kết nối lại

### Lỗi 4: .env file không đọc được
**Fix**: Đảm bảo file .env ở cùng thư mục với docker-compose.yml

---

## Dừng hệ thống

```bash
docker compose down
```

## Xóa toàn bộ (bao gồm dữ liệu database)

```bash
docker compose down -v
```

---

## Cần help?

Xem hướng dẫn chi tiết: **ONBOARDING_GUIDE.md**
