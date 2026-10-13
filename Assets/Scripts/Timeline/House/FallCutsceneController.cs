using System.Collections;
using UnityEngine;

public class FallCutsceneController : CutsceneControllerBase
{
    [SerializeField] private PlayerAppearance _player;
    [SerializeField] private HouseEscapingSequence escapeEvent;

    [SerializeField] private DoorBangingEffect doorBanging;

    protected override void OnCutsceneStartSetup()
    {
        if (doorBanging != null) doorBanging.StopBanging();
    }

    protected override void OnCutsceneFinished()
    {
        if (HouseChase.Instance != null)
        {
            HouseChase.Instance.BeginChase();
        }
        else if (MapManager.Instance != null && escapeEvent != null)
        {
            MapManager.Instance.ChangeMap(EMapType.House_Hallway, "houseChase_playerFallPosition", () => escapeEvent.StartEscapeSequence());
        }
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(duration);
        }
    }
}
