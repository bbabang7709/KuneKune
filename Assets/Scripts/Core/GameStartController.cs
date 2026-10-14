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

    public static EGameStartMode StartMode = EGameStartMode.EditorPlay;

    private bool _hasLocked = false;

    private IEnumerator Start()
    {
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
