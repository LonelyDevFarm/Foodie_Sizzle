# Đặc Tả Kỹ Thuật: Bộ Kiểm Tra LevelData (Validator)

**Thực hiện:** Gemini 3.1 Pro (High)
**Thời gian:** 30/07/2026
**Cơ sở dữ liệu:** `2.3.0_BaseCohortFix.json` (161 level), đối chiếu với codebase hiện tại.

---

## 1. Các Bất Biến Bắt Buộc (Invariants)

Dựa trên giới hạn thiết kế của `GameplayManager`, `Grill`, `LevelJsonImporter` và hệ thống UI, một `LevelData` phải thỏa mãn các điều kiện sau để chạy được:

| Thành phần | Ràng buộc (Invariant) | Ý nghĩa / Hậu quả |
| :--- | :--- | :--- |
| **Bàn chơi (Board)** | `1 <= rows <= 4` | Hệ thống sinh đĩa chờ (`waitingPlate`) và nắp khóa chỉ thiết kế đồ họa cho tối đa 4 hàng. Vượt quá sẽ lỗi layout. |
| | `1 <= columns <= 3` | Bếp (`Grill.cs`) chỉ có đúng 3 slot cho `activeSlots` và `waitingSlots`. Vượt quá sẽ out-of-bounds array. |
| | `1 <= grills.Count <= 12` | Số lượng bếp tối đa hỗ trợ hiển thị (từ code importer). |
| **Vật phẩm (Items)** | `totalSkewers > 0` | Màn chơi phải có ít nhất 1 món, nếu không sẽ lỗi Game Over ngay lập tức hoặc Deadlock. |
| | `totalSkewers % 3 == 0` | Game yêu cầu ghép bộ 3 (triplet). Số lượng tổng cộng bắt buộc chia hết cho 3 để có cơ hội dọn sạch bàn. |
| | `ItemCount(MatchKey) % 3 == 0` | Mỗi nhóm món ăn cụ thể cũng phải có số lượng là bội của 3. (Game gốc xử lý qua `shuffleIcons`, bản hiện tại chưa có). |
| | Không có item rỗng/null | Layer không được chứa ID rỗng chưa được ánh xạ trong `FoodIdMappingData`. |
| | `layer.Count <= rows * columns` | Một lớp vật phẩm trên bếp không thể chứa nhiều xiên hơn số slot vật lý (tối đa 12). |
| **Bếp Khóa (Lock)**| `sourceLockId > 0` phải có map | Khóa bằng món ăn cụ thể phải có trong bảng ánh xạ, nếu không người chơi không bao giờ mở được bếp. |
| **Order** | `1 <= numberOfFood <= 3` | Code UI `OrderUIController.cs` chỉ chuẩn bị đúng 3 slot ảnh cho món ăn. |
| | `timeLimitSeconds > 0` | Thời gian không được âm, nếu không Order sẽ lập tức thất bại. |
| | `matchesToTrigger <= maxMatches`| `maxMatches = totalSkewers / 3`. Nếu trigger đòi hỏi 10 bộ ba nhưng toàn bộ màn chỉ có 8 bộ ba, Order sẽ không bao giờ xuất hiện. |

---

## 2. Phân Loại Mức Độ Nghiêm Trọng

### 🔴 ERROR (Lỗi Nghiêm Trọng) — *Phải loại level, game sẽ sập hoặc lỗi logic*
- `rows`, `cols`, `grills.Count` ngoài khoảng cho phép.
- `totalSkewers == 0` hoặc `totalSkewers % 3 != 0`.
- Có `Item` bị null/thiếu (ID không được ánh xạ mà không bị lọc).
- Số món trong 1 `layer` vượt quá `rows * columns`.
- Order có `timeLimitSeconds <= 0` hoặc `numberOfFood < 1` hoặc `numberOfFood > 3`.
- Level có `timeLimitSeconds <= 0`.

