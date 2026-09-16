# Chiến thuật AI Bastion Blood

> Trạng thái: Đã thiết kế AI
>
> Phân loại tài liệu: Normal Enemy
>
> Entity type trong cấu hình: NPC
>
> Enemy key: `bastion_blood`
>
> Script chính: [`enemy_ai_bastion_blood.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_bastion_blood.lua)

## Tổng quan

`bastion_blood` ([`enemy_ai_bastion_blood.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_bastion_blood.lua)) triển khai chiến thuật phối hợp đặc trưng của tộc Darkborn Demon giữa [`Sythra`](../../cards/darkborn/demon/luminar-bastion/sythra.md), [`Mireya`](../../cards/darkborn/demon/luminar-bastion/mireya.md) và các công trình trụ xương [`Bone Spire`](../../cards/darkborn/demon/luminar-bastion/bone_spire.md) / [`Blood Spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md):

1. **Khởi tạo bộ thẻ (`init_cards`)**: Sau khi load `bastion_blood` entity key từ backend, bài trên tay chắc chắn gồm `sythra`, `mireya`, và `bone_spire`. Mặc định 2 lá `bone_spire` và 1 lá `blood_spire` được đưa sẵn vào `omega_the_void`.
2. **Phòng thủ (`defend`)**: Không có phản ứng kỹ năng phòng thủ riêng (`defend(state)` trả về `nil`), phòng thủ dựa vào việc ẩn bài bằng trạng thái úp (`face_up = false`) và chỉ số DEF cao của `bone_spire` (DEF 250) ở tiền tuyến.
3. **Triển khai Ưu tiên & Dàn dựng Hậu tuyến (`deploy`)**:
   - Ưu tiên deploy ngay `bone_spire` từ tay bài lên `omega_front_line` ở trạng thái **úp ẩn bài (`face_up = false`)** để bảo vệ tiền tuyến.
   - **Triển khai toàn bộ Blood Mist xuống Hậu tuyến**: Khi rút được lá `blood_mist` (dù 1 hay nhiều lá), AI deploy **toàn bộ** xuống hậu tuyến (`omega_back_line`) ở trạng thái **úp (`face_up = false`)**.
   - **Khi Sythra hoặc Mireya bị tiêu diệt**: AI tập trung phòng thủ cầm cự để chờ rút lại `Sythra` hoặc `Mireya` khác. Nếu rút được Character chiến đấu khác (không phải Sythra/Mireya), AI có thể deploy ra tiền tuyến làm mồi dồn sát thương nhưng **bắt buộc giữ lại đủ 2 slot trống ở tiền tuyến** cho `Bone Spire`.
   - **Khi Sythra & Mireya đều đang sống trên tiền tuyến (chưa có Blood Spire)**: Nếu rút thêm `Sythra` hoặc `Mireya`, AI **chỉ triển khai thêm `Sythra`** lên tiền tuyến để gia tăng khả năng dứt điểm enemy (200 ATK & kích passive triệu hồi `Bone Spire`), giữ Mireya duy nhất làm nhân tố kết liễu Blood Drain.
   - **Sau khi triệu hồi thành công Blood Spire**: AI chủ động **triển khai thêm Mireya từ tay bài khi có thể** (khi tiền tuyến còn slot trống) để duy trì hiệu quả màn sương [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md).
4. **Lập kế hoạch tấn công & Kích hoạt Kỹ năng Dự phòng (`plan_attack`)**:
   - **Giai đoạn 1 (Sythra Kill Setup - Crimson Spire)**: Nếu tiền tuyến **không có lá `bone_spire` nào hoặc chỉ mới có 1 `bone_spire`** (do `bone_spire` ban đầu có thể đã bị phe Alpha tiêu diệt), AI điều phối sát thương để `Sythra` tung đòn kết liễu enemy, kích hoạt nội tại [`crimson_spire`](../../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md) nhằm triệu hồi `bone_spire` từ `the_void` ra sân cho đến khi tích lũy đủ 2 `bone_spire` ở tiền tuyến.
   - **Giai đoạn 2 (Mireya Finish - Blood Drain)**: Khi đã đủ 2 `bone_spire` ở tiền tuyến, AI dồn sát thương bào DEF của Defender Alpha xuống ngưỡng dứt điểm, để `Mireya` tung đòn kết liễu cuối cùng. Đòn kết liễu này kích hoạt nội tại [`blood_drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md), đẩy 2 `bone_spire` vào `the_void`, tăng +100 ATK cho Mireya và biến đổi/triệu hồi `blood_spire` (DEF 500) lên tiền tuyến.
   - **Giai đoạn 3 (Kích hoạt Blood Mist & Dự phòng tái kích hoạt)**: Khi `Blood Spire` đã xuất hiện trên tiền tuyến và `Mireya` còn sống, AI lập tức kích hoạt **duy nhất 1 lá `blood_mist` làm Aura chính** trước khi tấn công. Các lá `blood_mist` còn lại tiếp tục giữ úp trên hậu tuyến. Nếu lá `blood_mist` chính bị đối thủ giải trừ/hủy bỏ, AI lập tức **kích hoạt lá `blood_mist` dự phòng** tiếp theo để tái khôi phục Aura.

