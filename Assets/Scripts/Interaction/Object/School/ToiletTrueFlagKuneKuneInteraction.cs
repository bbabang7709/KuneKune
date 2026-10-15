using UnityEngine;

/// <summary>
/// TrueFlag 화장실의 쿠네쿠네에 닦아주는 선택지를 표시하고 기존 컷씬을 시작합니다.
/// </summary>
public class ToiletTrueFlagKuneKuneInteraction : InteractableBase
{
    [SerializeField] private ToiletTrueFlagCutsceneController cutscene;
    [SerializeField] private ToiletTrueFlagUnlockedCutsceneController unlockedCutscene;
    [SerializeField] private string choiceText = "닦아준다";

    public string ChoiceText => choiceText;

    public bool CanInteract => cutscene != null && cutscene.CanInteractWithKuneKune &&
        unlockedCutscene != null && unlockedCutscene.CanPlay;

    private void Awake()
    {
        if (cutscene == null)
            cutscene = GetComponentInParent<ToiletTrueFlagCutsceneController>();

        if (unlockedCutscene == null)
        {
            MapArea map = GetComponentInParent<MapArea>();
            if (map != null)
                unlockedCutscene = map.GetComponentInChildren<ToiletTrueFlagUnlockedCutsceneController>(true);
        }
    }

    public override void OnInteractPressed()
    {
        if (!CanInteract) return;

        PlayInteractionCutscene();
    }

    private void PlayInteractionCutscene()
    {
        unlockedCutscene.TryPlay(cutscene);
    }
}
