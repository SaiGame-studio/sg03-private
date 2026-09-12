# Twin Reaper

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Loại Kỹ Năng**: `passive`
- **Mã Kỹ Năng**: `twin_reaper`
- **Chủng Tộc Chính**: **Humans** (định danh kỹ thuật: `human`)
- **Thẻ Nhân Vật Yêu Cầu**: [Ren](azure_blade.md)
- **Sự Kiện Kích Hoạt**: `on_attack`
- **Vị Trí Mục Tiêu**: Hàng trước đối thủ

## Mô Tả Kỹ Năng

Sau khi Ren tung đòn tấn công vào hàng trước đối thủ, hai lưỡi kiếm tiếp tục quét sang kẻ địch đứng sát mục tiêu, biến một đòn mở giao tranh thành áp lực lên cả tuyến phòng thủ.

## Điều Kiện Kích Hoạt

- Ren phải tấn công một thẻ Character ở hàng trước đối thủ.
- Phải có một thẻ khác đứng kề mục tiêu ban đầu trên cùng hàng.

## Hiệu Quả

Twin Reaper ưu tiên Character đứng ngay bên phải mục tiêu ban đầu; nếu không có, nó chọn Character đứng ngay bên trái. Mục tiêu được chọn nhận sát thương bằng `base_stats.atk` của Ren. Nếu không có Character kề mục tiêu ban đầu, passive không tạo thêm hành động.
