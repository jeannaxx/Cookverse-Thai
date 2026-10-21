# Cookverse Thai — ตั้งค่าและลงเกมบน Meta Quest 3

อัปเดต: 22 ตุลาคม 2026  
อุปกรณ์: Meta Quest 3 + Touch Plus Controller / เครื่องพัฒนา Mac

คู่มือนี้ใช้ทุกครั้งที่ต้อง Build APK ลงแว่น ส่วนเช็กลิสต์ของแต่ละรอบทดสอบแยกเป็นไฟล์ตามวันที่ เช่น [`QUEST_3_TEST_2026-10-27.md`](QUEST_3_TEST_2026-10-27.md)

## สิ่งที่ตั้งไว้ในโปรเจกต์แล้ว (ตรวจจากไฟล์)

Unity 6000.5.9f1, OpenXR 1.17.1, Unity OpenXR Meta 2.5.1 และ XR Interaction Toolkit 3.5.1 อยู่ในโปรเจกต์แล้ว Android ใช้ IL2CPP และ ARM64 OpenXR เปิด Meta Quest Support, Quest 3 และ Touch Plus Controller Profile แล้ว

ข้อมูลนี้มาจากการอ่านไฟล์ **ยังไม่เคย Build APK สำเร็จ** และยังไม่ได้ยืนยันว่าติดตั้ง Android SDK/NDK/JDK ครบ

## เตรียมแว่น

1. ตั้งค่า Quest 3 และจับคู่กับแอป Meta Horizon บนโทรศัพท์
2. ใช้บัญชีนักพัฒนา Meta ที่ยืนยันแล้ว ในแอป Meta Horizon เลือกแว่น → Headset Settings → Developer Mode → เปิด (ชื่อเมนูอาจต่างตามรุ่นแอป)
3. ต่อแว่นกับ Mac ด้วยสาย USB-C ที่ **ส่งข้อมูลได้** สวมแว่นแล้วกดอนุญาต USB Debugging
4. ใช้ Meta Quest Developer Hub บน Mac ตรวจการเชื่อมต่อได้ (macOS ไม่ต้องลงไดรเวอร์)

## ตั้งค่า Unity และ Build

1. Unity Hub → Editor 6000.5.9f1 → Add modules: Android Build Support, Android SDK & NDK Tools, OpenJDK
2. File → Build Profiles → Meta Quest (ถ้าไม่มีให้ใช้ Android) → Switch Platform
3. ตั้ง Scene List ให้ Scene ที่จะทดสอบเป็นลำดับแรก
   - รอบทดสอบระบบ Gameplay (เช่น 27 ต.ค.): `Assets/Scenes/GameplayTest_M1.unity`
   - รอบเดโมฉากหลัก: `Assets/Scenes/MainVR.unity`
   - ปิด `SampleScene` และ `BasicScene`
4. Project Settings → XR Plug-in Management → Android: เปิด OpenXR และ Initialize XR on Startup แล้วแก้ Error ใน Project Validation
5. Player Settings: ตรวจว่าเป็น IL2CPP และ ARM64 อย่าเปลี่ยนเวอร์ชันแพ็กเกจหรือ SDK แบบสุ่ม
6. ตรวจว่า XR Interaction Simulator ไม่ทำงานใน Build อุปกรณ์จริง
7. Commit งานก่อน Build และจด commit SHA
8. Run Device → เลือก Quest 3 (กด Refresh ถ้าไม่ขึ้น) → Build And Run เก็บ APK ไว้ที่ `Builds/Quest3/` (นอกโฟลเดอร์ Assets และไม่ commit)

ถ้าไม่เห็นแว่นใน Run Device ให้ตรวจสาย Developer Mode และการอนุญาต USB Debugging ก่อนแก้อย่างอื่น

> บน Mac ต้อง Build APK แล้วรันบนแว่นเท่านั้น การกด Play ใน Unity แล้วแสดงผลในแว่นผ่าน Meta Link ใช้ได้เฉพาะ Windows

## ข้อควรจำ

- รอบแรกใช้ Controller เป็นหลัก Hand Tracking เป็นการทดสอบเพิ่มเติม
- ผล APK ผ่าน **ไม่เท่ากับ** WebXR ผ่าน WebXR ทดสอบผ่านเบราว์เซอร์บนแว่นและต้องจดรุ่นเบราว์เซอร์
- ไม่ใช้ผล Simulator แทนผลบนแว่นจริง

## เอกสารอ้างอิง

- [Meta: ตั้งค่าแว่น Developer Mode และ Build And Run](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/)
- [Unity OpenXR 1.17: Meta Quest Support](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/features/metaquest.html)
