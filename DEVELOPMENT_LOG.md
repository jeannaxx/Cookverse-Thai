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

## 15 September 2026 — Mac M1 gameplay session

### Session plan

| Phase | งาน | สถานะ |
|---|---|---|
| M1-1 | Sync งานจาก `develop`, เปิดโปรเจกต์ และเตรียม Gameplay Test Scene | Completed |
| M1-2 | สร้าง Prefab วัตถุดิบแบบ Blockout และทดสอบ Grab/Drop | Completed |
| M1-3 | สร้าง Collection Zone และตรวจวัตถุดิบที่นำมาวาง | Completed |
| M1-4 | สร้าง Objective UI แบบหยาบและทดสอบระบบย่อย | Completed |

### Phase M1-1 — Gameplay workspace setup

- Start time: 20:37 ICT
- End time: 23:43 ICT
- Status: Completed
- Branch: `feature/gameplay-content-ui`

#### Tasks

- รวม `develop` ล่าสุดเข้า branch ของ Mac M1
- ยืนยันว่าได้รับ MainVR, Thai house blockout, Functional Zones และ XR Foundation จาก Mac M5
- เตรียม Test Scene สำหรับพัฒนาระบบย่อยโดยไม่แก้ Main XR Scene พร้อมกัน
- ตรวจ Console หลัง Unity โหลดไฟล์ครบ

#### Result

- สลับจาก `feature/xr-webxr-integration` มาที่ `feature/gameplay-content-ui` สำเร็จ
- Fast-forward จาก `develop` ถึง merge commit `03d4c0d` สำเร็จโดยไม่มี Conflict เวลา 20:37 ICT
- สร้างสำเนา MainVR สำหรับงาน M1 ที่ `Assets/Scenes/GameplayTest_M1.unity` สำเร็จ เวลา 21:04 ICT
- Unity import และ compile สำเร็จ โดยสามารถเปิด Play Mode และทดสอบ Script ใน `GameplayTest_M1` ได้
- Phase M1-1 completed เวลา 23:43 ICT

### Phase M1-2 — Ingredient blockout prefab

- Start time: 21:09 ICT
- End time: 21:44 ICT
- Status: Completed
- Branch: `feature/gameplay-content-ui`

#### Tasks

- ใช้ `Krapow` ที่มีอยู่เป็นวัตถุดิบต้นแบบ
- ตรวจ Rigidbody, Collider และ XR Grab Interactable
- สร้าง Prefab ในโฟลเดอร์ Gameplay ของ Mac M1
- ทดสอบ Grab, Drop และการชนพื้นใน `GameplayTest_M1`

#### Result

- ตรวจไฟล์ Scene แล้วพบว่า `Krapow` มี Rigidbody, Box Collider และ XR Grab Interactable ครบ
- สร้าง `Assets/Gameplay/Prefabs/Ingredients/Krapow.prefab` สำเร็จ และตรวจยืนยันว่ามี Rigidbody, Box Collider และ XR Grab Interactable ครบ เวลา 21:29 ICT
- ย้าย Krapow instance ขึ้นมาเหนือพื้นบ้านในพื้นที่ครัวและบันทึกตำแหน่งใน `GameplayTest_M1`
- Play Mode test ผ่าน: หยิบ ปล่อย และตกชนพื้นบ้านได้โดยไม่ทะลุ เวลา 21:44 ICT
- มือหรือวัตถุที่ถือสามารถผ่านผนังเมื่อขยับ Controller จำลองด้วย WASD โดยตรง; เก็บการปรับ Hand/Held-object collision ไว้ภายหลังและไม่กีดขวางระบบ Grab/Drop ขั้นพื้นฐาน
- Phase M1-2 completed เวลา 21:44 ICT

### Phase M1-3 — Krapow collection zone

- Start time: 21:47 ICT
- End time: 23:43 ICT
- Status: Completed
- Branch: `feature/gameplay-content-ui`

#### Tasks

- สร้าง Collection Zone แบบ Trigger ใน `GameplayTest_M1`
- สร้าง Prefab ของ Collection Zone ใต้ `Assets/Gameplay`
- เพิ่ม Script ตรวจว่า Krapow เข้ามาในพื้นที่
- แสดงผลตรวจสอบแบบหยาบและทดสอบใน Play Mode

#### Result

