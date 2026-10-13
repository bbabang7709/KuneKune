using System.Collections;
using UnityEngine;

public enum EGameStartMode
{
    None,
    NewGame,
    Continue,
    EditorPlay
}

public class GameStartController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float uiFadeDuration = 1f;

#if UNITY_EDITOR
    [Header("Editor Test")]
    [Tooltip("에디터 실행 시 세이브를 변경하지 않고 트루엔딩부터 시작합니다.")]
    [SerializeField] private bool startWithTrueEndingPreview;
#endif

    public static EGameStartMode StartMode = EGameStartMode.EditorPlay;

    private bool _hasLocked = false;

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        if (startWithTrueEndingPreview)
            StartMode = EGameStartMode.EditorPlay;
#endif
        if (StartMode == EGameStartMode.None) yield break;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameplayUIAlpha(0f);
            UIManager.Instance.SetFadeAlpha(1f);
        }

        yield return null;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            _hasLocked = true;
        }

        StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        if (StartMode == EGameStartMode.None) yield break;

#if UNITY_EDITOR
        if (startWithTrueEndingPreview && StartMode == EGameStartMode.EditorPlay)
        {
            StartMode = EGameStartMode.None;
            StartTrueEndingPreview();
            yield break;
        }
#endif
 
        if (StartMode == EGameStartMode.NewGame || StartMode == EGameStartMode.EditorPlay)
        {
            StartMode = EGameStartMode.None;

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveChapterCheckpoint(EChapter.Chapter1_Outside);
            if (MapManager.Instance != null)
                MapManager.Instance.InitMap(EMapType.Outside_Busstop, "outside_gamestart");
            if (BackgroundManager.Instance != null)
                BackgroundManager.Instance.SetBackgroundForChapter(EChapter.Chapter1_Outside);

            yield return new WaitForSecondsRealtime(0.5f);

            if (UIManager.Instance != null && UIManager.Instance.gameObject.activeInHierarchy)
            {
                UIManager.Instance.StartCoroutine(UIManager.Instance.FadeIn(screenFadeDuration));
                UIManager.Instance.StartCoroutine(UIManager.Instance.FadeInGameplayUI(uiFadeDuration));
                
                yield return new WaitForSecondsRealtime(Mathf.Max(screenFadeDuration, uiFadeDuration) + 0.1f);
            }

            UnlockInput();
        }
        else if (StartMode == EGameStartMode.Continue)
        {
            StartMode = EGameStartMode.None;
            UnlockInput();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Load(skipFadeOut: true);
            }
        }
    }

#if UNITY_EDITOR
    private void StartTrueEndingPreview()
    {
        MapManager mapManager = MapManager.Instance;
        if (mapManager == null || mapManager.Player == null)
        {
            Debug.LogError("트루엔딩 테스트에 필요한 MapManager와 플레이어가 없습니다.", this);
            UnlockInput();
            return;
        }

        // 일반 부팅의 체크포인트 저장이나 진엔딩 해금 저장을 호출하지 않습니다.
        mapManager.KuneKune?.Despawn();
        mapManager.InitMap(EMapType.Chase_Ricefield, "true_ending_preview");
        MapArea map = mapManager.CurrentMap;
        TrueEnding ending = map != null ? map.GetComponentInChildren<TrueEnding>(true) : null;
        if (ending == null || map.GetEntryPoint("true_ending_preview") == null)
        {
            Debug.LogError("추격 맵의 트루엔딩 컷씬과 테스트 시작 위치를 확인해주세요.", this);
            UnlockInput();
            UIManager.Instance?.StartCoroutine(UIManager.Instance.FadeIn(screenFadeDuration));
            return;
        }

        GameObject player = mapManager.Player;
        player.GetComponent<PlayerAppearance>()?.SetAge(EPlayerAge.Child);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement?.StopImmediately();
        movement?.SetFacingDirection(-1f);
        Physics2D.SyncTransforms();

        BackgroundManager.Instance?.SetBackgroundForChapter(EChapter.Chapter5_Chase);
        SoundManager.Instance?.PlayBGM("DontLeave", 0.01f);
        UIManager.Instance?.SetGameplayUIAlpha(1f);
        ending.gameObject.SetActive(true);
        ending.PlayCutscene();
        // 컷씬의 잠금이 적용된 후 테스트 부팅용 잠금만 해제합니다.
        UnlockInput();
    }
#endif

    private void UnlockInput()
    {
        if (_hasLocked && ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            _hasLocked = false;
        }
    }

    private void OnDisable()
    {
        UnlockInput();
    }
}
