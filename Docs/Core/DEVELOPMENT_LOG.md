# Cookverse Thai — Daily Development Log

ไฟล์นี้ใช้บันทึกว่างานในแต่ละวันทำอะไรไปแล้ว ทดสอบอะไร และควรทำอะไรต่อ

กติกา:

- บันทึกเฉพาะสิ่งที่ทำและทดสอบจริง แผนงานให้เขียนใน `Docs/Planning`
- ระบุเสมอว่าทดสอบบน **Simulator**, **แว่นจริง (APK)** หรือ **WebXR**
- ใช้แบบฟอร์มท้ายไฟล์ และเพิ่มรายการใหม่ไว้ก่อนแบบฟอร์ม

> หมายเหตุ: บันทึกช่วงวันที่ 7 ก.ย.–26 ก.ย. ใช้คำว่า Mac M1 / Mac M5 ตามการแบ่งงานสองเครื่องในตอนนั้น ตั้งแต่ ต.ค. ผู้พัฒนาคนเดียวทำทั้งสองฝั่ง

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

### 22 กันยายน 2026 — เตรียมเนื้อหา M1 และแผนทดสอบ Quest 3

#### Completed

- จัดทำ `Docs/Planning/Gameplay/M1_GAMEPLAY_CONTENT_V1.md` สำหรับเมนูผัดกะเพราไก่ โดยกำหนดวัตถุดิบ สถานะ `ingredientId`, `stepId`, Objective UI, บทพูด NPC, Recipe Rubric และ Action Log Contract v1
- กำหนดลำดับงาน M1-5 ถึง M1-8 ให้ต่อยอดจาก Krapow Collection Correct/Wrong flow เดิมโดยไม่รื้อระบบที่ทดสอบผ่านแล้ว
- จัดทำ `Docs/Planning/VR/QUEST_3_TEST_PLAN_2026-09-25.md` สำหรับทดสอบ M5 Environment/XR Foundation บน Quest 3 วันศุกร์ที่ 25 กันยายน 2026

#### Tested

- ตรวจเนื้อหาให้ใช้ชื่อ `Krapow` เดิมเป็นช่วงเปลี่ยนผ่านไปยัง `ingredientId = krapow_leaf`
- ยังไม่ได้ทดสอบ Gameplay Content ชุดใหม่นี้ใน Play Mode และยังไม่ได้ Build Quest 3

#### Problems / decisions

- สูตร จำนวนวัตถุดิบ และ Recipe Rubric เป็น v1 สำหรับต้นแบบ ต้องให้อาจารย์ยืนยันก่อนล็อก
- การทดสอบ Quest 3 รอบแรกเน้น Controller, Environment, Teleport และ Collider ยังไม่ใช้ตัดสิน Hand Tracking, WebXR หรือ Gameplay Loop

#### Next work

- เริ่ม M1-5: ข้อมูลกลางและ Objective Manager ใน `GameplayTest_M1`
- เตรียม Android Build และทดสอบ Quest 3 ตามเช็กลิสต์วันที่ 25 กันยายน 2026

### 26 กันยายน 2026 — เลื่อนการทดสอบ Quest 3

#### Problems / decisions

- เลื่อนการทดสอบ Quest 3 เนื่องจากยังไม่สามารถเดินทางไปใช้อุปกรณ์ได้
- ยังไม่มีผลทดสอบ APK, Controller, Hand Tracking หรือ WebXR จากอุปกรณ์จริง
- เก็บเช็กลิสต์เดิมไว้และรอกำหนดวันทดสอบใหม่ ห้ามรายงานว่าส่วนอุปกรณ์จริงผ่านแล้ว

---

> บันทึกช่วง 19 ก.ย.–17 ต.ค. ด้านล่างเขียนย้อนหลังเมื่อ 22 ต.ค. 2026 จากประวัติ commit และไฟล์ในโปรเจกต์ ผลการทดสอบที่ไม่มีหลักฐานในไฟล์จะระบุว่า "ยังไม่มีบันทึกผล"

