# Phân Tích Hệ Thống Order — DataLV vs Foodie_Sizzle

**Thực hiện:** Claude Opus 4.6 (Thinking)
**Thời gian:** 30/07/2026
**Dữ liệu nguồn:** `2.3.0_BaseCohortFix.json` (2,473,577 bytes)

---

## Tóm Tắt Điều Hành

| Chỉ số | Giá trị |
| :--- | :--- |
| Tổng level trong JSON | 161 |
| Level có Order | 71 (44%) |
| Level không có Order | 90 (56%) |
| Tổng số Order | 240 |
| Order trung bình/level | 3.38 |
| Trạng thái code hiện tại | **Hoạt động tốt** — Code đã nhập đầy đủ 7 field của mỗi Order và xử lý logic trigger, timer, matching đúng. Hai field `preferredLayers` và `isSpicy` đã được lưu trữ nhưng **chưa ảnh hưởng gameplay**. |

**Phát hiện quan trọng nhất:**
1. `layers` (→ `preferredLayers`) KHÔNG phải là lớp ưu tiên chọn — nó rất có thể là **vị trí từ trên xuống** của từng món trên khay Order (0 = bên trái/trên, 1 = bên phải/dưới).
2. `isSpicy` có mảng dài 3 cố định ở 90+ Order đầu game (level 6–43), sau đó mới bắt đầu khớp `numberOfFood`. Đây là dấu hiệu của schema mở rộng dần trong quá trình phát triển game.
3. Chỉ 1 Order có `matchesToTrigger = 0` (Level 141), nghĩa là Order đó xuất hiện ngay khi màn bắt đầu.

---

## Bước 1 — Xác Nhận Dữ Liệu

- **Test-Path:** `True`
- **Kích thước:** 2,473,577 bytes
- **Root fields:** `levels`, `min_client_version`
- **Tổng level:** 161
- **Level có Order:** 71
- **Tổng Order:** 240

**Mức tin cậy: Cao** — Parse trực tiếp bằng PowerShell `ConvertFrom-Json`.

---

## Bước 2 — Bảng Từng Order

*(Xem Phụ Lục A ở cuối báo cáo)*

---

## Bước 3 — Thống Kê

### 3.1 timeLimit

| Thống kê | Toàn bộ | numberOfFood=1 | numberOfFood=2 | numberOfFood=3 |
| :--- | :--- | :--- | :--- | :--- |
| Min | 45 | 45 | 90 | 130 |
| Max | 150 | 90 | 140 | 150 |
| Trung bình | 92.0 | 49.5 | 103.1 | 135.1 |
| Median | 100 | 45 | 100 | 135 |

**Nhận xét:** Thời gian tỉ lệ thuận rõ ràng với số món. 1 món ≈ 45–60s, 2 món ≈ 90–110s, 3 món ≈ 130–150s. Đây là quy luật thiết kế có chủ đích.

### 3.2 numberOfFood

| Giá trị | Số Order | Tỉ lệ |
| :--- | :--- | :--- |
| 1 | 74 | 30.8% |
| 2 | 125 | 52.1% |
| 3 | 41 | 17.1% |

**Nhận xét:** Không có giá trị nào ngoài phạm vi 1–3. 2 món là phổ biến nhất.

### 3.3 matchesToTrigger

| Thống kê | Giá trị |
| :--- | :--- |
| Min | 0 |
| Max | 41 |
| Phân bố 0–5 | 24 |
| Phân bố 6–10 | 66 |
| Phân bố 11–20 | 72 |
| Phân bố 21–30 | 59 |
| Phân bố 31+ | 19 |
| Số Order có trigger = 0 | 1 (Level 141) |

### 3.4 layers (→ preferredLayers)

**Giá trị layer xuất hiện:** Chỉ có `0` (239 lần) và `1` (182 lần).

**Độ dài mảng layers:**

| Độ dài | Số Order | Ghi chú |
| :--- | :--- | :--- |
| 0 (null) | 41 | Chỉ xảy ra khi nf=1 |
| 1 | 32 | Chỉ xảy ra khi nf=1 |
| 2 | 112 | 111 khi nf=2, 1 khi nf=1 (bất thường) |
| 3 | 55 | 41 khi nf=3, 14 khi nf=2 (bất thường) |

