# Skull Cavern Scanner 1.0.0 (mã nguồn)

Mod SMAPI dành cho Stardew Valley 1.6.

## Tính năng
- Chỉ hoạt động trong Hang Đầu Lâu (Skull Cavern).
- Viền vàng các tảng đá có khả năng tạo lối xuống theo phép kiểm tra ứng viên tương thích 1.6.
- Quét lại sau khi đá bị phá vì số đá còn lại ảnh hưởng xác suất tạo lối xuống.
- Khi game đã tạo lối xuống, đọc tọa độ tile lối xuống từ dữ liệu của MineShaft và đánh dấu màu xanh.
- Hiển thị các khoảng cách rơi hợp lệ của hố: 3–9, 11, 13 hoặc 15 tầng; áp dụng giới hạn tầng 100 khi còn ở phía trên tầng 100.
- F8 bật/tắt mặc định; có thể đổi `ToggleKey` trong config.json.

## Quan trọng về độ chính xác
- Viền vàng có nghĩa là đá đủ điều kiện RNG để tạo lối xuống (thang hoặc hố), không khẳng định viên đá đó chắc chắn tạo ra hố.
- Khi lối xuống đã xuất hiện, mod đánh dấu tọa độ lối xuống do game cung cấp. API/dữ liệu được đọc ở đây không phân biệt chắc chắn sprite đó là thang hay hố ở mọi bản đồ/phiên bản.
- Số tầng hiển thị là tập hợp kết quả hợp lệ theo luật game, không phải kết quả seed chính xác của ngày/tầng hiện tại. Stardew Valley xác định kết quả cụ thể từ tầng, ngày và Random Seed; không nên giả vờ dự đoán chính xác nếu chưa tái tạo đúng RNG nội bộ.
- Bản này chưa được biên dịch hoặc chạy thử trong game ở môi trường tạo file. Cần build và kiểm thử trên máy có Stardew Valley 1.6 + SMAPI.

## Cài đặt / build trên Windows
1. Cài .NET 6 SDK và SMAPI tương thích với Stardew Valley 1.6.
2. Giải nén gói này.
3. Mở PowerShell trong thư mục `SkullCavernScanner_1.0.0`.
4. Chạy `./scripts/build.ps1`.
5. Nếu build thành công, script sẽ tạo thư mục `dist/SkullCavernScanner` chứa DLL, manifest và config. Chép thư mục đó vào `Stardew Valley/Mods/`.
6. Chạy game bằng SMAPI. Vào Hang Đầu Lâu và nhấn F8 để bật/tắt.

## Build thủ công
```powershell
cd SkullCavernScanner
 dotnet build -c Release
```

Gói này là mã nguồn; không chứa DLL đã biên dịch.

## Biên dịch trên GitHub (không cần cài .NET trên máy cá nhân)

1. Tạo một repository GitHub mới, ví dụ `SkullCavernScanner`.
2. Tải lên **toàn bộ nội dung bên trong** thư mục `SkullCavernScanner_1.0.0` (bao gồm `.github` và `Directory.Build.props`).
3. Đảm bảo thư mục dự án `SkullCavernScanner` nằm ở thư mục gốc repository.
4. Mở tab **Actions** của repository và bật GitHub Actions nếu GitHub hỏi.
5. Push lên nhánh `main`, hoặc vào workflow **Build SkullCavernScanner** rồi chọn **Run workflow**.
6. Khi job hoàn tất thành công, mở lần chạy đó, kéo xuống phần **Artifacts**, tải `SkullCavernScanner-installable`.
7. Giải nén ZIP artifact. Bên trong sẽ có ZIP cài đặt mod do ModBuildConfig tạo ra; giải nén ZIP cài đặt đó vào `Stardew Valley/Mods`.

Nếu workflow báo lỗi, mở job **build**, sao chép phần log lỗi màu đỏ hoặc tải log gửi lại để sửa. Việc workflow xanh chỉ xác nhận biên dịch thành công; vẫn cần kiểm thử trong game bằng SMAPI để xác nhận hành vi thực tế.
