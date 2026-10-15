using UnityEngine;

public class ToiletBulkhead : MonoBehaviour, IInteractable
{
    [Header("Door Sprite")]
    [SerializeField] private Sprite openedSprite;

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

        SoundManager.Instance?.PlaySFX("StallDoor_Open");

        if (innerBowlCollider != null) innerBowlCollider.enabled = true;

        if (TryGetComponent<Collider2D>(out var doorCollider))
        {
            doorCollider.enabled = false;
        }

        enabled = false;
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}
}
