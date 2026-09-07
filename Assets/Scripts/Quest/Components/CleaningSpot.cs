using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CleaningSpot : QuestObjective, IHoldInteractable
{
    [Header("청소 상호작용")]
    [Tooltip("청소를 끝내기 위해 F 키를 누르고 있어야 하는 시간입니다.")]
    [Min(0f)] [SerializeField] private float _cleaningDuration = 3f;

    [Header("청소 연출")]
    [Tooltip("청소하는 동안 좌우로 움직이는 거리입니다.")]
    [Min(0f)] [SerializeField] private float _shakeDistance = 0.05f;

    [Tooltip("청소 구간이 좌우로 움직이는 빠르기입니다.")]
    [Min(0f)] [SerializeField] private float _shakeSpeed = 12f;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _collider;

    private bool _isCleaned;
    private float _cleaningTimer;
    private float _initialAlpha;
    private Vector3 _initialLocalPosition;

    /// <summary>
    /// 청소를 끝내기 위해 F 키를 누르고 있어야 하는 시간입니다.
    /// </summary>
    public float HoldDuration => _cleaningDuration;

    /// <summary>
    /// 현재 청소가 얼마나 진행되었는지 0부터 1 사이로 알려줍니다.
    /// </summary>
    public float HoldProgress => _cleaningDuration <= 0f ? 1f : Mathf.Clamp01(_cleaningTimer / _cleaningDuration);

    protected override void Init()
    {
        base.Init();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sortingLayerName = "Foreground";
        _initialAlpha = _spriteRenderer.color.a;
        _initialLocalPosition = transform.localPosition;

        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask; // 플레이어만 감지합니다.

        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

        // 이미 끝난 퀘스트의 청소 지점은 처음부터 상호작용을 막습니다.
        if (QuestManager.Instance.IsQuestCompleted(QuestData.QuestID))
            HandleQuestCompleted(QuestData.QuestID);
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void HandleQuestCompleted(string questID)
    {
        if (QuestData == null || questID != QuestData.QuestID)
            return;

        // 남은 청소 지점의 모습은 유지하고 상호작용만 끕니다.
        _isCleaned = true;
        ResetShakePosition();
        _collider.enabled = false;
        enabled = false;
    }

    public void OnInteractPressed()
    {
        // 이전에 청소한 진행 시간부터 이어서 시작합니다.
    }

    public void OnInteractHeld()
    {
        if (_isCleaned)
            return;

        _cleaningTimer = Mathf.Min(_cleaningTimer + Time.deltaTime, _cleaningDuration);
        UpdateCleaningVisual();
        UpdateCleaningShake();

        if (HoldProgress >= 1f)
            CompleteCleaning();
    }

    public void OnInteractReleased()
    {
        // F 키를 놓아도 청소 시간과 투명도를 그대로 유지합니다.
        ResetShakePosition();
    }

    /// <summary>
    /// 청소가 진행된 만큼 청소 구간을 투명하게 만듭니다.
    /// </summary>
    private void UpdateCleaningVisual()
    {
        Color color = _spriteRenderer.color;
        color.a = Mathf.Lerp(_initialAlpha, 0f, HoldProgress);
        _spriteRenderer.color = color;
    }

    /// <summary>
    /// 청소하는 동안 원래 위치를 중심으로 좌우로 작게 움직입니다.
    /// </summary>
    private void UpdateCleaningShake()
    {
        float horizontalOffset = Mathf.Sin(Time.time * _shakeSpeed) * _shakeDistance;
        transform.localPosition = _initialLocalPosition + Vector3.right * horizontalOffset;
    }

    private void ResetShakePosition()
    {
        transform.localPosition = _initialLocalPosition;
    }

    private void CompleteCleaning()
    {
        if (TryReportCompleted() == false)
            return;

        _isCleaned = true;
        ResetShakePosition();

        // 청소가 끝난 지점을 화면에서 숨깁니다.
        gameObject.SetActive(false);
    }
}
