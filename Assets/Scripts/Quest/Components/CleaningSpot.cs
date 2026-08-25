using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CleaningSpot : QuestObjective, IInteractable
{
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _collider;

    private bool _isCleaned;

    private void Start()
    {
        Init();
    }

    protected override void Init()
    {
        base.Init();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sortingLayerName = "Foreground";

        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _collider.excludeLayers = ~playerMask;  // 플레이어만 인식
        
        QuestManager.Instance.StartQuest(QuestData.QuestID);
    }

    public void OnInteractPressed()
    {
        if (_isCleaned)
            return;

        _isCleaned = true;

        // 청소 이미지나 애니메이션을 적용합니다.

        gameObject.SetActive(false);
        TryReportCompleted();
    }
}