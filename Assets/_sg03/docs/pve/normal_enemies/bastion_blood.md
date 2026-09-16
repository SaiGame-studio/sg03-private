# Chiến thuật AI Bastion Blood

## Thông Tin Enemy

> Trạng thái: Đã thiết kế AI
>
> Phân loại tài liệu: Normal Enemy
>
> Entity type trong cấu hình: Enemy
>
> Enemy key: `bastion_blood`
>
> Script chính: [`enemy_ai_bastion_blood.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_bastion_blood.lua)

> Lực chiến dự kiến: **11,420 điểm**
>
>   - Điểm bộ bài: 9,360
>   - Điểm choose: 910
>   - Điểm Void: 250
>   - Điểm chiến thuật: 900
>
> Xem [công thức dùng chung](../enemy_power_score.md#công-thức).

> **Quy tắc cơ bản**: Xem [Quy Tắc Trận Đấu Cơ Bản Cho Enemy AI](../enemy_ai_basic_rules.md).


## Tổng quan

`bastion_blood` ([`enemy_ai_bastion_blood.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_bastion_blood.lua)) triển khai chiến thuật phối hợp đặc trưng của tộc Darkborn Demon giữa [`Sythra`](../../cards/darkborn/demon/luminar-bastion/sythra.md), [`Mireya`](../../cards/darkborn/demon/luminar-bastion/mireya.md) và các công trình trụ xương [`Bone Spire`](../../cards/darkborn/demon/luminar-bastion/bone_spire.md) / [`Blood Spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md):

1. **Khởi tạo bộ thẻ (`init_cards`)**: Metadata entity chọn `sythra`, `blood_mist` và `mireya`; entity cũng chỉ định 2 lá `bone_spire` cho `omega_the_void`.
2. **Phòng thủ (`defend`)**: Không có phản ứng kỹ năng phòng thủ riêng (`defend(state)` trả về `nil`), phòng thủ dựa vào việc ẩn bài bằng trạng thái úp (`face_up = false`) của `bone_spire` ở tiền tuyến.
3. **Triển khai Ưu tiên & Dàn dựng Hậu tuyến (`deploy`)**:
   - Ưu tiên deploy ngay `bone_spire` từ tay bài lên `omega_front_line` ở trạng thái **úp ẩn bài (`face_up = false`)** để bảo vệ tiền tuyến (tối đa 1 Character/lượt).
   - **Triển khai toàn bộ Blood Mist xuống Hậu tuyến**: Khi rút được lá `blood_mist` (dù 1 hay nhiều lá trên tay), AI deploy **toàn bộ** xuống hậu tuyến (`omega_back_line`) ở trạng thái **úp (`face_up = false`)** ngay trong lượt triển khai đó (bài Ability không bị giới hạn 1 lá/lượt).
   - **Khi Sythra hoặc Mireya bị tiêu diệt**: AI tập trung phòng thủ cầm cự để chờ rút lại `Sythra` hoặc `Mireya` khác. Nếu rút được Character chiến đấu khác (không phải Sythra/Mireya), AI có thể deploy ra tiền tuyến làm mồi dồn sát thương nhưng **bắt buộc giữ lại đủ 2 slot trống ở tiền tuyến** cho `Bone Spire`.
   - **Khi Sythra & Mireya đều đang sống trên tiền tuyến (chưa có Blood Spire)**: Nếu rút thêm `Sythra` hoặc `Mireya`, AI **chỉ triển khai thêm `Sythra`** (1 Character/lượt) lên tiền tuyến để tăng cơ hội dứt điểm và kích passive triệu hồi `Bone Spire`, giữ Mireya duy nhất làm nhân tố kết liễu Blood Drain.
   - **Sau khi triệu hồi thành công Blood Spire**: AI chủ động **triển khai thêm Mireya từ tay bài khi có thể** (khi tiền tuyến còn slot trống) để duy trì hiệu quả màn sương [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md).
4. **Lập kế hoạch tấn công & Kích hoạt Kỹ năng Dự phòng (`plan_attack`)**:
   - **Giai đoạn 1 (Sythra Kill Setup - Crimson Spire)**: Nếu tiền tuyến **không có lá `bone_spire` nào hoặc chỉ mới có 1 `bone_spire`** (do `bone_spire` ban đầu có thể đã bị phe Alpha tiêu diệt), AI điều phối sát thương để `Sythra` tung đòn kết liễu enemy, kích hoạt nội tại [`crimson_spire`](../../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md) nhằm triệu hồi `bone_spire` từ `the_void` ra sân cho đến khi tích lũy đủ 2 `bone_spire` ở tiền tuyến.
   - **Giai đoạn 2 (Mireya Finish - Blood Drain)**: Khi đã đủ 2 `bone_spire` ở tiền tuyến, AI dồn sát thương để `Mireya` tung đòn kết liễu cuối cùng. Đòn kết liễu này kích hoạt nội tại [`blood_drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md), đẩy 2 `bone_spire` vào `the_void`, tăng sức tấn công cho Mireya và triệu hồi `blood_spire` lên tiền tuyến.
   - **Giai đoạn 3 (Kích hoạt Blood Mist & Dự phòng tái kích hoạt)**: Khi `Blood Spire` đã xuất hiện trên tiền tuyến và `Mireya` còn sống, AI lập tức kích hoạt **duy nhất 1 lá `blood_mist` làm Aura chính** trước khi tấn công. Các lá `blood_mist` còn lại tiếp tục giữ úp trên hậu tuyến. Nếu lá `blood_mist` chính bị đối thủ giải trừ/hủy bỏ, AI lập tức **kích hoạt lá `blood_mist` dự phòng** tiếp theo để tái khôi phục Aura.

