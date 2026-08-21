using UnityEngine;

/// <summary>
/// 플레이어가 상호작용하여 획득할 수 있는 아이템을 관리한다.
/// 일정 시간 동안 Interact 버튼을 유지하면 아이템을 획득한다.
/// </summary>
public class ItemPickup : MonoBehaviour, IInteractable, IHoldInteractable
{
    [Header("Prompt Settings")]

    /// <summary>
    /// 상호작용 안내 UI가 표시될 위치.
    /// </summary>
    [SerializeField] private Transform promptPoint;

    /// <summary>
    /// 상호작용 안내 UI가 사용할 위치를 반환한다.
    /// </summary>
    public Transform PromptPoint => promptPoint;

    [Header("Item Data")]

    /// <summary>
    /// 획득할 아이템 데이터.
    /// </summary>
    [SerializeField] private ItemData itemData;

    [Header("Acquire Illustration")]

    /// <summary>
    /// 아이템 획득 시 표시할 대화 데이터.
    /// </summary>
    [SerializeField] private DialogData acquireDialog;

    [Header("Hold Interaction Settings")]

    /// <summary>
    /// 아이템 획득에 필요한 상호작용 유지 시간.
    /// </summary>
    [SerializeField] private float holdDuration = 3f;

    /// <summary>
    /// 아이템 획득에 필요한 상호작용 유지 시간.
    /// </summary>
    public float HoldDuration => holdDuration;

    /// <summary>
    /// 현재 아이템 획득 진행률.
    /// </summary>
    public float HoldProgress =>
        holdDuration <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / holdDuration);

    /// <summary>
    /// 현재 상호작용 유지 시간.
    /// </summary>
    private float holdTimer;

    /// <summary>
    /// 아이템이 이미 획득되었는지 여부.
    /// </summary>
    private bool isCollected;

    /// <summary>
    /// 아이템 애니메이터.
    /// </summary>
    private Animator anim;

    /// <summary>
    /// 필요한 컴포넌트를 초기화한다.
    /// </summary>
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();

        if (isCollected)
        {
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
        if (isCollected || itemData == null)
            return;

        isCollected = true;
        holdTimer = 0f;

        Debug.Log($"{name} 아이템 획득");

        InventoryManager.Instance?.AddItem(itemData);

        anim?.SetTrigger("Collect");

        DialogSystem.Instance.PlayIllustrationDialog(
            itemData.Icon,
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