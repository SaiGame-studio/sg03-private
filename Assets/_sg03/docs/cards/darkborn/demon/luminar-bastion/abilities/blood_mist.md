# Blood Mist

- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Loại Kỹ Năng**: `active aura`
- **Mã Kỹ Năng**: `blood_mist`
- **Số sao**: 3
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Mireya](../mireya.md)

## Mô Tả Kỹ Năng

Blood Mist trở thành Aura trên chiến trường của Mireya. Aura này dùng Bone Spire hút máu của những chiến binh đã ngã xuống trên chiến trường, nâng cấp Bone Spire đó thành Blood Spire. Blood Spire tạo ra màn sương đỏ sẫm phủ lên đội hình đối phương và làm suy yếu khả năng tấn công của Character enemy.

## Hiệu Ứng

- Mỗi Character enemy bị giảm `final_atk` theo giá trị `atk_reduced` dương của Blood Mist.
- `final_atk` sau giảm không thấp hơn `0`.
- Khi Blood Mist rời chiến trường, toàn bộ giảm ATK do Aura này áp dụng bị gỡ.

## Thẻ Liên Kết

- [Mireya](../mireya.md)
- [Bone Spire](../bone_spire.md)
