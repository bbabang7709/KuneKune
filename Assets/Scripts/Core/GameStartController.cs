using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 시작 시 초기 연출(페이드 인, UI 등장, 플레이어 조작 제어)을 담당하는 컨트롤러.
/// </summary>
public class GameStartController : MonoBehaviour
{
    [Header("References")]
    
    /// <summary>
    /// 플레이어 이동 컴포넌트. 시작 연출 중 조작을 제한하기 위해 참조한다.
    /// </summary>
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("Settings")]
    
    /// <summary>
    /// 화면 전체 페이드 인(검은 화면 -> 게임 화면)에 걸리는 시간(초).
    /// </summary>
    [SerializeField] private float screenFadeDuration = 1f;
    
    /// <summary>
    /// 게임 플레이 UI가 서서히 나타나는 데 걸리는 시간(초).
    /// </summary>
    [SerializeField] private float uiFadeDuration = 1f;

    /// <summary>
    /// 씬이 시작될 때 게임 시작 연출 코루틴을 실행한다.
    /// </summary>
    void Start()
    {
        StartCoroutine(StartGameRoutine());
    }

    /// <summary>
    /// 게임 시작 연출의 전체 흐름을 관리하는 코루틴.
    /// 플레이어 이동 제한 -> 대기 -> 화면 페이드 인 -> UI 페이드 인 -> 조작 제한 해제 순으로 진행된다.
    /// </summary>
    private IEnumerator StartGameRoutine()
    {
        // 연출이 진행되는 동안 플레이어의 조작을 막는다.
        ControlLockManager.Instance?.AddInputLock(this);

        // 초기 상태 설정: 게임 UI는 투명하게, 화면은 완전히 검은색(알파 1)으로 설정한다.
        UIManager.Instance?.SetGameplayUIAlpha(0f);
        UIManager.Instance?.SetFadeAlpha(1f);

        SaveManager.Instance?.SaveChapterCheckpoint(EChapter.Chapter1_Outside);

        MapManager.Instance?.InitMap(EMapType.Outside_Busstop, "outside_gamestart");
        // 씬 로딩 후 안정적인 시작을 위해 잠시 대기한다.
        yield return new WaitForSeconds(0.5f);

        // 1. 화면을 검은색에서 게임 화면으로 부드럽게 밝힌다.
        yield return UIManager.Instance?.FadeIn(screenFadeDuration);

        // 2. 이어서 게임 플레이 UI를 서서히 나타나게 한다.
        yield return UIManager.Instance?.FadeInGameplayUI(uiFadeDuration);

        // 모든 연출이 끝나면 플레이어의 이동 제한을 해제하여 게임을 시작한다.
        ControlLockManager.Instance?.RemoveInputLock(this);
    }
}