---

## Used by AI

Các thẻ nhân vật và kỹ năng được AI `bastion_blood` sử dụng trực tiếp trong luồng chiến thuật:

- **Thẻ Nhân Vật (Characters)**:
  - [`Sythra`](../../cards/darkborn/demon/luminar-bastion/sythra.md) — Character chủ lực dồn sát thương & triệu hồi Bone Spire khi kết liễu.
  - [`Mireya`](../../cards/darkborn/demon/luminar-bastion/mireya.md) — Character hỗ trợ dứt điểm & kích hoạt Blood Drain nâng cấp Blood Spire.
  - [`Bone Spire`](../../cards/darkborn/demon/luminar-bastion/bone_spire.md) — Character công trình phòng thủ (deploy ẩn bài `face_up = false`).
  - [`Blood Spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md) — Character công trình cao cấp (mặc định ở `the_void`, triệu hồi bởi Mireya).
- **Kỹ Năng Liên Kết (Abilities)**:
  - [`Crimson Spire`](../../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md) — Passive của Sythra triệu hồi Bone Spire khi dứt điểm.
  - [`Blood Drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md) — Passive của Mireya thu hồi 2 Bone Spire, buff ATK & triệu hồi Blood Spire khi dứt điểm.
  - [`Bloodmight`](../../cards/darkborn/demon/luminar-bastion/abilities/bloodmight.md) — Active ability chủ động liên kết Sythra.
  - [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) — Active ability chủ động liên kết Mireya (deploy úp toàn bộ ở hậu tuyến; kích hoạt ngay 1 lá chính & dự phòng các lá còn lại để kích hoạt lại khi bị giải trừ).

---

## Cấu hình Entity & Bộ Bài

Theo cấu hình NPC Bastion Blood:

| Thuộc tính | Giá trị |
| --- | --- |
| Name | Bastion Blood |
| Rarity | Common |
| Type | NPC |
| Category | Normal Enemy |
| `choose_card_1` | `sythra` |
| `choose_card_2` | `mireya` |
| `choose_card_3` | `bone_spire` |

Danh sách 27 card của Bastion Blood (9 loại, mỗi loại 3 bản):

