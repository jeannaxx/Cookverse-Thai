using System.Linq;
using UnityEngine;

// ของในฉากที่ใช้เฉพาะบางเมนู (เช่น ขวดซอสหอยนางรม = กะเพรา · หม้อบนเตา = ต้มยำ)
// MenuSceneSwitcher เปิด/ปิดตามเมนูปัจจุบัน · rendererOnly = ซ่อนแค่ภาพ คง Collider ไว้ (เช่น ฐานกระทะที่รองของในหม้อ)
public class MenuOnly : MonoBehaviour
{
    [SerializeField] private string[] menuIds = { MenuCatalog.KrapowChickenId };
    [SerializeField] private bool rendererOnly;

    public void Configure(string[] ids, bool onlyRenderers)
    {
        menuIds = ids;
        rendererOnly = onlyRenderers;
    }

    public bool UsedIn(string menuId) => menuIds != null && menuIds.Contains(menuId);

    public void Apply(string menuId)
    {
        bool show = UsedIn(menuId);

        if (rendererOnly)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = show;
            }
        }
        else if (gameObject.activeSelf != show)
        {
            gameObject.SetActive(show);
        }
    }
}
