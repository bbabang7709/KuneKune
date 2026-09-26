using System.Collections;
using UnityEngine;

public class FallCutsceneController : CutsceneControllerBase
{
    [SerializeField] private PlayerAppearance _player;
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

        if (MapManager.Instance != null)
        {
            // 임시로 바로 학교로 가게 해놓음.
            // TODO: 카운트다운과 함께 피하기 이벤트 만들기. 문은 다 잠궈놓고 어짜피 잡히도록 만들기.
            // MapManager.Instance.InitMap(EMapType.House_Hallway, "houseChase_playerFallPosition");
            MapManager.Instance.ChangeMap(EMapType.School_Hallway, "school_hallway_0", () => _player.SetAge(EPlayerAge.Student));
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveChapterCheckpoint(EChapter.Chapter3_School);
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
