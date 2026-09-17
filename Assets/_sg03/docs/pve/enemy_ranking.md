# Xếp Hạng Lực Chiến Enemy PvE

> Phạm vi: Các Enemy AI đã có tài liệu và script trong PvE tại thời điểm cập nhật.
>
> Tiêu chí: Xếp từ yếu đến mạnh theo **điểm lực chiến dự kiến**. Điểm này gồm điểm bộ bài, choose card, Void card và mechanic chiến thuật theo [công thức lực chiến](enemy_power_score.md); đây không phải kết quả mô phỏng trận đấu.

## Thứ Tự Tổng Quan

| Hạng | Enemy | Phân cấp | Lực chiến | Lý do chính |
| ---: | --- | --- | ---: | --- |
| 1 | [The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md) | Normal | 9,280 | Có điểm chiến thuật cao hơn Silas (500 so với 450), nhưng điểm bộ bài thấp hơn 870 và không có điểm Void metadata, nên tổng lực chiến thấp nhất trong các Enemy đang xếp hạng. |
| 2 | [Goblin Shaman](normal_enemies/goblin_shaman.md) | Normal | 9,700 | Điểm bộ bài 8,730 và choose 820 vượt The Bent Spoon #1; Totem Pulse phản ứng phòng thủ đóng góp 150 điểm chiến thuật. |
| 3 | [Silas](normal_enemies/silas.md) | Normal | 10,110 | Điểm bộ bài 8,640 và choose 1,020 vượt Goblin Shaman; Totem Pulse, Brute Call và giữ slot combo đóng góp 450 điểm chiến thuật. |
| 4 | [Bastion Blood](normal_enemies/bastion_blood.md) | Normal | 11,820 | Có điểm bộ bài cao nhất (9,660), điểm choose 910, hai Bone Spire trong Void cho 350 điểm, và chuỗi Crimson Spire/Blood Drain/Blood Mist cho 900 điểm chiến thuật. |

## Phân Tích Từng Enemy

### 1. The Bent Spoon #1

The Bent Spoon #1 có điểm bộ bài **7,770**, thấp nhất trong các Enemy được xếp hạng. Dù nó có **500** điểm chiến thuật từ điều phối Misthy kết liễu, Abyssal Mist và Eagle Eye, điểm choose **1,010** cùng điểm Void metadata **0** đưa tổng lực chiến về **9,280**.

Nguồn điểm: [bảng card và lực chiến The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md) và [công thức](enemy_power_score.md#công-thức).

### 2. Goblin Shaman

Goblin Shaman đạt **9,700** điểm: bộ bài **8,730**, choose **820**, Void metadata **0**, và **150** điểm chiến thuật từ phản ứng Totem Pulse trước khi đòn đánh giải quyết. Lực chiến của Goblin Shaman vượt The Bent Spoon #1 (9,280) nhờ chỉ số bộ bài cao hơn, nhưng thấp hơn Silas (10,110).

Nguồn điểm: [bảng card và lực chiến Goblin Shaman](normal_enemies/goblin_shaman.md) và [công thức](enemy_power_score.md#công-thức).

### 3. Silas

Silas đạt **10,110** điểm: bộ bài **8,640** cao hơn The Bent Spoon #1, choose **1,020**, Void metadata **0**, và **450** điểm chiến thuật. Cụm chiến thuật này chỉ gồm phản ứng Totem Pulse, Brute Call triệu hồi từ Void và giữ hai slot cho combo; vì vậy Silas ít điểm chiến thuật hơn The Bent Spoon #1, nhưng vẫn vượt tổng lực chiến nhờ chỉ số bộ bài.

Nguồn điểm: [bảng card và lực chiến Silas](normal_enemies/silas.md) và [công thức](enemy_power_score.md#công-thức).

### 4. Bastion Blood

Với tổng **11,820**, Bastion Blood đứng đầu vì đồng thời có điểm bộ bài **9,660** cao nhất, choose **910**, điểm Void **350** từ hai Bone Spire đã khai báo và **900** điểm chiến thuật. Điểm chiến thuật này phản ánh điều phối đòn kết liễu, triệu hồi Bone Spire, chuyển thành Blood Spire, Aura Blood Mist và việc giữ slot cho chuỗi combo.

Nguồn điểm: [bảng card và lực chiến Bastion Blood](normal_enemies/bastion_blood.md) và [công thức](enemy_power_score.md#công-thức).

## Elite Và Boss

Hiện thư mục [Elite Enemies](elite_enemies/_elite_enemies.md) và [Boss Enemies](boss_enemies/_boss_enemies.md) đều ghi danh sách đang cập nhật, không có Enemy AI cụ thể để đưa vào thứ tự. Khi có entry và script tương ứng, chúng cần được đánh giá theo cùng tiêu chí trước khi chèn vào bảng trên.
