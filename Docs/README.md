# Cookverse Thai — สารบัญเอกสาร

อัปเดต: 22 ตุลาคม 2026

## Core — สถานะและข้อตกลง

| เอกสาร | ใช้ทำอะไร |
|---|---|
| [`Core/PROJECT_CONTEXT.md`](Core/PROJECT_CONTEXT.md) | **อ่านก่อน** ขอบเขต ข้อตกลง วิธีทำงาน และสถานะล่าสุด |
| [`Core/DEVELOPMENT_LOG.md`](Core/DEVELOPMENT_LOG.md) | บันทึกงานที่ทำและทดสอบจริงตามวัน |

## Planning — แผนและสเปก

| เอกสาร | ใช้ทำอะไร |
|---|---|
| [`Planning/Schedule/PLAN_Oct-Nov_2026.md`](Planning/Schedule/PLAN_Oct-Nov_2026.md) | แผนงาน 22 ต.ค.–30 พ.ย. และการนับความคืบหน้า |
| [`Planning/Gameplay/GAMEPLAY_CONTENT_V1.md`](Planning/Gameplay/GAMEPLAY_CONTENT_V1.md) | สเปกเมนูผัดกะเพราไก่: วัตถุดิบ ขั้นภารกิจ เตาแก๊ส บทพูด Rubric และ Action Log |
| [`Planning/Environment/ENVIRONMENT_LAYOUT.md`](Planning/Environment/ENVIRONMENT_LAYOUT.md) | ผังบ้าน ครัว สวน ขนาด และ Hierarchy ใน `MainVR` |
| [`Planning/VR/QUEST_3_SETUP.md`](Planning/VR/QUEST_3_SETUP.md) | วิธีตั้งค่าและ Build APK ลง Quest 3 |
| [`Planning/VR/QUEST_3_TEST_2026-10-27.md`](Planning/VR/QUEST_3_TEST_2026-10-27.md) | เช็กลิสต์ทดสอบแว่น 27 ต.ค. |

ภาพผังอยู่ใน `Planning/Environment/` ได้แก่ `Cookverse_Floor_Plan_Illustrated_v5.png` (บ้านและสวน) และ `Cookverse_House_8x6_Layout_Hierarchy_v1.png` (ครัว 3D)

## หลักการอัปเดต

- แผนงานเขียนใน `Planning` สิ่งที่ทำและทดสอบจริงเขียนใน `DEVELOPMENT_LOG.md`
- การตัดสินใจหรือขอบเขตที่เปลี่ยนให้อัปเดต `PROJECT_CONTEXT.md`
- รอบทดสอบแว่นใหม่ให้สร้างไฟล์ `QUEST_3_TEST_YYYY-MM-DD.md` ใหม่ ไม่แก้ทับไฟล์เดิม
- ภาพผังเวอร์ชันใหม่ใช้เลขเวอร์ชันถัดไป และลบเวอร์ชันเก่าที่ไม่ใช้แล้ว (ดูย้อนหลังได้ใน Git)
- เอกสารที่ล้าสมัยให้ลบออก ไม่ต้องเก็บสำเนาไว้ เพราะ Git เก็บประวัติให้แล้ว
