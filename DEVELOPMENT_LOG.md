# SiamCulinaryXR — Daily Development Log

ไฟล์นี้ใช้บันทึกว่างานในแต่ละวันทำอะไรไปแล้ว กำลังทดสอบอะไร และควรทำอะไรต่อ

## 7 September 2026

### Direction and project setup

- ปรับแผนตามคำแนะนำของอาจารย์ให้เริ่มจาก Unity VR/XR Project ตั้งแต่ต้น
- สร้างโปรเจกต์ใหม่จาก Unity VR Template
- สำรองโปรเจกต์เดิมไว้ใน Git branch `legacy`
- ย้ายไฟล์จากโปรเจกต์ VR ใหม่มาเป็นโปรเจกต์หลักบน branch `main`
- ล้าง Unity cache เก่าที่ไม่ควรเก็บใน Git ได้แก่ `Library`, `Logs` และ `UserSettings`
- Commit การเปลี่ยนโปรเจกต์หลักเป็น VR Template
  - Commit: `8be8a7e`
  - Message: `Replace main project with VR template`

### Scene setup

- สร้าง Scene ใหม่ชื่อ `MainVR`
- นำ `XR Origin Hands (XR Rig)` จาก Sample Scene มาใช้ใน `MainVR`
- ลบ Main Camera เดิมเพื่อไม่ให้ซ้ำกับกล้องภายใน XR Origin
- ตั้งตำแหน่งและมุมหมุนของ XR Origin กลับเป็นศูนย์
- สร้างพื้นทดสอบชื่อ `TestFloor` ขนาดประมาณ 5 × 5 เมตร

## 8 September 2026

### XR Foundation prototype

- เพิ่ม `Teleportation Area` ให้ `TestFloor`
- สร้าง Cube สำหรับทดสอบ Interaction
- เพิ่ม `Rigidbody` และ `XR Grab Interactable` ให้ Cube
- Import `XR Interaction Simulator` จาก XR Interaction Toolkit
- เปิด `Use XR Interaction Simulator in scenes`
- ตั้ง `Camera Y Offset` เป็น 1.6 เมตร สำหรับจำลองระดับสายตาผู้ใหญ่

### Tests completed

- XR Interaction Simulator เปิดและทำงานใน Play Mode
- ควบคุมมุมมองและการเคลื่อนที่ด้วยเมาส์และคีย์บอร์ดได้
- Cube ตกและชนพื้นตาม Physics ได้
- หยิบ ปล่อย โยน และหยิบ Cube ซ้ำได้
- Teleport บนพื้นทดสอบได้

### Current milestone

Phase 1 — XR Foundation prototype ผ่านการทดสอบพื้นฐานแล้ว:

```text
XR Origin
→ Keyboard/Mouse Simulation
→ Movement and Turning
→ Teleport
→ Grab, Drop, and Throw
```

### Next work

- บันทึกและ Commit ผลการทดสอบ XR Foundation
- เริ่ม Phase 2 — Environment Blockout
- สร้างบ้านเรือนไทยขนาดเล็กแบบ Blockout
- แบ่งพื้นที่เป็นจุดเริ่มต้น/รับภารกิจ สวน และครัวไทย
- ทดสอบขนาดพื้นที่และตำแหน่งวัตถุด้วย XR Simulation

### Phase 2 — Environment Blockout progress

#### Completed

- สร้างกลุ่ม `Environment` สำหรับจัดวัตถุภายในฉาก
- สร้าง `Ground` ขนาด 20 × 20 เมตร
- สร้างกลุ่ม `ThaiHouse`
- สร้าง `HouseFloor` ขนาด 6 × 8 เมตร และยกพื้นจากพื้นดินประมาณ 1 เมตร
- สร้างกลุ่ม `Pillars` เพื่อจัดเสาบ้านให้เป็นระเบียบ
- สร้างเสารองบ้านจำนวน 8 ต้น
- แบ่งชื่อเสาตามตำแหน่งหน้า กลาง หลัง และซ้าย กลาง ขวา
- สร้างกลุ่ม `FrontStairs`
- สร้างบันไดหน้าบ้านจำนวน 5 ขั้น เชื่อมจากพื้นดินขึ้นสู่พื้นบ้าน

#### Current hierarchy

```text
Environment
├── Ground
└── ThaiHouse
    ├── HouseFloor
    ├── Pillars
    │   ├── Pillar_FL
    │   ├── Pillar_FC
    │   ├── Pillar_FR
    │   ├── Pillar_ML
    │   ├── Pillar_MR
    │   ├── Pillar_BL
    │   ├── Pillar_BC
    │   └── Pillar_BR
    └── FrontStairs
        ├── Step_01
        ├── Step_02
        ├── Step_03
        ├── Step_04
        └── Step_05
```

#### Breakpoint / next session

- หยุดงานไว้ก่อนเริ่มสร้างกลุ่ม `Walls`
- งานถัดไปคือสร้างผนังด้านหลัง ด้านซ้าย ด้านขวา และผนังหน้าที่เว้นช่องทางเข้า
- หลังสร้างผนังแล้วจะแบ่งพื้นที่ภายในเป็นจุดตื่น/รับภารกิจ ชานบ้าน และครัวไทย

---

## 14 September 2026 — Mac M5 XR integration session

### Session plan

| Phase | งาน | สถานะ |
|---|---|---|
| M5-1A | ทำผนังบ้าน Blockout และแบ่งพื้นที่ใช้งาน | Completed |
| M5-1 | เปิด `Assets/Scenes/MainVR.unity` และยืนยัน XR Foundation | Completed |

