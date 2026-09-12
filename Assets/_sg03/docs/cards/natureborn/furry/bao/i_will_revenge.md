# I Will Revenge

- **Mã Thẻ Bài**: `i_will_revenge`
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Số sao**: 4
- **Chủng Tộc Chính**: **Natureborn**
- **Tộc Nhánh**: Furry (định danh kỹ thuật: `furry`)
- **Vị Trí Nhắm Mục Tiêu**: [Bao](bao.md) đang là mục tiêu của một đòn tấn công đã được lên kế hoạch
- **Thẻ Nhân Vật Yêu Cầu**: [Bao](bao.md)
- **Thẻ Nhân Vật Được Triệu Hồi**: [Sapphire](sapphire.md)

## Mô Tả Kỹ Năng

I Will Revenge là kỹ năng riêng của Bao. Khi Player HP của đối thủ đã bị giảm vì bất kỳ lý do nào và Bao đang chuẩn bị nhận một đòn tấn công đã được lên kế hoạch sẽ khiến Bao bị đánh bại, người chơi có thể kích hoạt I Will Revenge với chính Bao đang bị tấn công làm mục tiêu.

Khi kích hoạt, nếu Sapphire đang ở `the_void` của phe Bao và có một vị trí liền kề trống bên trái hoặc bên phải Bao, Sapphire được triệu hồi vào vị trí trống đó. Nếu cả hai vị trí liền kề của Bao đều không trống, Sapphire không được triệu hồi.

Sau khi xử lý hiệu ứng, I Will Revenge luôn được đưa vào `the_void`, bất kể Sapphire được triệu hồi thành công hay không.

## Điều Kiện Sử Dụng

- Player HP của đối thủ phải thấp hơn HP tối đa, không phụ thuộc nguyên nhân đã gây ra lượng HP bị giảm.
- Bao phải là mục tiêu của một đòn tấn công đang ở trạng thái planning.
- Sát thương của đòn tấn công đó phải khiến Bao bị đánh bại.
- Chọn chính Bao đang bị tấn công làm mục tiêu của I Will Revenge.

## Cơ Chế & Luồng Thực Thi

1. Xác nhận Player HP của đối thủ đã bị giảm và Bao là mục tiêu của đòn tấn công đang planning.
2. Xác nhận Bao sẽ bị đánh bại sau sát thương của đòn tấn công đó.
3. Kiểm tra Sapphire trong `the_void` của phe Bao.
4. Nếu Sapphire có mặt, kiểm tra hai vị trí liền kề của Bao; ưu tiên vị trí bên trái khi cả hai vị trí đều trống.
5. Nếu có vị trí trống, triệu hồi Sapphire từ `the_void` vào vị trí đó; nếu không có, bỏ qua việc triệu hồi Sapphire.
6. Đưa I Will Revenge vào `the_void` trong mọi trường hợp.