---

## Used by AI

Các thẻ nhân vật và kỹ năng được AI `bastion_blood` sử dụng trực tiếp trong luồng chiến thuật:

- **Thẻ Nhân Vật (Characters)**:
  - [`Sythra`](../../cards/darkborn/demon/luminar-bastion/sythra.md) — Character chủ lực dồn sát thương & triệu hồi Bone Spire khi kết liễu.
  - [`Mireya`](../../cards/darkborn/demon/luminar-bastion/mireya.md) — Character hỗ trợ dứt điểm & kích hoạt Blood Drain nâng cấp Blood Spire.
  - [`Bone Spire`](../../cards/darkborn/demon/luminar-bastion/bone_spire.md) — Character công trình phòng thủ (deploy ẩn bài `face_up = false`).
  - [`Blood Spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md) — Character công trình cao cấp được dùng trong chuỗi chiến thuật.
  - `kira`, `misthy`, `zombie_female`, `zombie_male` — Character bổ trợ có trong danh sách Ability của entity.
- **Kỹ Năng Liên Kết (Abilities)**:
  - [`Crimson Spire`](../../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md) — Passive của Sythra triệu hồi Bone Spire khi dứt điểm.
  - [`Blood Drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md) — Passive của Mireya thu hồi 2 Bone Spire, tăng sức tấn công và triệu hồi Blood Spire khi dứt điểm.
  - [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) — Active ability chủ động liên kết Mireya (deploy úp toàn bộ ở hậu tuyến cùng lúc; kích hoạt ngay 1 lá chính & dự phòng các lá còn lại để kích hoạt lại khi bị giải trừ).

---

## Cấu hình Entity & Bộ Bài

Theo dữ liệu entity Bastion Blood:

| Thuộc tính | Giá trị |
| --- | --- |
| Name | Bastion Blood |
| Rarity | Common |
| Type | Enemy |
| Category | Normal Enemy |
| `choose_card_1` | `sythra` |
| `choose_card_2` | `blood_mist` |
| `choose_card_3` | `mireya` |
| `void_card_1` | `bone_spire` |
| `void_card_2` | `bone_spire` |
| Drop Pack IDs | Win Game Pack; Win Items |

Danh sách 27 card của Bastion Blood (9 loại, mỗi loại 3 bản theo `ENEMY_CARD_COUNT_DEFAULT`):