### 🟡 WARNING (Cảnh Báo) — *Vẫn chạy được nhưng cần xem xét kỹ*
- **Số lượng mỗi `MatchKey` không chia hết cho 3:** Game hiện tại sẽ bị Deadlock vì không đủ bộ 3 (trừ phi dùng Booster Box để cưỡng ép).
- **Cần Solver (Giải Thuật):** Kể cả khi tất cả `MatchKey` chia hết cho 3, **tuyệt đối không tự khẳng định level "solvable" (có thể giải)**. Các lớp xiên đè lên nhau có thể tạo ra chu trình khóa (cyclical dependency) gây Deadlock ngay từ đầu. *Cần một solver mô phỏng chơi thử (brute-force hoặc A\*) mới kết luận được.*
- Order có `matchesToTrigger > totalSkewers / 3`. (Order không xuất hiện).
- `sourceLockId > 0` nhưng không tìm thấy trong mapping. (Không mở được bếp).

### 🔵 INFO (Thông Tin) — *Dữ liệu nguồn có, game hiện tại bỏ qua*
- `useDda`, `shuffleIcons`, `sourceUseSmartIconSelection`.
- `sourceGrillConveyorSpeed`, `sourceConveyorRowCount`.
- Bếp có `sourceLockId < 0` (Khóa đặc biệt — game đã hỗ trợ thuộc tính nhưng chưa có nút bấm).

---

## 3. Thống Kê Trên Toàn Bộ 161 Level (2.3.0_BaseCohortFix.json)

Quét bằng script phân tích tự động trên 161 level gốc:

| Luật (Rule) | Số Level Vi Phạm | Mức độ | Ghi chú |
| :--- | :--- | :--- | :--- |
| Rows/Cols/Grills Limit | **25 / 161** | Error | Có bếp > 12 hoặc số hàng/cột khác biệt. Level hỗ trợ thực tế: **136**. |
| `totalSkewers % 3 != 0` | **61 / 161** | Error | Rất cao. Nhiều level game gốc cố ý thiếu món, yêu cầu cơ chế sinh thêm món (shuffle/conveyor). |
| `ItemCount % 3 != 0` | **47 / 161** | Warning | Bị thiếu lẻ tẻ món cụ thể. Phụ thuộc lớn vào tính năng `shuffleIcons`. |
| `numberOfFood` (1-3) | **0 / 161** | Error | Data nguồn hoàn toàn sạch. |
| `timeLimit <= 0` | **0 / 161** | Error | Data nguồn hoàn toàn sạch. |
| `useDda` == true | **156 / 161** | Info | 97% level gốc có bật cân bằng độ khó động. |
| `shuffleIcons` == true | **160 / 161** | Info | 99% level gốc có bật trộn icon, giải thích cho lỗi thiếu món lẻ tẻ. |

---

## 4. Đề Xuất API Cho LevelDataValidator

*(Không chứa code cài đặt, chỉ khai báo cấu trúc)*

### A. Cấu trúc dữ liệu

```csharp
public enum ValidationSeverity { Info, Warning, Error }

public class ValidationMessage
{
    public ValidationSeverity Severity;
    public string RuleCode; // vd: "ERR_ROW_LIMIT", "WARN_ITEM_MOD3"
    public string Message;
    public string TargetContext; // vd: "Grill[2].Layer[0]", "Order[1]"
}

public class LevelValidationResult
{
    public bool IsValid; // True nếu KHÔNG CÓ Error. Warning vẫn là True.
    public List<ValidationMessage> Messages;
}
```

### B. Hàm API chính

```csharp
public static class LevelDataValidator
{
    // Chạy toàn bộ rule và trả về kết quả
    public static LevelValidationResult Validate(LevelData level, FoodIdMappingData mapping = null);
    
    // Gọi riêng lẻ (dùng cho unit test hoặc tool chuyên sâu)
    public static IEnumerable<ValidationMessage> CheckGridConstraints(LevelData level);
    public static IEnumerable<ValidationMessage> CheckFoodQuantities(LevelData level);
    public static IEnumerable<ValidationMessage> CheckOrders(LevelData level, int totalPossibleMatches);
    public static IEnumerable<ValidationMessage> CheckLocks(LevelData level, FoodIdMappingData mapping);
}
```

