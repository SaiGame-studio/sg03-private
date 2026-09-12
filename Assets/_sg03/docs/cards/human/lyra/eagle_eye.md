# Eagle Eye

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Số sao**: 2
- **Chủng Tộc Chính**: **Humans** (định danh kỹ thuật: `human`)
- **Vị Trí Nhắm Mục Tiêu**: Một thẻ Character đang úp của đối thủ
- **Thẻ Nhân Vật Yêu Cầu**: [Lyra](lyra.md)

## Mô Tả Kỹ Năng

Lyra phái đại bàng chiến bay qua chiến tuyến của đối thủ. Từ trên cao, nó xác định chính xác thân phận của một thẻ Character đang úp và báo lại cho Lyra, biến yếu tố bất ngờ của đối thủ thành thông tin chiến thuật cho phe bạn.

## Điều Kiện Sử Dụng

Lyra phải đang có mặt trên sới đấu của phe bạn. Chọn 1 thẻ Character đang úp của đối thủ; thẻ Ability không phải là mục tiêu hợp lệ.

## Hiệu Quả

Thẻ Character được chọn bị **Expose**: đặt `face_up = true` và `expose = true`, để toàn bộ thông tin của thẻ hiển thị cho người chơi. Lyra cũng bị **Expose** và ngửa lá (`face_up = true`), nhưng không được đặt vào trạng thái `trigger`. Client nhận action tấn công từ Lyra đến thẻ được chọn để thể hiện rõ Lyra là người triển khai Eagle Eye. Eagle Eye không gây sát thương, không thay đổi chỉ số và không kích hoạt hiệu ứng của thẻ bị lộ.
