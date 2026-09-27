using System.Collections;
using UnityEngine;

public enum EGameStartMode
{
    None,
    NewGame,
    Continue,
    EditorPlay
}

/// <summary>
/// 게임 시작 시 초기 연출(페이드 인, UI 등장, 플레이어 조작 제어)을 담당하는 컨트롤러.
/// </summary>
public class GameStartController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float uiFadeDuration = 1f;

    public static EGameStartMode StartMode = EGameStartMode.EditorPlay;

    // void Start()
    // {
    //     if (StartMode == EGameStartMode.None) return;

    //     if (ControlLockManager.Instance != null)
    //     {
    //         ControlLockManager.Instance.AddInputLock(this);
    //         _hasLocked = true;        
    //     }

    //     if (UIManager.Instance != null)
    //     {
    //         UIManager.Instance.SetGameplayUIAlpha(0f);
    //         UIManager.Instance.SetFadeAlpha(1f);
    //     }

    //     StartCoroutine(BootRoutine());
    // }

    private IEnumerator Start()
    {
        if (StartMode == EGameStartMode.None) yield break;

        yield return null;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameplayUIAlpha(0f);
            UIManager.Instance.SetFadeAlpha(1f);
        }

        // 이제 안전하게 BootRoutine을 실행합니다.
        StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        Debug.Log("Boot Routine 시작.");
        Debug.Log($"현재 상태 : {StartMode}");

        // 모드나 None 이면 이미 SaveManager가 로드를 진행 중이므로 코루틴 종료
        if (StartMode == EGameStartMode.None) yield break;
 
        if (StartMode == EGameStartMode.NewGame || StartMode == EGameStartMode.EditorPlay)
        {
            // 다음 씬 리로드 시 이 로직이 또 돌지 않도록 None으로 변경
            StartMode = EGameStartMode.None;
            Debug.Log($"현재 모드 : {StartMode}");

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveChapterCheckpoint(EChapter.Chapter1_Outside);
            if (MapManager.Instance != null)
                MapManager.Instance.InitMap(EMapType.Outside_Busstop, "outside_gamestart");

            yield return new WaitForSeconds(0.5f);

            if (UIManager.Instance != null)
            {
                yield return UIManager.Instance.FadeIn(screenFadeDuration);
                yield return UIManager.Instance.FadeInGameplayUI(uiFadeDuration);
            }

            Debug.Log("Lock 해제 순서.");
            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.RemoveInputLock(this);
                Debug.Log("Lock 해제.");
            }
        }
        else if (StartMode == EGameStartMode.Continue)
        {
            // 씬 리로드 후 GameStartController가 다시 Load를 부르지 않도록 차단
            StartMode = EGameStartMode.None;

            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.RemoveInputLock(this);
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Load(skipFadeOut: true);
            }
        }
    }
}