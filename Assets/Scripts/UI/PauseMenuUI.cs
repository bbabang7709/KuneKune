using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : UIBase
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("ESC를 눌렀을 때 켜질 메뉴 Panel")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("로드 시 띄울 경고 메세지 Panel")]
    [SerializeField] private GameObject loadWarningPanel;

    [Tooltip("메뉴 배경과 버튼 사이에 표시할 독백 연출")]
    [SerializeField] private PauseMenuEcho pauseMenuEcho;

    [Header("Settings")]
    [SerializeField] private string titleSceneName = "Scene_Intro";

    public bool IsPaused = false;
    private bool _isReturningToTitle;

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        pauseMenuPanel.SetActive(false);
        loadWarningPanel.SetActive(false);
    }

    public void TogglePauseMenu()
    {
        if (_isReturningToTitle) return;
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading) return;
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) return;
        
        bool isOpening = !pauseMenuPanel.activeSelf;
        pauseMenuPanel.SetActive(isOpening);

        if (isOpening)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading) return;
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) return;
        if (IsPaused) return;

        ControlLockManager.Instance?.AddMovementLock(true);
        ControlLockManager.Instance?.AddInputLock(true);

        IsPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        pauseMenuEcho?.Play();
    }

    public void ResumeGame()
    {
        if (_isReturningToTitle) return;
        pauseMenuEcho?.Stop();
        IsPaused = false;
        pauseMenuPanel.SetActive(false);
        loadWarningPanel.SetActive(false);

        ControlLockManager.Instance.RemoveMovementLock(true);
        ControlLockManager.Instance.RemoveInputLock(true);

        // 메뉴를 닫아도 진행 중인 맵 전환의 정지는 해제하지 않습니다.
        Time.timeScale = MapManager.Instance != null && MapManager.Instance.IsChangingMap ? 0f : 1f;
    }

    public void OnClickLoadGame()
    {
        if (_isReturningToTitle) return;
        ResumeGame();
        SaveManager.Instance?.Load();
    }

    public void OnClickGoToTitle()
    {
        if (_isReturningToTitle) return;
        _isReturningToTitle = true;
        pauseMenuEcho?.Stop();
        StartCoroutine(GoToTitleRoutine());
    }

    private IEnumerator GoToTitleRoutine()
    {
        // 페이드는 실제 시간을 사용하므로, 씬을 떠날 때까지 게임은 멈춰 둡니다.
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            yield return UIManager.Instance.FadeOut(1f);

        // LoadScene은 다음 프레임에 완료됩니다. 이전 씬이 움직이지 않도록
        // 메뉴 객체가 파괴된 뒤에도 호출되는 정적 콜백에서 시간을 복구합니다.
        SceneManager.sceneLoaded += ResumeTimeAfterSceneLoad;
        SceneManager.LoadScene(titleSceneName);
    }

    private static void ResumeTimeAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ResumeTimeAfterSceneLoad;
        Time.timeScale = 1f;
    }

    public void OpenLoadWarning()
    {
        if (_isReturningToTitle) return;
        pauseMenuEcho?.Stop();
        pauseMenuPanel.SetActive(false);
        loadWarningPanel.SetActive(true);
    }

    public void CloseLoadWarning()
    {
        if (_isReturningToTitle) return;
        loadWarningPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
        pauseMenuEcho?.Play();
    }
}
