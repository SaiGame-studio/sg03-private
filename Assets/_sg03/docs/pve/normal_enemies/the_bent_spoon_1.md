# AI The Bent Spoon #1

> Trạng thái: Đã triển khai AI
>
> Phân loại tài liệu: Normal Enemy
>
> Entity type trong cấu hình: NPC
>
> Enemy key: `the_bent_spoon_1`
>
> Battle AI dự kiến: `Assets/SaiGame/LuaScript/Scripts/enemy_ai_the_bent_spoon_1.lua`

## Tổng quan

`the_bent_spoon_1` là NPC Normal Enemy có tên hiển thị **The Bent Spoon #1**. AI xây đội hình quanh Misthy, kích hoạt một Abyssal Mist để tăng ATK và DEF cho các Misthy trên battle line, đồng thời giữ các Mist còn lại trong hand để thay thế khi Mist đang hoạt động bị vô hiệu hóa.

## Cấu hình entity

Theo cấu hình hiển thị trong ảnh được cung cấp:

| Thuộc tính | Giá trị |
| --- | --- |
| Name | The Bent Spoon #1 |
| Rarity | Common |
| Type | NPC |
| Documentation category | Normal Enemy |
| `choose_card_1` | `misthy` |
| `choose_card_2` | `abyssal_mist` |
| `choose_card_3` | `eagle_eye` |

## Bộ bài

Danh sách card của The Bent Spoon #1:

| Card code | Card count | Vai trò |
| --- | ---: | --- |
| `lyra` | 3 | Character chiến đấu |
| `eagle_eye` | 3 | Ability của Lyra |
| `zombie_male` | 3 | Character chiến đấu |
| `zombie_female` | 3 | Character chiến đấu |
| `kira` | 3 | Character chiến đấu |
| `abyssal_mist` | 3 | Ability hỗ trợ Misthy |
| `misthy` | 3 | Character chiến đấu |
| `skeleton` | 3 | Character chiến đấu |
| `goblin_shaman` | 3 | Character chiến đấu |
| **Tổng số card** | **27** | 9 loại card, mỗi loại 3 bản |

Khi build enemy deck, `battle_start.lua` dùng `card_count = 3` nếu một card trong cấu hình NPC không có field `card_count`.

## Quy tắc rút bài và opening hand

Theo `init_cards.lua`, opening hand Omega chỉ lấy các card preset theo thứ tự metadata, không rút ngẫu nhiên để lấp thêm bài. Với cấu hình hiện tại, opening hand được yêu cầu là:

1. `misthy`
2. `abyssal_mist`
3. `eagle_eye`

Mỗi card preset phải tồn tại trong `omega_the_source`; nếu không tồn tại và cũng không có trong `omega_the_void`, quá trình khởi tạo trả về lỗi.

## Triển khai Misthy và Abyssal Mist (`deploy`)

Mỗi lần `deploy(state)`, AI chỉ được đưa tối đa **một card** từ `omega_hand` lên battle line. Thứ tự ưu tiên là:

1. `abyssal_mist` khi có Misthy chưa kích hoạt và chưa có Mist active.
2. `eagle_eye` khi Lyra và mục tiêu Alpha face-down hợp lệ đã có.
3. Một `misthy`.
4. Một `lyra`.
5. Một Character còn lại theo thứ tự hand.

Khi Omega có Misthy chưa kích hoạt trên battle line và chưa có `abyssal_mist_active`, AI đưa đúng một `abyssal_mist` từ hand xuống `omega_back_line`, rồi kích hoạt nó qua `lib_ability_core.trigger_ability_by_key`.

Handler `abyssal_mist_execute` chọn một Misthy chưa kích hoạt trên front-line trước, sau đó mới đến back-line; Misthy được chọn bị lật ngửa và đánh dấu `trigger = true`. Handler cũng chỉ định một source aura chính, nên các Mist khác không cộng dồn hiệu ứng.

Khi đã có Abyssal Mist đang hoạt động, AI không deploy hoặc kích hoạt Mist khác. Các bản sao còn lại được giữ trong hand. Nếu Mist đang hoạt động bị đưa khỏi battle line hoặc bị vô hiệu hóa, AI chỉ deploy một Mist dự phòng ở lần có Misthy hợp lệ tiếp theo.

## Eagle Eye qua Lyra (`deploy`)

AI chỉ deploy một `eagle_eye` từ hand khi đồng thời có:

1. Một `lyra` ở `omega_front_line`.
2. Một Character Alpha ở `alpha_front_line` đang face-down và chưa expose.
3. Một slot trống ở `omega_back_line` nếu Eagle Eye chưa có trên battle line.

Sau khi card đã có trên battle line, AI gọi Ability pipeline chuẩn với Character Alpha úp đó làm `defender_card`. Theo handler `eagle_eye_execute`, hiệu ứng lật ngửa Lyra và mục tiêu, sau đó tiêu thụ Eagle Eye vào `omega_the_void`. Nếu thiếu Lyra hoặc mục tiêu hợp lệ, Eagle Eye vẫn được giữ trên hand.

## Phòng thủ và tấn công

- `defend(state)`: không có phản ứng phòng thủ riêng.
- `plan_attack(state)`: dùng quy tắc tấn công chung của Omega: ưu tiên Character Alpha face-up có DEF còn lại thấp nhất; nếu không có, chọn Character face-down đầu tiên; nếu không có Character hợp lệ thì tấn công `alpha_hp`.

## Tình huống kiểm thử tối thiểu

1. Hand có nhiều Misthy: mỗi lượt AI chỉ deploy một Misthy, trước các Character khác.
2. Có Misthy và Abyssal Mist: chỉ một Mist được deploy/kích hoạt, Misthy được chọn bị trigger.
3. Có nhiều Abyssal Mist trong hand: sau khi một Mist đang hoạt động, các Mist khác vẫn ở hand.
4. Mist đang hoạt động bị vô hiệu hóa: AI deploy đúng một Mist dự phòng khi còn Misthy chưa kích hoạt.
5. Có Lyra, Eagle Eye, và Character Alpha úp: AI deploy và kích hoạt Eagle Eye, lật ngửa Lyra/mục tiêu, rồi Eagle Eye vào void.
6. Thiếu Lyra hoặc không có Character Alpha úp: Eagle Eye không bị deploy hay tiêu thụ.
