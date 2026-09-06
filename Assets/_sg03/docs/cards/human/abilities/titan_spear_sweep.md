# Spear Sweep

- **Loại Thẻ**: [`ability`](../../../04_abilities.md)
- **Mã thẻ bài**: `titan_spear_sweep`
- **Số sao**: 4
- **Chủng Tộc Chính**: **Humans** (định danh kỹ thuật: `human`)
- **Vị trí nhắm mục tiêu**: Hàng trước của phe đối thủ; một Character đồng minh kề Titan
- **Thẻ Nhân Vật Yêu Cầu**: [Titan](../titan.md)

## Mô Tả Kỹ Năng

Titan xoay ngọn giáo khổng lồ của mình, tạo ra một nhát quét năng lượng quét qua chiến tuyến đối thủ. Lực quét quá mạnh tạo thành dư chấn nguy hiểm cho một đồng minh đứng sát bên Titan. Chỉ Ren, người đã quen với nhịp đánh của Titan, có thể né khỏi vùng ảnh hưởng này.

## Điều Kiện Sử Dụng

Titan phải đang có mặt trên chiến trường của phe bạn và chưa hành động trong lượt.

## Hiệu Quả

- Titan gây **260 damage** cho mọi thẻ `character` ở hàng trước của phe đối thủ.
- Dư chấn của đòn quét gây cố định **100 damage** cho một `character` đồng minh đứng cạnh Titan. Ren sẽ né được dư chấn do cô đã quen với đòn tấn công của Titan.
- Sau khi dùng skill, Titan được tính là đã hành động và thẻ Ability được đưa vào `the_void` của phe sử dụng.