| Card code | Số lượng | Vai trò |
| --- | ---: | --- |
| `sythra` | 3 | Character chủ lực (200 ATK / 360 DEF) & trigger Crimson Spire |
| `mireya` | 3 | Character hỗ trợ kết liễu (150 ATK / 200 DEF) & trigger Blood Drain |
| `bone_spire` | 3 | Character công trình phòng thủ (0 ATK / 250 DEF), 1 ở Hand & 2 ở Void ban đầu |
| `blood_spire` | 3 | Character công trình cao cấp (0 ATK / 500 DEF), nằm sẵn ở Void |
| `bloodmight` | 3 | Ability kỹ năng chủ động liên kết Sythra |
| `blood_mist` | 3 | Ability kỹ năng liên kết Mireya (dàn dựng toàn bộ ở hậu tuyến, duy trì 1 chính & các lá dự phòng) |
| `skeleton` | 3 | Character chiến đấu bổ trợ |
| `zombie_male` | 3 | Character chiến đấu bổ trợ |
| `zombie_female` | 3 | Character chiến đấu bổ trợ |

---

## 1. Khởi tạo & Triển khai đội hình (`deploy`)

### Thiết lập ban đầu (`init_cards`)

Khi khởi tạo trận đấu với entity key `bastion_blood`:
- Tay bài ban đầu (`omega_hand`) bảo đảm có sẵn 3 lá bài chiến thuật: `sythra`, `mireya`, và `bone_spire`.
- Nghĩa địa (`omega_the_void`) được mặc định khởi tạo có sẵn ít nhất **2 lá `bone_spire`** và **1 lá `blood_spire`**.

### Quy trình triển khai bài (`deploy`)

Chi tiết luồng xử lý deploy trong script AI:

1. **Deploy Bone Spire ẩn bài làm trụ phòng thủ**:
   - Khi phòng thủ, AI ưu tiên deploy `bone_spire` từ `omega_hand` vào slot trống đầu tiên ở `omega_front_line` ở trạng thái **úp ẩn bài (`face_up = false`)**.
2. **Deploy TOÀN BỘ Blood Mist xuống Hậu tuyến (`omega_back_line`)**:
   - Khi rút được bất kỳ lá Ability `blood_mist` nào trên tay (dù có 1 hay nhiều lá), AI triển khai **tất cả** các lá này vào các slot trống ở hậu tuyến (`omega_back_line`) ở trạng thái **úp (`face_up = false`)**. Tất cả các lá `blood_mist` ở hậu tuyến sẽ ở trạng thái chờ sẵn cho chiến thuật chính và dự phòng.
3. **Quy tắc triển khai Sythra & Mireya theo tình huống sân đấu**:
   - **Trường hợp Sythra & Mireya đều đang sống trên tiền tuyến (chưa có Blood Spire)**:
     - Nếu rút thêm `sythra` hoặc `mireya` từ tay bài: **Chỉ triển khai thêm `sythra`** lên tiền tuyến (deploy `sythra` ngửa `face_up = true`) nhằm tối đa hóa tổng sát thương (200 ATK) và tăng cơ hội dứt điểm kích hoạt passive `crimson_spire`. Không deploy thêm Mireya thứ hai trước khi có Blood Spire.
   - **Trường hợp sau khi đã triệu hồi thành công Blood Spire lên tiền tuyến**:
     - Nếu trong tay bài `omega_hand` có lá `mireya` và `omega_front_line` còn slot trống: AI sẽ **ưu tiên triển khai Mireya khi có thể** (deploy ngửa `face_up = true`) nhằm duy trì hiệu ứng màn sương [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) ổn định và hiệu quả hơn trên chiến trường.
   - **Trường hợp Sythra hoặc Mireya bị tiêu diệt (tử trận)**:
     - AI chuyển sang chế độ phòng thủ cầm cự để tích lũy lượt rút bài, chờ rút lại lá `sythra` hoặc `mireya` mới từ bộ bài.
     - Nếu rút được Character chiến đấu khác (không phải Sythra hay Mireya, ví dụ: `skeleton`, `zombie_male`): AI có thể deploy lên tiền tuyến đóng vai trò làm **Attacker mồi dồn sát thương**.
     - **Bắt buộc**: Phải kiểm tra điều kiện `count_empty_front_slots >= 2` trước khi deploy Character ngoài bộ combo. Luôn luôn **dành sẵn ít nhất 2 slot trống trên tiền tuyến** cho 2 lá `Bone Spire` (hoặc slot triệu hồi công trình).
