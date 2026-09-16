# Quy Tắc Trận Đấu Cơ Bản Cho Enemy AI

> Phân loại tài liệu: Quy tắc chung PvE AI
>
> Áp dụng cho: Tất cả các Enemy AI (Normal Enemy, Elite Enemy, Boss Enemy)

## Tổng Quan

Mọi script Enemy AI trong chế độ PvE phía server Lua (phe Omega) phải tuân thủ nghiêm ngặt các quy tắc trận đấu cốt lõi và giới hạn hệ thống AI tiêu chuẩn dưới đây:

---

## 1. Cấu Hình Bộ Bài & Khởi Tạo Tay Bài (Deck Building & Initial Hand Setup)

- **Load cấu hình từ Backend**: Bộ bài của Enemy AI được tự động xây dựng dựa trên thông tin cấu hình load từ backend thông qua `enemy_key`.
- **Danh sách bài & Mặc định số lượng (`card_count`)**: Danh sách `abilities` trong cấu hình chính là danh sách các lá bài cấu thành bộ bài. Nếu một lá bài không khai báo số lượng `card_count`, số lượng mặc định của lá bài đó trong bộ bài là **3 bản** (ví dụ 9 loại bài x 3 bản = 27 lá).
- **Khởi tạo tay bài (`init_cards` & `choose_card`)**: Khi khởi tạo bài ban đầu (`init_cards`), hệ thống dựa vào metadata `choose_card` (`choose_card_1`, `choose_card_2`, `choose_card_3`) trong cấu hình NPC để đưa đúng các lá bài ưu tiên lên tay bài (`omega_hand`).
- **Giới hạn rút bài hàng lượt**: Mỗi lần thực hiện lượt rút bài từ bộ bài `omega_the_source`, AI tuân theo quy tắc **rút tối đa 2 lá bài** (`lib_battle_common.get_draw_card_count()`).

---

## 2. Ngân sách Triển khai Bài (Deployment Budget)

- **Giới hạn 1 Character/lượt**: Trong một lượt của phe Omega, hàm `deploy(state)` chỉ được chuyển **tối đa 1 lá bài Character** từ `omega_hand` lên tiền tuyến (`omega_front_line`).
- **Ngoại lệ lá Kỹ năng (Ability Cards)**: Các lá bài Kỹ năng (Ability cards như `blood_mist`) không bị giới hạn quy tắc 1 lá/lượt. AI được phép triển khai tất cả các lá Ability có sẵn trên tay xuống hậu tuyến (`omega_back_line`) trong cùng một lượt khi cần thiết.
- **Triệu hồi từ Void**: Các hiệu ứng kỹ năng triệu hồi Character/Structure từ `omega_the_void` (như `Crimson Spire`, `Blood Drain`, `Brute Call`) không thuộc luồng deploy từ tay bài nên không tính vào giới hạn deploy Character.

---

## 3. Quy Trình Kích Hoạt Kỹ Năng (Ability Pipeline)

- **Kích hoạt hợp lệ qua Core Pipeline**: Mọi đòn kích hoạt Ability (chủ động, bị động, Aura) phải thông qua helper tiêu chuẩn `enemy_ai_core.trigger_ability_and_append_actions`.
- **Điều kiện tiền đề**: Kỹ năng chỉ được kích hoạt sau khi lá bài nguồn sở hữu kỹ năng đã được deploy hợp lệ trên bàn đấu.

---

## 4. Quy Trắc Rút Bài & Quản Lý Dung Lượng Tay Bài (Draw Priority & Capacity)

- **Rút bài theo ưu tiên cấu hình**: Mọi lượt rút bài từ `omega_the_source` phải sử dụng `lib_battle_ai.find_omega_source_choice_index(state, source)` để ưu tiên rút các lá theo metadata `choose_card_1`, `choose_card_2`, `choose_card_3`.
- **Duy trì dung lượng tay bài**: Sau mỗi quy trình `deploy`, AI bắt buộc gọi `lib_battle_ai.ensure_omega_hand_draw_capacity(...)` để giải phóng dung lượng tay bài cho lượt rút kế tiếp.

---

## 5. Quy Tắc Tấn Công & Chọn Mục Tiêu (Attack & Targeting Rules)

- **Mặc định Ưu tiên mục tiêu Defender**: Khi chọn mục tiêu tấn công trên tiền tuyến Alpha (`alpha_front_line`), nếu AI không có thuật toán chọn mục tiêu riêng, mặc định sử dụng `enemy_ai_core.pick_alpha_front_line_character_target`:
  1. Ưu tiên lá Character ngửa (`face_up == true`) có **DEF còn lại** (`final_def - total_damage_received`) thấp nhất.
  2. Nếu không có bài ngửa, chọn lá Character úp đầu tiên.
- **Tấn công trực tiếp HP**: AI chỉ thiết lập đòn tấn công trực tiếp `alpha_hp` khi `alpha_front_line` hoàn toàn không còn lá Character nào.
- **Ngưỡng ATK của Attacker**: Không được đưa Character có sát thương tấn công hiệu lực (`get_attack_damage`, bao gồm `final_atk` sau buff/debuff) bằng **0 hoặc 1** vào kế hoạch tấn công. AI phải tiếp tục tìm Character hợp lệ khác; nếu không có, kết thúc lượt Omega.
- **Ngoại lệ Override theo Chiến thuật riêng**: Quy tắc chọn mục tiêu mặc định này **có thể bị ghi đè (override)** nếu một Enemy AI cụ thể sở hữu thuật toán chiến thuật dồn sát thương/combo riêng (ví dụ: thuật toán dứt điểm Misthy của `The Bent Spoon #1` hoặc thuật toán phối hợp 2 giai đoạn Sythra & Mireya của `Bastion Blood`). Nếu AI không có chiến thuật riêng, bắt buộc tuân thủ quy tắc mặc định này.

---

## 6. Quy Tắc Phòng Thủ & Úp Bài (Defense & Face-Down Rules)

- **Giấu thông tin phòng thủ**: Khi ưu tiên phòng thủ, các lá bài công trình hoặc bài phòng thủ (như `Bone Spire`, `Totem Pulse`) được đặt xuống sân ở trạng thái **úp (`face_up = false`)** để giấu thông tin bài trước đối thủ.

---

## 7. Tái Sử Dụng Generic Helpers (Reusing Shared Helpers)

- **Ưu tiên Shared Helpers**: Các thao tác cơ bản trên Lua (như tra cứu định nghĩa lá bài, đếm số lá trên hàng, đếm slot trống, tính sát thương Character, kiểm tra loại bài hoặc trạng thái bàn đấu, ghi nhận plan tấn công) phải ưu tiên tái sử dụng các helper có sẵn trong thư viện dùng chung (`lib_battle_ai`, `enemy_ai_core`, `lib_battle_common`).
- **Phát triển Helper dùng chung**: Khi phát sinh nhu cầu xử lý cơ bản chưa có helper sẵn, phải bổ sung helper tổng quát vào thư viện dùng chung tương ứng (ví dụ `enemy_ai_core`), tránh viết các hàm trùng lặp hoặc đặt tên theo riêng một kỹ năng/lá bài cụ thể. Chỉ giữ các helper nội bộ trong script AI nếu logic đó thực sự duy nhất cho lá bài/chiến thuật đó.