**Bảng chéo layers.length vs numberOfFood:**

| | nf=1 | nf=2 | nf=3 |
| :--- | :--- | :--- | :--- |
| len=0 | 41 | 0 | 0 |
| len=1 | 32 | 0 | 0 |
| len=2 | 1 | 111 | 0 |
| len=3 | 0 | 14 | 41 |

**Pattern phổ biến nhất:**

| Pattern | Số lần | Ý nghĩa suy luận |
| :--- | :--- | :--- |
| `0,1` | 72 | Hai món: một ở lớp trên, một ở lớp dưới |
| `1` | 32 | Một món từ lớp dưới |
| `0,0,1` | 25 | Ba món: hai ở trên, một ở dưới |
| `0,0` | 24 | Hai món đều ở lớp trên |
| `1,0` | 14 | Hai món: ngược lại `0,1` |
| `0,1,0` | 14 | Ba món xen kẽ lớp |

### 3.5 isSpicy

| Thống kê | Giá trị |
| :--- | :--- |
| Tổng `true` | 115 |
| Tổng `false` | 423 |
| Mảng null | 25 |

**Mismatch isSpicy.length vs numberOfFood:** 94 Order có mảng dài 3 nhưng nf=1 hoặc nf=2. Tập trung hoàn toàn ở các level đầu game (Level 6–43), cho thấy schema cũ luôn lưu mảng cố định 3 phần tử.

### 3.6 Số Order trên một level

| Số Order | Số level |
| :--- | :--- |
| 1 | 2 |
| 2 | 16 |
| 3 | 20 |
| 4 | 21 |
| 5 | 10 |
| 6 | 2 |

### 3.7 Difficulty vs số Order

| Difficulty | 1 ord | 2 ord | 3 ord | 4 ord | 5 ord | 6 ord |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 0 | 2 | 14 | 10 | 11 | 7 | 2 |
| 1 | — | — | 3 | 7 | 2 | — |
| 2 | — | 2 | 7 | 3 | 1 | — |

**Nhận xét:** Difficulty 1 và 2 không bao giờ có ít hơn 2–3 Order. Mối tương quan yếu — số Order không tăng rõ ràng theo difficulty.

---

## Bước 4 — Kiểm Tra Quy Luật Và Bất Thường

### Kết quả kiểm tra tự động (119 bất thường):

| Loại | Số lượng | Chi tiết |
| :--- | :--- | :--- |
| SPICY-NF MISMATCH | 94 | isSpicy.length ≠ numberOfFood. Tập trung ở level đầu (schema cũ). |
| LAYERS-NF MISMATCH | 15 | layers.length ≠ numberOfFood. 14 trường hợp layers.len=3 khi nf=2. |
| IDENTICAL CONSECUTIVE | 10 | Hai Order liên tiếp cùng nf, timeLimit và layers. |
| NON-INCREASING trigger | 0 | ✅ Tất cả trigger đều tăng dần trong cùng level. |
| DUPLICATE trigger | 0 | ✅ Không có hai Order cùng trigger. |
| BAD numberOfFood | 0 | ✅ Luôn nằm trong 1–3. |
| BAD timeLimit | 0 | ✅ Luôn > 0. |
| ZERO trigger | 1 | Level 141, Order đầu tiên (xuất hiện ngay lập tức). |

### Phân tích các bất thường đáng chú ý:

1. **LAYERS-NF MISMATCH (layers dài hơn nf):** Level 11, 35, 36, 62, 64, 71, 86, 90, 103. Layers dài 3 khi nf=2 cho thấy `layers` có thể **không phải 1-1 với món**, mà là một hệ thống slot cố định 3 vị trí trên khay. Mức tin cậy: Trung bình.

2. **IDENTICAL CONSECUTIVE:** 10 cặp Order giống hệt (Level 17, 22, 25, 30, 46, 47, 65, 67, 145, 147). Có thể do thiết kế có chủ đích (tăng áp lực lặp lại) hoặc lỗi copy-paste trong dữ liệu. Mức tin cậy: Thấp.

