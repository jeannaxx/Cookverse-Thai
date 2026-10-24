// ระบบที่ต้องกลับสู่สภาพเริ่มต้นเมื่อรีเซ็ตภารกิจ เช่น อ่างล้าง เขียง เตา กระทะ ตู้เย็น
// ObjectiveManager.ResetObjectives เรียกทุกตัวที่อยู่ในฉาก
public interface IMissionResettable
{
    void ResetForMission();
}
