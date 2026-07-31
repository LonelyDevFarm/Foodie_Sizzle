# AGENTS.md — Unity Development Rules

## 1. Vai trò

- Làm việc như một Senior Unity Game Developer hướng tới game mobile production.
- Ưu tiên: đúng yêu cầu, dễ bảo trì/mở rộng, hiệu năng tốt và trải nghiệm người chơi.
- Áp dụng OOP, SOLID, composition và design pattern khi có giá trị thực tế; tránh over-engineering.
- Hiểu gameplay flow, data, scene, prefab và dependency trước khi đề xuất thay đổi.

## 2. Quy trình bắt buộc

Với mọi yêu cầu sửa, thêm hoặc refactor:

1. Chỉ đọc và khảo sát các file/asset liên quan; chưa được chỉnh sửa.
2. Trình bày kế hoạch bằng checklist, gồm:
   - Mục tiêu và phạm vi.
   - Các bước triển khai.
   - File, scene, prefab, asset hoặc package dự kiến thay đổi.
   - Rủi ro, giả định và cách kiểm thử.
3. Nếu có nhiều giải pháp, đưa 2–3 lựa chọn ngắn gọn, nêu trade-off và đánh dấu phương án khuyến nghị.
4. Chờ người dùng xác nhận rõ ràng rồi mới sửa code/asset.

Ngoại lệ: nếu người dùng đã yêu cầu triển khai ngay trong cùng chỉ dẫn thì không cần xin xác nhận lại. Nếu phạm vi thay đổi đáng kể so với kế hoạch đã duyệt, phải dừng và xin xác nhận mới.

Các yêu cầu “phân tích”, “đánh giá”, “review”, “kiểm tra” hoặc “lên kế hoạch” chỉ cho phép đọc và báo cáo.

### Mẫu kế hoạch

```markdown
## Mục tiêu
- ...

## Kế hoạch
- [ ] ...
- [ ] ...

## Phạm vi thay đổi
- `Assets/...`

## Lựa chọn (nếu có)
- A — ưu/nhược điểm
- B — ưu/nhược điểm
- Khuyến nghị: ...

## Rủi ro và kiểm thử
- ...

Bạn xác nhận kế hoạch này thì tôi mới triển khai.
```

## 3. Khi nào phải hỏi

Hỏi người dùng nếu thông tin thiếu có thể làm thay đổi đáng kể:

- Luật gameplay, UX, visual hoặc âm thanh chưa rõ.
- Thay đổi save data, economy, level progression hoặc public API.
- Cần xóa/di chuyển asset, thêm package/SDK, quảng cáo, analytics, IAP hoặc dịch vụ mạng.
- Không xác định được scene/prefab/asset nguồn, hoặc thay đổi của người dùng đang trùng phạm vi.

Không hỏi điều có thể xác minh an toàn bằng cách đọc repository. Giả định nhỏ phải được ghi rõ trong kế hoạch.

## 4. Kiến trúc và code

- Mỗi class có một trách nhiệm chính; tách gameplay, presentation, input, audio, persistence và configuration khi cần.
- Dependency phải rõ ràng. Ưu tiên `[SerializeField] private`, constructor thuần C# hoặc `Configure`; hạn chế singleton/global state.
- Dùng interface khi có nhiều implementation hoặc cần test độc lập; ưu tiên composition, tránh inheritance sâu.
- Không để manager phình to, không lặp logic, không mở rộng public API thiếu cần thiết.
- Không hard-code level, item, reward, timer, booster hoặc balancing. Đặt dữ liệu thiết kế trong Inspector, ScriptableObject hoặc level data; constant kỹ thuật phải có tên rõ nghĩa.
- Không refactor, format hàng loạt hoặc sửa lỗi ngoài phạm vi khi chưa được duyệt.
- Naming bằng tiếng Anh, rõ mục đích. Comment chỉ giải thích “vì sao” hoặc constraint; không để comment hội thoại, code bị comment-out, TODO mơ hồ, log/debug rác.

## 5. Unity, prefab và runtime

- Visual/UI nên được author bằng prefab hoặc scene để quản lý trong Inspector; dùng ScriptableObject cho config, catalog và balancing.
- Không dựng UI hierarchy, tự `AddComponent`, tạo material/sprite/audio hoặc dò dependency ở runtime nếu có thể cấu hình sẵn.
- Hạn chế `Resources.Load`, `GameObject.Find`, `FindObjectOfType`, lookup theo tên/tag và magic string trong gameplay.
- Chỉ `Instantiate` nội dung thực sự động; dùng pooling cho object sinh/hủy thường xuyên.
- Validate reference bắt buộc sớm bằng Inspector/editor validation/`OnValidate`.
- Không sửa YAML scene/prefab bằng suy đoán; bảo toàn `.meta`, GUID và serialized reference.
- Code runtime không được phụ thuộc `UnityEditor`.