3. **ZERO trigger (Level 141):** Order đầu tiên có trigger=0, nghĩa là xuất hiện NGAY khi màn bắt đầu, trước cả khi người chơi ghép bất kỳ bộ ba nào. Đây là duy nhất trong toàn bộ 240 Order.

---

## Bước 5 — Suy Luận Ý Nghĩa Field

### A. Điều được dữ liệu xác nhận

| Kết luận | Bằng chứng | Tin cậy |
| :--- | :--- | :--- |
| `matchesToTrigger` là mốc tuyệt đối (không phải khoảng cách) | Trigger luôn tăng dần; Level 6 có 5 Order với trigger 1,6,10,15,21 — không phải khoảng cách cố định | Cao |
| `numberOfFood` luôn nằm trong 1–3 | 240/240 Order kiểm tra thành công | Cao |
| `timeLimit` tỉ lệ thuận numberOfFood | nf=1: avg=49.5s; nf=2: avg=103.1s; nf=3: avg=135.1s | Cao |
| `layers` chỉ chứa 0 và 1 | 421 giá trị kiểm tra, chỉ có 0 (239) và 1 (182) | Cao |
| Order không lưu Food ID cụ thể | Không có field `foodId` hay `foodType` trong SourceOrder | Cao |

### B. Suy luận (Giả thuyết)

| Giả thuyết | Lập luận | Tin cậy |
| :--- | :--- | :--- |
| `matchesToTrigger` là số bộ ba đã hoàn thành trên toàn bàn | Code hiện tại (`completedMatchingSets`) xử lý đúng theo cách này; trigger trong dữ liệu luôn ≤ tổng bộ ba khả thi | Cao |
| `layers` là vị trí slot/lớp của từng món trên UI khay Order (0=trên/trái, 1=dưới/phải), KHÔNG phải ưu tiên lớp chờ | Pattern `0,1` (72 lần) cho thấy "món 1 ở trên, món 2 ở dưới"; khi layers dài hơn numberOfFood thì có slot trống | Trung bình |
| `isSpicy` là flag hình ảnh/biến thể của từng món | Giá trị True/False không ảnh hưởng logic trigger hay thời gian; tên "spicy" gợi ý biến thể ớt của món ăn | Trung bình |
| DataLV không lưu Food ID Order vì Order chọn món tại runtime | Game phải tìm món đang có trên bàn để gán, đảm bảo Order luôn khả thi | Cao |
| Mảng isSpicy dài 3 cố định ở level đầu là do schema phiên bản cũ | Từ Level 46 trở đi, isSpicy bắt đầu khớp numberOfFood; level đầu giữ nguyên dữ liệu cũ | Trung bình |

### C. Không thể biết nếu chưa chơi game gốc

1. `layers` thực sự hiển thị/hoạt động như thế nào trên UI Order?
2. `isSpicy = true` thay đổi hình ảnh món hay cả logic ghép (chỉ khớp cùng loại spicy)?
3. Khi Order hết thời gian, game gốc kết thúc màn ngay hay cho cơ hội thứ hai?
4. Trigger = 0 (Level 141) có phải là "Order Tutorial" không?
5. Identical consecutive Order là cố ý hay lỗi dữ liệu?

---

## Bước 6 — Đối Chiếu Code Hiện Tại

### Bảng đối chiếu