| Card code | Số lượng | ATK | DEF | Điểm cơ bản | Vai trò |
| --- | ---: | ---: | ---: | ---: | --- |
| [`mireya`](../../cards/darkborn/demon/luminar-bastion/mireya.md) | 3 | 150 | 200 | 1,050 | Character liên kết Blood Drain |
| [`sythra`](../../cards/darkborn/demon/luminar-bastion/sythra.md) | 3 | 200 | 360 | 1,680 | Character liên kết Crimson Spire |
| [`bone_spire`](../../cards/darkborn/demon/luminar-bastion/bone_spire.md) | 3 | 0 | 250 | 750 | Character công trình phòng thủ |
| [`blood_spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md) | 3 | 0 | 500 | 1,500 | Character công trình trong chuỗi Blood Drain |
| [`blood_mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) | 3 | 0 | 0 | 0 | Ability Aura; không có `base_stats.atk` hoặc `base_stats.def`, hiệu ứng được chấm ở chiến thuật |
| [`kira`](../../cards/darkborn/demon/common/kira.md) | 3 | 140 | 160 | 900 | Character chiến đấu bổ trợ |
| [`misthy`](../../cards/darkborn/demon/common/misthy.md) | 3 | 200 | 400 | 1,800 | Character chiến đấu bổ trợ |
| [`zombie_female`](../../cards/darkborn/undead/common/zombie_female.md) | 3 | 50 | 200 | 750 | Character chiến đấu bổ trợ |
| [`zombie_male`](../../cards/darkborn/undead/common/zombie_male.md) | 3 | 70 | 240 | 930 | Character chiến đấu bổ trợ |
| **Tổng** | **27** | **2,430** | **6,930** | **9,360** | Xem [cách tính lực chiến](../enemy_power_score.md) |

---

## 1. Khởi tạo & Triển khai đội hình (`deploy`)

### Thiết lập ban đầu (`init_cards`)

Khi khởi tạo trận đấu với entity key `bastion_blood`, metadata entity chỉ định:
- `choose_card_1`: `sythra`.
- `choose_card_2`: `blood_mist`.
- `choose_card_3`: `mireya`.
- `void_card_1` và `void_card_2`: mỗi trường là một `bone_spire`.

Không có `void_card_3` trong metadata entity được cung cấp; vì vậy tài liệu này không xem `blood_spire` là lá được nạp sẵn bởi metadata đó.

### Quy trình triển khai bài (`deploy`)

Chi tiết luồng xử lý deploy trong script AI:

1. **Deploy Bone Spire ẩn bài làm trụ phòng thủ**:
   - Khi phòng thủ, AI ưu tiên deploy `bone_spire` từ `omega_hand` vào slot trống đầu tiên ở `omega_front_line` ở trạng thái **úp ẩn bài (`face_up = false`)** (tối đa 1 Character/lượt).
2. **Deploy TOÀN BỘ Blood Mist xuống Hậu tuyến (`omega_back_line`)**:
   - Khi rút được bất kỳ lá Ability `blood_mist` nào trên tay (dù có 1 hay nhiều lá), AI triển khai **toàn bộ** các lá này cùng lúc vào các slot trống ở hậu tuyến (`omega_back_line`) ở trạng thái **úp (`face_up = false`)**. Tất cả các lá `blood_mist` ở hậu tuyến sẽ ở trạng thái chờ sẵn cho chiến thuật chính và dự phòng.
3. **Quy tắc triển khai Sythra & Mireya theo tình huống sân đấu**:
   - **Trường hợp Sythra & Mireya đều đang sống trên tiền tuyến (chưa có Blood Spire)**:
     - Nếu rút thêm `sythra` hoặc `mireya` từ tay bài: **Chỉ triển khai thêm `sythra`** lên tiền tuyến (deploy `sythra` ngửa `face_up = true`, 1 Character/lượt) nhằm tăng cơ hội dứt điểm kích hoạt passive `crimson_spire`. Không deploy thêm Mireya thứ hai trước khi có Blood Spire.
   - **Trường hợp sau khi đã triệu hồi thành công Blood Spire lên tiền tuyến**:
     - Nếu trong tay bài `omega_hand` có lá `mireya` và `omega_front_line` còn slot trống: AI sẽ **ưu tiên triển khai Mireya khi có thể** (deploy ngửa `face_up = true`, 1 Character/lượt) nhằm duy trì hiệu ứng màn sương [`Blood Mist`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) ổn định và hiệu quả hơn trên chiến trường.
   - **Trường hợp Sythra hoặc Mireya bị tiêu diệt (tử trận)**:
     - AI chuyển sang chế độ phòng thủ cầm cự để tích lũy lượt rút bài, chờ rút lại lá `sythra` hoặc `mireya` mới từ bộ bài.
     - Nếu rút được Character chiến đấu khác trong danh sách Ability của entity (không phải Sythra hay Mireya, ví dụ: `kira`, `misthy`, `zombie_male`): AI có thể deploy (1 Character/lượt) lên tiền tuyến đóng vai trò làm **Attacker mồi dồn sát thương**.
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

