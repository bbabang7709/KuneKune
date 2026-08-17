using UnityEngine;

public class ToiletBulkhead : MonoBehaviour, IInteractable
{
    [Header("Door Sprite")]
    [SerializeField] private Sprite openedSprite;
    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    [Header("Toilet Bowl")]
    public ToiletBowl innerToiletBowl;

    private SpriteRenderer sr;
    private Collider2D innerBowlCollider;
    private bool isOpend = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // 문을 열기 전에는 변기의 Collider 비활성화
        if (innerToiletBowl != null)
        {
            innerBowlCollider = innerToiletBowl.GetComponent<Collider2D>();
            if (innerBowlCollider != null) innerBowlCollider.enabled = false;
        }
    }

    public void OnInteractPressed()
    {
        if (isOpend) return;

        isOpend = true;
        sr.sprite = openedSprite;

        if (innerBowlCollider != null) innerBowlCollider.enabled = true;
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}
}
