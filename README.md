# Luxen Hotel Management System
Hệ thống quản lý khách sạn toàn diện được xây dựng trên nền tảng ASP.NET Core 8.0 MVC, hỗ trợ quản lý đặt phòng, thanh toán trực tuyến và phân quyền người dùng đa cấp.

## 🏗 Cấu trúc dự án
LuxenHotel/
├── Areas/
│   ├── Admin/          # 🔐 Quản trị hệ thống (Dashboard, Quản lý phòng, nhân viên)
│   ├── Customer/       # 🏠 Giao diện người dùng (Đặt phòng, Xem dịch vụ)
│   ├── Staff/          # 🛠 Giao diện nhân viên (Xử lý tác vụ, Check-in/out)
│   └── Identity/       # 🔑 Quản lý xác thực (Đăng nhập, Đăng ký, Quên mật khẩu)
├── Config/             # ⚙️ Cấu hình hệ thống (Dependency Injection, Identity Config)
├── Data/               # 🗄️ Cơ sở dữ liệu (DbContext, SeedData, Migrations)
├── Helpers/            # 🛠️ Công cụ hỗ trợ (VNPay, File Storage, UI Helpers)
├── Models/             
│   ├── Entities/       # 📄 Thực thể DB (Booking, Identity, Order)
│   └── ViewModel/      # 🖼️ Dữ liệu hiển thị (DTOs, PaginatedList)
├── Services/           # 🧠 Logic nghiệp vụ (Xử lý Booking, Identity, Order)
├── wwwroot/            # 🌐 Tài nguyên tĩnh (CSS, JS, Images, Libs)
├── Startup.cs          # 🚀 Khởi tạo ứng dụng & Middleware
└── appsettings.json    # 📝 Cấu hình môi trường (DB Connection, VNPay Keys)

## 🚀 Hướng dẫn khởi chạy nhanh
### 1. Cơ sở dữ liệu (SQL Server)
Mở file `appsettings.json` và cập nhật chuỗi kết nối `DefaultConnection` phù hợp với SQL Server của bạn:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=LuxenHotel;User=sa;Password=YOUR_PASSWORD;..."
}
```

### 2. Khởi tạo Database (Migrations)
Mở Package Manager Console trong Visual Studio hoặc terminal tại thư mục dự án và chạy:

```bash
dotnet ef database update
```

Hệ thống sẽ tự động tạo bảng và khởi tạo dữ liệu mẫu (Seed Data) bao gồm các tài khoản quản trị và cấu hình phòng mặc định.

### 3. Chạy ứng dụng
Sử dụng Visual Studio (F5) hoặc chạy lệnh sau trong terminal:

```bash
dotnet run --project LuxenHotel/LuxenHotel/LuxenHotel.csproj
```

Ứng dụng sẽ chạy tại: `https://localhost:7077` hoặc `http://localhost:5270`

## 🛠 Công nghệ sử dụng
- **Backend:** .NET 8.0, ASP.NET Core MVC.
- **Database:** SQL Server, Entity Framework Core 8.0.
- **Identity:** Custom ASP.NET Core Identity (Quản lý User/Role tùy chỉnh).
- **Payment Gateway:** Tích hợp VNPay (Sandbox hỗ trợ thanh toán thử nghiệm).
- **Frontend:** Bootstrap 5, jQuery, DataTables, FlexSlider, SelectOrDie.
- **Helpers:** 
    - **VNPayLibrary:** Xử lý ký số và phản hồi từ cổng thanh toán.
    - **FileStorageService:** Quản lý tải lên hình ảnh phòng và dịch vụ.
    - **PartialHelper/ScriptHelper:** Tối ưu hóa render giao diện.

## ✨ Tính năng nổi bật
- **Phân quyền đa tầng:** Admin (Toàn quyền), Staff (Quản lý vận hành), Customer (Người dùng cuối).
- **Đặt phòng thông minh:** Kiểm tra tình trạng phòng trống, tính toán giá theo thời gian thực.
- **Thanh toán trực tuyến:** Kết nối trực tiếp với cổng VNPay, xử lý IPN tự động.
- **Quản lý Media:** Hệ thống lưu trữ và quản lý hình ảnh trực quan.
- **Giao diện Responsive:** Tương thích hoàn hảo trên Mobile, Tablet và Desktop.
