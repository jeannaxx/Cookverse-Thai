# Cookverse Thai — Web (WebBoard + หลังบ้าน API)

งานเว็บของโปรเจกต์ แยกจาก Unity ตามแผน `Docs/Planning/Schedule/PLAN_Nov_2026_v2.md` (สายงาน **WB** และ **AI**)

## โฟลเดอร์และ branch

| | โฟลเดอร์บนเครื่อง | branch |
|---|---|---|
| เกม Unity (M1/M5) | `~/Documents/UnityProjects/Cookverse-Thai` | `feature/gameplay-content-ui` |
| เว็บ (WB/AI) | `~/Documents/UnityProjects/Cookverse-Thai-Web` | `feature/web` |

- สองโฟลเดอร์เป็น repo เดียวกันบน GitHub (`jeannaxx/Cookverse-Thai`) แต่ **ไม่ต้องสลับ branch ข้ามกัน** — ห้ามสลับ branch ในโฟลเดอร์ Unity ไปทำเว็บ (ฉากจะย้อนเป็นเวอร์ชันเก่า)
- โฟลเดอร์เว็บเปิด sparse checkout ไว้ เห็นเฉพาะ `Web/` และ `Docs/` (ไม่ดึงไฟล์ Unity ลงมา ตั้งใจให้เปิดด้วย Unity ไม่ได้)
- GitHub Desktop: File → Add Local Repository → `Cookverse-Thai-Web` · ตั้ง Alias เป็น `Cookverse-Thai-Web` จะได้ไม่สับสนกับโฟลเดอร์ Unity

## โครงสร้างที่วางแผนไว้

```text
Web/
├── netlify/functions/   หลังบ้าน API: /api/sessions, /api/ai/evaluate, /api/ai/aunt, /api/ai/menu
├── site/                หน้าบ้าน WebBoard (เข้าสู่ระบบครู/แอดมิน · รายการรอบเล่น · รายละเอียด)
└── supabase/            SQL สร้างตาราง players · sessions · action_logs · results · menus · users
```

## กติกาความปลอดภัย

- `GEMINI_API_KEY`, `SUPABASE_SERVICE_ROLE_KEY` เก็บใน Environment Variables ของ Netlify เท่านั้น
- ห้ามใส่ key ในโค้ด ในเกม Unity ในแชท หรือ commit (`.env` ถูก ignore ไว้แล้ว)
- เกมต้องเล่นจบได้เสมอแม้หลังบ้านหรือ AI ล่ม (ส่งไม่สำเร็จ → เก็บในเครื่องแล้วส่งซ้ำ)