4. **Đảm bảo dung lượng rút bài**:
   - Sau khi hoàn tất lượt deploy, AI gọi `lib_battle_ai.ensure_omega_hand_draw_capacity(...)` để duy trì dung lượng tay bài cho lượt rút tiếp theo.

---

## 2. Phản ứng phòng thủ (`defend`)

Chi tiết mã nguồn nằm tại `enemy_ai_bastion_blood.lua`:

```lua
function defend(state)
    return nil
end
```

Bastion Blood không có phản ứng kỹ năng phòng thủ chủ động riêng. Khả năng phòng thủ của đội hình dựa vào việc **ưu tiên đặt các lá bài phòng thủ (đặc biệt là `bone_spire`) ở trạng thái úp (`face_up = false`)** để giấu thông tin bài phòng thủ và tận dụng chỉ số DEF cao của `bone_spire` (DEF 250) trên tiền tuyến.

---

## 3. Lập kế hoạch tấn công phối hợp & Quản lý Kích hoạt Blood Mist (`plan_attack`)

### Thuật toán điều phối đòn kết liễu & Cơ chế kích hoạt Aura dự phòng

```mermaid
flowchart TD
    A[Bắt đầu plan_attack] --> B{Chưa có Blood Mist ngửa VÀ Hậu tuyến có Blood Mist úp VÀ Tiền tuyến có Blood Spire VÀ Mireya còn sống?}
    B -- CÓ --> C[Lập tức kích hoạt 1 lá Blood Mist úp nhắm mục tiêu vào Mireya!]
    B -- KHÔNG --> D[Chọn Defender Alpha bằng pick_alpha_front_line_character_target]
    C --> D
    
    D --> E{Defender == nil?}
    E -- Có --> F[Tấn công alpha_hp]
    E -- Không --> G[Kiểm tra số lượng Bone Spire trên omega_front_line]
    
    G --> H{Số Bone Spire < 2 (0 hoặc 1 lá) VÀ Void có Bone Spire?}
    
    H -- CÓ: Giai đoạn 1 Sythra --> I{Sythra chưa tấn công có trên tiền tuyến?}
    I -- Có --> J[Tính remaining_def của Defender]
    J --> K{Sythra ATK >= remaining_def?}
    K -- Có --> L[Sythra trực tiếp kết liễu Defender -> Kích hoạt Crimson Spire triệu hồi thêm Bone Spire!]
    K -- Không --> M[Chọn Mồi Sát Thương: Mireya/Attacker khác đánh trước giảm DEF để Sythra dứt điểm]
    I -- Không --> N[Tấn công bằng Attacker thông thường]

    H -- KHÔNG: Giai đoạn 2 Mireya --> O{Đã đủ 2 Bone Spire VÀ Mireya có trên tiền tuyến?}
    O -- Có --> P[Tính remaining_def = defender.final_def - defender.total_damage_received]
    P --> Q{Mireya ATK >= remaining_def?}
    Q -- Có --> R[Mireya tung đòn kết liễu -> Kích hoạt Blood Drain đưa 2 Bone Spire vào Void & triệu hồi Blood Spire!]
    Q -- Không --> S[Sythra hoặc Attacker khác đánh mồi trước để đưa DEF xuống 0 < DEF <= Mireya ATK]
    O -- Không --> N
```

### Chi tiết các bước xử lý tấn công & Kích hoạt Aura Dự phòng

