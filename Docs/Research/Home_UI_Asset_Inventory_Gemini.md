# Tồn Kho Asset & Đề Xuất UI Màn Hình Home (Foodie Sizzle)

**Thời gian quét:** 31/07/2026
**Phạm vi:** `Assets/BaseGame`, `Assets/Texture2D`, `Assets/Generated`, `Assets/Font`, `Assets/Scenes`

Dựa trên nguyên tắc ưu tiên các thành phần giao diện đã có sẵn trong project và loại bỏ những chức năng chưa được hiện thực, dưới đây là bộ Asset phù hợp nhất để dựng màn hình Home.

---

## 1. Danh Sách Asset Đề Xuất (Tối Thiểu)

### 1.1. Background (Nền)
- **Asset Chính:** `PatternBG.png`
  - **Kích thước:** 256x256
  - **Đường dẫn:** `Assets/BaseGame/Sprite/_NewUI2/Spring/PatternBG.png`
  - **Lý do:** Kích thước vuông, liền mạch. Rất phù hợp để set Image Type thành `Tiled` làm nền lưới họa tiết lặp lại (phong cách phổ biến của game casual/match-3).
- **Asset Dự phòng:** `BG.png`
  - **Kích thước:** 1078x507
  - **Đường dẫn:** `Assets/BaseGame/Sprite/_NewUI2/BattlePass/BP/BG.png` (Làm nền tĩnh kéo giãn `Scale To Fit`).

### 1.2. Logo / Tiêu đề Game
- **Asset Chính:** `Logo1.png`
  - **Kích thước:** 256x256
  - **Đường dẫn:** `Assets/Texture2D/Logo1.png`
  - **Lý do:** Tỉ lệ 1:1, phù hợp làm Logo trung tâm lớn.
- **Asset Dự phòng:** `title.png`
  - **Kích thước:** 1007x261
  - **Đường dẫn:** `Assets/BaseGame/Sprite/_NewUI2/WonderlandCollection/title.png` (Dạng chữ nhật ngang banner nếu Logo1 không chứa text).

### 1.3. Ô Hiển Thị Level Hiện Tại
- **Asset Chính:** `Level.png`
  - **Kích thước:** 166x240
  - **Đường dẫn:** `Assets/Texture2D/Level.png`
  - **Lý do:** Khung dọc tỉ lệ ~2:3, hoàn hảo để đặt Text "Level 1" và trang trí. Đặt Image Type là `Sliced` (nếu có viền) hoặc giữ nguyên.
- **Asset Dự phòng:** `Level.png` (Vuông)
  - **Kích thước:** 97x97
  - **Đường dẫn:** `Assets/BaseGame/Sprite/_NewUI2/BattlePass/BP/Level.png`

### 1.4. Nút Play Khổng Lồ (Call to Action)
- **Asset Chính:** `Green Button.png`
  - **Kích thước:** 143x186
  - **Đường dẫn:** `Assets/Texture2D/Green Button.png`
  - **Lý do:** Nút bo tròn, bóng bẩy (Chunky button). Màu xanh lục luôn là chuẩn mực cho nút "Bắt đầu" trong game casual.
- **Asset Dự phòng:** `Big Button.png`
  - **Kích thước:** 143x186
  - **Đường dẫn:** `Assets/Texture2D/Big Button.png`

### 1.5. Mascot Trang Trí (Tùy chọn)
- **Asset Chính:** `capy.png`
  - **Kích thước:** 240x516
  - **Đường dẫn:** `Assets/Texture2D/capy.png`
  - **Lý do:** Sprite Capybara đứng dọc, rất hợp để đặt ở mép dưới bên trái/phải màn hình Home để giao diện đỡ trống.
- **Asset Dự phòng:** `Chef_Win.png`
  - **Kích thước:** 1254x1254
  - **Đường dẫn:** `Assets/BaseGame/GeneratedCharacters/Chef_Win.png` (Có thể scale nhỏ xuống).

---

## 2. Gợi Ý Wireframe (Màn hình dọc - Portrait)

```text
+---------------------------------------+
|                                       |
|                                       |
|             [ Logo1.png ]             |
|              (256 x 256)              |
|                                       |
|                                       |
|                                       |
|           +---------------+           |
|           |   Level.png   |           |
|           |               |           |
|           |   LEVEL 12    |           |
|           |               |           |
|           +---------------+           |
|                                       |
|                                       |
|         ( Green Button.png )          |
|         (      PLAY        )          |
|                                       |
|                                       |
|  [ capy.png ]                         |
|  [ Mascot   ]                         |
+---------------------------------------+
(Background: PatternBG.png set to Tiled)
```

**Ghi chú:**
- Không đưa Shop, Mạng (Hearts), Vàng/Tiền tệ vào màn hình này để giữ độ tinh gọn, tập trung duy nhất vào nút **Play** và **Tiến độ Level** hiện tại.
