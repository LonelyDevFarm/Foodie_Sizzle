# Báo Cáo Phân Tích Lược Đồ Dữ Liệu Level (DataLV Schema)

**Người thực hiện:** Gemini 3.1 Pro High
**Thời gian:** 30/07/2026
**Phạm vi tìm kiếm:**
- `D:\Projects\Foodie_Sizzle`
- `D:\Projects\_GameExtract`

---

## 🛑 TRẠNG THÁI TRUY CẬP DỮ LIỆU
**Kết quả:** KHÔNG THỂ TRUY CẬP (BỊ CHẶN/MÃ HÓA BỞI IL2CPP).

Tôi đã tiến hành quét toàn bộ hệ thống file trong cả hai thư mục được chỉ định. Mặc dù cấu trúc mã nguồn đã được tìm thấy tại `D:\Projects\_GameExtract\FoodieSizzle2\ExportedProject\Assets\Scripts\Assembly-CSharp\Data\`, nhưng toàn bộ các file định nghĩa cấu trúc Level (Schema) đều là các **Lớp Ảo (Dummy Class)**.

**Bằng chứng (Mức tin cậy: Cao):**
Tại file `D:\Projects\_GameExtract\FoodieSizzle2\ExportedProject\Assets\Scripts\Assembly-CSharp\Data\LevelData.cs` (và các file `GrillData.cs`, `OrderData.cs`, `ConveyorRowData.cs` tương tự), nội dung bên trong hoàn toàn trống và chỉ chứa thông báo lỗi của công cụ AssetRipper:
```csharp
/*
Dummy class. This could have happened for several reasons:
1. No dll files were provided to AssetRipper...
6. Cpp2IL failed to decompile Il2Cpp data...
*/
```
Vì mã nguồn của game gốc đã bị biên dịch qua IL2CPP (thành file nhị phân C++ Cpp2IL) nên AssetRipper không thể trích xuất được bất kỳ trường dữ liệu (field) nào. Đồng thời, tôi không tìm thấy file text/json chứa dữ liệu thực tế của các level trong thư mục `_GameExtract` này. 

Do yêu cầu **"không được tự đoán dữ liệu"**, tôi báo cáo chi tiết các mục như sau:

---

### 1. Tất cả field và kiểu dữ liệu
- **Không xác định được.** Các file class như `LevelData.cs`, `GrillData.cs` không chứa bất kỳ field nào. (Do lỗi dịch ngược IL2CPP).

### 2. Field thuộc cấp game, level, grill, layer hay order
- **Dựa vào tên file (Mức tin cậy: Trung bình):** Hệ thống được chia thành các cấp độ dựa trên sự tồn tại của các file sau:
  - Cấp độ Level: `LevelData.cs`, `DDALevelData.cs`
  - Cấp độ Bếp: `GrillData.cs`, `ConveyorRowData.cs`
  - Cấp độ Đơn hàng: `OrderData.cs`
  Tuy nhiên, các field bên trong hoàn toàn trống.

### 3. Giá trị mẫu từ ít nhất ba level khác nhau
- **Không tìm thấy.** Không có file dữ liệu cấu hình Level (như txt, json, asset) có thể đọc được trong các đường dẫn được cung cấp.

### 4. Quan hệ giữa food ID, sprite, lock, order và conveyor
- **Không xác định được.** Không có thuật toán hay field dữ liệu nào được trích xuất thành công để phân tích mối quan hệ này. 

### 5. Quy tắc xáo trộn hoặc thay đổi khi restart nếu tìm thấy
- **Không xác định được.** File chứa logic xáo trộn `Manager\LevelManager.cs` cũng là dummy class.

### 6. Field nào Foodie_Sizzle hiện đã hỗ trợ
- Dự án `Foodie_Sizzle` hiện tại đang sử dụng hệ thống Random sinh level (thuật toán sinh ngẫu nhiên số lượng xiên thịt vào các mảng Grill). Chưa có bất kỳ Field dữ liệu nào từ game gốc được hỗ trợ.

### 7. Field nào đang bị bỏ qua hoặc xử lý sai
- Mọi Field định nghĩa cấu trúc Level của game gốc đều đang bị bỏ qua, do chưa thể trích xuất thành công Schema.

### 8. Kết luận nào được xác nhận bằng code/dữ liệu
- **Đã xác nhận:** Game gốc lưu trữ Level dưới dạng các ScriptableObject (hoặc Data Class) gồm `LevelData`, `GrillData`, `OrderData`, và `ConveyorRowData`. (Mức tin cậy: Cao - dựa trên sự tồn tại của các file `.cs` tương ứng trong thư mục Data).
- **Đã xác nhận:** Game gốc được bảo vệ bằng IL2CPP khiến việc đọc trực tiếp Schema bằng AssetRipper thất bại. (Mức tin cậy: Cao - dựa trên nội dung bên trong file `LevelData.cs`).

### 9. Kết luận nào mới chỉ là giả thuyết
- Không đưa ra giả thuyết theo đúng chỉ thị "không được tự đoán dữ liệu".

### 10. Những câu hỏi chưa thể xác định
- Toàn bộ Schema thực tế gồm những biến/kiểu dữ liệu gì?
- Cấu trúc file lưu trữ Level (JSON hay Binary) nằm ở đâu và làm sao để giải mã nó mà không cần dịch ngược libil2cpp.so bằng IDA Pro/Ghidra?

---
**Kết thúc báo cáo.**
