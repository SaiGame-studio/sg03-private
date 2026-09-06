# Abyssal Mist

- **Mã Thẻ Bài**: `abyssal_mist`
- **Số sao**: 4
- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Misthy](../misthy.md)
- **Điều Kiện Kích Hoạt**: Misthy đang trên sân và chưa kích hoạt (`trigger = false`).
- **Hiệu Ứng**:
  - Misthy nhận `atk_added = 100`.
  - Mọi Character Darkborn và Natureborn của cả Alpha lẫn Omega nhận `def_added = 50`.
- **Trạng Thái Sau Khi Kích Hoạt**: Misthy có `trigger = true`.
- **Thời Gian Tồn Tại**: Abyssal Mist ở lại trên sân cho đến khi bị một lá bài khác hủy.

---

## Mô Tả

Abyssal Mist là màn sương vực sâu do Misthy triệu hồi. Sương mù bám trên chiến trường, khuếch đại linh lực của Misthy và bao phủ các Character Darkborn cùng Natureborn của cả hai phe.

Sau khi được triển khai, Abyssal Mist không tự biến mất. Nó duy trì trên sân cho đến khi một lá bài khác hủy hiệu ứng này.

## Các Bước Thực Thi

1. Kiểm tra Misthy đang trên sân và chưa kích hoạt.
2. Đặt `trigger = true` cho Misthy.
3. Đưa Abyssal Mist lên sân và áp dụng các hiệu ứng được khai báo trong metadata.
4. Giữ Abyssal Mist trên sân cho đến khi một lá bài khác hủy nó.

## Khi Bị Hủy

- Đưa Abyssal Mist vào `the_void` của phe sở hữu.
- Hủy toàn bộ buff mà Abyssal Mist đang áp dụng cho các Character.

## Thẻ Kỹ Năng Liên Kết

- [Mist Execution](mist_execution.md)

## Danh Sách Thẻ Khắc Chế

- [Lux Maxima](../../../../../cards/lightborn/lumina/diana/lux_maxima.md)