### 19 ก.ย.–1 ต.ค. 2026 — สร้างบ้าน ครัว และสวนใน `MainVR`

Branch: `feature/xr-webxr-integration` (merge เข้า `develop` แล้ว)

#### Completed

- `559ff20` (19 ก.ย.) รีเซ็ต `MainVR` เพื่อเริ่มสร้างฉากใหม่ตามแปลนบ้าน 8 × 6 เมตร
- `9e65fd3`, `66885b7`, `6e3c237` (21–22 ก.ย.) สร้างโครงบ้านเรือนไทยยกพื้น เสา บันได ราว หลังคา และสวนวัตถุดิบ
- `dc678cf`, `d665cb4`, `58a4f8a` (27–29 ก.ย.) จัดครัวและโซนการใช้งาน: `Ingredient_Zone`, `Washing_Zone`, `Preparation_Zone`, `Cooking_Zone`, `Plating_Zone`, `MissionComplete_Zone`
- `2430b47`, `72db63c` (1 ต.ค.) เก็บรายละเอียดบ้านและสวน วางตำแหน่ง `NPC_Area`, `PlayerSpawn`, `GasStove`, `Fridge`, `Sink`, `CuttingBoard`, `PlatingTable` และของตกแต่งครัวแบบ Blockout
- จัดทำแผนครัวฉบับ 1 ต.ค. (รวมเข้า `Docs/Planning/Environment/ENVIRONMENT_LAYOUT.md` แล้ว)

#### Tested

- ยังไม่มีบันทึกผลการเดินทดสอบฉากใหม่ใน Simulator และยังไม่ได้ทดสอบบนแว่นจริง

#### Problems / decisions

- ตัดห้องนอนแบบห้องปิดออก ใช้พื้นที่เป็นมุมนั่งเล่นและจุดรับภารกิจ
- ครัวรูปตัว L และมีโต๊ะเตรียมกลางครัว เว้นทางเดินหลัก 1.2–1.5 เมตร
- พบไฟล์ `Assets/MainVR.unity` ซ้ำกับ `Assets/Scenes/MainVR.unity` ต้องตรวจก่อนลบ

### 1–2 ต.ค. 2026 — เริ่มระบบภารกิจใหม่

Branch: `feature/gameplay-content-ui`

#### Completed

- `c11774f` ลบต้นแบบเก็บกะเพราชุดแรก (Krapow/CollectionZone/CollectionFeedbackUI เดิม) เพื่อเขียนใหม่บนโครงข้อมูลกลาง
- `ec88ac7` สร้างโครงระบบภารกิจ: `SessionManager`, `MissionManager`, `ObjectiveManager`, `MissionGiver`, `IngredientState`, `IngredientIdentity`

### 8–9 ต.ค. 2026 — NPC และการนับกะเพรา

#### Completed

- `2829e36` เพิ่ม `MissionInteractionZone` ผู้เล่นเดินเข้าโซนของป้าแสงแล้วรับภารกิจได้
- `8244e95` `ObjectiveManager` นับใบกะเพราด้วย `ingredientId = krapow_leaf` และ Instance ID กันการนับชิ้นเดิมซ้ำ ยังรองรับชื่อเดิม `Krapow`
- `bf71f63` เปลี่ยนข้อความ Console ของระบบภารกิจเป็นภาษาไทย

### 11 ต.ค. 2026 — จุดรับวัตถุดิบและ UI

#### Completed

- `be228fc` สร้าง Prefab `Krapow`, `Chili`, `CollectionZone_Krapow` (ย้ายไป `Prefabs/Stations`), `CollectionFeedbackUI`, `MissionChecklistUI`
- `IngredientCollector` ตรวจของหลังผู้เล่นปล่อยมือในจุดรับเท่านั้น ของที่ปล่อยนอกจุดรับไม่ถูกนับ
- ผลการวาง: ถูก / ผิดชนิด / ซ้ำ / ผิดขั้น / ข้อมูลไม่ครบ แสดงบน UI ภาษาไทย
- เครื่องมือ Editor `Tools > Cookverse Thai > M1 > Apply Remaining Gameplay Setup` สำหรับตั้งค่าฉากทดลอง

