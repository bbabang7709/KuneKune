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

    /// <summary>
    /// 화면 전체 페이드 효과를 위한 CanvasGroup.
    /// </summary>
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    /// <summary>
    /// 게임 플레이 UI 전체의 투명도를 조절하는 CanvasGroup.
    /// </summary>
    [SerializeField] private CanvasGroup gameplayUICanvasGroup;

    [Header("Letter Box")]
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;

    [SerializeField] private float letterboxHeight = 100f;
    
    private Coroutine letterboxCoroutine;

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
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);            
        }

        InitializeLetterbox();

    }

    /// <summary>
    /// 기존 전체 화면 글리치가 UI에도 같은 모양으로 적용됩니다.
    /// </summary>
    private void ApplyFullScreenGlitchToUI()
    {
        Canvas uiCanvas = GetComponent<Canvas>();
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

        gameplayUICanvasGroup.alpha = alpha;
    }

    /// <summary>
    /// 지정된 시간 동안 게임 플레이 UI를 서서히 나타나게(Fade In) 한다.
    /// </summary>
    /// <param name="duration">페이드 인에 걸리는 시간(초).</param>
    public IEnumerator FadeInGameplayUI(float duration)
    {
        if (gameplayUICanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = gameplayUICanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            gameplayUICanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);

            yield return null;
        }

        gameplayUICanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 지정된 시간 동안 게임 플레이 UI를 서서히 사라지게(Fade Out) 한다.
    /// </summary>
    /// <param name="duration">페이드 아웃에 걸리는 시간(초).</param>
    public IEnumerator FadeOutGameplayUI(float duration)
    {
        if (gameplayUICanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = gameplayUICanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            gameplayUICanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        gameplayUICanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 화면 전체를 지정된 시간 동안 서서히 밝아지게(Fade In) 한다. (검은 화면 -> 게임 화면)
    /// </summary>
    /// <param name="duration">페이드 인에 걸리는 시간(초).</param>
    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    /// <summary>
    /// 화면 전체를 지정된 시간 동안 서서히 어두워지게(Fade Out) 한다. (게임 화면 -> 검은 화면)
    /// </summary>
    /// <param name="duration">페이드 아웃에 걸리는 시간(초).</param>
    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    /// <summary>
    /// 전체 화면 페이드 처리를 수행하는 코루틴.
    /// </summary>
    /// <param name="startAlpha">시작 알파 값.</param>
    /// <param name="endAlpha">목표 알파 값.</param>
    /// <param name="duration">진행 시간(초).</param>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;

        fadeCanvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;

        if (Mathf.Approximately(endAlpha, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
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
}