### Phase M5-1A — Thai house functional blockout

- Start time: 23:22 ICT
- End time: 01:01 ICT (15 September 2026)
- Status: Completed
- Branch: `feature/xr-webxr-integration`

#### Tasks

- สร้างกลุ่ม `Walls` ใต้ `ThaiHouse`
- สร้างผนังหลัง ซ้าย ขวา และผนังหน้าสองฝั่งโดยเว้นทางเข้า
- ใช้ Collider ของ Cube เป็นขอบเขตบ้าน
- แบ่งตำแหน่งจุดรับภารกิจ ชานบ้าน และครัวแบบหยาบ
- ตรวจทางขึ้นบันไดและระยะเดินภายในบ้าน

#### Result

- สร้างและบันทึกผนัง `Wall_Back`, `Wall_Left`, `Wall_Right`, `Wall_Front_Left` และ `Wall_Front_Right` แล้ว เวลา 23:59 ICT
- เปิด Play Mode และยืนยันว่า XR Interaction Simulator ทำงานพร้อมแสดง Controller ซ้ายและขวาแล้ว เวลา 00:18 ICT วันที่ 15 กันยายน 2026
- ทดสอบการขยับอุปกรณ์จำลองด้วย WASD ขึ้นบันไดและผ่านทางเข้าบ้านได้ เวลา 00:19 ICT วันที่ 15 กันยายน 2026 แต่การขยับแบบนี้ไม่ใช้ระบบชนของ Locomotion
- พบว่า WASD สามารถผ่านผนังได้ตามพฤติกรรมของ XR Interaction Simulator ซึ่งขยับอุปกรณ์จำลองโดยตรง เวลา 00:38 ICT วันที่ 15 กันยายน 2026; ต้องทดสอบ Locomotion ผ่านแกนอนาล็อกซ้ายด้วย Shift + I/J/K/L
- ทดสอบ Locomotion ด้วย Shift + I/J/K/L แล้วไม่ทะลุผนัง ยืนยันว่า Wall Collider ทำงาน เวลา 00:41 ICT วันที่ 15 กันยายน 2026
- ตัดสินใจเก็บงาน Head Collision พร้อม Fade/จอมืดเมื่อศีรษะจริงเข้าใกล้ผนังไว้ปรับภายหลัง; ไม่กีดขวางระบบหลักของ Phase นี้ เวลา 00:43 ICT วันที่ 15 กันยายน 2026
- ทดสอบ XR Locomotion ด้วย Shift + I/J/K/L ขึ้นบันไดและผ่านประตูบ้านสำเร็จ เวลา 00:49 ICT วันที่ 15 กันยายน 2026
- ตรวจ Console หลังเปิด Play Mode: Error สีแดง 0 รายการ และมี Warning เรื่องไม่พบอุปกรณ์ Eye Tracking 1 รายการซึ่งยอมรับได้เมื่อใช้ Simulator เวลา 00:51 ICT วันที่ 15 กันยายน 2026
- สร้าง `FunctionalZones` พร้อม `Zone_QuestStart`, `Zone_Porch` และ `Zone_Kitchen` ครบ และตั้ง Box Collider เป็น Trigger ทั้งสามจุด
- ตรวจไฟล์ `Assets/Scenes/MainVR.unity` แล้วยืนยันว่าชื่อ ตำแหน่ง ขนาด และค่า Trigger ถูกบันทึกครบ
- Phase M5-1A completed เวลา 01:01 ICT วันที่ 15 กันยายน 2026

### Phase M5-1 — MainVR and XR Foundation

- Start time: 23:17 ICT
- End time: 01:03 ICT (15 September 2026)
- Status: Completed
- Branch: `feature/xr-webxr-integration`

#### Tasks

- เปิดฉาก `Assets/Scenes/MainVR.unity`
- ยืนยันว่ามี `XR Origin Hands (XR Rig)`
- ทดสอบ Movement, Turning, Teleport, Grab, Drop และ Throw
- ตรวจ Console ว่าไม่มี Error สีแดง

#### Result

- XR Interaction Simulator เปิดและแสดง Controller ซ้ายและขวาได้ เวลา 00:18 ICT วันที่ 15 กันยายน 2026
- การขยับอุปกรณ์จำลองด้วย WASD ผ่านบันไดและทางเข้าได้ แต่ยังไม่นับเป็น Movement/Collider test เพราะ WASD ขยับอุปกรณ์ XR โดยตรง
- Locomotion/Collider test ผ่านด้วยแกนอนาล็อกซ้ายจำลอง (Shift + I/J/K/L) เวลา 00:41 ICT วันที่ 15 กันยายน 2026
- Turning test ผ่านด้วยแกนอนาล็อกขวาจำลอง (J/L โดยไม่กด Shift) เวลา 01:03 ICT วันที่ 15 กันยายน 2026
- Teleport, Grab, Drop และ Throw ใช้ผลทดสอบที่ผ่านแล้วเมื่อวันที่ 8 กันยายน 2026 โดย XR Rig และระบบ Interaction เดิมยังอยู่ในฉาก
- Console test ผ่านโดยมี Error สีแดง 0 รายการ
- Phase M5-1 completed เวลา 01:03 ICT วันที่ 15 กันยายน 2026

---

## Daily entry template

### Date

#### Completed

- 

#### Tested

- 

#### Problems / decisions

- 

#### Next work

- 
