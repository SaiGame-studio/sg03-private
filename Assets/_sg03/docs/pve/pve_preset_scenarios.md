# Cấu Hình Kịch Bản Mẫu PvE & Metadata Bộ Bài

> [!NOTE]
> **Phạm vi**: Hướng dẫn cấu hình cho script khởi tạo (`init_cards.lua`) và metadata kịch bản mẫu trong chế độ PvE.

---

## 1. Khởi Tạo Kịch Bản (`init_cards.lua`)

Khi một trận đấu PvE bắt đầu, `init_cards.lua` chia bài lên tay ban đầu và tạo bộ bài rút cho cả hai bên dựa trên metadata kịch bản:

### Khởi Tạo Phía Alpha (Người Chơi)
- **Bài Chọn Sẵn**: Đọc `choose_card_1`, `choose_card_2`, và `choose_card_3` từ `alpha_preset_metadata` (khớp theo `inventory_item_id`).
- **Rút Ngẫu Nhiên**: Rút 2 lá ngẫu nhiên từ Bí Cảnh Cung (`alpha_the_source`) để lấp đầy 5 lá bài tay mở màn.

### Khởi Tạo Phía Omega (AI Boss)
- **Slot Preset**: Đọc `choose_card_1`, `choose_card_2`, `choose_card_3` từ `metadata.omega.metadata` (khớp theo `item_definition_code_name`).
- **Bộ Bài Boss**: Khởi tạo Bí Cảnh Cung (`omega_the_source`) với các lá bài tay sai và kỹ năng theo kịch bản (ví dụ `goblin_shaman`, `totem_pulse`, `back_stab`).
- **Draw Priority**: Every Omega draw prioritizes remaining source cards matching `choose_card_1`, then `choose_card_2`, then `choose_card_3`. Random selection is used only when no matching chosen card remains.
- **Opening-Hand Guarantee**: Battle setup inserts a configured chosen card into `omega_the_source` when missing and preserves it there until the opening-hand selection runs.

---

## 2. Cấu Trúc Payload Metadata Mẫu

```json
{
  "alpha_preset_metadata": {
    "choose_card_1": "item-uuid-001",
    "choose_card_2": "item-uuid-002",
    "choose_card_3": "item-uuid-003"
  },
  "metadata": {
    "omega": {
      "metadata": {
        "choose_card_1": "goblin_shaman",
        "choose_card_2": "totem_pulse",
        "choose_card_3": "back_stab"
      }
    }
  }
}
```
