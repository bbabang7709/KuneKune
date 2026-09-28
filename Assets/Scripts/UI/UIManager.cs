using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 게임 내 UI를 관리하는 싱글톤 매니저.
/// 플레이어 상태 UI, 대화 UI, 이미지 연출 UI 및 화면 페이드 효과 등을 관리한다.
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>
    /// UIManager의 싱글톤 인스턴스.
    /// </summary>
    public static UIManager Instance { get; private set; }

    [Header("Player Status")]

    /// <summary>
    /// 플레이어 스태미나 게이지 이미지.
    /// </summary>
    [SerializeField] private Image staminaGuage;

    /// <summary>
    /// 플레이어 열기(Heat) 게이지 이미지.
    /// </summary>
    [SerializeField] private Image heatGuage;

    /// <summary>
    /// 열기(Heat) 게이지 시각 효과를 제어하는 컴포넌트.
    /// </summary>
    [SerializeField] private HeatGaugeEffect heatGaugeEffect;

    [Header("References")]

    [Tooltip("HUD와 메뉴처럼 전체 화면 글리치를 적용할 UI의 Canvas입니다.")]
    [SerializeField] private Canvas glitchUICanvas;

    /// <summary>
    /// 플레이어 이동 컴포넌트.
    /// </summary>
    [SerializeField] private PlayerMovement playerMovement;

    /// <summary>
    /// 플레이어 입력 컴포넌트.
    /// </summary>
    [SerializeField] private PlayerInputHandler playerInput;

    /// <summary>
    /// 대화 텍스트 출력 시스템.
    /// </summary>
    [SerializeField] private DialogTextSystem dialogTextSystem;

    [Header("Dialog")]

    /// <summary>
    /// 대화창의 루트 오브젝트.
    /// </summary>
    [SerializeField] private GameObject dialogRoot;

    /// <summary>
    /// 대화 텍스트 컴포넌트.
    /// </summary>
    [SerializeField] private TextMeshProUGUI dialogText;

    [Header("Illustration")]

    /// <summary>
    /// 이미지 연출 루트 오브젝트.
    /// </summary>
    [SerializeField] private GameObject illustrationRoot;

    /// <summary>
    /// 이미지 연출에 사용할 Image 컴포넌트.
    /// </summary>
    [SerializeField] private Image illustrationImage;

    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private CanvasGroup gameplayUICanvasGroup;
    [Tooltip("체크하면 씬이 암전된 화면으로 시작합니다.")]
    [SerializeField] private bool startWithBlackScreen = true;

    private Coroutine _fadeCoroutine;
    private Coroutine _gameplayUIFadeCoroutine;

    [Header("Letter Box")]
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;

    [SerializeField] private float letterboxHeight = 100f;
    
    private Coroutine letterboxCoroutine;

    [Header("KuneKune Jumpscare Settings")]
    [Tooltip("공포 이미지가 들어있는 RectTransform")]
    [SerializeField] private RectTransform jumpscareRect;

    /// <summary>
    /// 싱글톤 인스턴스와 UI 초기 상태를 설정한다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ApplyFullScreenGlitchToUI();

        // Dialog 
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
        if (illustrationRoot != null)
            illustrationRoot.SetActive(false);
        dialogText.text = string.Empty;

        // Fade CanvasGroup
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(startWithBlackScreen);
            fadeCanvasGroup.alpha = startWithBlackScreen ? 1f : 0f;
            fadeCanvasGroup.blocksRaycasts = startWithBlackScreen;       
        }

        InitializeLetterbox();

    }

    private void Start()
    {
        if (jumpscareRect != null) jumpscareRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 지정한 Canvas에만 전체 화면 글리치를 적용한다.
    /// 나레이션과 인벤토리는 별도의 Overlay Canvas에서 그린다.
    /// </summary>
    private void ApplyFullScreenGlitchToUI()
    {
        // HUD와 메뉴가 없는 인트로 씬은 기존 Overlay Canvas를 유지한다.
        if (glitchUICanvas == null) return;

        // 기존 Canvas가 남긴 배율 0을 지워 자식 UI가 함께 사라지지 않게 한다.
        transform.localScale = Vector3.one;

        // 부모 배율이 0일 때 계산된 Canvas 배율도 함께 복구한다.
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.isRootCanvas)
                canvas.transform.localScale = Vector3.one * canvas.scaleFactor;
        }

        Canvas uiCanvas = glitchUICanvas;
        Camera mainCamera = Camera.main;

        if (uiCanvas == null || mainCamera == null)
        {
            Debug.LogError("UI 글리치에 필요한 Canvas 또는 Main Camera를 찾지 못했습니다.", this);
            return;
        }

        uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        uiCanvas.worldCamera = mainCamera;

        // UI를 카메라 바로 앞에 놓아 건물이나 캐릭터가 UI를 가리지 않게 합니다.
        uiCanvas.planeDistance = 1f;

        // 2D 건물보다 UI를 나중에 그려 항상 화면 앞에 보이게 합니다.
        uiCanvas.overrideSorting = true;
        uiCanvas.sortingLayerName = "UI";
        uiCanvas.sortingOrder = 1;
    }

    /// <summary>
    /// 플레이어 상태 변경(스태미나, 열기) 이벤트를 구독한다.
    /// </summary>
    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnStaminaChanged += UpdateStaminaUI;
            playerMovement.OnHeatChanged += UpdateHeatUI;
        }
    }

    /// <summary>
    /// 플레이어 상태 변경 이벤트 구독을 해제한다.
    /// </summary>
    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnStaminaChanged -= UpdateStaminaUI;
            playerMovement.OnHeatChanged -= UpdateHeatUI;
        }
    }

    // =========================================================
    // Stat UI
    // =========================================================

    /// <summary>
    /// 현재 스태미나 비율에 따라 게이지 UI를 갱신한다.
    /// </summary>
    /// <param name="current">현재 스태미나.</param>
    /// <param name="max">최대 스태미나.</param>
    public void UpdateStaminaUI(float current, float max)
    {
        if (staminaGuage == null || max <= 0f)
            return;

        staminaGuage.fillAmount = current / max;
    }

    /// <summary>
    /// 현재 열기(Heat) 비율에 따라 게이지 UI 및 시각 효과를 갱신한다.
    /// </summary>
    /// <param name="current">현재 열기 값.</param>
    /// <param name="max">최대 열기 값.</param>
    public void UpdateHeatUI(float current, float max)
    {
        if (heatGuage == null || max <= 0f)
            return;

        float ratio = Mathf.Clamp01(current / max);
        heatGuage.fillAmount = ratio;
        heatGaugeEffect?.SetHeatRatio(ratio);
    }

    // =========================================================
    // Dialog UI
    // =========================================================

    /// <summary>
    /// 대화창을 표시한다.
    /// 이전 대화 텍스트를 먼저 초기화하여
    /// 이전 대사가 잠시 보이는 현상을 방지한다.
    /// </summary>
    public void OpenDialog()
    {
        dialogTextSystem.ClearText();

        dialogRoot.SetActive(true);
    }

    /// <summary>
    /// 대화창을 닫고 현재 텍스트를 초기화한다.
    /// </summary>
    public void CloseDialog()
    {
        dialogTextSystem.ClearText();

        dialogRoot.SetActive(false);
    }

    // =========================================================
    // Illustration UI
    // =========================================================

    /// <summary>
    /// 이미지 연출을 시작한다.
    /// </summary>
    /// <param name="image">표시할 이미지.</param>
    public void OpenIllustration(Sprite image)
    {
        // 이전 텍스트를 먼저 제거한다.
        dialogTextSystem.ClearText();

        if (image != null)
        {
            illustrationImage.sprite = image;
            illustrationRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 이미지 연출을 종료한다.
    /// 이미지와 대화창을 함께 숨긴다.
    /// </summary>
    public void CloseIllustration()
    {
        illustrationRoot.SetActive(false);

        illustrationImage.sprite = null;
    }

    // =========================================================
    // Fade UI
    // =========================================================

    /// <summary>
    /// 전체 화면 페이드 CanvasGroup의 알파(투명도) 값을 직접 설정한다.
    /// </summary>
    /// <param name="alpha">설정할 알파 값 (0.0 ~ 1.0).</param>
    public void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup == null) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = alpha > 0f;
    }

    /// <summary>
    /// 게임 플레이 UI CanvasGroup의 알파(투명도) 값을 직접 설정한다.
    /// </summary>
    /// <param name="alpha">설정할 알파 값 (0.0 ~ 1.0).</param>
    public void SetGameplayUIAlpha(float alpha)
    {
        if (gameplayUICanvasGroup == null) return;

        if (_gameplayUIFadeCoroutine != null)
        {
            StopCoroutine(_gameplayUIFadeCoroutine);
            _gameplayUIFadeCoroutine = null;
        }

        gameplayUICanvasGroup.alpha = alpha;
    }

    /// <summary>
    /// 지정된 시간 동안 게임 플레이 UI를 서서히 나타나게(Fade In) 한다.
    /// </summary>
    /// <param name="duration">페이드 인에 걸리는 시간(초).</param>
    public IEnumerator FadeInGameplayUI(float duration)
    {
        if (gameplayUICanvasGroup == null) yield break;

        if (_gameplayUIFadeCoroutine != null) 
            StopCoroutine(_gameplayUIFadeCoroutine);

        _gameplayUIFadeCoroutine = StartCoroutine(GameplayUIFadeRoutine(1f, duration));
        yield return _gameplayUIFadeCoroutine;
    }

    /// <summary>
    /// 지정된 시간 동안 게임 플레이 UI를 서서히 사라지게(Fade Out) 한다.
    /// </summary>
    /// <param name="duration">페이드 아웃에 걸리는 시간(초).</param>
    public IEnumerator FadeOutGameplayUI(float duration)
    {
        if (gameplayUICanvasGroup == null) yield break;

        if (_gameplayUIFadeCoroutine != null) 
            StopCoroutine(_gameplayUIFadeCoroutine);

        _gameplayUIFadeCoroutine = StartCoroutine(GameplayUIFadeRoutine(0f, duration));
        yield return _gameplayUIFadeCoroutine;
    }

    private IEnumerator GameplayUIFadeRoutine(float targetAlpha, float duration)
    {
        float elapsed = 0f;
        float startAlpha = gameplayUICanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            gameplayUICanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        gameplayUICanvasGroup.alpha = targetAlpha;
        _gameplayUIFadeCoroutine = null;
    }

    /// <summary>
    /// 화면 전체를 지정된 시간 동안 서서히 밝아지게(Fade In) 한다. (검은 화면 -> 게임 화면)
    /// </summary>
    /// <param name="duration">페이드 인에 걸리는 시간(초).</param>
    public IEnumerator FadeIn(float duration)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, duration));
        yield return _fadeCoroutine;
    }

    /// <summary>
    /// 화면 전체를 지정된 시간 동안 서서히 어두워지게(Fade Out) 한다. (게임 화면 -> 검은 화면)
    /// </summary>
    /// <param name="duration">페이드 아웃에 걸리는 시간(초).</param>
    public IEnumerator FadeOut(float duration)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, duration));
        yield return _fadeCoroutine;
    }

    /// <summary>
    /// 전체 화면 페이드 처리를 수행하는 코루틴.
    /// </summary>
    /// <param name="startAlpha">시작 알파 값.</param>
    /// <param name="endAlpha">목표 알파 값.</param>
    /// <param name="duration">진행 시간(초).</param>
    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        _fadeCoroutine = null;
    }

    private void InitializeLetterbox()
    {
        if (letterboxTop == null || letterboxBottom == null) return;

        letterboxTop.anchoredPosition = new Vector2(0f, letterboxHeight);
        letterboxBottom.anchoredPosition = new Vector2(0f, -letterboxHeight);
    }

    public Coroutine ShowLetterbox(float duration)
    {
        if (letterboxCoroutine != null)
        {
            StopCoroutine(letterboxCoroutine);
        }

        letterboxCoroutine = StartCoroutine(LetterboxRoutine(true, duration));

        return letterboxCoroutine;
    }


    public Coroutine HideLetterbox(float duration)
    {
        if (letterboxCoroutine != null)
        {
            StopCoroutine(letterboxCoroutine);
        }

        letterboxCoroutine = StartCoroutine(LetterboxRoutine(false, duration));

        return letterboxCoroutine;
    }

    private IEnumerator LetterboxRoutine(bool show, float duration)
    {
        float elapsed = 0f;

        Vector2 topStart = letterboxTop.anchoredPosition;
        Vector2 bottomStart = letterboxBottom.anchoredPosition;

        Vector2 topTarget = show ? new Vector2(0f, 0f) : new Vector2(0f, letterboxHeight);
        Vector2 bottomTarget = show ? new Vector2(0f, 0f) : new Vector2(0f, -letterboxHeight);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            letterboxTop.anchoredPosition = Vector2.Lerp(topStart, topTarget, t);
            letterboxBottom.anchoredPosition = Vector2.Lerp(bottomStart, bottomTarget, t);

            yield return null;
        }

        letterboxTop.anchoredPosition = topTarget;
        letterboxBottom.anchoredPosition = bottomTarget;

        letterboxCoroutine = null;
    }

    public void HandleCancelInput()
    {
        // 컷신이나 대화 중이라면 메뉴 조작 무시
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) return;

        // 인벤토리가 열려있다면 인벤토리 닫기
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryOpen)
        {
            InventoryManager.Instance.ToggleInventory();
            return;
        }

        // 아무런 UI가 열려있지 않거나 이미 Pause 상태라면 Pause 메뉴 토글
        if (PauseMenuUI.Instance != null)
        {
            PauseMenuUI.Instance.TogglePauseMenu();
        }
    }

    public IEnumerator ShowJumpscareRoutine(float duration = 0.15f)
    {
        if (jumpscareRect == null) yield break;

        jumpscareRect.gameObject.SetActive(true);
        jumpscareRect.anchoredPosition = Vector2.zero;

        float time = 0;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, 1f, time / duration);
            jumpscareRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        jumpscareRect.localScale = Vector3.one;
    }
}