1. **Kích hoạt ngay lập tức & Tái kích hoạt Blood Mist khi bị giải trừ**:
   - Ngay ở đầu lượt xử lý tấn công (trước mọi đòn đánh), AI kiểm tra:
     1. Trên `omega_back_line` hiện **chưa có lá `blood_mist` nào đang ở trạng thái ngửa/hoạt động (active aura)** (hoặc lá `blood_mist` chính trước đó vừa bị quân địch giải trừ/hủy bỏ).
     2. Tiền tuyến `omega_front_line` có ít nhất 1 lá `blood_spire`.
     3. Tiền tuyến `omega_front_line` có lá `mireya` còn sống.
     4. Hậu tuyến `omega_back_line` còn ít nhất một lá `blood_mist` đang ở trạng thái úp (`face_up != true`).
   - Nếu thỏa mãn các điều kiện trên, AI gọi `enemy_ai_core.trigger_ability_and_append_actions` để **kích hoạt ngay lập tức 1 lá `blood_mist` dự phòng** (target là `mireya`), lật ngửa lá này lên thành Aura chính để phủ sương suy giảm ATK của địch. Các lá `blood_mist` úp còn lại tiếp tục giữ úp ở hậu tuyến.

2. **Xác định mục tiêu Defender**:
   - Sử dụng `enemy_ai_core.pick_alpha_front_line_character_target` để chọn mục tiêu chiến đấu ở `alpha_front_line` (ưu tiên Character ngửa có DEF còn lại thấp nhất). Nếu tiền tuyến Alpha trống, tấn công `alpha_hp`.

3. **Giai đoạn 1: Phối hợp cho Sythra kết liễu (Triệu hồi Bone Spire khi có 0 hoặc 1 Bone Spire)**:
   - **Điều kiện**: Hàng trước `omega_front_line` hiện **không có lá `bone_spire` nào (0 lá) hoặc chỉ mới có 1 `bone_spire`** (do `bone_spire` triển khai lúc đầu có thể đã bị đối thủ tiêu diệt trong lượt đánh trước) VÀ trong `omega_the_void` còn ít nhất 1 `bone_spire`.
   - **Mục tiêu**: Để `sythra` thực hiện đòn kết liễu làm kích hoạt passive [`crimson_spire`](../../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md), tự động triệu hồi thêm `bone_spire` từ void ra tiền tuyến cho đến khi xây dựng đủ 2 `bone_spire` phòng thủ.
   - **Xử lý**:
     - Tính `remaining_def = defender.final_def - defender.total_damage_received`.
     - Nếu `sythra.atk >= remaining_def`: Cho Sythra trực tiếp tấn công kết liễu.
     - Nếu `sythra.atk < remaining_def`: Cho Mireya hoặc Character khác tấn công trước (đảm bảo không lỡ tay dứt điểm target) để bào DEF xuống ngưỡng `0 < remaining_def <= sythra.atk`, sau đó nhường lượt kết liễu cho Sythra.

4. **Giai đoạn 2: Phối hợp cho Mireya kết liễu (Kích hoạt Blood Drain & Nâng cấp Blood Spire)**:
   - **Điều kiện**: Hàng trước `omega_front_line` đã có **đủ 2 `bone_spire`** và `mireya` đang sẵn sàng trên tiền tuyến.
   - **Mục tiêu**: Để `mireya` thực hiện đòn kết liễu mục tiêu, kích hoạt passive [`blood_drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md).
   - **Xử lý**:
     - Tính `remaining_def = defender.final_def - defender.total_damage_received`.
     - Nếu `mireya.atk >= remaining_def`: Mireya lập tức ra đòn kết liễu mục tiêu.
     - Nếu `mireya.atk < remaining_def`: Sử dụng Sythra (200 ATK) hoặc Attacker khác đánh trước làm mồi giảm DEF, đưa `remaining_def` của mục tiêu xuống ngưỡng `0 < remaining_def <= mireya.atk` (150 ATK). Khi mục tiêu đã ở ngưỡng tử ế, Mireya xuất chiêu kết liễu.
     - **Kết quả Blood Drain**: Khi Mireya dứt điểm thành công, 2 lá `bone_spire` ở tiền tuyến tự động thu hồi vào `the_void`, Mireya được buff +100 ATK, và 1 lá [`blood_spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md) (DEF 500) được triệu hồi từ `the_void` lên tiền tuyến thay thế vị trí trụ xương.

