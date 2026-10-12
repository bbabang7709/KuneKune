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

    protected override void OnCutsceneEndSetup()
    {
        StartCoroutine(EndSetupRoutine());
    }

    private IEnumerator EndSetupRoutine()
    {
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        if (MapManager.Instance != null && escapeEvent != null)
        {
            // TODO: 카운트다운과 함께 피하기 이벤트 만들기. 문은 다 잠궈놓고 어짜피 잡히도록 만들기.
            MapManager.Instance.ChangeMap(EMapType.House_Hallway, "houseChase_playerFallPosition", () => escapeEvent.StartEscapeSequence());
            // MapManager.Instance.ChangeMap(EMapType.School_Hallway, "school_hallway_0", () => _player.SetAge(EPlayerAge.Student));
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeIn(0.5f);
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
