# Quy Tắc Trận Đấu Cơ Bản Cho Enemy AI

> Phân loại tài liệu: Quy tắc chung PvE AI
>
> Áp dụng cho: Tất cả các Enemy AI (Normal Enemy, Elite Enemy, Boss Enemy)

## Tổng Quan

Mọi script Enemy AI trong chế độ PvE phía server Lua (phe Omega) phải tuân thủ nghiêm ngặt các quy tắc trận đấu cốt lõi và giới hạn hệ thống AI tiêu chuẩn dưới đây:

---

## 1. Ngân sách Triển khai Bài (Deployment Budget)

- **Giới hạn 1 Character/lượt**: Trong một lượt của phe Omega, hàm `deploy(state)` chỉ được chuyển **tối đa 1 lá bài Character** từ `omega_hand` lên tiền tuyến (`omega_front_line`).
- **Ngoại lệ lá Kỹ năng (Ability Cards)**: Các lá bài Kỹ năng (Ability cards như `blood_mist`) không bị giới hạn quy tắc 1 lá/lượt. AI được phép triển khai tất cả các lá Ability có sẵn trên tay xuống hậu tuyến (`omega_back_line`) trong cùng một lượt khi cần thiết.
- **Triệu hồi từ Void**: Các hiệu ứng kỹ năng triệu hồi Character/Structure từ `omega_the_void` (như `Crimson Spire`, `Blood Drain`, `Brute Call`) không thuộc luồng deploy từ tay bài nên không tính vào giới hạn deploy Character.

---

## 2. Quy Trình Kích Hoạt Kỹ Năng (Ability Pipeline)

- **Kích hoạt hợp lệ qua Core Pipeline**: Mọi đòn kích hoạt Ability (chủ động, bị động, Aura) phải thông qua helper tiêu chuẩn `enemy_ai_core.trigger_ability_and_append_actions`.
- **Điều kiện tiền đề**: Kỹ năng chỉ được kích hoạt sau khi lá bài nguồn sở hữu kỹ năng đã được deploy hợp lệ trên bàn đấu.

---

## 3. Quy Trắc Rút Bài & Quản Lý Dung Lượng Tay Bài (Draw Priority & Capacity)

- **Rút bài theo ưu tiên cấu hình**: Mọi lượt rút bài từ `omega_the_source` phải sử dụng `lib_battle_ai.find_omega_source_choice_index(state, source)` để ưu tiên rút các lá theo metadata `choose_card_1`, `choose_card_2`, `choose_card_3`.
- **Duy trì dung lượng tay bài**: Sau mỗi quy trình `deploy`, AI bắt buộc gọi `lib_battle_ai.ensure_omega_hand_draw_capacity(...)` để giải phóng dung lượng tay bài cho lượt rút kế tiếp.

---

## 4. Quy Tắc Tấn Công & Chọn Mục Tiêu (Attack & Targeting Rules)

- **Ưu tiên mục tiêu Defender**: Khi chọn mục tiêu tấn công trên tiền tuyến Alpha (`alpha_front_line`), AI sử dụng `enemy_ai_core.pick_alpha_front_line_character_target`:
  1. Ưu tiên lá Character ngửa (`face_up == true`) có **DEF còn lại** (`final_def - total_damage_received`) thấp nhất.
  2. Nếu không có bài ngửa, chọn lá Character úp đầu tiên.
- **Tấn công trực tiếp HP**: AI chỉ thiết lập đòn tấn công trực tiếp `alpha_hp` khi `alpha_front_line` hoàn toàn không còn lá Character nào.

---

## 5. Quy Tắc Phòng Thủ & Úp Bài (Defense & Face-Down Rules)

- **Giấu thông tin phòng thủ**: Khi ưu tiên phòng thủ, các lá bài công trình hoặc bài phòng thủ (như `Bone Spire`, `Totem Pulse`) được đặt xuống sân ở trạng thái **úp (`face_up = false`)** để giấu thông tin bài trước đối thủ.