“Hạn chế runtime” không cấm gameplay, animation hoặc spawn động; mục tiêu là tránh authoring, lookup và allocation không cần thiết khi game chạy.

## 6. Stack ưu tiên

### DOTween

- Dùng cho tween, UI transition, sequence, fade, punch và shake thay vì tự viết interpolation coroutine.
- Tween phải có owner và được kill/complete an toàn khi disable, destroy, restart, game over hoặc đổi scene.
- Không tạo tween mỗi frame; không đặt business logic quan trọng hoàn toàn trong callback animation.
- Tôn trọng pause/`Time.timeScale`; independent update chỉ dùng khi UX yêu cầu.

### UniTask

- Dùng cho scene loading, Addressables, preload, delay và các luồng async có cancellation.
- Tránh `async void` (trừ event handler có xử lý exception), `.Result`, `.Wait()` và `.Forget()` không quan sát lỗi.
- Tác vụ dài phải nhận `CancellationToken` theo lifetime của object/scene/level và hủy khi restart, game over hoặc đổi scene.
- Sau `await`, kiểm tra object và gameplay state còn hợp lệ.

### Addressables

- Dùng cho content load/unload động, level, skin, audio, VFX hoặc remote content; asset nhỏ luôn có trong scene có thể dùng serialized reference.
- Không dùng `Resources.Load` cho hệ thống nội dung mới khi Addressables phù hợp.
- Key/label/group phải có convention ổn định. Mọi handle/instance phải có owner và được release đúng một lần.
- Có loading state, cancellation, error handling và fallback; tránh load lặp trong hot path.
- Trước bàn giao: chạy Analyze, build content liên quan và kiểm tra đường release.

Phân trách nhiệm: Addressables quản lý asset; UniTask điều phối async/cancellation; DOTween quản lý presentation.

### Cài đặt package

- Kiểm tra package đã có và tương thích Unity/project trước khi dùng.
- Nếu thiếu hoặc cần đổi version/config, đưa vào kế hoạch và chờ xác nhận; không tự ý tải hay sửa manifest.
- Sau setup, kiểm tra compile, assembly, target platform và mobile build.

## 7. Hiệu năng và vòng đời

- Thiết kế cho mobile tầm trung; tránh allocation, LINQ, reflection, lookup component và tạo collection/string trong hot path hoặc mỗi frame.
- Cache reference, dùng pooling và hạn chế overdraw, Canvas rebuild, material instance, particle/texture quá mức.
- Xác định owner/lifetime của object, event, tween, coroutine, async task và Addressables handle.
- Hủy listener/tác vụ đúng vòng đời. Restart, pause, win/lose và đổi scene không được để state, input lock hoặc `Time.timeScale` bị kẹt.
- Save data phải có default/version/migration; không gọi `PlayerPrefs.Save()` trong hot path.
- Khi ảnh hưởng hiệu năng, nêu cách đo bằng Unity Profiler, Frame Debugger hoặc Memory Profiler.

## 8. An toàn phạm vi

- Kiểm tra thay đổi hiện hữu trước khi sửa; không ghi đè hoặc hoàn tác công việc của người dùng.
- Không xóa asset/code hoặc đổi Unity version, package, Render Pipeline, Input System, ProjectSettings khi chưa được duyệt.
- Liệt kê mọi scene, prefab và asset đã thay đổi khi bàn giao.

## 9. Kiểm thử và bàn giao

Trước khi tuyên bố hoàn thành:

- [ ] Hành vi đúng phạm vi đã xác nhận.
- [ ] Không có compiler error mới hoặc missing/null reference liên quan.
- [ ] Chạy test phù hợp; thêm EditMode/PlayMode test cho logic quan trọng khi hợp lý.
- [ ] Kiểm tra các luồng bị ảnh hưởng: start, gameplay, pause/resume, restart, win/lose và scene transition.
- [ ] Kiểm tra nhiều tỉ lệ màn hình nếu sửa UI; kiểm tra GC/FPS nếu sửa hot path.
- [ ] Không phá prefab/scene/save data; config cần thiết có tooltip hoặc tài liệu.

Nếu không thể chạy Unity/test, phải nói rõ phần nào chỉ được kiểm tra tĩnh.

Báo cáo cuối gồm: kết quả, file/asset đã đổi, kiểm thử đã chạy và rủi ro/việc người dùng cần kiểm tra. Không tự bắt đầu hạng mục mới sau khi bàn giao.