### 16 ต.ค. 2026 — มือ VR ที่ทำเอง

#### Completed

- `6fd4de5` นำเข้าโมเดลมือไว้ที่ `Assets/Gameplay/Art/Hands` สร้าง Prefab มือซ้าย–ขวา และเชื่อมกับ XR Hands ผ่าน `M1CustomHandVisual`
- Reset ภารกิจคืนตำแหน่ง ฟิสิกส์ และสถานะวัตถุดิบครบ รวมถึงปล่อยของออกจากมือ

#### Tested

- ตรวจท่าอ้างอิงกระดูกข้างละ 22 จุดผ่าน และเห็นมือทั้งสองข้างพร้อมการเปลี่ยนท่าใน **Simulator**
- ยังไม่ยืนยันการติดตามมือจริง ความพอดีของมือ หรือการหยิบวัตถุดิบบน Quest 3

#### Problems / decisions

- ตกลงแนวทางป้าแสง AI: ทำวงจรเล่นให้จบก่อน แล้วเพิ่มการพิมพ์คุย เสียงพูดเป็นขั้นถัดไป ระบบกฎเป็นผู้ตัดสินเสมอ (บันทึกไว้ใน `PROJECT_CONTEXT.md` หัวข้อ 7)

### 17 ต.ค. 2026 — พริก 3 เม็ดและชุดทดสอบอัตโนมัติ

#### Completed

- `f6fefc3` วางพริก 3 เม็ดในฉากทดลอง (`Tools > Cookverse Thai > M1 > Setup Three Chilies`)
- สร้าง `M1CollectionSmokeTest` เรียกจาก `Tools > Cookverse Thai > M1 > Test Collection Round (Play Mode)` ตรวจ 18 กรณี: รับภารกิจ, วางของผิด, ถือของในจุดรับยังไม่นับ, นับกะเพรา, ไม่นับซ้ำ, นับพริกทีละเม็ด, ผ่านเมื่อครบ, UI แสดง 3/3, Reset คืนจำนวน สถานะ ตำแหน่ง และฟิสิกส์, เริ่มรอบใหม่แล้วนับได้

#### Tested

- ยังไม่มีบันทึกผลการรันชุดทดสอบ ให้รันอีกครั้งและบันทึกผลในรายการ 22 ต.ค.

### 21–22 ต.ค. 2026 — ทบทวนสถานะและวางแผนใหม่

#### Completed

- ทบทวนทุก branch และโปรเจกต์บนเครื่อง พบว่างานช้ากว่าแผนเดิมประมาณ 3 สัปดาห์ (ระบบไปถึงขั้นเก็บวัตถุดิบในสวน)
- จัดเอกสารใหม่ทั้งชุด: อัปเดต `PROJECT_CONTEXT.md`, รวมแผนฉากเป็น `ENVIRONMENT_LAYOUT.md`, เปลี่ยนชื่อเนื้อหาเป็น `GAMEPLAY_CONTENT_V1.md` พร้อมสเปกเตาแก๊ส, ทำแผนใหม่ `PLAN_Oct-Nov_2026.md`, แผนทดสอบแว่น 27 ต.ค. และลบเอกสารที่ล้าสมัย

#### Problems / decisions

- ผู้พัฒนาคนเดียวทำทั้งสองฝั่ง ใช้ `feature/gameplay-content-ui` เป็น branch หลัก
- ใช้ชื่อ **Cookverse Thai** เป็นชื่อโปรเจกต์ในเอกสาร
- เตาแก๊สใช้ปุ่มบิด ปิด / ไฟเบา / ไฟกลาง / ไฟแรง (ไม่ลดขอบเขตส่วนนี้)
- ไม่ล้างไก่ดิบใต้ก๊อก (หลักความปลอดภัยอาหาร) ล้างเฉพาะผัก
- นัดทดสอบ Quest 3 วันอังคารที่ 27 ต.ค. 2026 (ภายหลังย้ายเป็น 26 ต.ค. บ่าย)

