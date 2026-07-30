# Báo Cáo Kiểm Toán: Máy Trạng Thái Input & Tương Tác

**Thực hiện:** Gemini 3.1 Pro (High)
**Thời gian:** 30/07/2026
**Phạm vi:** `GameplayManager.cs`, `Grill.cs`, `SkewerVisual.cs`

---

## 1. Bảng Trạng Thái Máy (State Machine Trace)

Bảng dưới đây mô phỏng sự thay đổi các biến trạng thái trong `GameplayManager` qua các bước của một luồng kéo thả bình thường:

| Hành động | `pointerDownGrill` | `draggedSkewer` | `hasDragged` | `selectedGrill` | `selectedSkewer` | `isBoardLocked` |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Trạng thái nghỉ | `null` | `null` | `false` | `null` | `null` | `false` |
| **PointerDown** trên xiên A | Bếp Nguồn | **Xiên A** | `false` | `null` | `null` | `false` |
| Di chuyển vượt ngưỡng | Bếp Nguồn | Xiên A | **`true`** | `null` | **Xiên A** | `false` |
| **PointerUp** (Khác bếp) | `null` | `null` | `false` | `null` | `null` | `false` |
| **PointerUp** (Tại chỗ) | `null` | `null` | `false` | Bếp Nguồn | Xiên A | `false` |

*Ghi chú:* Hàm `SetSelected(true)` và `BeginDrag()` được gọi ngay lập tức trên `draggedSkewer` tại lúc PointerDown, trước cả khi nó trở thành `selectedSkewer`.

---

## 2. Phân Tích Lỗi (Bug Audit)

Qua quá trình kiểm tra tương tác giữa các Coroutine và biến trạng thái, phát hiện các lỗi nghiêm trọng sau:

### 🔴 Lỗi 1: Kẹt Sorting Order khi ngắt Input (Pause / Result Popup)
- **Mức độ:** Nghiêm trọng (Chắc chắn xảy ra).
- **Kịch bản:** Đang kéo xiên (Drag), tay vẫn giữ màn hình, dùng tay kia bấm nút Pause hoặc thời gian kết thúc làm hiện bảng Result che bảng.
- **Nguyên nhân:** Hàm `SetPaused(true)` gọi `ClearCurrentSelection()` và `ResetPointerDrag()`. Tuy nhiên, `ResetPointerDrag()` chỉ gán `draggedSkewer = null` mà không gọi `draggedSkewer.EndDrag()`. Việc này khiến thao tác trừ ngược `sortingOrder` (`-20`) không bao giờ được thực hiện. Khi quay lại game, xiên này kẹt ở `sortingOrder` cực cao. Bấm kéo thêm lần nữa sẽ cộng dồn thành +40, đè lên UI.
- **Cách sửa tối thiểu:** 
  Trong `GameplayManager.SetPaused`, đổi đoạn reset thành:
  ```csharp
  // Thay thế ClearCurrentSelection() và ResetPointerDrag() bằng:
  CancelPointerInteraction();
  ```

### 🔴 Lỗi 2: Xung đột Coroutine gây Glitch hình ảnh khi ghép thành bộ ba
- **Mức độ:** Nghiêm trọng (Ảnh hưởng thẩm mỹ cốt lõi).
- **Kịch bản:** Di chuyển xiên vào một bếp để tạo thành 3 món giống nhau. Xiên đang mờ/nhỏ đi thì đột nhiên phình to lại rồi kẹt lửng lơ.
- **Nguyên nhân:** Khi thả thành công, `TryCompleteDrag()` (hoặc `OnGrillClicked`) gọi `DeselectSkewerAfterDelay` chạy trong **0.25s**. Đồng thời, `Grill` gọi `CheckAndClear()`, chờ `clearArrivalDelay` (0.22s) và chạy animation thu nhỏ xiên biến mất trong `0.18s`. 
  Đúng tại mốc **0.25s**, hàm delay kích hoạt `skewer.SetSelected(false)`, kéo theo `AnimateSelectionScale(scaleBeforeSelection)`. Lệnh này đè ngang coroutine thu nhỏ, ép xiên phóng to trở lại kích thước cũ.
- **Cách sửa tối thiểu:** 
  Trong `SkewerVisual.cs`:
  Thêm cờ `bool isDestroying = false;`. Set thành `true` trong hàm `ClearSelectionEffectImmediately()`. Đầu hàm `SetSelected(bool)` thêm: `if (isDestroying) return;`.

### 🟡 Lỗi 3: Kẹt Scale bé tí khi chọn xiên đang Spawn
- **Mức độ:** Trung bình.
- **Kịch bản:** Bếp vừa ăn xong một bộ ba, đĩa chờ đẩy 3 xiên mới lên bếp. Người chơi bấm chọn xiên đó ngay lập tức khi nó đang phóng to ra. Khi bỏ chọn, xiên bị teo nhỏ vĩnh viễn.
- **Nguyên nhân:** `Grill.SpawnWaitingLayer` chạy coroutine đổi scale từ 0 lên 1, nhưng KHÔNG set cờ `isAnimating = true`. Do đó, người chơi vẫn chạm được. `SkewerVisual.SetSelected(true)` lấy `scaleBeforeSelection = transform.localScale` (lúc này đang là 0.2). Khi bỏ chọn, xiên trả về 0.2.
- **Cách sửa tối thiểu:** 
  Trong `SkewerVisual.SetSelected(true)`:
  Thay vì lưu `transform.localScale`, hãy lưu:
  `scaleBeforeSelection = CalculatedScale;`

### 🟡 Lỗi 4: Rò rỉ Visual (Viền sáng) do Tap hụt vào bếp Animating
- **Mức độ:** Trung bình (Giả thuyết có thể xảy ra trong Edge Case).
- **Kịch bản:** Tap nhanh (không drag) một xiên cầm lên, sau đó lập tức thả vào một bếp đang bận Animating.
- **Nguyên nhân:** Khi PointerDown, `draggedSkewer` (lúc này chưa là `selectedSkewer`) được bật viền sáng. Khi PointerUp, code chạy vào `OnGrillClicked`. Tại đây, có hàm chặn:
  ```csharp
  if (grill.IsAnimating) { ClearCurrentSelection(); return; }
  ```
  Tuy nhiên, `ClearCurrentSelection()` chỉ xóa cờ của `selectedSkewer` cũ (vốn đang rỗng). `draggedSkewer` (được truyền vào qua biến `clickedSkewer`) bị bỏ quên, mãi mãi sáng viền.
- **Cách sửa tối thiểu:**
  ```csharp
  if (grill.IsAnimating) { 
      clickedSkewer?.SetSelected(false); // Thêm dòng này
      ClearCurrentSelection(); 
      return; 
  }
  ```

---

**Xác nhận:** Đã đọc toàn bộ các luồng Input trong `GameplayManager`, `SkewerVisual`, và `Grill`. Không sửa bất kỳ file source code hay dữ liệu nào ngoài tài liệu báo cáo này.
