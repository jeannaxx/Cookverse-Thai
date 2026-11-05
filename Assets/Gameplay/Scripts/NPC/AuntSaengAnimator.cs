using UnityEngine;

// ท่าทางป้าแสง (โมเดล Meshy, Rig Humanoid) · ใส่ไว้ที่ราก Prefab AuntSaeng คู่กับ Animator
// - Idle วนตลอด (สถานะเริ่มต้นใน AuntSaeng.controller)
// - PlayWave: ผู้เล่นเดินเข้าพื้นที่สนทนา (MissionInteractionZone.onPlayerEntered)
// - PlayTalk: ป้าแสงให้ภารกิจ/พูด (MissionGiver.onMissionRequested) · ถ้ากำลังโบกมือ จะพูดต่อหลังโบกจบ
// - PlayAgree: ภารกิจสำเร็จ (ObjectiveManager.MissionCompleted) · ยังไม่มีท่า Agree → ใช้ Talk แทน
// - หันหน้าตามผู้เล่น (แกน Y) เมื่ออยู่ใกล้ faceDistance · ไกลแล้วค่อย ๆ หันกลับท่าเดิม
public class AuntSaengAnimator : MonoBehaviour
{
    public const string WaveTrigger = "Wave";
    public const string TalkTrigger = "Talk";
    public const string AgreeTrigger = "Agree";

    [SerializeField] private Animator animator;

    [Header("Face Player")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField, Min(0f)] private float faceDistance = 3.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 120f;   // องศาต่อวินาที

    private Transform player;
    private Quaternion homeRotation;
    private ObjectiveManager objectiveManager;
    private bool missionWasCompleted;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        homeRotation = transform.rotation;
    }

    private void OnEnable()
    {
        objectiveManager = FindFirstObjectByType<ObjectiveManager>();
        if (objectiveManager != null)
        {
            missionWasCompleted = objectiveManager.MissionCompleted;
            objectiveManager.ObjectiveChanged += OnObjectiveChanged;
        }
    }

    private void OnDisable()
    {
        if (objectiveManager != null) objectiveManager.ObjectiveChanged -= OnObjectiveChanged;
    }

    public void PlayWave() => SetTrigger(WaveTrigger);

    public void PlayTalk() => SetTrigger(TalkTrigger);

    public void PlayAgree()
    {
        if (!SetTrigger(AgreeTrigger)) SetTrigger(TalkTrigger);
    }

    private void OnObjectiveChanged()
    {
        bool done = objectiveManager.MissionCompleted;
        if (done && !missionWasCompleted) PlayAgree();
        missionWasCompleted = done;
    }

    private bool SetTrigger(string trigger)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
            {
                animator.SetTrigger(trigger);
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (!facePlayer) return;
        if (player == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            player = cam.transform;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        Quaternion target = toPlayer.sqrMagnitude <= faceDistance * faceDistance && toPlayer.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(toPlayer.normalized, Vector3.up)
            : homeRotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
