using UnityEngine;

public class DirtyDesk : InteractableBase, IHoldInteractable
{
    [SerializeField] private DirtyDeskCutsceneController cutscene;
    [SerializeField, Min(0f)] private float holdDuration = 3f;

    private float _holdTimer;
    private bool _hasTriggered;

    public float HoldDuration => holdDuration;
    public float HoldProgress => holdDuration <= 0f ? 1f : Mathf.Clamp01(_holdTimer / holdDuration);

    public override void OnInteractPressed()
    {
        _holdTimer = 0f;
    }

    public void OnInteractHeld()
    {
        if (_hasTriggered || cutscene == null) return;

        _holdTimer += Time.deltaTime;
        if (HoldProgress < 1f) return;

        Debug.Log($"{name} 상호작용 : 컷씬 재생합니다.");
        cutscene.PlayCutscene();
        if (!cutscene.HasPlayed) return;

        _hasTriggered = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    public void OnInteractReleased()
    {
        _holdTimer = 0f;
    }
}