Bastion Blood không có phản ứng kỹ năng phòng thủ chủ động riêng. Khả năng phòng thủ của đội hình dựa vào việc **ưu tiên đặt các lá bài phòng thủ (đặc biệt là `bone_spire`) ở trạng thái úp (`face_up = false`)** để giấu thông tin bài phòng thủ trên tiền tuyến.

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
     - Nếu sát thương của Sythra đủ: Cho Sythra trực tiếp tấn công kết liễu.
     - Nếu sát thương của Sythra chưa đủ: Cho Mireya hoặc Character khác tấn công trước để mở đường cho Sythra kết liễu.

4. **Giai đoạn 2: Phối hợp cho Mireya kết liễu (Kích hoạt Blood Drain & Nâng cấp Blood Spire)**:
   - **Điều kiện**: Hàng trước `omega_front_line` đã có **đủ 2 `bone_spire`** và `mireya` đang sẵn sàng trên tiền tuyến.
   - **Mục tiêu**: Để `mireya` thực hiện đòn kết liễu mục tiêu, kích hoạt passive [`blood_drain`](../../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md).
   - **Xử lý**:
     - Tính `remaining_def = defender.final_def - defender.total_damage_received`.
     - Nếu sát thương của Mireya đủ: Mireya lập tức ra đòn kết liễu mục tiêu.
     - Nếu sát thương của Mireya chưa đủ: Sử dụng Sythra hoặc Attacker khác đánh trước để Mireya có thể kết liễu.
     - **Kết quả Blood Drain**: Khi Mireya dứt điểm thành công, 2 lá `bone_spire` ở tiền tuyến tự động thu hồi vào `the_void`, Mireya được tăng sức tấn công, và một lá [`blood_spire`](../../cards/darkborn/demon/luminar-bastion/blood_spire.md) được triệu hồi từ `the_void` lên tiền tuyến thay thế vị trí trụ xương.

---

## Tóm tắt thuật toán Lua

```text
INIT CARDS:
  1. Tay bài omega_hand đảm bảo chứa: sythra, mireya, bone_spire.
  2. Nghĩa địa omega_the_void mặc định nạp sẵn: 2x bone_spire, 1x blood_spire.

DEPLOY:
  1. Nếu hand có bone_spire -> Deploy úp (face_up = false) lên omega_front_line để ưu tiên ẩn bài phòng thủ (tối đa 1 Character/lượt).
  2. Nếu hand có blood_mist (1 hoặc nhiều lá) -> Deploy TOÀN BỘ các lá blood_mist ở trạng thái úp (face_up = false) xuống omega_back_line cùng lúc.
  3. Kiểm tra trạng thái sân đấu:
     - Nếu đã triệu hồi thành công BLOOD SPIRE lên tiền tuyến:
       + Ưu tiên deploy Mireya từ hand khi tiền tuyến còn slot trống (1 Character/lượt) để duy trì hiệu quả Blood Mist.
     - Nếu chưa có Blood Spire và Sythra & Mireya đều ĐANG SỐNG:
       + Nếu hand có Sythra -> Deploy thêm Sythra (face_up = true, 1 Character/lượt) để tăng cơ hội kết liễu.
       + Không deploy thêm Mireya thứ 2.
     - Nếu Sythra HOẶC Mireya BỊ CHẾT (tử trận):
       + Vào trạng thái cầm cự phòng thủ, chờ rút lại Sythra/Mireya từ bộ bài.
       + Nếu hand có Character chiến đấu khác (non-Sythra/Mireya): Deploy (1 Character/lượt) làm mồi sát thương (Setup Attacker), nhưng BẮT BUỘC chừa đủ 2 slot trống trên tiền tuyến cho 2 Bone Spire.
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
     - Nếu remaining_def không lớn hơn sát thương của Sythra -> Sythra dứt điểm.
     - Nếu remaining_def lớn hơn sát thương của Sythra -> Dùng Mireya/Attacker khác đánh mồi trước, sau đó Sythra dứt điểm.
  5. Nếu tiền tuyến đã có ĐỦ 2 Bone Spire VÀ Mireya sẵn sàng:
     - Mục tiêu: Mireya kết liễu để kích hoạt Blood Drain.
     - Nếu remaining_def không lớn hơn sát thương của Mireya -> Mireya dứt điểm.
     - Nếu remaining_def lớn hơn sát thương của Mireya -> Dùng Sythra/Attacker khác đánh mồi trước để Mireya có thể dứt điểm.
     - Blood Drain tự động đẩy 2 Bone Spire vào Void, tăng sức tấn công cho Mireya và triệu hồi Blood Spire ra tiền tuyến.
  6. Fallback: Nếu không thỏa mãn combo -> Tấn công bằng Attacker thông thường.
```