- สร้างกลุ่ม `GameplaySystems` ที่ Transform ค่าเริ่มต้นและบันทึกใน Scene สำเร็จ เวลา 21:47 ICT
- กำหนดโครงสร้าง Hierarchy ให้ `Environment` เก็บบ้าน สวน ทางเดิน และพื้น ส่วน `GameplaySystems` เก็บ Collection Zone และ Manager ต่าง ๆ
- สร้าง `GardenArea_Blockout` ใต้ `Environment` ที่ Position `(-5, 0.05, -7)` และ Scale `(3, 0.2, 4)` พร้อม Box Collider แบบไม่เป็น Trigger เวลา 22:49 ICT
- ย้าย Krapow instance ไปบนแปลงสวนที่ Position `(-5, 0.4, -7)` เวลา 22:52 ICT
- สร้าง `GardenPath_Blockout` เชื่อมขอบบันไดกับสวนที่ Position `(-1.75, 0.03, -6.6)` และ Scale `(3.5, 0.06, 1.2)` พร้อม Box Collider แบบไม่เป็น Trigger เวลา 22:57 ICT
- XR Locomotion test ผ่าน: เดินจากบันไดผ่าน GardenPath ไปถึง GardenArea ได้โดยไม่ติดขอบหรือตก เวลา 22:59 ICT
- เพิ่ม Gameplay Flow และโครงสร้างไฟล์สำหรับใช้อธิบายอาจารย์ไว้ใน `PROJECT_CONTEXT.md` โดยแยกสถานะงานที่เสร็จแล้วกับงานที่กำลังทำ
- สร้าง `CollectionZone_Krapow` ใต้ `GameplaySystems` ที่ Position `(-1.5, 1.25, 1.8)`, Scale `(1.2, 0.1, 1.2)` และเปิด Box Collider เป็น Trigger เวลา 23:06 ICT
- สร้าง `Assets/Gameplay/Prefabs/Zones/CollectionZone_Krapow.prefab` สำเร็จและตรวจยืนยันว่า Box Collider ยังเป็น Trigger เวลา 23:09 ICT
- สร้าง `Assets/Gameplay/Scripts/IngredientCollector.cs` สำหรับตรวจชื่อวัตถุดิบแบบ Rule-based โดยกำหนด Krapow เป็นค่าที่ถูกต้อง พร้อม Event สำหรับต่อ UI ภายหลัง เวลา 23:18 ICT
- ติด `IngredientCollector` กับ CollectionZone instance และ Apply กลับเข้า `CollectionZone_Krapow.prefab` สำเร็จ โดย Expected Ingredient Name เป็น Krapow และ Accept Only Once เปิดอยู่ เวลา 23:26 ICT
- Correct-flow test ผ่าน: ผู้เล่นหยิบ Krapow จากสวน นำกลับครัว และวางใน CollectionZone แล้ว Console แสดง `[IngredientCollector] Correct ingredient: Krapow` เวลา 23:37 ICT
- Wrong-ingredient test ผ่าน: วาง Chili ใน CollectionZone แล้ว Console แสดง `[IngredientCollector] Wrong ingredient: Chili. Expected: Krapow` เวลา 23:43 ICT
- Phase M1-3 completed เวลา 23:43 ICT

### Phase M1-4 — Collection result UI

- Start time: 23:47 ICT
- End time: 00:48 ICT วันที่ 2026-09-16
- Status: Completed
- Branch: `feature/gameplay-content-ui`

#### Tasks

- สร้าง World Space UI แบบ Blockout ใกล้ Collection Zone
- แสดงข้อความเริ่มต้นให้ผู้เล่นนำกะเพรามาวาง
- แสดงผลถูกต้องเมื่อวาง Krapow
- แสดงคำเตือนเมื่อวางวัตถุดิบชนิดอื่น
- ทดสอบ UI ใน Play Mode และตรวจ Console

#### Result

- เริ่ม Phase M1-4 เวลา 23:47 ICT
- สร้าง `CollectionFeedbackUI` ใต้ `GameplaySystems` เป็น World Space Canvas ขนาด 600 × 180 และ Scale 0.003 เวลา 23:54 ICT
- เพิ่มฟอนต์ `Sarabun-Regular.ttf` ที่รองรับภาษาไทย พร้อมใบอนุญาต OFL ไว้ใน `Assets/Gameplay/UI/Fonts` เวลา 00:00 ICT วันที่ 2026-09-16
- สร้าง TextMesh Pro Font Asset ชื่อ `Sarabun-Regular SDF` โดยรวมอักขระภาษาอังกฤษ ตัวเลข เครื่องหมาย และภาษาไทย สำหรับ UI เวลา 00:11 ICT วันที่ 2026-09-16
- สร้าง `CollectionFeedbackUI.cs` สำหรับแสดงข้อความเริ่มต้น ผลถูกต้องสีเขียว และผลวัตถุดิบผิดสีแดง เวลา 00:17 ICT วันที่ 2026-09-16
- ติด `CollectionFeedbackUI.cs` กับ World Space Canvas และเชื่อม `FeedbackText` เข้าช่องอ้างอิงสำเร็จ เวลา 00:38 ICT วันที่ 2026-09-16
- เชื่อม Event ของ `IngredientCollector` เข้ากับ UI สำเร็จ: ผลถูกเรียก `ShowCorrect()` และผลผิดเรียก `ShowWrong()` เวลา 00:42 ICT วันที่ 2026-09-16
- Wrong-flow UI test ผ่าน: วาง Chili แล้วป้ายแสดงข้อความเตือนสีแดงและ Console แสดงผลวัตถุดิบผิด
- Correct-flow UI test ผ่าน: เริ่ม Play Mode ใหม่ วาง Krapow แล้วป้ายแสดงข้อความสำเร็จสีเขียวและ Console แสดงผลผ่าน
- ตรวจไฟล์ Scene ยืนยันว่าไม่มีลิงก์ Prefab ของ UI ที่สูญหาย, Font Asset ภาษาไทยถูกอ้างอิง, `FeedbackText` ถูกเชื่อม และ Event ทั้งสองชี้ไปยังเมธอดที่ถูกต้อง
- Phase M1-4 completed เวลา 00:48 ICT วันที่ 2026-09-16

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
