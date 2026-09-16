# Cách Tính Điểm Lực Chiến Enemy PvE

> Phạm vi: Công thức tài liệu để so sánh sức mạnh dự kiến của một Enemy AI trước trận. Đây không phải là điểm runtime hoặc thay thế cho sát thương và `final_atk`/`final_def` trong trận.

## Dữ Liệu Đầu Vào

Mỗi Enemy được chấm từ bốn nguồn:

1. **Danh sách Ability/card**: Card code, số lượng, `base_stats.atk` và `base_stats.def`.
2. **Danh sách choose**: `choose_card_1` đến `choose_card_3`; đây là ba lựa chọn được ưu tiên khi tạo bài đầu trận.
3. **Danh sách Void**: `void_card_n`; đây là các card được nạp sẵn vào `the_void` để chiến thuật có thể gọi lại.
4. **Chiến thuật AI**: Điều kiện triển khai, dứt điểm, triệu hồi, Aura và các giai đoạn của AI.

Với card không có `base_stats.atk` hoặc `base_stats.def`, giá trị thiếu là `0` trong phần lực chiến cơ bản. Ability vẫn có thể nhận điểm chiến thuật nếu AI thực sự dùng hiệu ứng của nó.

## Công Thức

`Điểm lực chiến = Điểm bộ bài + Điểm choose + Điểm Void + Điểm chiến thuật`

| Thành phần | Công thức | Ý nghĩa |
| --- | --- | --- |
| Điểm bộ bài | `Σ(số_bản × (ATK + DEF))` | Tổng lực chiến gốc của toàn bộ card list. |
| Điểm choose | `Σ(ATK + DEF của mỗi choose card)` | Giá trị của ba lựa chọn ưu tiên; chỉ tính một lần cho từng lựa chọn, không thêm bản sao mới vào bộ bài. |
| Điểm Void | `50% × Σ(ATK + DEF của mỗi void card)` | Card trong Void đã sẵn sàng cho mechanic gọi lại nhưng chưa ở trên sân, nên chỉ nhận nửa giá trị. |
| Điểm chiến thuật | Tổng điểm của các mechanic được AI hiện tại thực thi | Ghi nhận giá trị không thể hiện trực tiếp qua ATK/DEF, như dứt điểm có điều phối và Aura. |

## Điểm Chiến Thuật Chuẩn

| Mechanic đã có trong AI | Điểm |
| --- | ---: |
| Điều phối đòn mồi để một Character chỉ định kết liễu | 200 |
| Triệu hồi Character từ Void sau đòn kết liễu | 200 |
| Nâng cấp hoặc thay thế công trình phòng thủ qua mechanic | 200 |
| Aura chủ động có điều kiện và có thể tái kích hoạt | 200 |
| Giữ slot để bảo toàn chuỗi combo | 100 |

Chỉ cộng một dòng khi script AI và Ability handler hiện có thật sự thực thi mechanic đó. Không dùng bảng này để suy diễn buff, debuff hoặc synergy không tồn tại trong source.

## Ví Dụ: Bastion Blood

Nguồn card và metadata: [Bastion Blood](normal_enemies/bastion_blood.md).

### 1. Điểm Bộ Bài

`3 × ((150 + 200) + (200 + 360) + (0 + 250) + (0 + 500) + (0 + 0) + (140 + 160) + (200 + 400) + (50 + 200) + (70 + 240)) = 9,360`

`blood_mist` có ATK/DEF cơ bản là `0` trong phép tính vì bản định nghĩa hiện có không công bố hai stat này; hiệu ứng giảm ATK được chấm tại phần chiến thuật.

### 2. Điểm Choose

`sythra` + `blood_mist` + `mireya`:

`(200 + 360) + (0 + 0) + (150 + 200) = 910`

### 3. Điểm Void

Hai `bone_spire` đã khai báo trong Void:

`50% × (2 × (0 + 250)) = 250`

### 4. Điểm Chiến Thuật

| Mechanic Bastion Blood | Điểm | Bằng chứng |
| --- | ---: | --- |
| Điều phối đòn mồi cho Sythra/Mireya kết liễu | 200 | [`enemy_ai_bastion_blood.lua`](../../../SaiGame/LuaScript/Scripts/enemy_ai_bastion_blood.lua) tìm setup attacker trước khi lập đòn kết liễu. |
| Crimson Spire gọi Bone Spire từ Void | 200 | [Crimson Spire](../cards/darkborn/demon/luminar-bastion/abilities/crimson_spire.md) và giai đoạn 1 của AI. |
| Blood Drain đổi hai Bone Spire lấy Blood Spire | 200 | [Blood Drain](../cards/darkborn/demon/luminar-bastion/abilities/blood_drain.md) và giai đoạn 2 của AI. |
| Blood Mist Aura có dự phòng tái kích hoạt | 200 | [Blood Mist](../cards/darkborn/demon/luminar-bastion/abilities/blood_mist.md) và `try_trigger_blood_mist`. |
| Giữ hai slot cho Bone Spire | 100 | `can_deploy_character_without_blocking_bone_spires` trong AI. |
| **Tổng** | **900** | |

### Kết Quả

`9,360 + 910 + 250 + 900 = 11,420`

**Điểm lực chiến dự kiến của Bastion Blood: 11,420.** Điểm này dùng để so sánh cấu hình Enemy; kết quả trận thực tế vẫn phụ thuộc lượt rút, trạng thái bàn đấu, mục tiêu của Alpha và điều kiện kích hoạt Ability.
