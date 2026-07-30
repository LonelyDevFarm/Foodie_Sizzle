# Tồn Kho Asset & Đề Xuất UI Màn Hình Home (Foodie Sizzle) - V2

**Phương pháp:** Quét toàn bộ thư mục và *MỞ XEM TRỰC TIẾP* từng ảnh (không dựa vào tên). 
**Bối cảnh:** Dựa trên giao diện gameplay hiện tại (dùng ván gỗ hoặc gradient dọc), sau khi loại trừ các ảnh là sprite sheet (như `capy.png`) hoặc huy hiệu (như `Logo1.png`), dưới đây là danh sách ứng viên tốt nhất (tối đa 3/nhóm).

---

## 1. Background (Nền dọc 9:16 hoặc Tiled)

### Ứng viên 1: SanBG
![SanBG](file:///D:/Projects/Foodie_Sizzle/Assets/Texture2D/SanBG.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\Texture2D\SanBG.png`
- **Tên sprite:** `SanBG` (Ảnh đơn)
- **Kích thước:** 20x66
- **Trong suốt (Alpha):** Không (Ảnh màu đặc).
- **Vì sao phù hợp:** Đây là dải màu gradient xanh teal dọc. Có thể dùng Image Type = `Sliced` hoặc scale 9:16 ra toàn màn hình cực đẹp, dung lượng siêu nhẹ, tạo độ sâu và làm nổi bật màu cam/đỏ/vàng của thức ăn. Đang được dùng làm phông nền ngoài grid của SampleScene.
- **Nhược điểm:** Trông hơi trống trải nếu không có họa tiết phụ đè lên.

### Ứng viên 2: BGChinh
![BGChinh](file:///D:/Projects/Foodie_Sizzle/Assets/Texture2D/BGChinh.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\Texture2D\BGChinh.png`
- **Tên sprite:** `BGChinh` (Ảnh đơn)
- **Kích thước:** 75x99
- **Trong suốt (Alpha):** Không.
- **Vì sao phù hợp:** Texture mặt bàn/ván gỗ. Gỗ là chất liệu "chân ái" của game nấu ăn/nướng BBQ. Rất dễ set `Tiled` để tạo thành một chiếc bàn nướng vô cực.
- **Nhược điểm:** Màu nâu có thể làm chìm các nút bấm màu đỏ/cam, cần setup viền đen dày cho nút bấm.

---

## 2. Logo / Tiêu đề (Liên quan đồ ăn/nướng)

### Ứng viên 1: CapiLogo
![CapiLogo](file:///D:/Projects/Foodie_Sizzle/Assets/BaseGame/Sprite/Logo/CapiLogo.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\BaseGame\Sprite\Logo\CapiLogo.png`
- **Tên sprite:** `CapiLogo` (Ảnh đơn)
- **Kích thước:** 512x512
- **Trong suốt (Alpha):** Không (Có nền tỏa sáng màu vàng).
- **Vì sao phù hợp:** Vẽ tay cực đẹp, capy đội mũ đầu bếp đang đứng trước vỉ nướng thịt và bắp. Thể hiện đúng nội dung "Sizzle" (tiếng xèo xèo của thịt nướng).
- **Nhược điểm:** Ảnh này bao gồm cả phần nền vuông tỏa sáng phía sau. Nếu đặt lên màn hình Home có thể bị lộ khối vuông cứng nhắc.

### Ứng viên 2: ic_capy_foreground
![ic_capy_foreground](file:///D:/Projects/Foodie_Sizzle/Assets/BaseGame/Sprite/Logo/Adaptive/ic_capy_foreground.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\BaseGame\Sprite\Logo\Adaptive\ic_capy_foreground.png`
- **Tên sprite:** `ic_capy_foreground` (Ảnh đơn)
- **Kích thước:** 432x432
- **Trong suốt (Alpha):** CÓ (Nền trong suốt).
- **Vì sao phù hợp:** Tương tự như `CapiLogo` nhưng đã được tách nền trong suốt 100%. Rất dễ dàng đặt đè lên bất cứ Background nào ở trên mà không lo bị ô vuông viền ngoài.
- **Nhược điểm:** Dù có hình, nhưng thiếu chữ "Foodie Sizzle". Sẽ cần dùng Component Text (hoặc TextMeshPro) để ghép thêm tiêu đề chữ bên dưới.

---

## 3. Nút Play (Bo tròn)

### Ứng viên 1: Green Button
![Green Button](file:///D:/Projects/Foodie_Sizzle/Assets/Texture2D/Green Button.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\Texture2D\Green Button.png`
- **Tên sprite:** `Green Button` (Ảnh đơn)
- **Kích thước:** 143x186
- **Trong suốt (Alpha):** CÓ (Bo góc trong suốt).
- **Vì sao phù hợp:** Nút bấm bo tròn màu xanh lá mạ mập mạp, viền xanh đậm dày. Mang chuẩn phong cách casual game vui nhộn.
- **Nhược điểm:** Hơi thiên về nút dọc, nếu text "PLAY" quá dài sẽ bị ép vào giữa. Cần dùng Image Type = `Sliced` để kéo ngang.

### Ứng viên 2: Big Button
![Big Button](file:///D:/Projects/Foodie_Sizzle/Assets/Texture2D/Big Button.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\Texture2D\Big Button.png`
- **Tên sprite:** `Big Button` (Ảnh đơn)
- **Kích thước:** 143x186
- **Trong suốt (Alpha):** CÓ (Bo góc trong suốt).
- **Vì sao phù hợp:** Cùng form với Green Button nhưng màu đỏ ánh cam, viền nâu đậm. Cực kỳ nổi bật nếu dùng trên nền gỗ (`BGChinh.png`) hoặc nền gradient xanh (`SanBG.png`).
- **Nhược điểm:** Tương tự Green Button, cần slice để kéo ngang.

---

## 4. Icon Play
*(Khảo sát chi tiết thư mục Texture2D và BaseGame/Sprite)*
- **Kết quả:** **KHÔNG TÌM THẤY** một icon ảnh đơn hình "tam giác Play" độc lập nào (các file như `Play Button.png` chỉ là phần nền/khung nút, không chứa icon tam giác).
- **Đề xuất thay thế:** Dùng text đè lên nút ở Mục 3. Viết chữ **"PLAY"** màu trắng bằng font **Lilita One** hoặc **Mikado** (đã có sẵn trong thư mục Font) và thêm outline đen.

---

## 5. Khung Hiển Thị Level

### Ứng viên 1: Level
![Level](file:///D:/Projects/Foodie_Sizzle/Assets/Texture2D/Level.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\Texture2D\Level.png`
- **Tên sprite:** `Level` (Ảnh đơn)
- **Kích thước:** 166x240
- **Trong suốt (Alpha):** CÓ (Cắt theo hình dạng ruy băng).
- **Vì sao phù hợp:** Đây là hình một cái khiên/banner/ruy-băng dọc màu vàng kim (gold) viền nâu mộc. Thiết kế quá hoàn hảo để viết text "LEVEL 12" ở giữa, cắm thẳng từ mép trên hoặc đặt lơ lửng giữa màn hình.
- **Nhược điểm:** Gần như không có, sinh ra là để làm khung Level.

---

## 6. Nhân vật Capy/Đầu bếp (Mascot dạng ảnh đơn)

### Ứng viên 1: Chef_Win
![Chef_Win](file:///D:/Projects/Foodie_Sizzle/Assets/BaseGame/GeneratedCharacters/Chef_Win.png)
- **Đường dẫn:** `D:\Projects\Foodie_Sizzle\Assets\BaseGame\GeneratedCharacters\Chef_Win.png`
- **Tên sprite:** `Chef_Win` (Ảnh đơn)
- **Kích thước:** 1254x1254
- **Trong suốt (Alpha):** CÓ.
- **Vì sao phù hợp:** Một cậu bé đầu bếp Chibi cực kỳ tươi tắn, đứng ăn mừng với hai tay giơ lên. Đây là ảnh đơn hoàn chỉnh, render sắc nét, không bị dính vào sprite sheet như các file Capy (vốn là atlas bóc tách xương).
- **Nhược điểm:** Nhân vật khá to so với tỷ lệ của UI, phải scale down (thu nhỏ) nhiều trong Canvas.

*(Lưu ý: Tất cả các file có tên `capy.png` đều là sprite sheet chứa tay, chân, đầu nổ rải rác hoặc atlas nén nên không thể dùng dưới dạng Image UI thẳng được mà phải extract/slice qua cơ chế của Unity).*
