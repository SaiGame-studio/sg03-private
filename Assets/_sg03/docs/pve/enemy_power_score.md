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
| Phản ứng phòng thủ Ability trước khi đòn đánh giải quyết | 150 |
| Ability expose Character úp trước khi lập kế hoạch tấn công | 100 |

Chỉ cộng một dòng khi script AI và Ability handler hiện có thật sự thực thi mechanic đó. Không dùng bảng này để suy diễn buff, debuff hoặc synergy không tồn tại trong source.

## Quy Ước Cập Nhật

Tài liệu này chỉ chứa công thức và bảng điểm dùng chung. Khi thêm hoặc thay đổi Enemy, ghi card list, phép tính từng thành phần, lực chiến tổng và bằng chứng AI trong tài liệu riêng của Enemy; không thêm ví dụ Enemy vào đây.
