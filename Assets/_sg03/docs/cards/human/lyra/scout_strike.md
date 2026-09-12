# Scout Strike

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Loại Kỹ Năng**: `passive`
- **Mã Kỹ Năng**: `scout_strike`
- **Chủng Tộc Chính**: **Humans** (định danh kỹ thuật: `human`)
- **Thẻ Nhân Vật Yêu Cầu**: [Lyra](lyra.md)
- **Sự Kiện Kích Hoạt**: `on_attack`
- **Vị Trí Mục Tiêu**: Hàng trước đối thủ

## Mô Tả Kỹ Năng

Sau khi Lyra tấn công, đại bàng chiến của cô lập tức do thám Character đang ẩn mình bên cạnh mục tiêu, khiến đối thủ không thể tiếp tục che giấu thông tin ở điểm yếu của chiến tuyến.

## Điều Kiện Kích Hoạt

- Lyra phải tấn công một thẻ Character ở hàng trước đối thủ.
- Phải có một Character đang úp, chưa bị Expose, đứng kề mục tiêu ban đầu trên cùng hàng.

## Hiệu Quả

Scout Strike ưu tiên Character đứng ngay bên trái mục tiêu ban đầu; nếu không có mục tiêu hợp lệ, nó chọn Character đứng ngay bên phải. Character được chọn bị **Expose**: đặt `face_up = true` và `expose = true`. Nếu không có Character đang úp liền kề, passive không tạo thêm hành động.