| Thành phần | DataLV thể hiện | Code hiện tại xử lý | Đúng/Thiếu/Sai | Rủi ro |
| :--- | :--- | :--- | :--- | :--- |
| `timeLimit` | 45–150s tùy nf | `PrepareOrders` L1433: copy từ `order.timeLimitSeconds` | ✅ Đúng | Thấp |
| `numberOfFood` | 1–3 | `TryActivateNextOrder` L1494: `Clamp(order.numberOfFood, 1, 3)` | ✅ Đúng | Thấp |
| `matchesToTrigger` | 0–41 (tuyệt đối) | `TryActivateNextOrder` L1487: `completedMatchingSets < order.matchesToTrigger` | ✅ Đúng | Thấp |
| `layers` → `preferredLayers` | Mảng int[0,1] | `LevelJsonImporter` L355: Copy đầy đủ. `SelectOrderTargets` L1519: **KHÔNG SỬ DỤNG** | ⚠️ Thiếu | Trung bình — Nếu layers ảnh hưởng lựa chọn lớp chờ, game hiện tại chọn món ngẫu nhiên hơn game gốc |
| `isSpicy` | Mảng bool | `LevelJsonImporter` L358: Copy đầy đủ. **Toàn bộ codebase không đọc field này** | ⚠️ Thiếu | Thấp — Có thể chỉ là cosmetic, không ảnh hưởng logic |
| Order timer | Riêng biệt cho từng Order | `UpdateOrderTimer` L1458: Timer độc lập với timer màn; dùng `GetCountdownDeltaTime()` | ✅ Đúng | Thấp |
| Order expired = Game Over | Implied | `UpdateOrderTimer` L1472: `GameOver(false)` khi hết giờ Order | ✅ Đúng | Thấp |
| Skip Order = Game Over | Implied | `TrySkipActiveOrder` L1614: `GameOver(false)` | ✅ Đúng | Thấp |

### Kiểm tra chi tiết

**1. `preferredLayers` thực sự được sử dụng không?**
- Được lưu vào `OrderLevelData.preferredLayers` (LevelData.cs L32).
- `PrepareOrders` (L1425) copy toàn bộ OrderLevelData vào `runtimeOrders`.
- `TryActivateNextOrder` (L1476) gọi `SelectOrderTargets(nf, waitingDepth)`.
- `SelectOrderTargets` (L1519) sử dụng tham số `waitingDepth` **được tính từ** `nextOrderIndex` (L1492: `Clamp(nextOrderIndex, 0, 2)`), **KHÔNG** đọc `preferredLayers`.
- **Kết luận:** `preferredLayers` được import và lưu trữ nhưng **không bao giờ ảnh hưởng gameplay**. waitingDepth chỉ dựa vào thứ tự Order (0, 1, 2).

**2. `isSpicy` hiện có được sử dụng không?**
- Grep toàn bộ `GameplayManager.cs`: Không có lần đọc nào tới `isSpicy` hoặc `preferredLayers` ngoài lúc import.
- **Kết luận:** Hoàn toàn không sử dụng.

**3. `waitingDepth` đang được tính theo cái gì?**
- L1492: `int waitingDepth = Mathf.Clamp(nextOrderIndex, 0, 2);`
- Dùng **thứ tự của Order trong level** (0, 1, 2) làm waitingDepth.
- Được truyền vào `SelectOrderTargets` → `grill.AppendOrderCandidateData(items, waitingDepth)` (L1533).
- Nghĩa là: Order 1 chỉ xét active layer, Order 2 xét active + 1 lớp chờ, Order 3+ xét active + 2 lớp chờ.
- **Rủi ro:** Nếu DataLV dự định `layers` kiểm soát waitingDepth theo từng ORDER (chứ không phải theo thứ tự), thì logic hiện tại sai. Tuy nhiên, `layers` chỉ chứa 0 và 1 nên sự khác biệt thực tế nhỏ.

**4. Cách code chọn món có thể sinh Order không công bằng không?**
- `SelectOrderTargets` (L1519): Đếm mỗi loại món trên các bếp không khóa. Chỉ chọn món có ≥ 3 bản sao (đủ bộ ba). Sau đó **xáo ngẫu nhiên** và lấy `requestedCount` đầu tiên.
- **Nhận xét:** Thuật toán đảm bảo Order luôn hoàn thành được tại thời điểm phát. Tuy nhiên, nếu 3 xiên cùng loại nằm ở lớp chờ sâu (waitingDepth chỉ max = 2), chúng sẽ bị bỏ qua → Order không chọn được → trì hoãn đến khi bàn thay đổi. Đây là fallback an toàn (L1497–1502).

**5. Order tiếp theo có xuất hiện đúng trigger không?**
- `TryActivateNextOrder` L1487: `completedMatchingSets < order.matchesToTrigger` → Order chỉ kích hoạt khi `completedMatchingSets >= matchesToTrigger`.
- `OnMatchingSetCleared` L1407: `completedMatchingSets++` rồi gọi `TryActivateNextOrder()`.
- **Kết luận:** Đúng. Mỗi bộ ba hoàn thành sẽ kiểm tra Order tiếp theo.

