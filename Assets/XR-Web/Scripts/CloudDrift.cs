using UnityEngine;

// M5: หมุนกลุ่มเมฆรอบฉากช้า ๆ (ใช้กับ Countryside_M5/Clouds) · ถูกมาก หมุน Transform เดียว
public sealed class CloudDrift : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 0.35f;

    private void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
