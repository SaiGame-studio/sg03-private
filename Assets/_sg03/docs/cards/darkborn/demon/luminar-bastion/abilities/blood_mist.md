# Blood Mist

- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Loại Kỹ Năng**: `active aura`
- **Mã Kỹ Năng**: `blood_mist`
- **Số sao**: 3
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Mireya](../mireya.md)

## Mô Tả Kỹ Năng

Blood Mist chỉ kích hoạt khi target được chọn là Mireya và hàng trước phe sở hữu đã có một Blood Spire. Khi kích hoạt, thẻ trở thành Aura trên chiến trường của Mireya; màn sương đỏ sẫm từ Blood Spire phủ lên đội hình đối phương và làm suy yếu khả năng tấn công của Character enemy.

## Điều Kiện Kích Hoạt

- Target phải là [Mireya](../mireya.md).
- Blood Mist phải ở hàng sau của phe sở hữu.
- Hàng trước phe sở hữu phải có ít nhất một [Blood Spire](../blood_spire.md).

## Hiệu Ứng

- Mỗi Character enemy bị giảm `final_atk` theo giá trị `atk_reduced` dương của Blood Mist.
- `final_atk` sau giảm không thấp hơn `0`.
- Khi Blood Mist rời chiến trường, toàn bộ giảm ATK do Aura này áp dụng bị gỡ.

## Kỹ Năng Khắc Chế

- [Lux Maxima](../../../../../lightborn/lumina/diana/lux_maxima.md) có thể khắc chế Blood Mist khi Aura này đang bị lộ diện ở hàng sau.

## Thẻ Liên Kết

- [Mireya](../mireya.md)
- [Blood Spire](../blood_spire.md)