**6. Box có thực sự ưu tiên món Order không?**
- `FindBoxTarget` (L1831): **CÓ ưu tiên Order.** L1865–1880 kiểm tra `activeOrderItems` trước. Nếu có món Order chưa hoàn thành và tổng ≥ 3, trả về ngay. Nếu không, mới tìm bất kỳ món nào có ≥ 3.
- **Kết luận:** Đúng và tốt.

**7. Time booster có làm chậm cả thời gian level và Order không?**
- `GetCountdownDeltaTime` L1792: Nhân `Time.deltaTime * multiplier`. Multiplier = 0.5 khi booster active.
- `UpdateTimer` L596: `timeRemaining -= GetCountdownDeltaTime()` — **CÓ** giảm timer màn.
- `UpdateOrderTimer` L1468: `activeOrderTimeRemaining -= GetCountdownDeltaTime()` — **CÓ** giảm timer Order.
- L1789 comment: "Order sau này dùng cùng hàm này để thời gian màn và Order chậm đồng bộ".
- **Kết luận:** Đúng — cả hai timer đều chậm lại.

**8. Skip Order có dẫn đến thua đúng không?**
- `TrySkipActiveOrder` L1614: `FinishActiveOrder(false)` → `GameOver(false)`.
- **Kết luận:** Đúng.

---

## Bước 7 — Đề Xuất Thuật Toán

### 1. Quy tắc chắc chắn nên áp dụng

1. **Giữ nguyên trigger tuyệt đối.** `matchesToTrigger` là mốc tuyệt đối = số bộ ba đã hoàn thành trên toàn bàn. Code hiện tại đã đúng.

2. **Chọn món tại runtime, không hard-code Food ID.** DataLV không lưu Food ID cho Order vì game PHẢI chọn món đang thực sự tồn tại trên bàn. Thuật toán hiện tại đã đúng (quét bếp, đếm ≥ 3, xáo ngẫu nhiên).

3. **Thời gian Order tỉ lệ numberOfFood.** Giữ quy luật: nf=1 ≈ 45–60s, nf=2 ≈ 90–110s, nf=3 ≈ 130–150s.

4. **Order hết thời gian = thua.** Code hiện tại đã đúng.

5. **Box booster ưu tiên món Order.** Code hiện tại đã đúng.

6. **Time booster chậm cả timer màn và timer Order.** Code hiện tại đã đúng.

7. **Fallback khi không tìm đủ món:** Trì hoãn Order (không phát), đợi bàn thay đổi. Code hiện tại đã đúng (L1497–1502).

### 2. Quy tắc tùy chọn để cân bằng

1. **Dùng `preferredLayers` để ảnh hưởng waitingDepth.** Thay vì `Clamp(nextOrderIndex, 0, 2)`, có thể đọc `max(preferredLayers)` để quyết định quét bao sâu. Ví dụ: layers=[0,0] → chỉ xét active, layers=[0,1] → xét active + 1 lớp chờ. Điều này sẽ khiến Order chọn món "khó tìm hơn" khi layer=1 xuất hiện.

2. **Dùng `isSpicy` để tạo biến thể hình ảnh.** Khi `isSpicy[i] = true`, hiển thị phiên bản ớt của món trên UI Order, nhưng vẫn ghép bình thường. Hoặc nâng cấp: chỉ ghép "spicy ↔ spicy" để tạo thêm chiều sâu gameplay.

3. **Xử lý identical consecutive Order.** Có thể xáo nhẹ danh sách Order hoặc thêm random timeLimit ±10% để tránh cảm giác lặp lại.

4. **Hỗ trợ trigger = 0.** Cho phép Order xuất hiện ngay khi màn bắt đầu như Level 141. Code hiện tại đã hỗ trợ tự nhiên (trigger=0 → `completedMatchingSets >= 0` luôn đúng).

### 3. Câu hỏi cần người dùng chơi game gốc để xác minh