- พบว่าฟอนต์ Sarabun ถูกลบไปใน `c11774f` UI ปัจจุบันใช้ฟอนต์ของระบบ (`Thonburi`) ซึ่งไม่มีบน Quest 3 ต้องฝังฟอนต์ไทยกลับก่อนทดสอบแว่น
- เอกสารที่อ้างถึงในบันทึกเก่าถูกเปลี่ยนชื่อ: `M1_GAMEPLAY_CONTENT_V1.md` → `GAMEPLAY_CONTENT_V1.md`, `Quest_3_Setup_and_First_Test.md` → `QUEST_3_SETUP.md` และลบ `QUEST_3_TEST_PLAN_2026-09-25.md`, `SPACE_LAYOUT_GARDEN_PLAN.md`, `M5_LATEST_WORK_PLAN_TH.md`, แผนรายวัน M1/M5 (.md/.html), Gantt ใน `outputs/` และภาพผัง v2

#### Next work

- เขียน `ActionLog` และปรับ `ObjectiveManager` ให้ใช้ตารางข้อมูลขั้นภารกิจ
- รัน `M1CollectionSmokeTest` และบันทึกผล
- ฝังฟอนต์ไทยกลับเข้าโปรเจกต์

### 24 ต.ค. 2026 — ครัว ล้าง หั่น เตา (ทำงานของ 23–25 ต.ค. รวดเดียว)

Branch / commit: `feature/gameplay-content-ui` · `3a2d34a`, `b74849c`

#### Completed

- แก้ `IngredientCollector` ไม่ให้บันทึก "Duplicate (ผิด)" ปลอมหลังวางของถูก
- ฝังฟอนต์ไทย Sarabun กลับเข้าโปรเจกต์ (กู้จาก `ea2b460`) และให้ UI ใช้ผ่าน `ThaiFont.cs`
- ใช้บล็อกในสวนเป็นวัตถุดิบ: ต้นกะเพรา 6 ต้นหยิบได้ · บล็อกพริกแดง 3 ก้อน · ลบช่อกะเพราที่ใบหลุดไปห่าง 6.8 ม. และพริกแผ่นแบน 44 × 66 ซม.
- ตู้เย็นเปิดได้ ไก่ 1 ชิ้น กระเทียม 3 กลีบ (`Tools > Cookverse Thai > M1 > Setup Kitchen`)
- อ่างล้าง เขียง + มีด เตาแก๊สปุ่มบิด 4 ตำแหน่ง (`XRKnob` ล็อกทุก 60°) และกระทะคำนวณความสุก (`Setup Cooking Stations`)
- ตารางภารกิจเพิ่มขั้น `wash_ingredients`, `prepare_ingredients`, `cook_krapow`
- วัตถุดิบตั้งเป็น kinematic จนกว่าจะถูกหยิบ เพราะใบกะเพราที่ซ้อนกันจะดันกันกระเด็น

#### Tested

- แพลตฟอร์ม: **Simulator**
- `M1CollectionSmokeTest` ผ่านครบ (24 ต.ค. 02:29)
- เล่นจบวงจร: รับภารกิจ → สวน → ครัว → ล้าง (ระบบปฏิเสธการล้างไก่ถูกต้อง) → หั่น → ใส่ไก่ลงกระทะ → บิดปุ่มเตา → ไก่สุก
- ชุดทดสอบไม่ผ่าน 1 ข้อเมื่อรันต่อจากการเล่นในรอบเดียวกัน (จำตำแหน่งเริ่มต้นผิด) แก้ชุดทดสอบแล้ว รอรันใหม่

