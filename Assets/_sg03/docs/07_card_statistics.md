# 07. Thống Kê Card Và Enemy AI

Số liệu trong tài liệu này được tổng hợp từ [Danh Mục Thẻ Nhân Vật](03_characters.md), [Danh Mục Kỹ Năng](04_abilities.md), và [Tổng Quan PvE](pve/pve_overview.md). Chỉ Ability chủ động được cộng vào tổng Ability và tổng số card.

## Tổng Quan

### Thống Kê Thẻ Bài

| Loại thẻ | Số lượng |
| --- | ---: |
| [Character](03_characters.md) | 27 |
| [Ability](04_abilities.md) | 26 |
| Ability bị động | 3 |
| **Tổng số card** | **53** |

### Thống Kê Enemy AI (PvE)

| Phân cấp Enemy AI | Số lượng |
| --- | ---: |
| [Kẻ Địch Thường (Normal Enemies)](pve/normal_enemies/_normal_enemies.md) | 3 |
| [Kẻ Địch Tinh Anh (Elite Enemies)](pve/elite_enemies/_elite_enemies.md) | 0 |
| [Kẻ Địch Boss (Boss Enemies)](pve/boss_enemies/_boss_enemies.md) | 0 |
| **Tổng số Enemy AI** | **3** |

## Thống Kê Theo Chủng Tộc

| Chủng tộc | Character | Ability | Bị động | Tổng card |
| --- | ---: | ---: | ---: | ---: |
| Darkborn | 14 | 10 | 1 | 24 |
| Lightborn | 3 | 4 | 0 | 7 |
| Natureborn | 6 | 7 | 0 | 13 |
| Humans | 4 | 5 | 2 | 9 |
| **Tổng** | **27** | **26** | **3** | **53** |

## Thống Kê Theo Tộc Nhánh

| Chủng tộc | Tộc nhánh | Character | Ability | Bị động | Tổng card |
| --- | --- | ---: | ---: | ---: | ---: |
| Darkborn | Undead | 5 | 3 | 0 | 8 |
| Darkborn | Demon | 9 | 7 | 1 | 16 |
| Lightborn | Light Elf | 2 | 3 | 0 | 5 |
| Lightborn | Lumina | 1 | 1 | 0 | 2 |
| Natureborn | Goblin | 4 | 3 | 0 | 7 |
| Natureborn | Furry | 2 | 4 | 0 | 6 |
| Humans | - | 4 | 5 | 2 | 9 |
| **Tổng** |  | **27** | **26** | **3** | **53** |

## Thống Kê Chi Tiết Enemy AI

Số liệu Enemy AI được thống kê theo các script Lua và tài liệu thiết kế kịch bản PvE đã tạo trong dự án:

| Phân cấp | Tên Enemy AI | Script Lua | Trạng thái |
| --- | --- | --- | --- |
| Normal Enemy | [Goblin Shaman](pve/normal_enemies/goblin_shaman.md) | [`enemy_ai_goblin_shaman.lua`](../../SaiGame/LuaScript/Scripts/enemy_ai_goblin_shaman.lua) | Hoàn thành |
| Normal Enemy | [Silas](pve/normal_enemies/silas.md) | [`enemy_ai_silas.lua`](../../SaiGame/LuaScript/Scripts/enemy_ai_silas.lua) | Hoàn thành |
| Normal Enemy | [The Bent Spoon #1](pve/normal_enemies/the_bent_spoon_1.md) | [`enemy_ai_the_bent_spoon_1.lua`](../../SaiGame/LuaScript/Scripts/enemy_ai_the_bent_spoon_1.lua) | Hoàn thành |
| **Tổng cộng** | **3 Enemy AI** | | |

> [!NOTE]
> Script [`enemy_ai_core.lua`](../../SaiGame/LuaScript/Scripts/enemy_ai_core.lua) là thư viện xử lý logic dùng chung (shared library) cho AI, không tính là một Enemy AI độc lập.