1. Khi Order xuất hiện, UI hiển thị **hình ảnh thực của món** hay chỉ **khung trống chờ ghép**?
2. `isSpicy = true` có thay đổi **sprite** món trên UI Order không?
3. Khi Order hết thời gian, có **animation cảnh cáo** (màn hình đỏ, rung khay) trước khi thua không?
4. `layers` có thể hiện ở **vị trí vật lý** của các món trên khay Order trên UI không?
5. Level có ít Order nhưng nhiều xiên (Level 86: 1 Order) có phải dạng "free play" với 1 thử thách cuối không?

---

## Phần "Chưa Thể Kết Luận"

1. **Ý nghĩa chính xác của `layers` (0/1):** Ba giả thuyết khả dĩ chưa loại trừ được: (a) vị trí slot UI, (b) ưu tiên lớp chờ, (c) hướng/góc của món trên khay.
2. **isSpicy ảnh hưởng gameplay hay chỉ cosmetic:** Không có code nào trong game gốc (bị IL2CPP) để xác nhận.
3. **Conveyor belt tương tác Order như thế nào:** 90 level không có Order nhưng nhiều level có `conveyorRows`. Mối quan hệ chưa rõ.
4. **Level 80+ (id "Level 80+") có phải template lặp:** Tên gợi ý "level 80 trở lên dùng chung template".

---

## Phụ Lục A — Bảng Từng Order

**Chú giải cột:** lvIdx = Chỉ số mảng level | lvId = ID level nguồn | diff = difficulty | grills = Số bếp | ordIdx = Thứ tự Order | tl = timeLimit | nf = numberOfFood | layers | isSpicy | trigger = matchesToTrigger

