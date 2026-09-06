# Mist Execution

- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Loại Kỹ Năng**: `passive`
- **Mã Kỹ Năng**: `mist_execution`
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Misthy](../misthy.md)
- **Sự Kiện Kích Hoạt**: `on_attack`
- **Vị Trí Mục Tiêu**: Hàng sau phe sở hữu

## Mô Tả Kỹ Năng

Mỗi khi Misthy hạ gục mục tiêu bằng chính đòn tấn công của mình, cô kéo Abyssal Mist từ `the_void` vào hàng sau của phe sở hữu, phủ chiến trường bằng sương mù vực sâu.

## Điều Kiện Kích Hoạt

- Misthy phải tấn công một thẻ Character.
- Mục tiêu phải bị đánh bại trong chính đòn tấn công đó của Misthy.
- `the_void` của phe sở hữu phải có Abyssal Mist.
- Hàng sau của phe sở hữu chưa có Abyssal Mist.
- Hàng sau của phe sở hữu phải còn ít nhất một ô trống.

## Hiệu Quả

Mist Execution đưa Abyssal Mist từ `the_void` của phe sở hữu vào một ô trống ở hàng sau phe đó. Nếu không có Abyssal Mist trong `the_void`, hàng sau đã có Abyssal Mist, hoặc hàng sau không còn ô trống, passive không tạo thêm hành động.

## Thẻ Kỹ Năng Liên Kết

- [Abyssal Mist](abyssal_mist.md)