#### Problems / decisions

- ประตูตู้เย็นเปิด/ปิด 12 ครั้งตอนหยิบไก่ เพราะ Ray โดนประตูก่อน รอตัดสินใจหลังทดสอบแว่น
- Scene List ของ Build มีแค่ `SampleScene` ต้องเปลี่ยนเป็น `GameplayTest_M1` ก่อน Build
- ย้ายการทดสอบแว่นจาก 27 ต.ค. เป็น **26 ต.ค. บ่าย** และเลื่อนงานลำดับใส่ของในกระทะไป 27 ต.ค.

#### Next work

- เช้า 26 ต.ค.: รันชุดทดสอบ · Scene List · Project Validation · Build APK
- บ่าย 26 ต.ค.: ทดสอบ Quest 3 ตาม `QUEST_3_TEST_2026-10-26.md`

### 26–27 ต.ค. 2026 — ทดสอบแว่นผ่าน APK ไม่ได้ → ทำ WebXR แทน

Branch / commit: `feature/gameplay-content-ui` (`ca4293f`, `24376fa`) · `feature/webxr` (`a48457f`, `47847de` และต่อจากนั้น)

#### Completed

- แก้บันไดหน้าบ้าน: ทางลาดล่องหน `Stairs_WalkRamp` ชัน 26° และปิด Collider ของขั้นบันได (`Tools > Cookverse Thai > M1 > Fix Front Stairs`)
- เพิ่ม Teleportation Area ที่ `HouseFloor`
- Build APK สำเร็จ (`Builds/Quest/CookverseThai_M1.apk` 93 MB) แต่**ลงแว่นไม่ได้** (ดูปัญหาด้านล่าง)
- ทำเวอร์ชันเว็บบน branch `feature/webxr`: WebXR Export + WebXR Interactions 0.25.0, Template `WebXRFullView2020`, Gzip + Decompression Fallback (ไฟล์ 28 MB)
- ขึ้นเว็บที่ Netlify: `https://candid-scone-736ec1.netlify.app` (Public)
- แก้บั๊ก WebXR Export กับ Unity 6.5 (`getWasmTableEntry` หาย) ด้วย `Assets/Plugins/WebGL/WebXRWasmTableFix.jspre`
- แก้ตัวสูงเกินบนเว็บ (Camera Y Offset 1.6 ถูกบวกซ้ำ) ด้วย `WebXRHeightFix.cs` และตั้ง XR Origin เป็น Floor
- จอภารกิจเปลี่ยนจาก Screen Space Overlay (ติดขอบเลนส์ใน WebXR และไม่ขึ้นใน APK) เป็น World Space ลอยตามแบบหน่วง

#### Tested

- แพลตฟอร์ม: **Quest 3 ผ่าน WebXR (Meta Quest Browser)** ครั้งแรก
- เข้า VR ได้ ✅
- ตัวผู้เล่นสูงเกินบ้าน → แก้แล้ว (v4) รอยืนยันในแว่น

#### Problems / decisions

- **Developer Mode เปิดได้เฉพาะบัญชีเจ้าของแว่น (อาจารย์)** บัญชีที่เพิ่มทีหลังเปิดไม่ได้ → ลง APK ไม่ได้ ต้องให้อาจารย์เปิดให้ หรือใช้ WebXR
- Netlify ใช้ระบบเครดิต (เหลือ ~249) → รวมแก้หลายอย่างแล้วค่อย Deploy ครั้งเดียว
- **ค้างแก้ (จดไว้ 27 ต.ค.):**
  - **จอภารกิจจมพื้น:** ตั้งไว้ต่ำกว่าสายตา 0.2 ม. ถ้ากล้องอยู่ต่ำ (เช่น ยังไม่เข้า VR บน Mac หรือความสูงจาก WebXR มาช้า) ป้ายจะจมใต้พื้นและตัวหนังสือถูกตัด → ต้องกันไม่ให้ป้ายต่ำกว่าพื้นที่ยืน และตั้ง Offset 0 เฉพาะตอนเข้า VR จริง
  - ยังไม่ได้ทดสอบในแว่น: ตัวจับ/มือ เดิน จับของ ล้าง หั่น เตา ความลื่น