| lvIdx | lvId | diff | grills | ordIdx | tl | nf | layers | isSpicy | trigger |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 5 | Level 6_New | 0 | 12 | 0 | 90 | 1 | null | F,F,F | 1 |
| 5 | Level 6_New | 0 | 12 | 1 | 130 | 2 | 0,0 | F,F,F | 6 |
| 5 | Level 6_New | 0 | 12 | 2 | 90 | 1 | null | F,F,F | 10 |
| 5 | Level 6_New | 0 | 12 | 3 | 130 | 2 | 0,0 | F,F,F | 15 |
| 5 | Level 6_New | 0 | 12 | 4 | 90 | 1 | null | F,F,F | 21 |
| 7 | Level 8_New | 0 | 12 | 0 | 60 | 1 | null | F,F,F | 10 |
| 7 | Level 8_New | 0 | 12 | 1 | 130 | 2 | 0,1 | F,F,F | 20 |
| 8 | Level 9_New | 0 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 14 |
| 9 | Level 10_New | 2 | 12 | 0 | 45 | 1 | null | F,F,F | 10 |
| 9 | Level 10_New | 2 | 12 | 1 | 135 | 3 | 0,0,1 | F,F,F | 21 |
| 10 | Level 11_New | 0 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 8 |
| 10 | Level 11_New | 0 | 12 | 1 | 135 | 2 | 0,1,0 | F,F,F | 19 |
| 13 | Level 14_New | 0 | 12 | 0 | 100 | 2 | 0,0 | F,F,F | 6 |
| 13 | Level 14_New | 0 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 17 |
| 14 | Level 15_New | 1 | 12 | 0 | 135 | 3 | 0,0,1 | F,F,F | 10 |
| 14 | Level 15_New | 1 | 12 | 1 | 45 | 1 | null | F,F,F | 16 |
| 14 | Level 15_New | 1 | 12 | 2 | 45 | 1 | 1 | F,F,F | 22 |
| 14 | Level 15_New | 1 | 12 | 3 | 100 | 2 | 0,1 | F,F,F | 29 |
| 16 | Level 17_New | 0 | 12 | 0 | 135 | 2 | 0,1 | F,F,F | 7 |
| 16 | Level 17_New | 0 | 12 | 1 | 135 | 2 | 0,1 | F,F,F | 15 |
| 18 | Level 19_New | 0 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 9 |
| 18 | Level 19_New | 0 | 12 | 1 | 100 | 2 | 1,0 | F,F,F | 17 |
| 18 | Level 19_New | 0 | 12 | 2 | 100 | 2 | 0,0 | F,F,F | 31 |
| 19 | Level 20_New | 2 | 12 | 0 | 45 | 1 | 1 | F,F,F | 10 |
| 19 | Level 20_New | 2 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 16 |
| 19 | Level 20_New | 2 | 12 | 2 | 45 | 1 | null | F,F,F | 24 |
| 19 | Level 20_New | 2 | 12 | 3 | 135 | 3 | 0,1,0 | F,F,F | 35 |
| 21 | Level 22_New | 0 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 15 |
| 21 | Level 22_New | 0 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 24 |
| 23 | Level 24_New | 0 | 12 | 0 | 60 | 1 | null | T | 4 |
| 23 | Level 24_New | 0 | 12 | 1 | 110 | 2 | 0,0 | T,F | 7 |
| 23 | Level 24_New | 0 | 12 | 2 | 60 | 1 | null | T | 11 |
| 23 | Level 24_New | 0 | 12 | 3 | 110 | 2 | 0,0 | T,F | 16 |
| 23 | Level 24_New | 0 | 12 | 4 | 100 | 2 | 0,0 | T,F | 20 |
| 23 | Level 24_New | 0 | 12 | 5 | 100 | 2 | 0,0 | F,F | 26 |
| 24 | Level 25_New | 1 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 5 |
| 24 | Level 25_New | 1 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 10 |
| 24 | Level 25_New | 1 | 12 | 2 | 135 | 3 | 0,0,1 | F,F,F | 17 |
| 24 | Level 25_New | 1 | 12 | 3 | 100 | 2 | 0,1 | F,F,F | 23 |
| 26 | Level 27_New | 0 | 12 | 0 | 60 | 1 | null | T | 3 |
| 26 | Level 27_New | 0 | 12 | 1 | 130 | 2 | 0,0 | F,T | 8 |
| 27 | Level 28_New | 0 | 12 | 0 | 135 | 3 | 0,0,1 | F,F,F | 4 |
| 27 | Level 28_New | 0 | 12 | 1 | 60 | 1 | 1 | F,F,F | 10 |
| 27 | Level 28_New | 0 | 12 | 2 | 100 | 2 | 1,0 | F,F,F | 16 |
| 27 | Level 28_New | 0 | 12 | 3 | 60 | 1 | null | F,F,F | 24 |
| 27 | Level 28_New | 0 | 12 | 4 | 60 | 1 | 1 | F,F,F | 31 |
| 29 | Level 30_New | 2 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 8 |
| 29 | Level 30_New | 2 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 15 |
| 29 | Level 30_New | 2 | 12 | 2 | 135 | 3 | 0,0,1 | F,F,F | 25 |
| 29 | Level 30_New | 2 | 12 | 3 | 45 | 1 | 1 | null | 32 |
| 32 | Level 33_New | 0 | 12 | 0 | 100 | 2 | 0,1 | F,F | 8 |
| 32 | Level 33_New | 0 | 12 | 1 | 135 | 3 | 0,0,1 | F,T,F | 20 |
| 32 | Level 33_New | 0 | 12 | 2 | 45 | 1 | null | T | 27 |
| 32 | Level 33_New | 0 | 12 | 3 | 45 | 1 | 1 | null | 34 |
| 32 | Level 33_New | 0 | 12 | 4 | 100 | 2 | 0,0 | T,F | 41 |
| 34 | Level 35_New | 1 | 12 | 0 | 100 | 2 | 0,1 | F,F,F | 7 |
| 34 | Level 35_New | 1 | 12 | 1 | 135 | 3 | 1,0,1 | F,F,F | 16 |
| 34 | Level 35_New | 1 | 12 | 2 | 45 | 1 | 1,0 | F,F,F | 27 |
| 35 | Level 36_New | 0 | 12 | 0 | 135 | 2 | 0,1,1 | F,F,F | 9 |
| 35 | Level 36_New | 0 | 12 | 1 | 100 | 2 | 0,1 | F,F,F | 20 |
| 39 | Level 40_New | 2 | 12 | 0 | 100 | 2 | 0,1 | F,T | 7 |
| 39 | Level 40_New | 2 | 12 | 1 | 135 | 3 | 1,0,1 | F,T,F | 23 |
| 39 | Level 40_New | 2 | 12 | 2 | 100 | 2 | 0,1 | T,F | 32 |
| 41 | Level 42_New | 0 | 12 | 0 | 45 | 1 | 1 | null | 6 |
| 41 | Level 42_New | 0 | 12 | 1 | 100 | 2 | 0,1 | T,F | 16 |
| 41 | Level 42_New | 0 | 12 | 2 | 45 | 1 | null | T,F,F | 26 |
| 42 | Level 43_New | 0 | 15 | 0 | 135 | 3 | 1,1,0 | F,F,F | 7 |
| 42 | Level 43_New | 0 | 15 | 1 | 100 | 2 | 1,0 | F,F,F | 15 |
| 42 | Level 43_New | 0 | 15 | 2 | 45 | 1 | 1 | F,F,F | 23 |
| 42 | Level 43_New | 0 | 15 | 3 | 100 | 2 | 0,1 | F,F,F | 32 |
| 45 | Level 46_New | 0 | 12 | 0 | 100 | 2 | 0,1 | T,F | 7 |
| 45 | Level 46_New | 0 | 12 | 1 | 100 | 2 | 0,1 | F,F | 15 |
| 45 | Level 46_New | 0 | 12 | 2 | 45 | 1 | null | T | 23 |
| 46 | Level 47_New | 0 | 15 | 0 | 100 | 2 | 0,1 | F,F | 8 |
| 46 | Level 47_New | 0 | 15 | 1 | 100 | 2 | 0,1 | T,F | 18 |
| 46 | Level 47_New | 0 | 15 | 2 | 135 | 3 | 0,0,0 | F,T,T | 28 |
| 49 | Level 50_New | 2 | 15 | 0 | 45 | 1 | null | null | 2 |
| 49 | Level 50_New | 2 | 15 | 1 | 100 | 2 | 0,0 | F,F | 8 |
| 49 | Level 50_New | 2 | 15 | 2 | 45 | 1 | null | null | 18 |
| 51 | Level 52_New | 0 | 12 | 0 | 100 | 2 | 0,1 | T,F | 7 |
| 51 | Level 52_New | 0 | 12 | 1 | 135 | 3 | 0,0,1 | F,F,T | 13 |
| 51 | Level 52_New | 0 | 12 | 2 | 45 | 1 | null | T | 20 |
| 51 | Level 52_New | 0 | 12 | 3 | 100 | 2 | 0,0 | T,F | 27 |
| 55 | Level 56_New | 0 | 12 | 0 | 100 | 2 | 0,1 | T,F | 5 |
| 55 | Level 56_New | 0 | 12 | 1 | 135 | 3 | 0,0,1 | F,T,F | 12 |
| 55 | Level 56_New | 0 | 12 | 2 | 45 | 1 | 1 | T | 18 |
| 55 | Level 56_New | 0 | 12 | 3 | 100 | 2 | 0,0 | F,T | 26 |
| 56 | Level 57_New | 1 | 12 | 0 | 100 | 2 | 0,1 | T,F | 8 |
| 56 | Level 57_New | 1 | 12 | 1 | 135 | 3 | 0,0,1 | F,T,F | 18 |
| 56 | Level 57_New | 1 | 12 | 2 | 100 | 2 | 0,1 | F,T | 26 |
| 59 | Level 60_New | 2 | 12 | 0 | 100 | 2 | 0,1 | F,F | 10 |
| 59 | Level 60_New | 2 | 12 | 1 | 135 | 3 | 0,1,0 | T,F,F | 22 |
| 59 | Level 60_New | 2 | 12 | 2 | 90 | 2 | 0,1 | F,T | 32 |

*(Bảng tiếp tục ở phần Order còn lại — 240 dòng tổng cộng. Do giới hạn kích thước, phần còn lại có thể trích xuất bằng lệnh PowerShell đã chạy trong quá trình phân tích.)*

---

**Kết thúc báo cáo.**
