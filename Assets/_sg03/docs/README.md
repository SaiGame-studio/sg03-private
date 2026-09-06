# Chỉ Mục Tài Liệu Dự Án _sg03

Chào mừng bạn đến với tài liệu kỹ thuật chi tiết của hệ thống trận đấu thẻ bài chiến thuật `_sg03` thuộc SaiGame.

---

## 📁 Cấu Trúc Thư Mục Tài Liệu

```
Assets/_sg03/docs/
├── README.md                      # Chỉ mục & Cấu trúc thư mục (File này)
│
├── 01_game_rules.md              # Luật chơi, Các khu vực bài & Công thức tính toán
├── 02_architecture.md            # Kiến trúc mã nguồn Client & Server
├── 03_characters.md              # Danh mục Thẻ nhân vật
├── 04_abilities.md               # Danh mục kỹ năng và hướng dẫn thực thi
├── 05_card_star_scaling.md       # Phân cấp sức mạnh Thẻ bài theo Cấp độ Sao (1-9 Sao)
├── 06_card_race.md               # Bốn chủng tộc và quan hệ giữa các race
├── 07_card_statistics.md          # Thống kê card theo race và tộc nhánh
│
├── pve/                          # [DÀNH RIÊNG CHẾ ĐỘ PVE]
│   ├── pve_overview.md           # Tổng quan chế độ PvE & Luồng trận đấu
│   ├── normal_enemies/           # Kẻ Địch Thường (Ví dụ: Goblin Shaman)
│   ├── elite_enemies/            # Kẻ Địch Tinh Anh
│   ├── boss_enemies/             # Kẻ Địch Boss
│   └── pve_preset_scenarios.md   # Cấu hình bộ bài Mẫu & Metadata Kịch bản
│
└── pvp/                          # [ĐỊNH HƯỚNG MỞ RỘNG PVP]
    └── pvp_roadmap.md            # Lộ trình kiến trúc Multiplayer 1v1 & Đồng bộ Realtime
```

---

## 📖 Thứ Tự Đọc Tài Liệu Tuần Tự

### Giai đoạn 1: Hệ Thống Cốt Lõi (Core Engine)

1. **[Chủng Tộc Card](06_card_race.md)** — Bản sắc, tộc nhánh, quy ước phân loại và vòng quan hệ giữa Darkborn, Lightborn, Natureborn và Humans.
2. **[Luật Chơi & Cơ Chế Trận Đấu](01_game_rules.md)** — Khái niệm tổng quan, điều kiện Thắng/Thua, các khu vực bài, phase machine, công thức phòng thủ/máu, giới hạn thả nhân vật và tương tác Đèn Linh Hồn.
3. **[Kiến Trúc Mã Nguồn & Hệ Thống](02_architecture.md)** — Kiến trúc C# Unity client (`BattleState`, `BattleScripts`, `CardSelection`, `ClientActions`, `CardSpawning`, `DeskPositionCtrl`, `LampOfSoulCtrl`), Lua 5.1 SS-GO server runtime, đồng bộ trạng thái và pipeline xử lý sự kiện.
4. **[Danh Mục Thẻ Nhân Vật & Thông Số](03_characters.md)** — Danh mục đầy đủ các thẻ Character phân theo Darkborn, Lightborn, Natureborn và Humans.
5. **[Hệ Thống Kỹ Năng & Hướng Dẫn Thực Thi](04_abilities.md)** — Chi tiết luồng thực thi, quy tắc kiểm tra vị trí nhắm mục tiêu và logic script của các thẻ Ability.
6. **[Phân Cấp Sức Mạnh Thẻ Bài Theo Cấp Độ Sao](05_card_star_scaling.md)** — Mô tả sức mạnh và thang tăng trưởng chỉ số/hiệu ứng của thẻ bài theo cấp độ sao (1 đến 9 sao) chia làm 3 nhóm Early game, Mid game và Late game.
7. **[Thống Kê Card](07_card_statistics.md)** — Tổng số Character, Ability và phân bố card theo race, tộc nhánh.

---

### Giai đoạn 2: Tài Liệu Theo Chế Độ Chơi

#### ⚔️ Chế độ PvE (Đang Triển Khai) `[/docs/pve/]`
- **[Tổng Quan Chế Độ PvE](pve/pve_overview.md)** — Hướng dẫn luồng trận đấu đánh Boss singleplayer và khởi tạo kịch bản.
- **[Thuật Toán AI Goblin Shaman (Normal Enemy)](pve/normal_enemies/goblin_shaman.md)** — Phân tích kỹ thuật cây quyết định của script `enemy_ai_goblin_shaman.lua`.
- **[Cấu Hình Kịch Bản Mẫu PvE](pve/pve_preset_scenarios.md)** — Hướng dẫn cấu hình metadata bộ bài preset và khởi tạo trận đấu.

#### ⚔️ Chế độ PvP (Định Hướng Mở Rộng) `[/docs/pvp/]`
- **[Lộ Trình Kiến Trúc PvP](pvp/pvp_roadmap.md)** — Lộ trình thiết kế đấu mạng 1v1 realtime, đồng bộ handler đối xứng, đếm ngược thời gian và kết nối WebSocket.
