# Foodie Sizzle — Development Roadmap

Cập nhật gần nhất: 31/07/2026

## Đang làm

- Sol/Codex: đã thêm trạng thái Home, luồng Play và quay Home từ Pause/kết quả;
  chờ Unity refresh để dựng và kiểm tra giao diện trực tiếp.
- Gemini/Claude Antigravity: đã hoàn thành hai vòng khảo sát asset Home; vòng
  thứ hai tìm được mascot capy nướng thịt phù hợp.
- Chờ người dùng kiểm tra trực tiếp:
  - Sau khi dùng Box vẫn chọn và kéo xiên ngay được.
  - Hiệu ứng chọn phóng nhẹ lên rồi trở về đúng scale.
  - Order chọn món hợp lý sau khi dùng `preferredLayers`.

## Đã hoàn thành

### Vòng chơi chính

- Bố cục 12 bếp thích ứng màn hình dọc.
- Kéo thả theo con trỏ và chạm-chọn/chạm-đích.
- Chỉ nhận vị trí thả hợp lệ; sai thì trở về vị trí cũ.
- Viền trắng và scale khi chọn/kéo xiên.
- Ghép ba xiên theo `FoodItemData.AreMatching`.
- Hàng chờ nhiều lớp và chồng đĩa thay đổi theo lớp.
- Gợi ý rung bộ ba sau một thời gian không thao tác.
- Phát hiện deadlock và xáo bàn tự động.

### Level và dữ liệu

- `LevelData`, `LevelDatabase` và lưu level hiện tại bằng PlayerPrefs.
- Import JSON nguồn thành LevelData.
- Chỉ nhận thiết kế tối đa 4 hàng × 3 cột (12 bếp).
- Chuẩn hóa ID âm và ID có cờ hàng nghìn về món cơ sở.
- Chỉ đưa level có tổng xiên và từng MatchKey chia hết cho ba vào database.
- Ước tính 109/161 level nguồn tương thích hoàn toàn với gameplay hiện tại.
- Ánh xạ Food ID nguồn sang FoodItemData.
- Khóa bếp bằng món và khóa đặc biệt bằng vật phẩm Plus.

### Order

- Trigger theo tổng số bộ ba đã hoàn thành.
- 1–3 món, thời gian riêng và thua khi hết giờ/bỏ Order.
- Chọn món runtime và chỉ phát Order khi có đủ ba xiên cùng loại.
- Dùng `preferredLayers` để giới hạn lớp bếp/hàng chờ được xét.
- Box ưu tiên món Order; Time Booster làm chậm cả timer level và Order.
- UI Capy, bong bóng, ô món, thanh thời gian và nút bỏ Order.

### Booster

- Box: loại một bộ ba, ưu tiên Order.
- Refresh: xáo món trên bếp và toàn bộ hàng chờ.
- Time: làm chậm thời gian trong một khoảng.
- Plus: mở một bếp khóa đặc biệt.
- Số lượng lưu bằng PlayerPrefs; hiện đặt 999 để test.

### UI

- HUD level, timer, mục tiêu và Pause.
- Pause: Nhạc, Âm thanh, Rung; lưu PlayerPrefs.
- Màn thắng/thua, chơi lại và tiếp tục level.
- Nền tối, nút và nhân vật kết quả.
- Home cùng scene gameplay; timer chưa chạy trước khi bấm Play.
- Nút Home trong Pause và màn kết quả quay lại Home, giữ level hiện tại.

### Âm thanh và rung

- Thư viện AudioClip tự đồng bộ từ `Assets/AudioClip`.
- Nhạc nền gameplay tôn trọng công tắc Âm nhạc.
- SFX cho chọn/thả xiên, bộ ba, Order, Booster, UI và thắng/thua.
- Bốn âm combo thay đổi khi ghép liên tiếp.
- Cảnh báo âm thanh khi Order còn 20% thời gian (tối đa 10 giây).
- Rung ngắn theo mức độ sự kiện trên Android; tôn trọng công tắc Rung.

## Ưu tiên tiếp theo

1. Chạy thử và cân âm lượng/rung trên Editor/Android.
2. Bổ sung hai kiểm tra còn lại cho LevelData:
   - khóa và Order tham chiếu tới món tồn tại;
   - trigger Order không vượt tiến trình khả thi.
3. Chạy kiểm thử nhiều level có Order, khóa đặc biệt và nhiều lớp chờ.
4. Hoàn thiện tiến trình dài hạn và màn chọn level.

## Tạm hoãn

- `isSpicy`: đã nhập dữ liệu nhưng chưa đủ bằng chứng về ý nghĩa gameplay.
- Băng chuyền và level 15 bếp: ngoài cấu trúc game hiện tại.
- Quảng cáo, cửa hàng, mạng và phát hành.
- Build iOS: cần máy macOS hoặc dịch vụ build phù hợp ở giai đoạn sau.

## Quy tắc làm song song

- Sol/Codex giữ quyền quyết định kiến trúc và sửa gameplay chính.
- Gemini/Claude mặc định chỉ phân tích và viết báo cáo trong `Docs/Research`.
- Chỉ cho trợ lý thứ hai sửa code khi đã chỉ định rõ file không trùng với phần
  Sol/Codex đang làm.
