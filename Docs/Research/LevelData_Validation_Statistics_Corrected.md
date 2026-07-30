# Báo Cáo Thống Kê & Cấu Trúc LevelData (Đã Đính Chính)

**Thực hiện:** Gemini 3.1 Pro (High)
**Thời gian:** 30/07/2026
**Cơ sở dữ liệu:** `2.3.0_BaseCohortFix.json` (161 level)

---

## 1. Các Quy Tắc Cấu Trúc (Từ Source Code & JsonImporter)

Dựa trên việc đọc `LevelJsonImporter.cs`, `LevelData.cs` và `Grill.cs`, các giới hạn thiết kế của Foodie Sizzle được xác nhận lại như sau:

- **Bố cục bàn (Board Layout):** `rows` (tối đa 4) và `cols` (tối đa 3) trong file JSON quyết định số lượng lưới đặt bếp trên màn hình. Số lượng bếp tối đa là `4 x 3 = 12` bếp.
- **Sức chứa của Bếp (Grill Capacity):** Mỗi bếp (`Grill.cs`) cố định có đúng **3 vị trí nấu** (`activeSlots`) và **3 vị trí đĩa chờ** (`waitingSlots`).
- **Giới hạn Layer:** Mỗi `layer` (mảng `foodIds`) trong mảng `foodQueue` của bếp chứa tối đa 3 món (khớp với sức chứa của đĩa/vỉ nướng).
- **Khóa Đặc Biệt (Plus Lock):** Bếp bị khóa với `sourceLockId < 0` là Khóa Đặc Biệt. Gameplay (`Grill.cs`) đã hỗ trợ thuộc tính này (`isSpecialLock = true`), hiển thị nắp đậy, icon Plus và có sẵn hàm `TryUnlockSpecial()`.
- **Shuffle Icons:** Thuộc tính `shuffleIcons` hiện tại chỉ xáo trộn các ID món ăn sẵn có. **Nó không sinh thêm hay làm thay đổi tổng số lượng của từng loại xiên.** 

---

## 2. Thống Kê Tổng Quan (161 Level)

| Hạng mục | Số lượng / Trạng thái |
| :--- | :--- |
| Tổng số Level gốc | **161** |
| Phù hợp giới hạn (<= 12 bếp) | **136** |
| Vi phạm vượt quá 12 bếp | **25** (Error) |
| Vi phạm layer > 3 xiên | **0** (Data cực chuẩn) |
| Level có Khóa Thường (`lock > 0`) | **28** |
| Level có Khóa Đặc Biệt (`lock < 0`) | **138** (Chiếm đa số) |

---

## 3. Các Điều Kiện Chắc Chắn Làm Level Không Thể Thắng

Dựa trên phân tích logic, do Foodie Sizzle **không sinh thêm xiên** ngoài những xiên có sẵn trong dữ liệu (kể cả khi bật `shuffleIcons`), một level sẽ vĩnh viễn **KHÔNG THỂ THẮNG (UNWINNABLE)** nếu rơi vào 1 trong 2 trường hợp sau:

1. **Tổng số xiên không chia hết cho 3** (`totalSkewers % 3 != 0`).
2. **Số lượng của MỘT LOẠI MÓN BẤT KỲ không chia hết cho 3** (`ItemCount % 3 != 0`).

*Lý do:* Để thắng, người chơi phải ghép toàn bộ xiên thành các bộ 3. Nếu tổng số lượng một món (vd: Thịt bò) trên toàn bộ bàn chơi (bao gồm cả các lớp ẩn dưới đĩa chờ) là 4 hoặc 5, người chơi sẽ ghép được 1 bộ ba và vĩnh viễn thừa lại 1 hoặc 2 xiên Thịt bò không thể xóa (Deadlock).

---

## 4. Danh Sách Level Đáng Ngờ (Cần Rà Soát Tính Logic)

Kết quả quét JSON cho thấy có tới **110 / 161 level (68%)** bị lỗi số lượng xiên không chia hết cho 3. Cụ thể:
- **27 level** có tổng số xiên toàn màn không chia hết cho 3.
- **83 level** có tổng số xiên chia hết cho 3, nhưng số lượng từng món lại không chia hết cho 3.

**Ví dụ một số level đáng ngờ bị lẻ món (ItemCount % 3 != 0):**
- `Level 14_New` (Tổng 108 món)
- `Level 15_New` (Tổng 120 món)
- `Level 16_New` (Tổng 114 món)
- `Level 17_New` (Tổng 105 món)
- `Level 18_New` (Tổng 105 món)
- `Level 19_New` (Tổng 120 món)
- `Level 20_New` (Tổng 132 món)

**Bằng chứng và Suy luận logic:** 
Việc 68% số level của game gốc bị lẻ món là một con số quá lớn để gọi là "lỗi data nhập liệu". Điều này dẫn tới một suy luận chắc chắn: 
**Game gốc (FoodieSizzle2) PHẢI CÓ một cơ chế ngầm định để sửa chữa việc thiếu món này khi chơi thực tế.** 
Cơ chế đó có thể là:
- Thuộc tính `useSmartIconSelection` (Đổi icon của món bị che khuất thành món mà người chơi đang thiếu).
- Cơ chế `useDDA` (Tự động thả thêm xiên nếu người chơi bế tắc).
- Băng chuyền (`conveyorRows`) mang theo các xiên bổ sung (Foodie Sizzle hiện tại chưa import mảng băng chuyền).

**Hậu quả hiện tại:** Với bộ source của Foodie Sizzle (chưa có thuật toán Smart Icon Selection hay DDA), 110 level này hiện đang **unplayable (chơi là chắc chắn thua)** trừ phi lạm dụng Booster Box (ăn mất xiên).

---

## 5. Đề Xuất Bộ Validator Tối Thiểu

Dựa trên các sự thật mới, không cần thiết kế một hệ thống phức tạp, chỉ cần chèn các dòng check cực nhẹ vào Editor Script:

Trong `LevelJsonImporter.cs`, hàm `CreateOrUpdateLevel`, sau vòng lặp tổng hợp mảng `ItemCount`:

```csharp
// 1. Kiểm tra giới hạn bếp (Đã có sẵn, chỉ cần chuẩn hóa Max = 12)
if (source.grills.Count > 12) return Error("Vượt 12 bếp");

// 2. Kiểm tra tổng số xiên
if (totalSkewers == 0 || totalSkewers % 3 != 0) 
    return Error($"Tổng số xiên ({totalSkewers}) không chia hết cho 3.");

// 3. Kiểm tra tính chia hết cho 3 của TỪNG món ăn
foreach (var count in itemTypeCounts.Values)
{
    if (count % 3 != 0)
    {
        Debug.LogWarning($"Level có loại món bị lẻ (số lượng = {count}). " +
                         $"Level này sẽ DEADLOCK cho đến khi tính năng Smart Icon / DDA được lập trình.");
        // Lưu ý: Chỉ để Warning, vì nếu chặn Error sẽ rớt mất 68% level của game.
    }
}
```

---
**Tài liệu tham khảo để đối chiếu:**
- *Dữ liệu thực tế (Đọc bằng PowerShell)*: `2.3.0_BaseCohortFix.json`
- *Source Code*: `LevelJsonImporter.cs`, `LevelData.cs`, `Grill.cs`, `GameplayManager.cs`
- Không có bất kỳ file code hay JSON nào bị thay đổi trong quá trình phân tích này.
