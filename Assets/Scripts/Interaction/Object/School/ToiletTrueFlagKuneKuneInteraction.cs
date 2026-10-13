using UnityEngine;

/// <summary>
/// TrueFlag 화장실의 컷씬용 쿠네쿠네를 F키 한 번 누르기로 상호작용합니다.
/// </summary>
public class ToiletTrueFlagKuneKuneInteraction : InteractableBase
{
    [SerializeField] private ToiletTrueFlagCutsceneController cutscene;
    [SerializeField] private ToiletTrueFlagUnlockedCutsceneController unlockedCutscene;

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
