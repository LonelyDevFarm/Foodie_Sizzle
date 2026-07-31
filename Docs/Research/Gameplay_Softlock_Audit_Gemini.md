# Báo Cáo Kiểm Tra Nguy Cơ Softlock trong GameplayScene

Dựa trên quá trình kiểm tra mã nguồn `GameplayManager.cs`, `Grill.cs`, `OrderUIController.cs` và luồng chạy của các Booster, dưới đây là phân tích chi tiết về các rủi ro softlock và edge cases.

## 1. PHÁT HIỆN LỖI NGHIÊM TRỌNG (SOFTLOCK CHẮC CHẮN 100%)

### ❌ Lỗi: Shuffle không tự động dọn bộ ba, dẫn đến kẹt bàn vĩnh viễn
- **Phân tích:** 
  - Khi bàn chơi không còn nước đi (`IsDeadlock() == true`), game tự động gọi `ShuffleAllGrillsCoroutine()`. Hoặc khi người chơi dùng Refresh Booster.
  - Vòng lặp Shuffle sử dụng `Grill.ReplaceAllFoodData()` để đổi vị trí các ID món ăn trên tất cả các xiên. 
  - **Lỗi:** Hàm `ReplaceAllFoodData` thay đổi data nhưng **KHÔNG HỀ gọi `CheckAndClear()`**. 
  - **Cơ chế kẹt:** Khi Shuffle vô tình (hoặc cố ý do thuật toán xáo ngẫu nhiên) đặt 3 xiên giống hệt nhau lên cùng 1 bếp đang bị đầy (3/3), bộ ba này ĐÁNG LẼ phải tự nổ, nhưng nó lại đứng im.
  - Lúc này, bàn chơi vẫn ở trạng thái đầy 100%. Hàm `IsDeadlock()` vẫn tiếp tục trả về `true`. Hàm `FindHintTriplet()` cố tình bỏ qua các bếp đã chứa đủ 3 xiên giống nhau. Vòng lặp shuffle thử 50 lần rồi bỏ cuộc, kết thúc coroutine.
- **Chuỗi tái hiện:** 
  1. Chơi đến khi tất cả các bếp đang mở khóa đều đầy 3/3 xiên (không có chỗ trống).
  2. Bàn không có bộ 3 nào -> Deadlock. Game kích hoạt Shuffle tự động.
  3. Shuffle xáo lại, có thể tạo ra 3 xiên giống nhau trên cùng 1 bếp. Nhưng do thiếu `CheckAndClear()`, bếp không nổ.
  4. Người chơi KHÔNG THỂ kéo xiên đi đâu vì tất cả các bếp khác đều đã đầy (3/3). Bếp chứa bộ 3 cũng không thể nhấc xiên đặt lại tại chỗ để kích hoạt nổ (do hàm nội bộ `MoveSkewerWithinGrill` cũng không gọi `CheckAndClear()`).
  5. Người chơi hoàn toàn kẹt cứng chờ hết giờ.
- **Vị trí file/dòng:** `GameplayManager.cs` -> `ShuffleAllGrillsCoroutine()` (Khoảng dòng 2184).
- **Đề xuất sửa:** Sau vòng lặp `foreach (Grill grill in grills)` gọi `ReplaceAllFoodData`, phải thêm một vòng lặp nữa gọi `grill.CheckAndClear()` để xử lý các bộ ba vừa được tạo ra do xáo trộn.

## 2. PHÁT HIỆN LỖI STATE (RỦI RO NHẸ)

### ⚠️ Lỗi: Order hết giờ trong lúc Shuffle Coroutine đang chạy ngầm
- **Phân tích:**
  - `ShuffleAllGrillsCoroutine` dùng `yield return new WaitForSeconds(...)`.
  - Trong lúc đang xáo (isShufflingBoard = true), vòng lặp `Update()` vẫn gọi `UpdateOrderTimer()`.
  - Nếu Order đếm ngược về 0 đúng khoảnh khắc này, hàm `GameOver(false)` được gọi. Game Over sẽ đặt `isGameActive = false` và `isBoardLocked = true`.
  - Tuy nhiên, Coroutine xáo bàn KHÔNG bị ngừng. Khi hết thời gian yield, coroutine xáo chạy đến cuối và đặt lại cờ: `isBoardLocked = false`, `isShufflingBoard = false`.
