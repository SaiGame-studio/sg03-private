# Xếp Hạng Độ Khó Enemy PvE

> Phạm vi: Các Enemy AI đã có tài liệu và script trong PvE tại thời điểm cập nhật.
>
> Tiêu chí: Xếp từ yếu đến mạnh theo độ ổn định của đội hình, phản ứng phòng thủ, khả năng tạo lợi thế từ Ability và mức độ chủ động trong việc chọn đòn kết liễu. Đây không phải bảng xếp hạng ATK/DEF đơn lẻ.

## Thứ Tự Tổng Quan

| Hạng | Enemy | Phân cấp | Lý do chính |
| ---: | --- | --- | --- |
| 1 | [Goblin Shaman](normal_enemies/goblin_shaman.md) | Normal | Có một hướng phòng thủ rõ ràng là Totem Pulse, nhưng kế hoạch tấn công và chọn Character deploy không có combo dứt điểm riêng. |
| 2 | [Silas](normal_enemies/silas.md) | Normal | Giữ được phòng thủ Totem Pulse như Goblin Shaman và có thể triệu hồi Goblin Brute, nhưng combo chỉ bắt đầu từ turn 4 và còn phụ thuộc bài trên tay, hai ô tiền tuyến liền kề, một ô hậu tuyến và Brute ở Void. |
| 3 | [The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md) | Normal | Ưu tiên Misthy, dùng Eagle Eye để xử lý Character úp của Alpha, đồng thời tính sát thương còn thiếu để sắp xếp đòn mồi cho Misthy kết liễu. |
| 4 | [Bastion Blood](normal_enemies/bastion_blood.md) | Normal | Có chuỗi hai giai đoạn Sythra/Mireya: xây Bone Spire, chủ động dàn sát thương cho đòn kết liễu, nâng thành Blood Spire và kích hoạt Blood Mist khi đủ điều kiện. |

## Phân Tích Từng Enemy

### 1. Goblin Shaman

Goblin Shaman là mốc độ khó thấp nhất vì sức mạnh chiến thuật tập trung vào một phản ứng phòng thủ: khi tiền tuyến bị sát thương, `totem_pulse` chỉ được kích hoạt nếu có Goblin Shaman chưa trigger trên tiền tuyến. Ngoài phản ứng này, AI chọn mục tiêu theo helper mặc định và chỉ giữ tối đa một Character úp; không có logic tạo combo hoặc điều phối đòn đánh để bảo đảm kết liễu.

Nguồn: [thuật toán Goblin Shaman](normal_enemies/goblin_shaman.md), đặc biệt các phần `defend` và `plan_attack`.

### 2. Silas

Silas mạnh hơn Goblin Shaman vì vẫn có Totem Pulse nhưng bổ sung đường triệu hồi `goblin_brute` bằng `brute_call`. Tuy vậy, đây là lợi thế có độ trễ và nhiều điều kiện: từ turn 4, phải giữ được Goblin Shaman cùng Brute Call trên tay, có hai ô tiền tuyến liền kề, một ô hậu tuyến, và Goblin Brute trong Void. Khi không đủ điều kiện, Silas quay về deploy Character ngoài bộ dự trữ và tấn công theo luồng chuẩn, nên độ đe dọa chưa ổn định bằng hai AI phía sau.

Nguồn: [thuật toán Silas](normal_enemies/silas.md), phần `can_combo`, `defend` và `plan_attack`.

### 3. The Bent Spoon #1

The Bent Spoon #1 có áp lực tấn công chủ động hơn: AI ưu tiên đưa Misthy lên sân, dùng Eagle Eye khi Alpha có Character úp và Lyra đã có trên tiền tuyến, rồi tính `remaining_def` của mục tiêu. Nếu Misthy chưa đủ sát thương, AI tìm một Character khác có sát thương an toàn để bào DEF trước, nhằm mở đường cho Misthy kết liễu. Khả năng đọc trạng thái mục tiêu và điều phối đòn mồi này khiến đối thủ khó giữ Character yếu hơn Silas.

AI không có phản ứng phòng thủ riêng, vì vậy nó vẫn xếp dưới Bastion Blood, vốn có cả nhiều giai đoạn xây dựng đội hình lẫn Aura.

Nguồn: [thuật toán The Bent Spoon #1](normal_enemies/the_bent_spoon_1.md), phần `stage_eagle_eye` và `plan_attack`.

### 4. Bastion Blood

Bastion Blood là enemy mạnh nhất trong danh sách hiện có vì AI không chỉ chọn một đòn tấn công mà theo đuổi chuỗi điều kiện liên kết. Đầu tiên AI ưu tiên bảo toàn chỗ cho Bone Spire và sắp xếp để Sythra kết liễu nhằm triệu hồi thêm Bone Spire. Khi đã có đủ hai Bone Spire, AI lại điều phối sát thương để Mireya kết liễu, đổi hai Bone Spire lấy Blood Spire và tăng sức tấn công cho Mireya. Sau khi Blood Spire cùng Mireya hiện diện, AI còn kích hoạt hoặc kích hoạt lại Blood Mist từ hậu tuyến để làm suy yếu Character Alpha đủ điều kiện.

Chuỗi này có điều kiện thiết lập, nhưng phần thưởng gồm phòng thủ Blood Spire, tăng cường Mireya và Aura khiến mức đe dọa tăng theo diễn biến trận, vượt các AI Normal còn lại.

Nguồn: [thuật toán Bastion Blood](normal_enemies/bastion_blood.md), các phần `deploy`, `plan_attack`, Crimson Spire, Blood Drain và Blood Mist.

## Elite Và Boss

Hiện thư mục [Elite Enemies](elite_enemies/_elite_enemies.md) và [Boss Enemies](boss_enemies/_boss_enemies.md) đều ghi danh sách đang cập nhật, không có Enemy AI cụ thể để đưa vào thứ tự. Khi có entry và script tương ứng, chúng cần được đánh giá theo cùng tiêu chí trước khi chèn vào bảng trên.
