# Đặc Tả Validator Mở Rộng: Khóa & Order (Foodie_Sizzle)

**Mục tiêu:** Xác định các điều kiện hợp lệ cho Khóa (Lock) và Đơn hàng (Order) sau khi đã áp dụng các quy tắc chuẩn hóa cơ bản của Sol (tối đa 12 bếp, `abs(id)%1000`, tổng xiên và từng loại xiên chia hết cho 3).

---

## 1. Phân Tích Điều Kiện Validator

### A. Validator cho Khóa thường (Normal Lock)
Khóa thường yêu cầu người chơi ghép thành công một bộ ba của món ăn cụ thể (`unlockItemId`) để mở nắp bếp.
- **Điều kiện chắc chắn sai (Error):** Tổng số xiên của `unlockItemId` trong toàn bộ level bằng 0. (Người chơi vĩnh viễn không thể mở được bếp này vì món yêu cầu không tồn tại).
- **Nguy cơ khó / Kẹt vòng lặp (Warning):** Số xiên của `unlockItemId` nằm trong các **bếp không bị khóa** nhỏ hơn 3. Điều này có nghĩa là món dùng để mở khóa lại đang bị giấu một phần hoặc toàn bộ bên dưới các bếp khóa khác. Nếu người chơi không khéo léo hoặc hệ thống sinh ra một vòng lặp khóa (bếp A cần món trong bếp B, bếp B cần món trong bếp A), level có thể không giải được (Deadlock). Tuy nhiên, vì người chơi có Boosters (Box, Refresh) và có cơ chế Smart Icon (nếu làm sau này), đây chỉ tính là cảnh báo.

### B. Validator cho Order
Order là các nhiệm vụ xuất hiện giữa dòng game sau khi người chơi đã ghép được một số lượng bộ ba nhất định.
- **Điều kiện chắc chắn sai (Error):** 
  - `timeLimit <= 0` hoặc `numberOfFood < 1` (Dữ liệu cấu hình sai).
  - Tổng số bộ ba cần ghép để hoàn thành Order (`matchesToTrigger + numberOfFood`) lớn hơn tổng số bộ ba có trong level (`totalSkewers / 3`). Nếu điều này xảy ra, Order sẽ không bao giờ hoàn thành vì game hết xiên trước khi Order kịp xong.
- **Nguy cơ khó (Warning):** Game không có khái niệm "không đủ món phù hợp" ở mức cấu hình JSON vì cơ chế sinh Order sẽ ngẫu nhiên (hoặc dựa vào `preferredLayers`) chọn các món đang có thật trên bàn tại thời điểm Trigger. Miễn là tổng xiên còn lại đủ nhiều, Order sẽ hợp lệ.

---

## 2. Thống Kê Trên Tập Dữ Liệu Nguồn (161 Level)

Sau khi áp dụng các luật lọc cơ bản của Sol (loại bỏ các level có cấu trúc mồ côi hoặc không chia hết cho 3), chúng ta còn **109 level hợp lệ** để chạy Validator mở rộng.

Kết quả quét:
- **Lỗi Thiếu Món Mở Khóa (Error):** 0 level. (Tất cả khóa đều có món tồn tại trong map).
- **Lỗi Order Bất Khả Thi (Error):** 0 level. (Tất cả 71 level có Order đều thỏa mãn `matchesToTrigger + numberOfFood <= totalMatches`).
- **Nguy cơ Deadlock Khóa (Warning):** 2 level (`Level 46_New`, `Level 138`). Cả hai level này đều yêu cầu một món mở khóa nhưng món đó lại có dưới 3 xiên ở các vùng bếp mở (bị kẹt bên trong bếp khóa).

*Kết luận: Dữ liệu của 109 level hợp lệ rất sạch. Không cần loại thêm level nào vì Error = 0. Chỉ cần log Warning cho 2 level có nguy cơ khó.*

---

## 3. Đề Xuất Pseudocode (Tích hợp vào `LevelJsonImporter.cs`)

Chèn logic này vào hàm `TryConvertGrills` hoặc sau khi đã tính toán xong `matchCounts` và `totalSkewers`:

```csharp
// --- 1. Chuẩn bị dữ liệu đếm cho Lock Validator ---
int totalMatches = totalSkewers / 3;
Dictionary<string, int> unlockedMatchCounts = new Dictionary<string, int>();
foreach (var grill in convertedGrills)
{
    if (grill.sourceLockId <= 0) // Bếp không khóa
    {
        foreach (var layer in grill.layers)
        {
            foreach (var itemId in layer.itemIds)
            {
                unlockedMatchCounts.TryGetValue(itemId, out int count);
                unlockedMatchCounts[itemId] = count + 1;
            }
        }
    }
}

// --- 2. Validator Khóa Thường ---
foreach (var grill in convertedGrills)
{
    if (grill.sourceLockId > 0 && !string.IsNullOrEmpty(grill.unlockItemId))
    {
        // Điều kiện Error (Chắc chắn sai)
        if (!matchCounts.ContainsKey(grill.unlockItemId) || matchCounts[grill.unlockItemId] == 0)
        {
            error = $"Khóa yêu cầu món {grill.unlockItemId} nhưng level không có món này.";
            return false; 
        }

        // Điều kiện Warning (Nguy cơ khó)
        unlockedMatchCounts.TryGetValue(grill.unlockItemId, out int unlockedCount);
        if (unlockedCount < 3)
        {
            Debug.LogWarning($"[Level {source.id}] CẢNH BÁO: Món mở khóa {grill.unlockItemId} chỉ có {unlockedCount} xiên ở bếp mở. Nguy cơ kẹt vòng lặp (Deadlock).");
        }
    }
}

// --- 3. Validator Order (nếu gọi trong vòng lặp parse Order) ---
if (source.orders != null)
{
    for (int i = 0; i < source.orders.Count; i++)
    {
        var srcOrder = source.orders[i];
        if (srcOrder == null) continue;

        if (srcOrder.timeLimit <= 0 || srcOrder.numberOfFood < 1)
        {
            error = $"Order {i} cấu hình sai thời gian hoặc số lượng món.";
            return false;
        }

        if (srcOrder.matchesToTrigger + srcOrder.numberOfFood > totalMatches)
        {
            error = $"Order {i} yêu cầu trigger muộn ({srcOrder.matchesToTrigger}) và cần {srcOrder.numberOfFood} món, nhưng tổng level chỉ có {totalMatches} bộ ba.";
            return false;
        }
    }
}
```
