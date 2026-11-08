using UnityEngine;

// สายน้ำจากก๊อก + หยดน้ำกระเซ็น ใช้เป็น workingVisual ของอ่างล้าง (StationZone)
// StationZone เปิด GameObject นี้เมื่อมีผักอยู่ใต้ก๊อก และปิดเมื่อล้างเสร็จ/เอาออก
// - สายน้ำไหลจาก spout ลงไปจนชนผัก (มี IngredientIdentity) หรือถึงก้นอ่าง (basin)
// - หยดน้ำกระเซ็นตรงจุดที่น้ำกระทบ · สายน้ำสั่นเล็กน้อยให้ดูมีชีวิต
// วัสดุตั้งจาก Editor (M1WashSinkEditor) เพื่อให้ Shader ถูก Build ไปด้วยบนเว็บ
public class WaterStreamFx : MonoBehaviour
{
    [SerializeField] private Transform spout;
    [SerializeField] private Transform basin;
    [SerializeField] private Transform stream;          // Cylinder สูง 2 หน่วยที่สเกล 1
    [SerializeField] private ParticleSystem splash;
    [SerializeField, Min(0.002f)] private float radius = 0.009f;

    public void Configure(Transform spoutPoint, Transform basinPoint, Transform streamMesh, ParticleSystem splashFx)
    {
        spout = spoutPoint;
        basin = basinPoint;
        stream = streamMesh;
        splash = splashFx;
    }

    private void OnEnable()
    {
        UpdateStream();
        if (splash != null) splash.Play(true);
    }

    private void OnDisable()
    {
        if (splash != null) splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LateUpdate() => UpdateStream();

    private void UpdateStream()
    {
        if (spout == null || basin == null || stream == null) return;

        Vector3 top = spout.position;
        float maxDrop = Mathf.Max(0.02f, top.y - basin.position.y);
        float drop = maxDrop;

        // น้ำกระทบผักที่ถืออยู่ใต้ก๊อกก่อนถึงก้นอ่าง
        RaycastHit[] hits = Physics.RaycastAll(top, Vector3.down, maxDrop, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.distance < drop && h.collider.GetComponentInParent<IngredientIdentity>() != null) drop = h.distance;
        }

        float wobble = 1f + Mathf.Sin(Time.time * 37f) * 0.08f;
        stream.position = top + Vector3.down * (drop / 2f);
        stream.rotation = Quaternion.identity;
        stream.localScale = Vector3.one;   // ป้องกันสเกลแม่ (ตั้งเป็น world ด้านล่าง)
        Vector3 parentScale = stream.parent != null ? stream.parent.lossyScale : Vector3.one;
        stream.localScale = new Vector3(radius * 2f * wobble / parentScale.x, drop / 2f / parentScale.y, radius * 2f / wobble / parentScale.z);

        if (splash != null) splash.transform.position = top + Vector3.down * drop;
    }
}