- **Hệ quả:** Bàn chơi bị mở khóa `isBoardLocked = false` ngay bên dưới lớp màn hình Game Over. Rất may hàm `OnGrillClicked` có kiểm tra `!isGameActive` nên người chơi không thể kéo xiên lậu. Tuy nhiên, việc state cờ bị sai có thể gây bug cho các tính năng tương lai.
- **Vị trí file/dòng:** `GameplayManager.cs` -> `GameOver()` (Dòng 2202). Thiếu logic `StopCoroutine(ShuffleAllGrillsCoroutine)` (hoặc thiếu gọi chung `StopAllCoroutines()` giống như `RestartLevel` đang làm).

## 3. CÁC EDGE CASES ĐÃ ĐƯỢC XỬ LÝ RẤT TỐT (KHÔNG BỊ SOFTLOCK)

### ✅ Box Booster dùng trong lúc có Order
- **Hành vi:** Hàm `FindBoxTarget()` rất thông minh. Nó cố tình quét mảng `activeOrderItems`, nếu tìm thấy một món đang cần cho Order mà trên bàn có ít nhất 3 xiên, nó sẽ chọn món đó làm mục tiêu Box.
- **Kết quả:** Box hút 3 xiên đó, gọi `OnMatchingSetCleared(target, false)`, và hàm `ApplyMatchingSetToOrder` ghi nhận món đó đã hoàn thành trong Order. Không hề có lỗi, hoàn toàn hỗ trợ người chơi.

### ✅ Pause / Dùng Booster khi Animation Bộ Ba đang biến mất
- **Hành vi:**
  - `SetPaused(true)` gọi `Time.timeScale = 0f`. Vòng lặp `ClearGrillCoroutine` tính time bằng `Time.deltaTime`. Do đó animation thu nhỏ xiên sẽ tạm dừng mượt mà và chạy tiếp khi Resume.
  - Các Booster (`CanUseBoosterNow()`) đều kiểm tra `!HasAnimatingGrill()`. Không thể bấm Box, Refresh, Plus khi các xiên đang nổ hoặc đĩa chờ đang đẩy lên. Tránh được hoàn toàn lỗi reference null.

### ✅ Refresh Booster khi có Bếp Khóa (Lock) và Đĩa Chờ (Queue)
- **Hành vi:** 
  - `ReplaceAllFoodData()` có lệnh chặn `if (isLocked) return;`. Do đó bếp đang khóa không bao giờ bị xáo trộn hình ảnh món ăn, giữ đúng thiết kế nguyên bản của Level Designer.
  - Nó xáo toàn bộ `waitingLayerQueue` rất bài bản.
- **Kết quả:** An toàn 100%.

### ✅ Check Deadlock sau khi Lớp Chờ đẩy lên
- **Hành vi:** Coroutine `ClearGrillCoroutine` giữ cờ `isAnimating = true` suốt cả quá trình thu nhỏ, hủy xiên cũ, và đẩy lớp xiên mới từ dưới lên.
- **Kết quả:** Chỉ khi TẤT CẢ bếp kết thúc chuyển động, `isAnimating` mới về `false`. Lúc đó `CheckGameStatus()` mới được quyền gọi `IsDeadlock()`. Hoàn toàn không có chuyện xáo bàn khi xiên mới mới lú lên một nửa.

## TỔNG KẾT
Kiến trúc luồng xử lý input và coroutine khá vững. Chỉ cần vá duy nhất lỗ hổng gọi `CheckAndClear()` sau khi Shuffle là có thể loại bỏ hoàn toàn rủi ro game breaker.
