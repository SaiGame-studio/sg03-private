# Holy Glow

- **Mã Thẻ Bài**: `holy_glow`
- **Số sao**: 1
- **Loại Thẻ**: [`ability`](../../../../04_abilities.md)
- **Chủng Tộc Chính**: **Lightborn**
- **Tộc Nhánh**: Light Elf (định danh kỹ thuật: `elf` / `light_elf`)
- **Vị Trí Nhắm Mục Tiêu**: Player HP của Alpha hoặc Omega; `own_frontline`, `own_backline`, `own_source`, `own_void`
- **Thẻ Nhân Vật Yêu Cầu**: Character Lightborn nữ

---

## ✨ Cơ Chế & Luồng Thực Thi Kỹ Năng

- **Cấu Hình**: `target_positions = { "own_frontline", "own_backline", "own_source", "own_void" }`, `can_target_player_hp = true`
- **Điều Kiện**: Yêu cầu 1 Character Lightborn nữ chưa kích hoạt trên `own_frontline`.

### Các Bước Thực Thi
1. Tìm 1 Character Lightborn nữ chưa kích hoạt trên `own_frontline`.
2. Đánh dấu Character đã chọn là `trigger = true` và lật ngửa bài.
3. Đọc giá trị `hp_restore` từ định nghĩa lá bài `holy_glow`.
4. Hồi phục HP mục tiêu đã chọn (Alpha hoặc Omega, không vượt quá `max_hp`):
   $$\text{new\_hp} = \min(\text{current\_hp} + \text{hp\_restore}, \text{max\_hp})$$
5. Tiêu thụ lá bài `holy_glow` và chuyển từ tay/sân vào mộ `the_void`.
6. Phát Client Action `[side]_card_ability` kèm thông tin hồi máu và `[side]_card_sent_to_void`.
