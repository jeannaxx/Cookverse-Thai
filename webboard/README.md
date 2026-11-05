# Cookverse WebBoard

เว็บสำหรับผู้สอนและแอดมินของเกม Cookverse Thai (React + Vite) — ดูผลการเล่น คะแนน Rubric, Action Log, คำแนะนำจากป้าแสง และจัดการเมนู/ผู้ใช้

## เริ่มใช้งาน

```bash
npm install
npm run dev      # เปิด http://localhost:5173
npm run build    # ได้โฟลเดอร์ dist/ สำหรับ Deploy
```

เข้าสู่ระบบด้วยอีเมลอะไรก็ได้ (โหมดทดลอง) · อีเมลที่มีคำว่า `admin` จะเห็นเมนูผู้ใช้และสิทธิ์ และแก้น้ำหนัก Rubric ได้

## โครงสร้าง

```text
src/
  App.jsx               เส้นทางหน้าเว็บ
  auth.jsx              เข้าสู่ระบบแบบจำลอง (แทนด้วย Supabase Auth ทีหลัง)
  data/mock.js          ข้อมูลตัวอย่างทั้งหมด (แทนด้วย Supabase ทีหลัง)
  components/           Layout (เมนูข้าง) · RubricBars · SessionTable · Logo
  pages/                Login · Overview · Sessions · SessionDetail · Menus
  styles.css            สีและขนาดทั้งหมดอยู่ใน :root ด้านบนไฟล์
```

## ยังไม่ได้ทำ

- ต่อ Supabase (Auth + ตาราง players / sessions / action_logs / results / menus / users)
- ปุ่ม "เพิ่มเมนู" "เชิญผู้ใช้" และ "บันทึก" ความเห็นผู้สอน ยังไม่ส่งข้อมูลขึ้นระบบ

## Deploy (Netlify)

มี `netlify.toml` แล้ว: build = `npm run build`, publish = `dist` และ redirect ทุก path ไป `index.html`

## โฟลเดอร์และ branch (สำคัญ)

| | โฟลเดอร์บนเครื่อง | branch | GitHub Desktop |
|---|---|---|---|
| เกม Unity (M1/M5) | `~/Documents/UnityProjects/Cookverse-Thai` | `feature/gameplay-content-ui` | `Cookverse-Thai-Unity` |
| เว็บ (WB/AI) | `~/Documents/UnityProjects/Cookverse-Thai-Web` | `feature/web` | `Cookverse-Thai-Web` |

- สลับงานด้วยการ **สลับ Repository** ใน GitHub Desktop — **ห้ามสลับ Branch** ในโฟลเดอร์ใด (ฉาก Unity จะย้อนเป็นเวอร์ชันเก่า)
- โฟลเดอร์เว็บดึงเฉพาะ `webboard/` และ `Docs/` (sparse checkout) · ไฟล์ Unity บน branch นี้เป็นของเดิมจาก `develop` ห้ามลบ (ตอน merge กลับจะลบโปรเจกต์ Unity)
- งาน AI/หลังบ้านจะอยู่ใน `webboard/netlify/functions/` (`/api/sessions`, `/api/ai/evaluate`, `/api/ai/aunt`, `/api/ai/menu`)

## กติกาความปลอดภัย

- `GEMINI_API_KEY`, `SUPABASE_SERVICE_ROLE_KEY` เก็บใน Environment Variables ของ Netlify เท่านั้น
- ห้ามใส่ key ในโค้ด ในเกม Unity ในแชท หรือ commit (`.env` ถูก ignore ไว้แล้ว)
- เกมต้องเล่นจบได้เสมอแม้หลังบ้านหรือ AI ล่ม (ส่งไม่สำเร็จ → เก็บในเครื่องแล้วส่งซ้ำ)
