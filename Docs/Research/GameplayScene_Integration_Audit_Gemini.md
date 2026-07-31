# Báo Cáo Kiểm Tra Tích Hợp GameplayScene (Chế Độ Multi-Scene)

Dựa trên việc kiểm tra trực tiếp mã nguồn và phân tích cấu trúc YAML của `GameplayScene.unity`, dưới đây là các phát hiện và đánh giá khi dự án được chia thành luồng `BootScene → HomeScene → GameplayScene`.

## 1. Các Component & Manager Trùng Lặp

### ⚠️ Trùng Lặp `GameSettingsManager` (Ưu tiên: Trung Bình)
- **Tình trạng:** 
  - File `AppBootstrap.cs` (Dòng 29) tự động thêm `GameSettingsManager` và áp dụng `DontDestroyOnLoad`. Object này sẽ tồn tại vĩnh viễn.
  - Tuy nhiên, trong `GameplayScene.unity` (FileID 875696219, GameObject 889841294) **vẫn chứa một bản sao** của `GameSettingsManager` được gắn kèm với các Toggle UI.
- **Rủi ro:** Khi đi theo luồng Boot → Home → Game, hệ thống sẽ tồn tại song song 2 `GameSettingsManager`. Bản ở Boot không có Toggle, bản ở Game có Toggle. Do code được viết tĩnh (static properties) nên không gây crash, nhưng đây là **rác bộ nhớ và sai thiết kế kiến trúc**.
- **Đề xuất sửa:** Xóa component `GameSettingsManager` cứng trong `GameplayScene`. Di chuyển logic giao diện Settings (các Toggle) thành một thành phần UI độc lập, khi mở lên sẽ gửi thông điệp tới `GameSettingsManager` tĩnh.

### ✅ Các Manager Khác (An Toàn)
- `EventSystem`: Nằm nội bộ trong `GameplayScene`, tự sinh và tự diệt khi LoadScene(Single), không trùng lặp.
- `GameFeedbackManager`: `GameplayManager.cs` (Dòng 124) tự gọi `AddComponent<GameFeedbackManager>()` nếu chưa có trên chính GameObject của nó. Nó bị hủy cùng Scene nên rất an toàn, không trùng.
- `DontDestroyOnLoad`: Quét toàn project, chỉ duy nhất `AppBootstrap.cs` gọi hàm này. Khá sạch sẽ.

## 2. Rủi Ro Trạng Thái (State) Khi Đổi Scene/Thao Tác

### ⚠️ Quản lý Nhạc Nền / AudioSource (Ưu tiên: Trung Bình)
- **Tình trạng:** Hàm `GameSettingsManager.ApplyMusicState()` (Dòng 145) quét tất cả `AudioSource` có `loop = true` trong scene để ngắt/mở tiếng.
- **Rủi ro:** Vì AudioSource gắn trực tiếp trên Camera/GameObject nội bộ của `HomeScene` và `GameplayScene`, khi bạn gọi `AppSceneFlow.LoadHome()`, nhạc nền sẽ bị **tắt cái rụp** và chơi lại từ đầu ở Scene tiếp theo, trải nghiệm nghe sẽ bị gãy.
- **Đề xuất sửa:** Đưa AudioSource phát nhạc nền chính vào một prefab nạp cùng `AppBootstrap` (hoặc để trực tiếp vào đó) để nhạc duy trì mượt mà xuyên Scene.

### ⚠️ Trạng Thái Pause & Time.timeScale (Ưu tiên: Thấp / Tuỳ thiết kế)
- **Tình trạng:** Hàm `GameplayManager.SetPaused(bool)` (Dòng 805) **hoàn toàn KHÔNG can thiệp** vào `Time.timeScale`. Trạng thái pause chỉ là bật cờ `isPaused = true`.
- **Rủi ro/Tính năng:** Các Update custom của bạn dừng lại, nhưng toàn bộ Unity Animation, Particle System, DOTween (nếu có dùng mặc định) vẫn sẽ chạy trong nền. Nếu đây là chủ ý để background UI/VFX vẫn bay lượn lúc Pause thì rất tuyệt. Còn nếu muốn game đứng hình hoàn toàn, thì đang bị thiếu `Time.timeScale = 0`. (Hàm `RestartLevel` và `AppSceneFlow.Load()` luôn reset timescale nên không bao giờ bị kẹt slow-motion).

### ✅ Luồng Chuyển Đổi State (Cực Kỳ An Toàn)
- **Restart / Win / Lose:** `GameUIManager.Restart()` gọi `gameplayManager.RestartLevel()`. Phương thức này dừng toàn bộ coroutine, mở khóa bàn, gọi lại `StartNewLevel()`, dọn sạch Order và con trỏ.
- **Drag & Drop:** `EnterHomeState()` (Dòng 728) cẩn thận gọi `CancelPointerInteraction()`, đẩy xiên đang cầm lơ lửng về vị trí cũ và hủy cờ select. Ngăn chặn triệt để lỗi "ma" giữ xiên khi thoát ra Home.

## 3. Tàn Dư Từ HomeScreen_v1 (Missing References)

### 🧹 Reference Rác Trong GameUIManager (Ưu tiên: Thấp)
- **Tình trạng:** `GameUIManager.cs` vẫn chứa các field `homeScreen`, `homePlayButton`, `homeLevelText` (Dòng 37-39). Trong `GameplayScene.unity`, các Inspector Field này đang bị bỏ trống (`{fileID: 0}`).
- **Mức độ an toàn:** Rất an toàn. Các hàm như `ShowHome()` hay `RefreshHomeLevel()` đều có chặn check null (`if (homeScreen == null) return;`). Ngoài ra `ReturnHome()` (Dòng 527) có fallback cực thông minh: `if (AppSceneFlow.CanLoadHome()) LoadHome(); else ShowHome();` giúp code không bao giờ vỡ dù test thẳng trong Editor.
- **Đề xuất:** Để code gọn hơn, khi UI Home đã qua Scene riêng, có thể xóa dần các field dư thừa này trong đợt dọn dẹp tới (không gấp).

## 4. Listener Của Nút Bấm
- **Kiểm tra trực tiếp code:** `pauseHomeButton` và `resultHomeButton` được cấu hình cứng trong Inspector của `GameplayScene`.
- Khi Awake, `GameUIManager.WireButtons()` (Dòng 279) chủ động tháo và lắp lại listener bằng code (`onClick.AddListener(ReturnHome)`).
- → **Đánh giá:** Nối listener qua script cực kỳ chuẩn xác và chắc chắn, tránh được lỗi mất tham chiếu thường gặp trong Unity Inspector.

## Tóm Lược Hành Động Khuyến Nghị Dành Cho Lập Trình Viên (Sắp Xếp Theo Ưu Tiên)
1. Bỏ component `GameSettingsManager` khỏi Hierarchy của `GameplayScene`.
2. Chuyển nhạc nền (AudioSource) lên `AppBootstrap` để không bị đứt đoạn lúc đổi Scene.
3. Cân nhắc có bổ sung `Time.timeScale = 0` vào hàm `SetPaused()` hay không.
