using System.Collections.Generic;
using UnityEngine;

// M5: ป้ายกำกับโมเดลครัว (Meshy) ที่วางทับของเดิม — จำว่าซ่อน Renderer ตัวไหนไว้ เพื่อถอดกลับได้
// สร้าง/ลบโดย Tools › Cookverse Thai › M5 › Kitchen (M5KitchenModelsEditor) ไม่มีโค้ดตอนเล่น
public sealed class M5KitchenDressed : MonoBehaviour
{
    public string modelName;
    public List<Renderer> hiddenRenderers = new List<Renderer>();
}
