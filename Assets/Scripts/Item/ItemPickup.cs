using UnityEngine;

/// <summary>
/// 플레이어가 상호작용하여 획득할 수 있는 아이템을 관리한다.
/// 일정 시간 동안 Interact 버튼을 유지하면 아이템을 획득한다.
/// </summary>
public class ItemPickup : MonoBehaviour, IHoldInteractable
{
    [Header("Item Data")]
    [SerializeField] private ItemData itemData;

    // 맵마다 배치된 아이템을 식별하기 위한 고유 ID (인스펙터에서 겹치지 않게 작성)
    // 예: "School_Key_1층화장실", "House_Water_거실"
    [SerializeField] private string uniquePickupID;

    [Header("Acquire Dialog")]
    [SerializeField] private DialogData acquireDialog;

    [Header("Hold Interaction Settings")]
    [SerializeField] private float holdDuration = 3f;

    [Header("Collect Sound ID")]
    [SerializeField] private string collectSoundID = "Item_Get";

    public float HoldDuration => holdDuration;
    public float HoldProgress =>
        holdDuration <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / holdDuration);

    private float holdTimer;
    private bool isCollected;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.GetFlag(uniquePickupID))
        {
            isCollected = true;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 상호작용 버튼을 처음 눌렀을 때 호출된다.
    /// 아이템 획득 진행 시간을 초기화한다.
    /// </summary>
    public void OnInteractPressed()
    {
        if (isCollected)
            return;

        holdTimer = 0f;
    }

    /// <summary>
    /// 상호작용 버튼을 누르고 있는 동안 호출된다.
    /// 일정 시간 이상 유지하면 아이템을 획득한다.
    /// </summary>
    public void OnInteractHeld()
    {
        if (isCollected)
            return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdDuration)
        {
            Collect();
        }
    }

    /// <summary>
    /// 상호작용 버튼을 놓았을 때 호출된다.
    /// 획득 진행도를 초기화한다.
    /// </summary>
    public void OnInteractReleased()
    {
        if (isCollected)
            return;

        holdTimer = 0f;
    }

    /// <summary>
    /// 아이템을 획득하고 Inventory에 등록한 후
    /// 아이템 이미지와 대화 연출을 재생한다.
    /// </summary>
    private void Collect()
    {
        if (isCollected || itemData == null) return;

        isCollected = true;
        SoundManager.Instance?.PlaySFX(collectSoundID);
        holdTimer = 0f;

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log($"{name} 아이템 획득");

        InventoryManager.Instance?.AddItem(itemData);

        if (SaveManager.Instance != null && !string.IsNullOrWhiteSpace(uniquePickupID))
        {
            SaveManager.Instance.SetFlag(uniquePickupID, true);
        }

        anim?.SetTrigger("Collect");

        DialogSystem.Instance.PlayIllustrationDialog(
            itemData.Illustration,
            acquireDialog,
            OnIllustrationFinished
        );
    }

    /// <summary>
    /// 이미지 및 대화 연출이 종료되었을 때 호출된다.
    /// </summary>
    private void OnIllustrationFinished()
    {
        gameObject.SetActive(false);
    }
}