# Bloodmight

- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Loại Kỹ Năng**: `active aura`
- **Mã Kỹ Năng**: `bloodmight`
- **Số sao**: 4
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Sythra](../sythra.md)

## Mô Tả Kỹ Năng

Bloodmight trở thành Aura trên chiến trường và cộng `base_stats.atk_added` của chính nó cho mọi Character Darkborn cùng phe với Aura.

## Điều Kiện Kích Hoạt

- Bloodmight phải ở hàng sau của phe sở hữu.
- Hàng trước phe sở hữu phải có Sythra và ít nhất ba Bone Spire.
- Bloodmight phải có `base_stats.atk_added` dương.

## Hiệu Quả

Khi kích hoạt, Bloodmight đưa đúng ba Bone Spire ở hàng trước phe sở hữu vào `the_void` của phe đó, sau đó duy trì Aura. Aura chỉ buff `final_atk` cho Character Darkborn cùng phe; Darkborn của đối thủ không nhận buff. Khi Bloodmight rời chiến trường, toàn bộ buff ATK do Aura này áp dụng bị gỡ.

## Thẻ Liên Kết

- [Sythra](../sythra.md)
- [Bone Spire](../bone_spire.md)
