using UnityEngine;

/// <summary>
/// TrueFlag 화장실의 컷씬용 쿠네쿠네를 F키 한 번 누르기로 상호작용합니다.
/// </summary>
public class ToiletTrueFlagKuneKuneInteraction : InteractableBase
{
    [SerializeField] private ToiletTrueFlagCutsceneController cutscene;

    public bool CanInteract => cutscene != null && cutscene.CanInteractWithKuneKune;

    private void Awake()
    {
        if (cutscene == null)
            cutscene = GetComponentInParent<ToiletTrueFlagCutsceneController>();
    }

    public override void OnInteractPressed()
    {
        if (!CanInteract) return;

        PlayInteractionCutscene();
    }

    private void PlayInteractionCutscene()
    {
        // TODO: 쿠네쿠네 상호작용 후 재생할 컷씬을 연결합니다.
        cutscene.StopKuneKune();
        cutscene.PauseKuneKuneAnimation();
        cutscene.CompleteTrueFlagInteraction();
        Debug.Log("피닦기");
    }
}
