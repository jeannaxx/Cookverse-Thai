using UnityEngine;

// เปิด/ปิดของในฉากตามเมนูที่ ObjectiveManager ใช้อยู่ (MenuOnly) · เรียกตอนเริ่มและเมื่อเปลี่ยนเมนู
[DefaultExecutionOrder(-50)]
public class MenuSceneSwitcher : MonoBehaviour
{
    [SerializeField] private ObjectiveManager objectiveManager;

    private string appliedMenuId;

    public void Configure(ObjectiveManager objective) => objectiveManager = objective;

    private void Awake()
    {
        if (objectiveManager == null) objectiveManager = FindAnyObjectByType<ObjectiveManager>();
        Apply();
    }

    private void OnEnable()
    {
        if (objectiveManager != null) objectiveManager.ObjectiveChanged += OnChanged;
    }

    private void OnDisable()
    {
        if (objectiveManager != null) objectiveManager.ObjectiveChanged -= OnChanged;
    }

    private void OnChanged()
    {
        if (objectiveManager != null && objectiveManager.Menu.menuId != appliedMenuId) Apply();
    }

    [ContextMenu("Apply Current Menu")]
    public void Apply()
    {
        if (objectiveManager == null) return;
        Apply(objectiveManager.Menu.menuId);
    }

    public void Apply(string menuId)
    {
        appliedMenuId = menuId;
        foreach (MenuOnly item in FindObjectsByType<MenuOnly>(FindObjectsInactive.Include))
        {
            item.Apply(menuId);
        }
    }
}
