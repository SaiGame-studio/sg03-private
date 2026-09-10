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
- Phải chọn một thẻ Aura Darkborn trong danh sách bị khắc chế ở hàng sau của một trong hai phe và thẻ đó phải đang `expose = true`.

## Hiệu Quả

- Chỉ hủy chính Aura Darkborn đã chọn; Aura này phải đang `expose = true`.
- Đưa Aura Darkborn bị hủy vào `the_void` của phe sở hữu thẻ đó.
- Hủy toàn bộ buff mà Aura Darkborn bị hủy đang áp dụng cho các Character.
- Sau khi hiệu ứng hoàn tất, Lux Maxima được đưa vào `the_void` của phe sở hữu.

Aura Darkborn khác code name với mục tiêu đã chọn không bị ảnh hưởng. Aura cùng code name nhưng chưa `expose` cũng không bị ảnh hưởng, kể cả khi đang face-up ở back line.

## Danh Sách Thẻ Aura Darkborn Bị Khắc Chế

- [Abyssal Mist](../../../../cards/darkborn/demon/common/abilities/abyssal_mist.md)

## Quy Ước Mở Rộng

Mỗi Aura Darkborn mới bị Lux Maxima khắc chế phải được thêm thành một liên kết riêng trong danh sách trên, thêm code name vào `counterable_darkborn_aura_codes` của cấu hình `lux_maxima`, và đăng ký Aura đó trong `lib_ability_aura`. Lux Maxima chỉ hủy chính Aura đã chọn khi Aura đó đang `expose = true`; Aura chưa expose không bị ảnh hưởng.
