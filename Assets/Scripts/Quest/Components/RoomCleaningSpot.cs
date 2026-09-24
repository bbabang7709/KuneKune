using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomCleaningSpot : QuestObjective, IHoldInteractable
{
    [Header("Cleaning Settings")]
    [Min(0f)] [SerializeField] private float _holdDuration = 3f;

    [Tooltip("청소를 진행하는 동안 발생시킬 먼지 파티클")]
    [SerializeField] private ParticleSystem dustEffect;

    [Tooltip("청소를 끝낸 뒤의 스프라이트")]
    [SerializeField] private Sprite spriteAfterCleaning;

    private bool _isCleaned;
    private float _timer;

    public float HoldDuration => _holdDuration;

    public float HoldProgress => _holdDuration <= 0f ? 1f : Mathf.Clamp01(_timer / _holdDuration);

    private SpriteRenderer _sr;
    private BoxCollider2D _collider;

    protected override void Init()
    {
        base.Init();
        _sr = GetComponent<SpriteRenderer>();
        
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask;
    }

    public void OnInteractPressed()
    {
        if (_isCleaned) return;
        dustEffect?.Play();
    }

    public void OnInteractHeld()
    {
        if (_isCleaned) return;

        _timer = Mathf.Min(_timer + Time.deltaTime, _holdDuration);

        if (HoldProgress >= 1f)
        {
            CompleteCleaning();
        }
    }

    public void OnInteractReleased()
    {
        if (_isCleaned) return;

        dustEffect?.Stop();
    }

    private void CompleteCleaning()
    {
        if (TryReportCompleted() == false) return;

        _isCleaned = true;
        _sr.sprite = spriteAfterCleaning;
        dustEffect?.Stop();
    }
}
