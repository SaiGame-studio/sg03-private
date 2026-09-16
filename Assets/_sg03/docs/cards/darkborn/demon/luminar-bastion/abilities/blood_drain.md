# Blood Drain

- **Loại Thẻ**: [`ability`](../../../../../04_abilities.md)
- **Loại Kỹ Năng**: `passive`
- **Mã Kỹ Năng**: `blood_drain`
- **Chủng Tộc Chính**: **Darkborn** (định danh kỹ thuật: `darkborn`)
- **Tộc Nhánh**: **Demon**
- **Thẻ Nhân Vật Yêu Cầu**: [Mireya](../mireya.md)
- **Sự Kiện Kích Hoạt**: `on_attack`

## Mô Tả Kỹ Năng

Khi Mireya kết liễu một Character enemy bằng chính đòn tấn công của mình, Blood Drain dùng hai Bone Spire ở hàng trước để hút máu từ chiến binh vừa ngã xuống.

## Điều Kiện Kích Hoạt

- Mục tiêu phải bị đánh bại trong chính đòn tấn công đó của Mireya.
- Hàng trước phe sở hữu phải có đúng hai [Bone Spire](../bone_spire.md). Nếu chỉ có một hoặc không có Bone Spire, Blood Drain không kích hoạt.

## Hiệu Quả

1. Đưa cả hai Bone Spire vào `the_void` của phe sở hữu.
2. Mireya nhận `atk_added = 100` cho đến khi lượt tiếp theo của phe sở hữu bắt đầu.
3. Nếu hàng trước chưa có Blood Spire và `the_void` của phe sở hữu có [Blood Spire](../blood_spire.md), đưa nó vào ô của Bone Spire đầu tiên vừa được dọn ở hàng trước.
4. Nếu hàng trước đã có Blood Spire, không đưa thêm Blood Spire nào ra sân. Hai Bone Spire vẫn bị đưa vào `the_void` và Mireya vẫn nhận ATK tăng thêm.
5. Nếu không có Blood Spire trong `the_void`, hai Bone Spire vẫn bị đưa vào `the_void` và Mireya vẫn nhận ATK tăng thêm.

## Thẻ Liên Kết

- [Mireya](../mireya.md)
- [Bone Spire](../bone_spire.md)
- [Blood Spire](../blood_spire.md)
- [Blood Mist](blood_mist.md)

## Used by AI

- [Bastion Blood](../../../../../pve/normal_enemies/bastion_blood.md)
