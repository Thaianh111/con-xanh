# Cồn Xanh – brief

## Ý tưởng một câu
Game xây dựng thư giãn (cozy builder) kiểu kéo-vẽ: người chơi vẽ hồ, đường, đất, rào bằng ngón tay,
game tự "mọc" chi tiết đẹp. Khác biệt: toàn bộ chất liệu là **cồn miền Tây**.

## Nguồn cảm hứng
Một game builder trên đảo (giống Tiny Glade) mà anh Thái Anh quay video gửi. Các giai đoạn quan sát được:
1. Đảo nền giữa biển; nắn địa hình (Đất/Đồi/Đào).
2. Nước: khép vòng thành hồ, chạm dòng để đổi kiểu thác/kênh/sông.
3. Đường: đi qua tường tự mở cổng vòm, qua nước tự mọc cầu đá.
4. Công trình: tường, tháp, nhà, cửa; kéo cao, đổi mái, chép, sơn.
5. Trang trí và sự sống: rào, vườn hoa, bụi cây, trại vật nuôi (quây một vòng là có con vật).
6. Mở rộng: nối nhiều đảo bằng cầu.
7. Phép và ngày đêm: thanh trượt mặt trời; "Phép" đánh thức công trình.
8. Danh sách nhiệm vụ nhỏ (18/22) dẫn người chơi qua từng công cụ.

**Không sao chép**: giữ cách chơi kéo-vẽ, nhưng thay toàn bộ chủ đề, công cụ và asset bằng miền Tây.

## Trạng thái hiện tại (prototype v3)
- Một file `index.html` (Three.js 0.160), chơi được trên điện thoại.
- Công cụ: Đất, Hồ, Đường (tự bắc cầu ván), Rào, Cây, Dừa, Bụi hoa, Nhà lá, Nhà gỗ, Cối xay, Giếng, Chợ, Đồ đạc, Xóa, Hoàn tác.
- Ngày/đêm, 8 nhiệm vụ, camera xoay/nghiêng/zoom.
- Asset CC0: Quaternius (thiên nhiên) và KayKit (nhà cửa, đồ đạc). Nhà lá tự dựng bằng code.

## Việc tiếp theo (đề xuất)
1. Bộ asset miền Tây riêng: nhà lá, xuồng ba lá, cầu khỉ, lu nước, dừa nước, ghe, chòi. Làm bằng AI 3D
   (Tripo / Hunyuan3D / TRELLIS) rồi chỉnh trong Blender cho cùng tông Quaternius/KayKit.
2. Kênh rạch thay cho "sông": kéo nét tạo dòng chảy có hướng, xuồng trôi theo.
3. Sự sống: gà vịt, chó (husky làm linh vật), người dân đi trên đường.
4. Lưu/tải cồn (localStorage), chụp ảnh cồn để chia sẻ.
5. Âm thanh: sóng, chim, dế ban đêm, tiếng đàn kìm nhẹ.
6. Màn mở đầu VTA Games (logo chó mực).
7. Hiệu năng điện thoại: gộp mesh, LOD cây, tắt GTAO trên máy yếu (đã có tự động).