### C. Nơi gọi Validator trong Pipeline
1. **Editor Import:** Trong `LevelJsonImporter.cs`, trước khi lưu `LevelData` thành asset `.asset`. Nếu là Error thì không lưu hoặc đưa vào thư mục `_Broken`.
2. **Editor Inspector:** Viết Custom Editor cho `LevelData` để vẽ ra các hộp cảnh báo (HelpBox) ngay trong cửa sổ Unity nếu level có Warning/Error.
3. **Runtime (Tùy chọn):** Trong `GameplayManager.Initialize()`, dùng Assert/LogWarning nếu load phải level rác.

---

## 5. Bảng Test Case Đặc Tả (Tối Thiểu 12 Case)

| ID | Kịch bản dữ liệu (Test Input) | Kết quả kỳ vọng (Expected) | Mã Rule |
| :--- | :--- | :--- | :--- |
| TC-01 | **Level Chuẩn:** 2 bếp, 6 món (3 ID "1", 3 ID "2"), Order Trigger = 1. | ✅ **Pass** (Không có Error/Warning). | N/A |
| TC-02 | **Quá Hàng/Cột:** Rows = 5, Cols = 3. | 🔴 **Error** (Hủy import hoặc báo đỏ). | `ERR_GRID_SIZE` |
| TC-03 | **Quá Số Bếp:** grills.Count = 13. | 🔴 **Error**. | `ERR_GRILL_COUNT` |
| TC-04 | **Tổng Số Xiên Sai:** Tổng cộng có 7 xiên. | 🔴 **Error**. (Chia dư) | `ERR_TOTAL_SKEWERS_MOD_3` |
| TC-05 | **Xiên Cụ Thể Lẻ:** Tổng 6 xiên: 4 xiên "1", 2 xiên "2". | 🟡 **Warning**. (Cần solver hoặc shuffle để qua). | `WARN_ITEM_COUNT_MOD_3` |
| TC-06 | **Món Bị Null:** `layer.itemIds` chứa một chuỗi rỗng "". | 🔴 **Error**. | `ERR_ITEM_NULL_OR_EMPTY` |
| TC-07 | **Lớp Bếp Tràn:** Grill 4x3 (12 slot), layer có 13 xiên. | 🔴 **Error**. | `ERR_LAYER_OVERFLOW` |
| TC-08 | **Khóa Mất Tích:** Bếp bị khóa bởi món ID "Apple" nhưng màn không có "Apple", hoặc ID không có trong map. | 🟡 **Warning** / 🔴 **Error**. | `WARN_LOCK_UNREACHABLE` |
| TC-09 | **Order Quá Món:** Order có `numberOfFood = 4`. | 🔴 **Error**. (Vượt giới hạn UI) | `ERR_ORDER_NUM_FOOD` |
| TC-10 | **Order Trigger Vô Lý:** Bàn có 6 xiên (2 bộ). Order có `matchesToTrigger = 3`. | 🟡 **Warning**. (Order không bao giờ hiện). | `WARN_ORDER_TRIGGER_UNREACHABLE` |
| TC-11 | **Order Âm Giờ:** Order có `timeLimitSeconds = -10`. | 🔴 **Error**. | `ERR_ORDER_TIME_LIMIT` |
| TC-12 | **Level Âm Giờ:** Level có `timeLimitSeconds = 0`. | 🔴 **Error**. | `ERR_LEVEL_TIME_LIMIT` |
| TC-13 | **Info Flags:** Level có `useDda = true`, `shuffleIcons = true`. | 🔵 **Info**. | `INFO_UNSUPPORTED_FLAG` |

---

**Xác nhận:** Đã đọc toàn bộ các file được yêu cầu, thống kê bằng script PowerShell độc lập và xuất tài liệu này. Tuyệt đối không chỉnh sửa bất kỳ file source code, prefab hay JSON nào của dự án.
