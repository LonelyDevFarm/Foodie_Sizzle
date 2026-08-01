# Foodie Sizzle Refactor Baseline

Ngày ghi nhận: 31/07/2026  
Unity: 6000.3.19f1

## Phạm vi production

- Build flow: `BootScene → HomeScene → GameplayScene`.
- `GameplayScene` là scene gameplay production.
- `SampleScene` không nằm trong Build Settings và được xem là legacy cho đến khi người dùng quyết định xóa.
- Batch nền tảng không thay đổi gameplay, level schema, booster count hay visual.

## Các hành vi phải được giữ nguyên

- [ ] Boot tự chuyển sang Home và chỉ có một AppRoot.
- [ ] Play từ Home mở Gameplay với đúng level đã lưu.
- [ ] Kéo/thả và tap-select/tap-target hoạt động.
- [ ] Match ba, grill lock, waiting layers và unlock hoạt động.
- [ ] Hint, deadlock recovery và shuffle không làm mất/nhân đôi item.
- [ ] Order kích hoạt, hoàn thành, cảnh báo và skip đúng.
- [ ] Box, Refresh, Time và Plus hoạt động; action thất bại không trừ lượt.
- [ ] Pause/resume, app focus pause, restart, win/lose và next level hoạt động.
- [ ] Home/Gameplay có nhạc; toggle Music/Sound/Vibration giữ trạng thái.

## Save key phải tương thích

- `FoodieSizzle.CurrentLevel`
- `FoodieSizzle.Booster.Box`
- `FoodieSizzle.Booster.Refresh`
- `FoodieSizzle.Booster.Time`
- `FoodieSizzle.Booster.Plus`
- `FoodieSizzle.Booster.Count999.v1`
- `Settings_Music`
- `Settings_Sound`
- `Settings_Vibration`

Không đổi hoặc xóa các key trên nếu chưa có save version và migration.

## Dependency baseline

- DOTween Free: 1.3.030, source chính thức Demigiant.
- UniTask: 2.5.11, nhập từ unitypackage chính thức vì máy phát triển không có Git CLI.
- Addressables: 2.7.6 từ Unity Registry.
- Unity Test Framework đã có; project chưa có test assembly riêng.

## Kiểm tra sau mỗi batch

1. Unity compile không có lỗi mới.
2. Không có missing serialized reference liên quan.
3. Chạy smoke checklist cho các flow chịu ảnh hưởng.
4. Ghi rõ phần chưa thể kiểm tra trên Editor hoặc thiết bị thật.
