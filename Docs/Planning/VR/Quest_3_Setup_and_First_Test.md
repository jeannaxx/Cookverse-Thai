# ตั้งค่าและทดสอบ Meta Quest 3 ครั้งแรก — SiamCulinaryXR

อุปกรณ์เป้าหมาย: Quest 3 รุ่นปกติ + Touch Plus / เครื่องพัฒนา Mac M5; M1 พัฒนาโมดูลและทดสอบตามชุดเดียวกันได้

## สิ่งที่ตรวจจากไฟล์แล้ว

Unity 6000.5.9f1, OpenXR 1.17.1, Unity OpenXR Meta 2.5.1 และ XR Interaction Toolkit 3.5.1 อยู่ในโปรเจกต์แล้ว Android ใช้ IL2CPP และ ARM64; ไฟล์ OpenXR เปิด Meta Quest Support, Quest 3 และ Meta Quest Touch Plus Controller Profile แล้ว ข้อมูลนี้เป็นการตรวจไฟล์ ไม่ใช่ผล build หรือการเชื่อมแว่น ยังไม่ได้ยืนยันว่า Android SDK/NDK/JDK ติดตั้งครบ

## เตรียมแว่น

1. ตั้งค่า Quest 3 และจับคู่ Meta Horizon บนโทรศัพท์ให้เรียบร้อย
2. ใช้บัญชีนักพัฒนา Meta ที่ผ่านการยืนยัน จาก Meta Horizon เลือกแว่น → Headset Settings → Developer Mode → เปิด (ชื่อเมนูอาจเปลี่ยนตามรุ่นแอป)
3. ต่อแว่นกับ Mac ด้วยสาย USB-C ที่ส่งข้อมูลได้ สวมแว่นและอนุญาต USB debugging สำหรับ Mac เครื่องนี้
4. ใช้ Meta Quest Developer Hub บน Mac ช่วยตรวจการเชื่อมต่อได้; macOS ไม่ต้องลง Oculus USB driver ของ Windows

## ตั้งค่า Unity และติดตั้งเกม

1. Unity Hub → Editor 6000.5.9f1 → Add modules: ตรวจ Android Build Support, Android SDK & NDK Tools และ OpenJDK
2. เปิด MainVR แล้ว File → Build Profiles → Meta Quest; ถ้าไม่พบใช้ Android และ Switch Platform
3. ตรวจ Scene List ให้ MainVR เปิดใช้งานและเป็นฉากแรกของ build ทดสอบ ไม่ให้ GameplayTest_M1 หรือ SampleScene เปิดแทน
4. Project Settings → XR Plug-in Management → Android: ตรวจ OpenXR และ Initialize XR on Startup; ใน OpenXR ตรวจ Meta Quest Support / Quest 3 / Touch Plus และแก้ข้อผิดพลาดใน Project Validation ที่เกี่ยวข้อง
5. ตรวจ Player Settings ว่า IL2CPP และ ARM64; ใช้ API/Graphics ที่รองรับตาม Validation ของแพ็กเกจรุ่นที่ติดตั้ง อย่าเปลี่ยนเวอร์ชันแพ็กเกจหรือ SDK แบบสุ่ม
6. ตรวจว่า XR Interaction Simulator ไม่แทรก input จำลองใน build อุปกรณ์จริง และ XR Rig รับ input จากคอนโทรลเลอร์จริง
7. ใน Run Device เลือก Quest 3 (Refresh ถ้าไม่ขึ้น) แล้ว Build And Run บันทึก APK ในโฟลเดอร์ build ที่ทีมใช้
8. ถ้าไม่เห็นอุปกรณ์: ตรวจสายส่งข้อมูล, Developer Mode, การอนุญาต USB debugging และสถานะเชื่อมต่อก่อนแก้ระบบเกม

Mac ใช้ build APK แล้วรันบนแว่นโดยตรง Meta Link ที่แสดงผลจาก Unity Editor รองรับ Windows เท่านั้น การเสียบสายกับ Mac จึงไม่ได้ทำให้ปุ่ม Play แสดงในแว่นอัตโนมัติ

## เกณฑ์ทดสอบรอบแรก (ประมาณ 20–30 นาทีหลัง build ผ่าน)

- [ ] เปิดแอปเข้า MainVR ได้ในโหมด VR และภาพตามการขยับศีรษะ
- [ ] ระดับพื้น/ความสูงผู้เล่นและขนาดบ้านเหมาะสม
- [ ] มือ/คอนโทรลเลอร์ซ้ายขวาปรากฏและปุ่มตอบสนอง
- [ ] เดิน หมุน Teleport ขึ้นบันได ผ่านประตู และชนผนังได้ตามระบบที่ทำไว้
- [ ] หยิบ/ปล่อยกะเพราได้ ของตกชนพื้น ไม่หายหรือค้างกับมือ
- [ ] หากรวม Collection Zone แล้ว: วางถูกและผิดแสดง UI ถูกต้อง อ่านภาษาไทยได้
- [ ] ปิดเปิดแอปใหม่แล้วยังเริ่มได้; จดอาการกระตุกหรือเวียนหัวเพื่อปรับการเคลื่อนที่

บันทึกวันที่, commit/build, รุ่น Quest OS, scene, ผลแต่ละข้อ และภาพ/คลิป/Log ที่มี ขั้นไหนไม่ผ่านให้ระบุว่าไม่ผ่าน ไม่ใช้ผล Simulator แทน

รอบแรกใช้ controllers เป็นฐาน ไม่ต้องรอ Hand Tracking, ระบบปรุงอาหาร หรือ AI ครบ การทดสอบ WebXR เป็นอีกเส้นทางหนึ่งในเบราว์เซอร์ของ Quest 3 ต้องบันทึกรุ่นเบราว์เซอร์ด้วย ผล APK ผ่านไม่เท่ากับ WebXR ผ่าน

## เอกสารทางการที่ใช้อ้างอิง

- [Meta: ตั้งค่าแว่น, Developer Mode, Build And Run และข้อจำกัด Link บน macOS](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/)
- [Unity OpenXR 1.17: Meta Quest Support](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/features/metaquest.html)

ปรับเอกสารวันที่ 18 ก.ย. 2026; ยังไม่ได้เปลี่ยน Project Settings หรือติดตั้งโปรแกรมให้เครื่อง
