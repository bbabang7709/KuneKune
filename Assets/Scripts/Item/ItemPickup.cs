using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable, IHoldInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    [Header("Item Data")]
    [SerializeField] private ItemData itemData;

    [Header("Hold Interaction Settings")]
    [SerializeField] private float holdDuration = 3f;
    public float HoldDuration => holdDuration;
    public float HoldProgress => 
        holdDuration <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / holdDuration);

    private float holdTimer;
    private bool isCollected = false;

    private Animator anim;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (isCollected) gameObject.SetActive(false);
    }

    public void OnInteractPressed()
    {
        if (isCollected) return;

        holdTimer = 0f;
    }

    public void OnInteractHeld()
    {
        if (isCollected) return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdDuration)
        {
            Collect();
        }
    }

    public void OnInteractReleased()
    {
        holdTimer = 0f;
    }

    private void Collect()
    {
        if (isCollected || itemData == null) return;

        isCollected = true;
        holdTimer = 0f;

        Debug.Log($"{name} 아이템 획득");

        InventoryManager.Instance?.AddItem(itemData);

        anim?.SetTrigger("Collect");
        gameObject.SetActive(false);
    }
}
