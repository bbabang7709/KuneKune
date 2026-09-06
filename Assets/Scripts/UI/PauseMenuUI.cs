using System.Collections;
using Unity.VisualScripting;
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

    [Header("Settings")]
    [SerializeField] private string titleSceneName = "Scene_Intro";

    public bool IsPaused = false;

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
        if (DialogSystem.Instance.IsDialogPlaying) return;

        ControlLockManager.Instance?.AddMovementLock(true);
        ControlLockManager.Instance?.AddInputLock(true);

        IsPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        pauseMenuPanel.SetActive(false);
        loadWarningPanel.SetActive(false);

        ControlLockManager.Instance.RemoveMovementLock(true);
        ControlLockManager.Instance.RemoveInputLock(true);

        Time.timeScale = 1f;
    }

    public void OnClickLoadGame()
    {
        ResumeGame();
        SaveManager.Instance?.Load();
    }

    public void OnClickGoToTitle()
    {
        StartCoroutine(GoToTitleRoutine());
    }

    private IEnumerator GoToTitleRoutine()
    {
        Time.timeScale = 1f;

        yield return UIManager.Instance.FadeOut(1f);

        SceneManager.LoadScene(titleSceneName);
    }

    public void OpenLoadWarning()
    {
        pauseMenuPanel.SetActive(false);
        loadWarningPanel.SetActive(true);
    }

    public void CloseLoadWarning()
    {
        loadWarningPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }
}
