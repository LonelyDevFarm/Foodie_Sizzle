# Báo Cáo Phân Tích Logic DDA, Shuffle & Lock (Dữ liệu Extracted)

**Thực hiện:** Gemini 3.1 Pro (High)
**Thời gian:** 30/07/2026
**Phạm vi tìm kiếm:** `D:\Projects\_GameExtract\FoodieSizzle2\ExportedProject\Assets\Scripts\Assembly-CSharp`

---

## 1. Kết Quả Tìm Kiếm Implementation (IL2CPP / Binary)

Sau khi quét toàn bộ thư mục chứa mã nguồn được extract của game gốc (`ExportedProject\Assets\Scripts\Assembly-CSharp`), bao gồm các file có tên khả nghi như `DDAManager.cs`, `LevelLoader.cs`, `LevelFoodIdScanner.cs`... tôi thu được kết quả:

**❌ KHÔNG THỂ XÁC ĐỊNH CHÍNH XÁC IMPLEMENTATION.**

**Lý do (Bằng chứng cụ thể):**
Tất cả các file `.cs` trong thư mục trên đều là các class trống (Dummy class) do AssetRipper tạo ra. Bên trong mỗi file chỉ chứa một khối comment lỗi chuẩn của AssetRipper:

> *Dummy class. This could have happened for several reasons:*
> *1. No dll files were provided to AssetRipper...*
> *6. Cpp2IL failed to decompile Il2Cpp data...*

*(Trích xuất thực tế từ file `D:\Projects\_GameExtract\FoodieSizzle2\ExportedProject\Assets\Scripts\Assembly-CSharp\DDAManager.cs`)*

Game gốc được build bằng **IL2CPP**. Quá trình trích xuất bằng AssetRipper/Cpp2IL đã thất bại trong việc dịch ngược logic (Method bodies) và các biến (Fields). Do đó, hoàn toàn không có mã nguồn để chứng minh các hành vi bên dưới.

---

## 2. Trả Lời Các Câu Hỏi (Dựa trên mức độ bằng chứng)

### A. `useDDA` thực sự làm gì?
- **Chắc chắn:** Không có dòng code nào trong bản extract mô tả hành vi của biến này. File duy nhất liên quan là `DDAManager.cs` nhưng nó là file rỗng.
- **Suy luận (Không chắc chắn):** Dynamic Difficulty Adjustment (DDA) trong game Match-3 thường tự động spawn thêm các item mà người chơi đang thiếu nếu thời gian sắp hết, hoặc thay đổi tốc độ băng chuyền (`speedGrillConveyor`). Nhưng do không có code, không thể kết luận nó có sinh thêm xiên hay không.

### B. `shuffleIcons` đổi vị trí hay đổi mapping ID?
- **Chắc chắn:** Không có code để chứng minh.
- **Suy luận (Không chắc chắn):** Dựa vào tên biến `shuffleIcons`, nó thường xáo trộn (đảo vị trí) các ItemID trên những xiên đã spawn, hơn là sinh ra xiên mới. Tuy nhiên, nếu tổng số lượng 1 món không chia hết cho 3 (như phát hiện ở báo cáo trước), `shuffleIcons` độc lập KHÔNG THỂ giúp người chơi thắng.

### C. Ý nghĩa nhóm `locked` âm (-1..-9 và -1003...)
- **Chắc chắn:** Code `Grill.cs` của Foodie_Sizzle hiện tại chỉ đơn giản gom tất cả các giá trị `< 0` thành `isSpecialLock = true` (Hiển thị icon dấu Plus) và chưa phân loại chi tiết. Trong game gốc, logic phân giải này nằm ở các hàm đã bị mất.
- **Suy luận (Không chắc chắn):**
  - Nhóm `-1, -2, -3...`: Thường là các loại khóa cơ bản mở bằng Boosters, Coins, hoặc Xem Ads. (VD: -1 = Khóa xem quảng cáo, -2 = Khóa trả bằng coin).
  - Nhóm `-1003...`: Rất có thể là khóa liên quan đến một Event đặc biệt, hoặc ID của IAP Package (Mua bằng tiền thật), hoặc level đặc thù.

### D. `SmartIconSelection`
- **Chắc chắn:** Hoàn toàn không tìm thấy định nghĩa (khác file rỗng) trong project.
- **Suy luận (Không chắc chắn):** Nó có thể là cơ chế tự động đổi icon của các món ăn nằm ở lớp dưới (chưa hiện lên) thành món mà người chơi đang cần ghép, nhằm vá lỗi "thiếu món không chia hết cho 3".

---

## 3. Kết Luận Khuyến Nghị

Vì toàn bộ mã nguồn của game gốc trong folder extract đã bị hỏng do lỗi dịch ngược IL2CPP, **KHÔNG NÊN** phỏng đoán và copy logic một cách mù quáng. 

Nếu Foodie_Sizzle muốn thiết kế các tính năng này, team nên tự viết logic mới phù hợp với cấu trúc `LevelData` và `GameplayManager` hiện tại thay vì cố gắng tái tạo lại (reverse-engineer) một black-box.

---
*Xác nhận: Tôi đã kiểm tra kỹ cấu trúc `ExportedProject\Assets\Scripts\Assembly-CSharp` và xác thực tình trạng IL2CPP Dummy Class. Không có file nào trong dự án hiện tại bị chỉnh sửa.*
