# Cookverse Thai

เกม VR สอนทำอาหารไทย ผู้เล่นมาเรียนทำ **ผัดกะเพราไก่** กับป้าแสงในบ้านเรือนไทย ตั้งแต่เก็บวัตถุดิบในสวน ล้าง หั่น ปรุงบนเตาแก๊ส จัดจาน จนได้รับผลประเมิน

- **เอนจิน:** Unity 6000.5.9f1 + XR Interaction Toolkit 3.5.1 + OpenXR
- **อุปกรณ์ทดสอบ:** Meta Quest 3 (APK) และ WebXR
- **สถานะ:** ต้นแบบ Blockout ภาคเรียนที่ 1 — ทำระบบภารกิจและการเก็บวัตถุดิบเสร็จแล้ว กำลังทำส่วนเตรียมและปรุง

## เริ่มต้น

1. เปิดโฟลเดอร์โปรเจกต์ด้วย Unity Hub (Editor 6000.5.9f1)
2. ฉากหลัก: `Assets/Scenes/MainVR.unity` · ฉากทดลองระบบ: `Assets/Scenes/GameplayTest_M1.unity`
3. กด Play แล้วใช้ XR Interaction Simulator ควบคุมด้วยคีย์บอร์ดและเมาส์
4. ลงแว่นจริงดูที่ [`Docs/Planning/VR/QUEST_3_SETUP.md`](Docs/Planning/VR/QUEST_3_SETUP.md)

## เอกสาร

เริ่มอ่านที่ [`Docs/README.md`](Docs/README.md) และ [`Docs/Core/PROJECT_CONTEXT.md`](Docs/Core/PROJECT_CONTEXT.md)

## Branch

| Branch | การใช้งาน |
|---|---|
| `feature/gameplay-content-ui` | branch ทำงานหลัก |
| `develop` | รวมงานที่ผ่านการทดสอบ |
| `main` | เวอร์ชันพร้อมนำเสนอ |
| `feature/xr-webxr-integration` | งานฉากช่วงแรก (รวมเข้า `develop` แล้ว) |
| `legacy` | โปรเจกต์เวอร์ชันเก่า |