#### Next work

- รวมแก้: จอภารกิจจมพื้น + ความสูงตอนยังไม่เข้า VR → Build v6 → Deploy
- ทดสอบในแว่นตามเช็กลิสต์ A–F (`QUEST_3_TEST_2026-10-26.md`)

### 28 ต.ค. 2026 — เก็บกวาดไฟล์ซ้ำ/โฟลเดอร์ว่าง (แชท M1)

Branch: `feature/gameplay-content-ui`

#### Completed

- ลบ `Assets/MainVR.unity` (ฉากเปล่าจาก VR template 7 ก.ย. มีแค่ Directional Light, Main Camera, TestFloor) ตรวจแล้วไม่มีไฟล์ใดอ้าง GUID `e4c6f7fc…` · ฉากหลักตัวจริงคือ `Assets/Scenes/MainVR.unity` ไม่ถูกแตะ
- ลบโฟลเดอร์ว่าง `Assets/Gameplay/Data/New Folder` (+ .meta)

#### Problems / decisions

- ไม่พบ `Builds/Quest/CookverseThai_M1.apk` ในเครื่องแล้ว → ต้อง Build ใหม่เมื่อจะลงแว่น (M5)
- ไฟล์ใน `Builds/` (เช่น `zisd9tgA`, `untitled folder`, WedXrV3–V5 ซ้อนใน WedXr1) ยังไม่แตะ — อยู่นอก git
- `Assets/Gameplay/Prefabs/NPC` โฟลเดอร์ว่าง เก็บไว้สำหรับป้าแสง

### 29 ต.ค. 2026 (00:10) — แยกไฟล์ M5 ออกจาก Gameplay (แชท M1)

Branch: `feature/webxr` (`4bf8ca4`) และ cherry-pick เข้า `feature/gameplay-content-ui`

#### Completed

- ย้าย `WebXRHeightFix.cs` → `Assets/XR-Web/Scripts/` · `M1TrackingOriginEditor.cs`, `M1StairsRampEditor.cs` → `Assets/XR-Web/Editor/` (พร้อม .meta, GUID เดิม, hash เนื้อหาตรงกัน)
- แก้ path ใน `Docs/Planning/VR/WEBXR_2026-10-27.md` · เพิ่มหัวข้อไฟล์ของ M5 และแถว `feature/webxr` ใน `PROJECT_CONTEXT.md`

#### Tested

- ตรวจก่อนย้าย: ไม่มี asmdef, ไม่มีฉาก/Prefab อ้าง GUID ของ 3 ไฟล์, ไม่มีโค้ดอื่นเรียกชื่อคลาส · Unity ไม่ได้เปิดอยู่ตอนย้าย
- ยังไม่ได้เปิด Unity ยืนยันคอมไพล์ → เปิดแล้วดู Console + ลองเมนู `Tools > Cookverse Thai > M1 > Fix ...` ว่ายังขึ้น

#### Problems / decisions

- พบการสลับ branch ไปมาตอน 00:00–00:01 (ไม่ได้มาจากแชท M1) → ตั้งกติกาทำทีละแชทและเช็ก branch ก่อนแก้
- บน `feature/webxr` ยังมี `Assets/MainVR.unity` และ `New Folder.meta` (ลบแค่บน branch M1 ใน `d4c339c`) จะหายเมื่อ merge

---

## Daily entry template

### วันที่ — หัวข้องาน

Branch / commit:

#### Completed

- 

#### Tested

- แพลตฟอร์ม: Simulator / แว่นจริง (APK) / WebXR (ระบุอุปกรณ์และเบราว์เซอร์)
- 

#### Problems / decisions

- 

#### Next work

- 
