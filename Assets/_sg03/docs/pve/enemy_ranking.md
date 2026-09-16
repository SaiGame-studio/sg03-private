# Xếp Hạng Lực Chiến Enemy PvE

> Phạm vi: Các Enemy AI đã có tài liệu và script trong PvE tại thời điểm cập nhật.
>
> Tiêu chí: Xếp từ yếu đến mạnh theo **điểm lực chiến dự kiến**. Điểm này gồm điểm bộ bài, choose card, Void card và mechanic chiến thuật theo [công thức lực chiến](enemy_power_score.md); đây không phải kết quả mô phỏng trận đấu.

## Thứ Tự Tổng Quan

| Hạng | Enemy | Phân cấp | Lực chiến | Lý do chính |
| ---: | --- | --- | ---: | --- |
| 1 | [The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md) | Normal | 9,280 | Có điểm chiến thuật cao hơn Silas (500 so với 450), nhưng điểm bộ bài thấp hơn 870 và không có điểm Void metadata, nên tổng lực chiến thấp nhất trong ba Enemy đang xếp hạng. |
| 2 | [Silas](normal_enemies/silas.md) | Normal | 10,110 | Điểm bộ bài 8,640 và choose 1,020 vượt The Bent Spoon #1; Totem Pulse, Brute Call và giữ slot combo đóng góp 450 điểm chiến thuật. |
| 3 | [Bastion Blood](normal_enemies/bastion_blood.md) | Normal | 11,420 | Có điểm bộ bài cao nhất (9,360), điểm choose 910, hai Bone Spire trong Void cho 250 điểm, và chuỗi Crimson Spire/Blood Drain/Blood Mist cho 900 điểm chiến thuật. |

## Phân Tích Từng Enemy

### Goblin Shaman — Tạm Thời Không Xếp Hạng

[`Goblin Shaman`](normal_enemies/goblin_shaman.md) được tạm thời loại khỏi bảng xếp hạng theo yêu cầu. Không gán hạng hoặc so sánh lực chiến cho Enemy này cho đến khi có quyết định đưa lại vào bảng.

### 1. The Bent Spoon #1

The Bent Spoon #1 có điểm bộ bài **7,770**, thấp nhất trong ba Enemy được xếp hạng. Dù nó có **500** điểm chiến thuật từ điều phối Misthy kết liễu, Abyssal Mist và Eagle Eye, điểm choose **1,010** cùng điểm Void metadata **0** đưa tổng lực chiến về **9,280**.

Nguồn điểm: [bảng card và lực chiến The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md) và [công thức](enemy_power_score.md#công-thức).

### 2. Silas

Silas đạt **10,110** điểm: bộ bài **8,640** cao hơn The Bent Spoon #1, choose **1,020**, Void metadata **0**, và **450** điểm chiến thuật. Cụm chiến thuật này chỉ gồm phản ứng Totem Pulse, Brute Call triệu hồi từ Void và giữ hai slot cho combo; vì vậy Silas ít điểm chiến thuật hơn The Bent Spoon #1, nhưng vẫn vượt tổng lực chiến nhờ chỉ số bộ bài.

Nguồn điểm: [bảng card và lực chiến Silas](normal_enemies/silas.md) và [công thức](enemy_power_score.md#công-thức).

### 3. Bastion Blood

Với tổng **11,420**, Bastion Blood đứng đầu vì đồng thời có điểm bộ bài **9,360** cao nhất, choose **910**, điểm Void **250** từ hai Bone Spire đã khai báo và **900** điểm chiến thuật. Điểm chiến thuật này phản ánh điều phối đòn kết liễu, triệu hồi Bone Spire, chuyển thành Blood Spire, Aura Blood Mist và việc giữ slot cho chuỗi combo.

Nguồn điểm: [bảng card và lực chiến Bastion Blood](normal_enemies/bastion_blood.md) và [công thức](enemy_power_score.md#công-thức).

## Elite Và Boss

Hiện thư mục [Elite Enemies](elite_enemies/_elite_enemies.md) và [Boss Enemies](boss_enemies/_boss_enemies.md) đều ghi danh sách đang cập nhật, không có Enemy AI cụ thể để đưa vào thứ tự. Khi có entry và script tương ứng, chúng cần được đánh giá theo cùng tiêu chí trước khi chèn vào bảng trên.