---

## Tóm tắt thuật toán Lua

```text
INIT CARDS:
  1. Tay bài omega_hand đảm bảo chứa: sythra, mireya, bone_spire.
  2. Nghĩa địa omega_the_void mặc định nạp sẵn: 2x bone_spire, 1x blood_spire.

DEPLOY:
  1. Nếu hand có bone_spire -> Deploy úp (face_up = false) lên omega_front_line để ưu tiên ẩn bài phòng thủ.
  2. Nếu hand có blood_mist (1 hoặc nhiều lá) -> Deploy TOÀN BỘ các lá blood_mist ở trạng thái úp (face_up = false) xuống omega_back_line.
  3. Kiểm tra trạng thái sân đấu:
     - Nếu đã triệu hồi thành công BLOOD SPIRE lên tiền tuyến:
       + Ưu tiên deploy Mireya từ hand khi tiền tuyến còn slot trống để duy trì hiệu quả Blood Mist.
     - Nếu chưa có Blood Spire và Sythra & Mireya đều ĐANG SỐNG:
       + Nếu hand có Sythra -> Deploy thêm Sythra (face_up = true) để tăng sát thương kết liễu (200 ATK).
       + Không deploy thêm Mireya thứ 2.
     - Nếu Sythra HOẶC Mireya BỊ CHẾT (tử trận):
       + Vào trạng thái cầm cự phòng thủ, chờ rút lại Sythra/Mireya từ bộ bài.
       + Nếu hand có Character chiến đấu khác (non-Sythra/Mireya): Deploy làm mồi sát thương (Setup Attacker), nhưng BẮT BUỘC chừa đủ 2 slot trống trên tiền tuyến cho 2 Bone Spire.
  4. Gọi ensure_omega_hand_draw_capacity.

DEFEND:
  Trả về nil (không có phản ứng kỹ năng; phòng thủ ẩn bài nhờ face_up = false của bone_spire).

PLAN ATTACK:
  1. Kiểm tra kích hoạt / tái kích hoạt Blood Mist ngay lập tức:
     - Nếu chưa có blood_mist nào ngửa AND back_line còn blood_mist úp AND front_line có blood_spire AND front_line có mireya còn sống:
       -> Gọi trigger_ability kích hoạt 1 lá blood_mist úp làm Aura chính ngay lập tức trước khi tấn công.
  2. Chọn Defender Alpha bằng pick_alpha_front_line_character_target.
  3. Nếu không có Defender -> Tấn công alpha_hp.
  4. Nếu tiền tuyến KHÔNG CÓ (0 lá) HOẶC CHỈ CÓ 1 Bone Spire (do có thể bị đối thủ tiêu diệt trước đó) VÀ Void còn Bone Spire:
     - Mục tiêu: Sythra kết liễu để kích hoạt Crimson Spire.
     - Nếu remaining_def <= Sythra ATK (200) -> Sythra dứt điểm.
     - Nếu remaining_def > Sythra ATK -> Dùng Mireya/Attacker khác đánh mồi trước để hạ DEF, sau đó Sythra dứt điểm.
  5. Nếu tiền tuyến đã có ĐỦ 2 Bone Spire VÀ Mireya sẵn sàng:
     - Mục tiêu: Mireya kết liễu để kích hoạt Blood Drain.
     - Nếu remaining_def <= Mireya ATK (150) -> Mireya dứt điểm.
     - Nếu remaining_def > Mireya ATK -> Dùng Sythra/Attacker khác đánh mồi trước hạ DEF xuống <= 150, sau đó Mireya dứt điểm.
     - Blood Drain tự động đẩy 2 Bone Spire vào Void, buff +100 ATK cho Mireya và triệu hồi Blood Spire (DEF 500) ra tiền tuyến.
  6. Fallback: Nếu không thỏa mãn combo -> Tấn công bằng Attacker thông thường.
```
