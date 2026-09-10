# Chiến thuật AI Silas

> Trạng thái: Đã triển khai AI
>
> Phân loại tài liệu: Normal Enemy
>
> Entity type trong cấu hình: NPC
>
> Enemy key: `silas`
>
> Script chính: [`enemy_ai_silas.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua)

## Tổng quan

Silas ([`enemy_ai_silas.lua`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua)) triển khai chiến thuật combo kết hợp giữa việc giữ bộ bài triệu hồi `Goblin Brute` và phòng thủ phản ứng bằng `Totem Pulse`:

1. **Phòng thủ phản ứng (`defend`)**: Silas sử dụng `totem_pulse` để bảo vệ tiền tuyến khi bị tấn công thông qua cơ chế dùng chung `enemy_ai_core.defend_with_back_line_ability_when_front_line_takes_damage`.
2. **Quản lý bộ Combo trên tay**: Dự trữ **một `goblin_shaman` và một `brute_call` trên tay**, duy trì **2 slot trống liền kề** ở tiền tuyến cho tới khi đủ điều kiện kích hoạt từ turn 4.
3. **Triển khai Combo triệu hồi**: Từ turn 4 trở đi, khi thỏa mãn điều kiện, Silas deploy Shaman vào slot tiền tuyến đã dự trữ, deploy Brute Call vào hậu tuyến và kích hoạt `brute_call` qua pipeline Ability tiêu chuẩn để triệu hồi `Goblin Brute` từ void.
4. **Triển khai ngoài combo**: Ưu tiên deploy ngay `totem_pulse` xuống hậu tuyến khi rút được, đồng thời cho phép các Character ngoài bộ dự trữ được deploy vào các slot chưa bị reserve.

---

## Cấu hình Entity & Bộ Bài

Theo cấu hình NPC Silas:

| Thuộc tính | Giá trị |
| --- | --- |
| Name | Silas |
| Rarity | Common |
| Type | NPC |
| Category | Normal Enemy |
| `choose_card_1` | `goblin_shaman` |
| `choose_card_2` | `brute_call` |
| `choose_card_3` | `goblin_saboteur` |

Danh sách 27 card của Silas (9 loại, mỗi loại 3 bản):

| Card code | Số lượng | Vai trò |
| --- | ---: | --- |
| `goblin_shaman` | 3 | Character cho combo & trigger Totem |
| `goblin_saboteur` | 3 | Character chiến đấu |
| `skeleton` | 3 | Character chiến đấu |
| `goblin_grunt` | 3 | Character chiến đấu |
| `totem_pulse` | 3 | Ability phòng thủ Totem |
| `brute_call` | 3 | Ability triệu hồi Goblin Brute |
| `goblin_brute` | 3 | Character 4 sao (triệu hồi từ void) |
| `zombie_male` | 3 | Character chiến đấu |
| `zombie_female` | 3 | Character chiến đấu |

---

## 1. Phản ứng phòng thủ (`defend`)

Chi tiết mã nguồn nằm tại [`enemy_ai_silas.lua:L5-L9`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua#L5-L9):

```lua
function defend(state)
    return enemy_ai_core.defend_with_back_line_ability_when_front_line_takes_damage(
        state, "totem_pulse", "goblin_shaman"
    )
end
```

Silas chủ động phòng thủ tương tự Goblin Shaman: Khi tiền tuyến Omega bị nhắm tới bởi một đòn đánh gây sát thương (`pending_attack.damage_dealt > 0`), nếu có `totem_pulse` ở hậu tuyến và `goblin_shaman` chưa kích hoạt trên tiền tuyến, Silas sẽ kích hoạt Totem Pulse nâng DEF toàn bộ tiền tuyến trước khi đòn đánh giải quyết.

---

## 2. Triển khai đội hình (`deploy`)

Chi tiết mã nguồn nằm tại [`enemy_ai_silas.lua:L36-L135`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua#L36-L135).

### Triển khai Totem Pulse sớm

- Khi rút được `totem_pulse`, Silas deploy ngay vào hậu tuyến (`omega_back_line`) ở trạng thái úp (`face_up = false`).
- Để đảm bảo còn chỗ cho `brute_call`, Silas kiểm tra `count_empty_slots(back_line) >= min_back_slots` (với `min_back_slots = 2` nếu đang cầm `brute_call`, ngược lại là `1`).

### Kiểm tra điều kiện Combo (`can_combo`)

Combo được phép thực thi khi **đồng thời** thỏa mãn 6 điều kiện [`enemy_ai_silas.lua:L65-L70`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua#L65-L70):
1. `tonumber(state.turn or 0) >= 4` (từ Turn 4 trở đi).
2. Tay bài có `goblin_shaman`.
3. Tay bài có `brute_call`.
4. Tiền tuyến `omega_front_line` có 2 slot trống liền kề (`reserve_left ~= nil`).
5. Hậu tuyến `omega_back_line` còn ít nhất 1 slot trống cho `brute_call`.
6. `omega_the_void` có lá `goblin_brute`.

### Luồng thực thi Combo

Nếu `can_combo == true`:
1. Deploy `goblin_shaman` từ hand vào slot tiền tuyến `reserve_left` ở trạng thái ngửa (`face_up = true`).
2. Deploy `brute_call` từ hand vào slot trống ở hậu tuyến ở trạng thái úp (`face_up = false`).
3. Rebuild lại tay bài và cập nhật client actions.
4. Kích hoạt Ability `brute_call` thông qua helper:
   ```lua
   local event_data = {
       defender_card = shaman_card,
       defender_line_key = "omega_front_line",
       damage_dealt = 0,
   }
   local ability_err = enemy_ai_core.trigger_ability_and_append_actions(
       state, brute_call_card, "brute_call", "on_attack", event_data
   )
   ```
5. Hiệu ứng triệu hồi `goblin_brute` từ void được xử lý hoàn toàn bởi pipeline của Ability `brute_call`.

### Luồng triển khai trước/ngoài Combo

Nếu chưa thể thực hiện combo:
1. Xác định 2 slot trống liền kề trên tiền tuyến (`reserve_left`) để giữ lại cho combo.
2. Không deploy các card thuộc danh sách dự trữ: `shaman_card`, `brute_call_card`, và `goblin_brute`.
3. Có thể deploy các Character chiến đấu khác vào các slot tiền tuyến chưa bị reserve (`find_unreserved_empty_slot`).
4. Khi gọi `lib_battle_ai.ensure_omega_hand_draw_capacity`, Silas truyền danh sách `excluded_ids` (chứa các Character bị giữ lại) để việc giải phóng dung lượng tay bài không vô tình tiêu thụ bài dự trữ.

---

## 3. Lập kế hoạch tấn công (`plan_attack`)

Chi tiết mã nguồn nằm tại [`enemy_ai_silas.lua:L138-L141`](../../../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua#L138-L141):

```lua
function plan_attack(state)
    local defender = enemy_ai_core.pick_alpha_front_line_character_target(state)
    return enemy_ai_core.plan_omega_attack_with_target(state, defender)
end
```

Silas sử dụng luồng chọn mục tiêu và tấn công tiêu chuẩn qua `enemy_ai_core`:
1. **Chọn mục tiêu (`pick_alpha_front_line_character_target`)**: Ưu tiên Character ngửa trên `alpha_front_line` có `final_def - total_damage_received` thấp nhất. Nếu không có, chọn Character úp đầu tiên.
2. **Thực thi tấn công (`plan_omega_attack_with_target`)**: Chọn Attacker ngửa chưa trigger trên tiền tuyến để tấn công mục tiêu đã chọn, hoặc tấn công `alpha_hp` nếu tiền tuyến Alpha trống.

---

## Sơ đồ luồng quyết định

```mermaid
flowchart TD
    A[Bắt đầu lượt AI Silas] --> B[deploy: Deploy Totem Pulse nếu có vào hậu tuyến]
    B --> C{Kiểm tra can_combo: Turn >= 4 & có Shaman & Brute Call & 2 slot tiền tuyến & 1 slot hậu tuyến & Brute ở Void?}
    
    C -- ĐỦ ĐIỀU KIỆN --> D[Triển khai Shaman vào slot tiền tuyến dự trữ]
    D --> E[Triển khai Brute Call vào slot hậu tuyến]
    E --> F[Gọi trigger_ability brute_call qua Ability pipeline chuẩn]
    F --> G[Brute Call tự triệu hồi Goblin Brute từ Void vào slot liền kề]

    C -- CHƯA ĐỦ ĐIỀU KIỆN --> H[Giữ 2 slot tiền tuyến liền kề & giữ bài combo]
    H --> I[Deploy các Character không dự trữ vào slot chưa bị reserve]
    I --> J[Gọi ensure_omega_hand_draw_capacity với excluded_ids]

    G --> K[plan_attack: Tấn công bằng helper enemy_ai_core]
    J --> K
```

---

## Tóm tắt thuật toán Lua

```text
DEFEND:
  Kích hoạt Totem Pulse từ hậu tuyến nâng DEF tiền tuyến khi bị tấn công (nhờ enemy_ai_core).

DEPLOY:
  1. Rút được Totem Pulse -> Deploy ngay vào hậu tuyến (giữ slot cho Brute Call nếu cầm).
  2. Kiểm tra điều kiện combo (turn >= 4, có Shaman + Brute Call + Brute trong Void + đủ slot).
  3. Nếu đủ điều kiện:
     - Deploy Shaman ngửa vào front slot dự trữ.
     - Deploy Brute Call úp vào back slot.
     - Trigger Ability brute_call trên Shaman để triệu hồi Goblin Brute.
  4. Nếu chưa đủ điều kiện:
     - Dự trữ 2 slot tiền tuyến liền kề.
     - Chỉ deploy Character không thuộc nhóm reserve vào slot ngoài khu vực reserve.
     - Gọi ensure_omega_hand_draw_capacity ngoại trừ các lá reserved (dùng excluded_ids).

PLAN ATTACK:
  Sử dụng pick_alpha_front_line_character_target và plan_omega_attack_with_target.
```
