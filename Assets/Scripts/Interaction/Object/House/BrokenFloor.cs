using UnityEngine;
using UnityEngine.Playables;

public class BrokenFloor : InteractableBase
{
    [SerializeField] private PlayableDirector encounterCutscene;
    [SerializeField] private PlayableDirector fallCutscene;

    [SerializeField] private Collider2D triggerCollider;

    private bool _playedEncounterCutscene = false;
    private bool _playedFallCutscene = false;

    public override void OnInteractPressed()
    {
        if (CutsceneManager.Instance.IsCutscenePlaying) return;
        if (_playedFallCutscene) return;

        if (!_playedEncounterCutscene)
        {
            if (encounterCutscene != null) encounterCutscene.Play();
            _playedEncounterCutscene = true;
            Debug.Log("쿠네쿠네 눈 마주침 컷씬 종료. 다시 상호작용해 아래로 떨어질 수 있습니다.");
        }
        else
        {
            UIManager.Instance?.FadeOut(0.5f);
            if (fallCutscene != null) fallCutscene.Play();   
            _playedFallCutscene = true;
            triggerCollider.enabled = false;
        }
    }
}
