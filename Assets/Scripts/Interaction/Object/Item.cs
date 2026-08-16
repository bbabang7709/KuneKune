using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour, IInteractable, IHoldInteractable
{
    [Header("Item Info")]
    [SerializeField] private string itemName;

    [Header("Interaction Settings")]
    [SerializeField] private float holdThreshold = 3f;
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    public float HoldDuration => holdThreshold;

    public float HoldProgress =>
        holdThreshold <= 0f
            ? 1f
            : Mathf.Clamp01(holdTimer / holdThreshold);

    private float holdTimer;
    private bool isCollected = false;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();

        if (isCollected)
            gameObject.SetActive(false);
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

        if (holdTimer >= holdThreshold)
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
        if (isCollected) return;

        isCollected = true;
        holdTimer = 0f;

        Debug.Log($"{name} 아이템 획득");

        anim?.SetTrigger("Collect");
        gameObject.SetActive(false);
    }
}
