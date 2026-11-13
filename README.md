# Cồn Xanh 🌴

Game xây cồn thư giãn, cảm hứng miền Tây: kéo ngón tay để đắp đất, đào hồ, vẽ đường,
dựng rào, trồng dừa, cất nhà lá, rồi ngắm cồn chuyển từ ngày sang đêm.

**VTA Games · Võ Thái Anh**

## Chơi thử

Mở `index.html` qua một web server (hoặc bật GitHub Pages cho repo này, nhánh `main`, thư mục gốc).
Game chạy bằng Three.js 0.160 tải từ unpkg, nên cần mạng lúc mở.

## Đang có

| Công cụ | Làm gì |
|---|---|
| ⛰️ Đất | Kéo ra biển để đắp bãi mới, kéo trên đất thành đồi, kéo vào hồ thì lấp hồ |
| 💧 Hồ | Kéo để đào hồ; bờ đá, sậy, lá súng tự mọc |
| 〰️ Đường | Kéo đường đất; đi qua nước thì tự bắc cầu ván |
| 🪵 Rào | Kéo để dựng hàng rào gỗ theo nét |
| 🌳 🌴 🌺 | Cây, dừa, bụi hoa |
| 🛖 🏡 🌬️ 🪣 🏪 🛢️ | Nhà lá, nhà gỗ, cối xay gió, giếng, chợ, đồ đạc |
| ☀️→🌙 | Ngày đêm: đèn nhà lá sáng, đom đóm bay |

Camera: 2 ngón để zoom và xoay; các nút ⟲ ⟳ ⤒ ⤓ ＋ － ở bên phải màn hình.
Đồ họa: ánh sáng ACES, bóng mềm, GTAO, bloom; nút ✨ bật/tắt đồ họa cao.

## Cấu trúc

- `index.html`: toàn bộ prototype (một file).
- `assets/pack.b64.js`: gói model 3D (GLB nén, mã hóa base64 để host được trên trang artifact).
- `assets/source/pack.glb`: cùng gói ở dạng GLB gốc, dùng khi chuyển sang engine khác.
- `assets/source/build-pack.mjs`: script gltf-transform để dựng lại gói từ bộ asset gốc.
- `design/`: tài liệu thiết kế. Đọc `design/con-xanh-brief.md` trước.
- `.claude/`, `CLAUDE.md`, `project.yaml`: bộ Claude Code Game Studios (chạy `/start` để cấu hình).

## Bản quyền asset

Toàn bộ model bên ngoài đều là **CC0** (dùng thương mại thoải mái, không bắt buộc ghi công; ghi để cảm ơn):

- **Quaternius**, *Ultimate Stylized Nature*: cây, dừa, bụi, cỏ, hoa. https://quaternius.com
- **Kay Lousberg (KayKit)**, *Medieval Hexagon Pack*: nhà gỗ, cối xay, giếng, chợ, rào, đồ đạc, sậy. https://kaylousberg.com

Nhà lá, cầu ván, đá bờ hồ, địa hình, nước, cỏ ngọn: code thủ tục viết riêng cho game.

Khung Claude Code Game Studios © Donchitos, MIT (`.claude/LICENSE-Claude-Code-Game-Studios`, `LICENSE`).
