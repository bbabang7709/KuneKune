using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 시작 시 초기 연출(페이드 인, UI 등장, 플레이어 조작 제어)을 담당하는 컨트롤러.
/// </summary>
public class GameStartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("Settings")]
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float uiFadeDuration = 1f;

    // 💡 핵심 픽스: 씬이 리로드(사망 후 부활)되어도 값이 유지되는 정적 변수
    public static bool IsFirstStart = true;

    void Start()
    {
        // 최초 시작(새 게임)일 때만 강제 초기화 및 연출을 실행합니다.
        // 사망 후 씬이 리로드되었을 때는 무시하고 SaveManager의 LoadRoutine에게 맵 세팅과 페이드인을 양보합니다.
        if (IsFirstStart)
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    private IEnumerator StartGameRoutine()
    {
        ControlLockManager.Instance?.AddInputLock(this);

        UIManager.Instance?.SetGameplayUIAlpha(0f);
        UIManager.Instance?.SetFadeAlpha(1f);

        // 최초 1회만 버스정류장 위치로 세팅하고 세이브 파일을 덮어씌움
        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter1_Outside);
        MapManager.Instance?.InitMap(EMapType.Outside_Busstop, "outside_gamestart");
        
        // 💡 연출과 세팅이 끝났으므로 false로 변경하여 다음 씬 로드부터는 실행되지 않게 막음
        IsFirstStart = false; 

        yield return new WaitForSeconds(0.5f);

        yield return UIManager.Instance?.FadeIn(screenFadeDuration);
        yield return UIManager.Instance?.FadeInGameplayUI(uiFadeDuration);

        ControlLockManager.Instance?.RemoveInputLock(this);
    }
}