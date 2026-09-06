# Lux Maxima

- **Mã Thẻ Bài**: `lux_maxima`
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Số sao**: 2
- **Chủng Tộc Chính**: **Lightborn**
- **Tộc Nhánh**: Lumina
- **Thẻ Nhân Vật Yêu Cầu**: [Diana](diana.md)

## Mô Tả Kỹ Năng

Diana tập trung ma pháp ánh sáng để chọn một Aura Darkborn trong danh sách bị khắc chế, rồi hóa giải mọi Aura trên sân có cùng code name.

## Điều Kiện Sử Dụng

- Diana phải có mặt trên sân.
- Phải chọn một thẻ Aura Darkborn trong danh sách bị khắc chế ở hàng sau của một trong hai phe.

## Hiệu Quả

- Lấy code name của Aura Darkborn đã chọn.
- Hủy mọi Aura trên cả hai bên sân có đúng code name đó.
- Đưa mỗi thẻ Aura Darkborn bị hủy vào `the_void` của phe sở hữu thẻ đó.
- Hủy toàn bộ buff mà mỗi thẻ Aura Darkborn bị hủy đang áp dụng cho các Character.
- Sau khi hiệu ứng hoàn tất, Lux Maxima được đưa vào `the_void` của phe sở hữu.

Aura Darkborn khác code name với mục tiêu đã chọn không bị ảnh hưởng.

## Danh Sách Thẻ Aura Darkborn Bị Khắc Chế

- [Abyssal Mist](../../../../cards/darkborn/demon/common/abilities/abyssal_mist.md)

## Quy Ước Mở Rộng

Mỗi Aura Darkborn mới bị Lux Maxima khắc chế phải được thêm thành một liên kết riêng trong danh sách trên, thêm code name vào `counterable_darkborn_aura_codes` của cấu hình `lux_maxima`, và đăng ký Aura đó trong `lib_ability_aura`. Khi được chọn, Lux Maxima chỉ hủy toàn bộ Aura có đúng code name của Aura đã chọn; mỗi Aura bị hủy được đưa vào `the_void` của phe sở hữu và mất toàn bộ buff đang áp dụng cho các Character.
