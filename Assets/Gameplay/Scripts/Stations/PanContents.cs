using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// พื้นที่ในกระทะบนเตา
// - ปล่อยวัตถุดิบที่เตรียมแล้ว (ล้าง/หั่น) ลงในกระทะตอนถึงขั้นปรุง → AddedToPan
// - เครื่องปรุงจากขวด (SeasoningBottle) เท 1 ครั้ง = 1 Portion เทซ้ำไม่นับเพิ่ม
// - การคนจากตะหลิว (SpatulaStirrer): ครั้งแรกหลังใส่เครื่องหอม / ครั้งสุดท้ายหลังใส่กะเพรา
// - ลำดับตรวจโดย CookingOrder ใส่ผิดลำดับยังใส่ได้ แต่บันทึก isCorrect=false พร้อมรหัสใน value
// - ไก่ในกระทะสะสมความร้อนตามระดับไฟของ GasStove เฉพาะตอนเปิดไฟและไก่อยู่ในกระทะ
//   0 = ดิบ · >0 = กำลังสุก · ≥100 = สุก · ≥160 = ไหม้ (ค่าร่าง ปรับใน Inspector)
[RequireComponent(typeof(Collider))]
public class PanContents : MonoBehaviour, IMissionResettable
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private GasStove stove;
    [SerializeField] private CollectionFeedbackUI feedbackUI;

    [Header("Doneness")]
    [SerializeField] private string trackedIngredientId = ObjectiveManager.ChickenIngredientId;
    [SerializeField] private float cookedAtHeat = 100f;
    [SerializeField] private float burnedAtHeat = 160f;

    [Header("Pour / Stir Area")]
    [Tooltip("ขยายขอบกระทะแนวนอนเวลาเช็กว่าปากขวด/ปลายตะหลิวอยู่ในกระทะ (เมตร)")]
    [SerializeField] private float horizontalMargin = 0.05f;
    [Tooltip("ความสูงเหนือกระทะที่ยังนับว่าเทลงกระทะ (เมตร)")]
    [SerializeField] private float pourHeightAbovePan = 0.5f;
    [Tooltip("ขยายขอบกระทะแนวนอนเฉพาะการเทเครื่องปรุง (เมตร) · กว้างกว่าการคน เพราะปากขวดเล็งยากในแว่น")]
    [SerializeField] private float pourHorizontalMargin = 0.12f;

    private readonly Dictionary<Collider, IngredientIdentity> colliders = new();
    private readonly HashSet<IngredientIdentity> inPan = new();
    private readonly HashSet<IngredientIdentity> rejected = new();
    private readonly Dictionary<IngredientIdentity, float> heat = new();
    private readonly CookingOrder order = new();
    private Collider zoneCollider;

    public IReadOnlyCollection<IngredientIdentity> Contents => inPan;
    public CookingOrder Order => order;
    public int StirCount { get; private set; }
    public bool HasAnything => order.Added.Count > 0;
    public bool Plated { get; private set; }
    public int StoveLevel => stove != null ? stove.Level : 0;

    public float GetHeat(IngredientIdentity ingredient)
    {
        return ingredient != null && heat.TryGetValue(ingredient, out float value) ? value : 0f;
    }

    public void Configure(ObjectiveManager objective, GasStove gasStove, CollectionFeedbackUI feedback)
    {
        objectiveManager = objective;
        stove = gasStove;
        feedbackUI = feedback;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
    }

    public string CurrentStepId => objectiveManager != null ? objectiveManager.CurrentStepId : string.Empty;
    private string HeatValue => stove != null ? $"heat={stove.Level}" : "heat=?";

    // ---------- พื้นที่กระทะ ----------

    private Bounds PanBounds
    {
        get
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider>();
            }

            return zoneCollider.bounds;
        }
    }

    // ปลายตะหลิวอยู่ในกระทะ
    public bool ContainsPoint(Vector3 point)
    {
        Bounds b = PanBounds;
        b.Expand(new Vector3(horizontalMargin * 2f, 0.1f, horizontalMargin * 2f));
        return b.Contains(point);
    }

    // ปากขวดอยู่เหนือกระทะ (ในแนวนอนอยู่ในขอบกระทะ และสูงไม่เกิน pourHeightAbovePan)
    public bool IsAbovePan(Vector3 point)
    {
        Bounds b = PanBounds;
        float margin = Mathf.Max(horizontalMargin, pourHorizontalMargin);
        bool insideXZ =
            point.x >= b.min.x - margin && point.x <= b.max.x + margin &&
            point.z >= b.min.z - margin && point.z <= b.max.z + margin;
        return insideXZ && point.y >= b.min.y && point.y <= b.max.y + pourHeightAbovePan;
    }

    // ---------- วัตถุดิบที่ปล่อยลงกระทะ ----------

    private void OnTriggerEnter(Collider other)
    {
        IngredientIdentity ingredient = FindIngredient(other);

        if (ingredient != null && !ingredient.IsResetting)
        {
            colliders[other] = ingredient;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!colliders.TryGetValue(other, out IngredientIdentity ingredient))
        {
            return;
        }

        colliders.Remove(other);

        if (!colliders.ContainsValue(ingredient))
        {
            rejected.Remove(ingredient);
        }
    }

    private void Update()
    {
        IngredientIdentity[] inside = colliders.Values
            .Where(i => i != null)
            .Distinct()
            .ToArray();

        foreach (IngredientIdentity ingredient in inside)
        {
            if (!inPan.Contains(ingredient) && !rejected.Contains(ingredient) && !IsHeld(ingredient))
            {
                TryAdd(ingredient);
            }
        }

        float rate = stove != null ? stove.HeatRate : 0f;

        if (rate <= 0f)
        {
            return;
        }

        // ร้อนเฉพาะของที่ยังอยู่ในกระทะ ยกออกแล้วหยุดสุก
        foreach (IngredientIdentity ingredient in inside)
        {
            if (!inPan.Contains(ingredient) || ingredient.IngredientId != trackedIngredientId)
            {
                continue;
            }

            heat.TryGetValue(ingredient, out float value);
            value += rate * Time.deltaTime;
            heat[ingredient] = value;
            UpdateDoneness(ingredient, value);
        }
    }

    private void TryAdd(IngredientIdentity ingredient)
    {
        string stepId = CurrentStepId;
        IngredientState before = ingredient.CurrentState;

        string reason = "ระบบเป้าหมายยังไม่พร้อม";

        if (objectiveManager == null || !objectiveManager.CanAddToPan(ingredient, out reason))
        {
            rejected.Add(ingredient);
            ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredient,
                before, before, isCorrect: false, value: reason);
            Show($"{ingredient.DisplayNameThai}: {reason}", false);
            return;
        }

        string mistake = order.Evaluate(ingredient.IngredientId);
        order.MarkAdded(ingredient.IngredientId);

        ingredient.TrySetState(IngredientState.AddedToPan);
        inPan.Add(ingredient);
        heat[ingredient] = 0f;

        ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredient,
            before, IngredientState.AddedToPan, isCorrect: mistake == null,
            value: OrderValue(mistake));
        objectiveManager.RecordCookingProgress(ingredient.IngredientId, IngredientState.AddedToPan);
        ShowAdded(ingredient.DisplayNameThai, mistake);
    }

    // ---------- เครื่องปรุงจากขวด ----------

    public bool AddSeasoning(string ingredientId, string displayNameThai, string instanceId)
    {
        string stepId = CurrentStepId;

        string reason = "ระบบเป้าหมายยังไม่พร้อม";

        if (objectiveManager == null || !objectiveManager.CanCookNow(out reason))
        {
            ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredientId, instanceId,
                isCorrect: false, value: reason);
            Show($"{displayNameThai}: {reason}", false);
            return false;
        }

        if (order.Has(ingredientId))
        {
            ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredientId, instanceId,
                IngredientState.AddedToPan, IngredientState.AddedToPan,
                isCorrect: false, value: OrderValue(CookingOrder.Duplicate));
            Show($"ใส่{displayNameThai}ไปแล้ว ไม่ต้องใส่ซ้ำ", false);
            return false;
        }

        string mistake = order.Evaluate(ingredientId);
        order.MarkAdded(ingredientId);

        ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredientId, instanceId,
            IngredientState.Raw, IngredientState.AddedToPan,
            isCorrect: mistake == null, value: OrderValue(mistake));
        objectiveManager.RecordCookingProgress(ingredientId, IngredientState.AddedToPan);
        ShowAdded(displayNameThai, mistake);
        return true;
    }

    // ---------- การคน ----------

    public void RegisterStir()
    {
        string stepId = CurrentStepId;

        if (objectiveManager == null || !objectiveManager.CanCookNow(out _))
        {
            return;
        }

        if (!HasAnything)
        {
            Show("ยังไม่มีอะไรในกระทะให้คน", false);
            return;
        }

        StirCount++;
        string stirValue;

        if (!order.AromaticsStirred && order.AromaticsInPan)
        {
            order.AromaticsStirred = true;
            stirValue = "aromatics";
            objectiveManager.RecordCookingProgress(ObjectiveManager.StirAromaticsId, IngredientState.Cooking);
            Show("คนเครื่องหอมแล้ว ใส่ไก่ได้เลย", true);
        }
        else if (!order.FinalStirred && order.Has(ObjectiveManager.KrapowLeafIngredientId))
        {
            order.FinalStirred = true;
            stirValue = "final";
            objectiveManager.RecordCookingProgress(ObjectiveManager.StirFinalId, IngredientState.Cooking);
            Show("คนให้เข้ากันแล้ว ปิดเตาได้เลย", true);
        }
        else
        {
            stirValue = "extra";
            Show("คนต่อ...", true);
        }

        ActionLog.Record(ActionLog.Actions.FoodStirred, stepId,
            value: $"{stirValue};count={StirCount};{HeatValue}");
    }

    // ---------- ตักใส่จาน ----------

    // ตักได้เมื่อมีของในกระทะ ยังไม่เคยตัก และ (ปิดเตาแล้ว หรือปรุงครบขั้นปรุงรสแล้ว)
    // ปรุงครบแต่ไม่ปิดเตา ยังตักได้ แต่ DishPlated บันทึก heat>0 เป็นข้อผิดพลาด
    public bool CanScoop(out string reason)
    {
        if (objectiveManager == null || !objectiveManager.MissionAccepted || !HasAnything)
        {
            reason = "ยังไม่มีอาหารในกระทะ";
            return false;
        }

        if (Plated)
        {
            reason = "ตักใส่จานไปแล้ว";
            return false;
        }

        bool cookingDone = objectiveManager.IsStepComplete(ObjectiveManager.CookSeasonStepId);

        if (StoveLevel > 0 && !cookingDone)
        {
            reason = "ปิดเตาก่อนตักใส่จาน";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    // ย้ายอาหารจากกระทะลงจาน: สรุปจาน · บันทึก DishPlated · ย้ายวัตถุดิบออกจากฉาก (Reset คืนเอง)
    public DishSummary TakeDishForPlating()
    {
        if (!CanScoop(out string reason))
        {
            Show(reason, false);
            return null;
        }

        var summary = new DishSummary
        {
            HeatLevel = StoveLevel,
            ChickenState = ChickenStateInPan(),
            OrderMistakes = 0
        };

        foreach (string id in RequiredForDish)
        {
            if (!order.Has(id))
            {
                summary.Missing.Add(id);
            }
        }

        if (!order.AromaticsStirred) summary.Missing.Add(ObjectiveManager.StirAromaticsId);
        if (!order.FinalStirred) summary.Missing.Add(ObjectiveManager.StirFinalId);

        Plated = true;

        foreach (IngredientIdentity ingredient in inPan)
        {
            if (ingredient == null)
            {
                continue;
            }

            ingredient.TrySetState(IngredientState.Served);
            ParkIngredient(ingredient);
        }

        colliders.Clear();

        ActionLog.Record(ActionLog.Actions.DishPlated, CurrentStepId,
            isCorrect: summary.IsCorrect, value: summary.ToLogValue());
        objectiveManager.RecordServeProgress(ObjectiveManager.DishPlatedId);
        Show(summary.HeatLevel > 0 ? "ตักใส่จานแล้ว แต่ยังไม่ได้ปิดเตา" : "ตักใส่จานแล้ว นำไปเสิร์ฟป้าแสง", summary.HeatLevel == 0);
        return summary;
    }

    private static readonly string[] RequiredForDish =
    {
        ObjectiveManager.CookingOilIngredientId,
        ObjectiveManager.GarlicIngredientId,
        ObjectiveManager.ChiliIngredientId,
        ObjectiveManager.ChickenIngredientId,
        ObjectiveManager.OysterSauceIngredientId,
        ObjectiveManager.FishSauceIngredientId,
        ObjectiveManager.SugarIngredientId,
        ObjectiveManager.KrapowLeafIngredientId
    };

    private IngredientState ChickenStateInPan()
    {
        foreach (IngredientIdentity ingredient in inPan)
        {
            if (ingredient != null && ingredient.IngredientId == trackedIngredientId)
            {
                return ingredient.CurrentState;
            }
        }

        return IngredientState.Raw;
    }

    // ซ่อนวัตถุดิบที่ตักไปแล้ว: ย้ายไปใต้ฉากและตรึงไว้ · ResetIngredient คืนตำแหน่ง/ฟิสิกส์เดิม
    private void ParkIngredient(IngredientIdentity ingredient)
    {
        var body = ingredient.GetComponent<Rigidbody>();

        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
        }

        ingredient.transform.position = transform.position + Vector3.down * 20f;

        if (body != null)
        {
            body.position = ingredient.transform.position;
        }
    }

    // ---------- ความสุกของไก่ ----------

    private void UpdateDoneness(IngredientIdentity ingredient, float value)
    {
        IngredientState current = ingredient.CurrentState;

        if (value >= burnedAtHeat)
        {
            if (current == IngredientState.Burned)
            {
                return;
            }

            ingredient.TrySetState(IngredientState.Burned);
            LogFoodState(ingredient, current, IngredientState.Burned, value, false);
            Show($"{ingredient.DisplayNameThai}ไหม้แล้ว เริ่มการปรุงใหม่อีกครั้ง", false);
            return;
        }

        if (value >= cookedAtHeat)
        {
            if (current == IngredientState.Cooked || current == IngredientState.Burned)
            {
                return;
            }

            ingredient.TrySetState(IngredientState.Cooked);
            LogFoodState(ingredient, current, IngredientState.Cooked, value, true);
            order.ChickenCooked = true;
            if (objectiveManager != null)
            {
                objectiveManager.RecordCookingProgress(ingredient.IngredientId, IngredientState.Cooked);
            }
            Show($"{ingredient.DisplayNameThai}สุกแล้ว ปรุงรสต่อได้ อย่าปล่อยไว้นานจนไหม้", true);
            return;
        }

        if (current == IngredientState.AddedToPan)
        {
            ingredient.TrySetState(IngredientState.Cooking);
            LogFoodState(ingredient, current, IngredientState.Cooking, value, true);
            Show($"กำลังผัด{ingredient.DisplayNameThai}...", true);
        }
    }

    private void LogFoodState(
        IngredientIdentity ingredient,
        IngredientState before,
        IngredientState after,
        float value,
        bool isCorrect)
    {
        ActionLog.Record(
            ActionLog.Actions.FoodStateChanged,
            CurrentStepId,
            ingredient, before, after, isCorrect, $"heat={value:0}");
    }

    // ---------- ข้อความ ----------

    private string OrderValue(string mistake)
    {
        return mistake == null ? HeatValue : $"order={mistake};{HeatValue}";
    }

    private void ShowAdded(string displayNameThai, string mistake)
    {
        if (mistake == null)
        {
            Show($"ใส่{displayNameThai}ลงกระทะ", true);
        }
        else
        {
            Show($"ใส่{displayNameThai}แล้ว แต่ลำดับยังไม่ถูก: {CookingOrder.DescribeThai(mistake)}", false);
        }
    }

    public void ShowHint(string message)
    {
        Show(message, false);
    }

    private void Show(string message, bool success)
    {
        Debug.Log($"[กระทะ] {message}", this);
        feedbackUI?.ShowStationMessage(message, success);
    }

    private static bool IsHeld(IngredientIdentity ingredient)
    {
        var grab = ingredient.GetComponent<XRGrabInteractable>();
        return grab != null && grab.isSelected;
    }

    private static IngredientIdentity FindIngredient(Collider other)
    {
        IngredientIdentity ingredient = other.GetComponentInParent<IngredientIdentity>();

        if (ingredient != null || other.attachedRigidbody == null)
        {
            return ingredient;
        }

        return other.attachedRigidbody.GetComponent<IngredientIdentity>();
    }

    public void ResetForMission()
    {
        colliders.Clear();
        inPan.Clear();
        rejected.Clear();
        heat.Clear();
        order.Clear();
        StirCount = 0;
        Plated = false;
    }
}
