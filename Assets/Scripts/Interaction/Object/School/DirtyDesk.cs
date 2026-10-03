using UnityEngine;
using UnityEngine.Playables;

public class DirtyDesk : InteractableBase
{
    [SerializeField] private DirtyDeskCutsceneController cutscene;
    public override void OnInteractPressed()
    {
        Debug.Log($"{name} 상호작용 : 컷씬 재생합니다.");
        cutscene.PlayCutscene();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}
