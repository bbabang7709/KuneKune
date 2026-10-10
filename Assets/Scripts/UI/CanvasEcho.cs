using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// 지정한 Canvas에서 UI와 화면 테두리를 피해 반복하는, 소리 없는 메아리.
/// </summary>
public class CanvasEcho : MonoBehaviour
{
    [Header("Canvas")]
    [Tooltip("메아리를 표시할 Canvas. 비워 두면 부모 Canvas를 사용합니다.")]
    [SerializeField] private Canvas targetCanvas;
    [Tooltip("선택 사항: 메아리를 배치할 컨테이너. 비워 두면 Canvas 아래에 자동 생성합니다.")]
    [SerializeField] private RectTransform echoContainer;

    [Header("Presentation")]
    [Tooltip("글꼴과 크기를 복사할 UI 텍스트 또는 프리팹. 비워 두면 게임 기본 도트 글꼴을 사용합니다.")]
    [SerializeField] private TMP_Text echoText;
    [SerializeField] private Color echoColor = new Color(138f / 255f, 40f / 255f, 46f / 255f, 0.65f);
    [SerializeField] private Vector2 delayRange = new Vector2(0.5f, 1f);    // 0.5 ~ 1초마다 반복
    [Tooltip("반복할 때마다 동시에 표시할 메아리 수를 이 범위에서 추첨합니다. 1~8개까지 설정할 수 있습니다.")]
    [SerializeField] private Vector2Int echoCountRange = new Vector2Int(1, 6);
    [SerializeField, Min(0f)] private float fadeInDuration = 0.8f;
    [SerializeField, Min(0f)] private float holdDuration = 0.4f;
    [Tooltip("각 메아리의 페이드아웃 시간은 이 값의 60~140% 범위에서 따로 정합니다.")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;
    [Tooltip("메아리 텍스트가 페이드인/페이드아웃할 때 화면 픽셀 기준으로 좌우로 얼마나 미끄러질지 결정합니다.")]
    [SerializeField, Range(2f, 5f)] private float slidePixels = 4f;

    [Header("Random Placement")]
    [Tooltip("메아리와 겹쳐서는 안 되는 UI 영역. 비워 두어도 재생됩니다.")]
    [FormerlySerializedAs("menuButtons")]
    [SerializeField] private RectTransform[] excludedUI;
    [FormerlySerializedAs("buttonPaddingPixels")]
    [SerializeField, Min(0f)] private float excludedUIPaddingPixels = 16f;
    [SerializeField, Min(0f)] private float screenPaddingPixels = 12f;
    [Tooltip("화면 네 방향에서 각각 제외할 비율입니다. 테두리 안쪽에만 메아리가 표시되도록 합니다.")]
    [FormerlySerializedAs("verticalScreenPaddingRatio")]
    [SerializeField, Range(0f, 0.25f)] private float screenPaddingRatio = 0.05f;

    [Header("Inner Thoughts")]
    [SerializeField] private string[] phrases =
    {
        "왜...", "싫어...", "끔찍해...", "무서워...", "나가고 싶어...",
        "여기 싫어...", "그만...", "제발...", "아니야...", "아니라고...",
        "왜 또...", "또야...", "싫단 말이야...", "생각하기 싫어...", "잊고 싶어...",
        "없어졌으면...", "내버려 둬...", "그만해...", "왜 나야...", "왜 아직도...",
        "기억하기 싫어...", "그 얼굴...", "그 소리...", "너무 싫어...", "토할 것 같아...",
        "기분 나빠...", "역겨워...", "보기 싫어...", "사라져...", "사라졌으면...",
        "없었으면...", "모르고 싶어...", "아무것도 알고 싶지 않아...", "돌아가고 싶어...",
        "집에 가고 싶어...", "도망가고 싶어...", "여기서 나가고 싶어...", "혼자 있고 싶어...",
        "아무도 없었으면...", "왜 생각나...", "그만 생각해...", "싫어.",
        "싫어... 싫어...", "제발 그만..."
    };

    private Coroutine _echoRoutine;
    private int _lastPhraseIndex = -1;
    
    private int _screenWidth;
    private int _screenHeight;
    private Vector2 _excludedUIPadding;
    private readonly List<Echo> _echoList = new List<Echo>();
    private readonly HashSet<int> _batchPhraseIndices = new HashSet<int>();

    private sealed class Echo
    {
        public TMP_Text Text;
        public Vector2 Position;
        /// <summary>
        /// 메아리 텍스트가 페이드인/페이드아웃할 때 살짝 움직이는 거리와 방향
        /// </summary>
        public Vector2 Slide;
        public Rect MovementBounds;
        public float ElapsedTime;
        public float FadeInDuration;
        public float HoldDuration;
        public float FadeOutDuration;
        public float Lifetime => FadeInDuration + HoldDuration + FadeOutDuration;
    }

    private RectTransform _container;
    private RectTransform _generatedContainer;
    private readonly Vector3[] _worldCorners = new Vector3[4];
    public bool IsPlaying => _echoRoutine != null;

    private bool CanShowEcho()
    {
        return isActiveAndEnabled && targetCanvas != null && targetCanvas.isActiveAndEnabled &&
            _container != null && _container.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Inspector에서 지정한 Canvas에서 반복 재생합니다. 게임 상태와 무관하게 호출할 수 있습니다.
    /// </summary>
    public void Play()
    {
        Stop();
        if (!isActiveAndEnabled || phrases == null || phrases.Length == 0)
            return;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null || !targetCanvas.isActiveAndEnabled)
            return;

        RectTransform container = echoContainer;
        if (container != null && container.GetComponentInParent<Canvas>() != targetCanvas)
        {
            Debug.LogWarning("CanvasEcho: Echo Container는 Target Canvas 아래에 있어야 합니다.", this);
            return;
        }

        if (container == null)
        {
            if (_generatedContainer == null || _generatedContainer.parent != targetCanvas.transform)
            {
                ClearEchoList();
                if (_generatedContainer != null)
                    Destroy(_generatedContainer.gameObject);

                GameObject layer = new GameObject("Echo Layer", typeof(RectTransform), typeof(LayoutElement));
                layer.layer = targetCanvas.gameObject.layer;
                _generatedContainer = layer.GetComponent<RectTransform>();
                _generatedContainer.SetParent(targetCanvas.transform, false);
                _generatedContainer.anchorMin = Vector2.zero;
                _generatedContainer.anchorMax = Vector2.one;
                _generatedContainer.sizeDelta = Vector2.zero;
                layer.GetComponent<LayoutElement>().ignoreLayout = true;
            }

            container = _generatedContainer;
        }

        if (_container != container)
            ClearEchoList();
        _container = container;

        if (!CanShowEcho())
            return;

        // 원본은 템플릿으로 유지하고, 별도의 복사본만 표시합니다.
        if (echoText != null && echoText.transform.parent == _container && echoText.gameObject != gameObject)
            echoText.gameObject.SetActive(false);

        _echoRoutine = StartCoroutine(PlayEcho());
    }

    private void ClearEchoList()
    {
        foreach (Echo echo in _echoList)
            if (echo.Text != null)
                Destroy(echo.Text.gameObject);
        _echoList.Clear();
    }

    private void OnDestroy()
    {
        ClearEchoList();
        if (_generatedContainer != null)
            Destroy(_generatedContainer.gameObject);
    }

    public void Stop()
    {
        if (_echoRoutine != null)
        {
            StopCoroutine(_echoRoutine);
            _echoRoutine = null;
        }

        HideEchoTexts();
    }

    private void HideEchoTexts()
    {
        foreach (Echo echo in _echoList)
            HideEchoText(echo.Text);
    }

    private static void HideEchoText(TMP_Text text)
    {
        if (text == null)
            return;

        text.gameObject.SetActive(false);
        text.text = string.Empty;
        text.alpha = 0f;
        text.rectTransform.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        if (_echoRoutine == null)
            return;

        if (!CanShowEcho())
            Stop();
        else if (_screenWidth != Screen.width || _screenHeight != Screen.height)
            HideEchoTexts();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void LateUpdate()
    {
        if (_echoRoutine == null)
            return;

        // 레이아웃 변경도 반영해, 표시 중인 글자가 제외 UI나 테두리에 들어가면 숨깁니다.
        Canvas.ForceUpdateCanvases();
        float horizontalPadding = Mathf.Max(screenPaddingPixels, Screen.width * screenPaddingRatio);
        float verticalPadding = Mathf.Max(screenPaddingPixels, Screen.height * screenPaddingRatio);

        foreach (Echo echo in _echoList)
        {
            if (echo.Text == null || !echo.Text.gameObject.activeSelf)
                continue;

            Rect bounds = GetScreenBounds(echo.Text.rectTransform);
            if (bounds.xMin < horizontalPadding || bounds.xMax > Screen.width - horizontalPadding ||
                bounds.yMin < verticalPadding || bounds.yMax > Screen.height - verticalPadding ||
                OverlapsExcludedUIOnScreen(bounds))
                HideEchoText(echo.Text);
        }
    }

    private bool OverlapsExcludedUIOnScreen(Rect textBounds)
    {
        if (excludedUI == null)
            return false;

        foreach (RectTransform excluded in excludedUI)
        {
            if (excluded == null || !excluded.gameObject.activeInHierarchy)
                continue;

            Rect bounds = GetScreenBounds(excluded);
            bounds = Rect.MinMaxRect(bounds.xMin - excludedUIPaddingPixels, bounds.yMin - excludedUIPaddingPixels,
                bounds.xMax + excludedUIPaddingPixels, bounds.yMax + excludedUIPaddingPixels);
            
            if (textBounds.Overlaps(bounds))
                return true;
        }

        return false;
    }

    private Rect GetScreenBounds(RectTransform target)
    {
        target.GetWorldCorners(_worldCorners);
        Camera camera = GetUICamera(target.GetComponentInParent<Canvas>());
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector3 corner in _worldCorners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }
        
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private IEnumerator PlayEcho()
    {
        while (CanShowEcho())
        {
            float minDelay = Mathf.Max(0f, Mathf.Min(delayRange.x, delayRange.y));
            float maxDelay = Mathf.Max(minDelay, Mathf.Max(delayRange.x, delayRange.y));
            yield return new WaitForSecondsRealtime(Random.Range(minDelay, maxDelay));  // 일시 정지 때문에, 실제 시간을 기준으로 합니다.

            if (!CanShowEcho())
                break;

            HideEchoTexts();
            _batchPhraseIndices.Clear();

            int minCount = Mathf.Clamp(Mathf.Min(echoCountRange.x, echoCountRange.y), 1, 8);
            int maxCount = Mathf.Clamp(Mathf.Max(echoCountRange.x, echoCountRange.y), minCount, 8);
            int count = Random.Range(minCount, maxCount + 1);
            AddEchoList(count);
            
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            for (int i = 0; i < count; i++)
            {
                Echo echo = _echoList[i];
                int phraseIndex = ChoosePhraseIndex();
                echo.Text.text = phrases[phraseIndex];
                echo.Text.color = new Color(echoColor.r, echoColor.g, echoColor.b, 0f);

                if (!TryChoosePosition(echo))
                {
                    HideEchoText(echo.Text);
                    continue;
                }

                _lastPhraseIndex = phraseIndex;
                _batchPhraseIndices.Add(phraseIndex);

                AssignEchoTiming(echo);
                echo.Text.rectTransform.anchoredPosition = echo.Position + echo.Slide;
                echo.Text.gameObject.SetActive(true);
            }

            yield return AnimateEchoes();
            
            HideEchoTexts();
        }

        HideEchoTexts();
        _echoRoutine = null;
    }

    private void AddEchoList(int count)
    {
        while (_echoList.Count < count)
        {
            TMP_Text text;
            if (echoText != null)
                text = Instantiate(echoText, _container, false);
            else
            {
                GameObject instance = new GameObject("Echo", typeof(RectTransform), typeof(TextMeshProUGUI));
                instance.transform.SetParent(_container, false);
                text = instance.GetComponent<TextMeshProUGUI>();
                text.font = Resources.Load<TMP_FontAsset>("Fonts/NeoDunggeunmoPro-Regular") ?? TMP_Settings.defaultFontAsset;
                text.fontSize = 36f;
                text.alignment = TextAlignmentOptions.Center;
            }

            text.name = "Echo " + (_echoList.Count + 1);
            text.gameObject.layer = targetCanvas.gameObject.layer;
            text.raycastTarget = false;
            text.rectTransform.localScale = Vector3.one;
            text.rectTransform.localRotation = Quaternion.identity;
            LayoutElement layout = text.GetComponent<LayoutElement>();

            if (layout == null)
                layout = text.gameObject.AddComponent<LayoutElement>();
            
            layout.ignoreLayout = true;

            // 기존 메뉴에서는 템플릿과 같은 계층에서 버튼 뒤에 그립니다.
            if (echoText != null && echoText.transform.parent == _container)
                text.transform.SetSiblingIndex(echoText.transform.GetSiblingIndex() + _echoList.Count + 1);
            
            HideEchoText(text);
            _echoList.Add(new Echo { Text = text });
        }
    }

    private int ChoosePhraseIndex()
    {
        int start = Random.Range(0, phrases.Length);
        for (int offset = 0; offset < phrases.Length; offset++)
        {
            int index = (start + offset) % phrases.Length;
            // 중복 방지
            if (index != _lastPhraseIndex && !_batchPhraseIndices.Contains(index))
                return index;
        }

        // 위에서 아무것도 선택되지 않으면, 마지막으로 표시한 문구를 다시 표시합니다.
        for (int offset = 0; offset < phrases.Length; offset++)
        {
            int index = (start + offset) % phrases.Length;
            if (!_batchPhraseIndices.Contains(index))
                return index;
        }

        return start;
    }

    private bool TryChoosePosition(Echo echo)
    {
        TMP_Text text = echo.Text;
        RectTransform backgroundRect = _container;
        Canvas canvas = text.GetComponentInParent<Canvas>();
        
        if (backgroundRect == null || canvas == null)
            return false;

        Canvas.ForceUpdateCanvases();
        Canvas rootCanvas = canvas.rootCanvas;
        Camera uiCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

        // 화면 좌표를 backgroundRect 기준 UI 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, Vector2.zero, uiCamera, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, new Vector2(Screen.width, Screen.height), uiCamera, out Vector2 max))
            return false;

        Rect screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        // 픽셀 단위 설정값들을 UI 좌표 단위로 바꿈
        Vector2 pixelSize = new Vector2(screenRect.width / Mathf.Max(1, Screen.width), screenRect.height / Mathf.Max(1, Screen.height));
        Vector2 slide = Vector2.right * (pixelSize.x * slidePixels);
        Vector2 screenPadding = pixelSize * screenPaddingPixels;
        screenPadding.x = Mathf.Max(screenPadding.x, screenRect.width * screenPaddingRatio);
        screenPadding.y = Mathf.Max(screenPadding.y, screenRect.height * screenPaddingRatio);
        _excludedUIPadding = pixelSize * excludedUIPaddingPixels;
        Rect canvasBounds = GetLocalBounds(backgroundRect, targetCanvas.transform as RectTransform);
        Vector2 allowedMin = Vector2.Max(screenRect.min + screenPadding, canvasBounds.min);
        Vector2 allowedMax = Vector2.Min(screenRect.max - screenPadding, canvasBounds.max);

        // 짧은 문구는 작게, 긴 독백은 줄바꿈하여 실제 표시 크기로 겹침을 검사합니다.
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        text.margin = Vector4.zero;
        
        float textPadding = text.fontSize * 0.1f;   // 간격은 폰트 크기의 10%
        float maxWidth = screenRect.width * 0.35f;  // 최대 폭은 화면 너비의 35%

        // maxWidth와 비교해서 실제로 필요한 크기를 계산합니다. (줄바꿈 포함)
        Vector2 preferred = text.GetPreferredValues(text.text, maxWidth, Mathf.Infinity);   // 이 문자열을 maxWidth 안에 표시한다면 필요한 실제 크기
        Vector2 textSize = new Vector2(Mathf.Min(maxWidth, preferred.x), preferred.y) + (Vector2.one * (textPadding * 2f));   // 글자 너비 + 여백 / 글자 높이 + 여백
        textRect.sizeDelta = textSize;
        text.margin = new Vector4(textPadding, textPadding, textPadding, textPadding);

        // 페이드 전체의 이동 범위(시작 +4px, 종료 -2px)까지 화면 안에 둡니다.
        Vector2 motionMin = -slide * 0.5f;
        Vector2 motionMax = slide;

        // 화면 전체에서 아무 데나 나오지 않고, 텍스트의 중심이 minCenter ~ maxCenter 사이에서만 나오도록 제한합니다.
        Vector2 minCenter = allowedMin + (textSize * 0.5f) - motionMin;
        Vector2 maxCenter = allowedMax - (textSize * 0.5f) - motionMax;
        if (minCenter.x > maxCenter.x || minCenter.y > maxCenter.y)
            return false;

        // 랜덤 위치를 최대 128번 시도하면서, 버튼이나 다른 메아리와 겹치지 않는 안전한 위치를 찾습니다.
        for (int attempt = 0; attempt < 128; attempt++)
        {
            // 텍스트의 중심 좌표를 유효한 범위에서 랜덤하게 선택
            Vector2 center = new Vector2(Random.Range(minCenter.x, maxCenter.x), Random.Range(minCenter.y, maxCenter.y));
            
            // 텍스트가 움직이는 전체 영역을 포함하는 사각형
            Rect sweptRect = new Rect(center - (textSize * 0.5f) + motionMin, textSize + motionMax - motionMin);

            // 버튼과 겹치거나 다른 메아리와 겹치는지 확인
            if (OverlapsExcludedUI(backgroundRect, sweptRect, _excludedUIPadding) || OverlapsOtherEcho(sweptRect))
                continue;

            echo.Position = center - backgroundRect.rect.center;
            echo.Slide = slide;
            echo.MovementBounds = sweptRect;
            return true;
        }

        // 안전한 영역이 없으면 이번 메아리를 생략합니다.
        return false;
    }

    private bool OverlapsOtherEcho(Rect bounds)
    {
        foreach (Echo echo in _echoList)
            if (echo.Text != null && echo.Text.gameObject.activeSelf && bounds.Overlaps(echo.MovementBounds))
                return true;
        return false;
    }

    private bool OverlapsExcludedUI(RectTransform backgroundRect, Rect textBounds, Vector2 padding)
    {
        if (excludedUI == null)
            return false;

        foreach (RectTransform button in excludedUI)
        {
            if (button == null)
                continue;
            if (!button.gameObject.activeInHierarchy)
                continue;

            Rect buttonRect = GetLocalBounds(backgroundRect, button);
            Rect buttonBounds = Rect.MinMaxRect(buttonRect.xMin - padding.x, buttonRect.yMin - padding.y,
                buttonRect.xMax + padding.x, buttonRect.yMax + padding.y);
            
            if (textBounds.Overlaps(buttonBounds))
                return true;
        }

        return false;
    }

    private static Camera GetUICamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        Canvas root = canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    private Rect GetLocalBounds(RectTransform backgroundRect, RectTransform target)
    {
        target.GetWorldCorners(_worldCorners);
        Camera sourceCamera = GetUICamera(target.GetComponentInParent<Canvas>());
        Camera destinationCamera = GetUICamera(targetCanvas);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        
        foreach (Vector3 corner in _worldCorners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corner);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, screen, destinationCamera, out Vector2 local))
                return Rect.MinMaxRect(-float.MaxValue / 4f, -float.MaxValue / 4f, float.MaxValue / 4f, float.MaxValue / 4f);
        
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void AssignEchoTiming(Echo echo)
    {
        echo.ElapsedTime = 0f;
        echo.FadeInDuration = Mathf.Max(0f, fadeInDuration);
        float maxHold = Mathf.Max(0f, holdDuration);
        echo.HoldDuration = Random.Range(Mathf.Min(0.2f, maxHold), maxHold);
        echo.FadeOutDuration = Mathf.Max(0f, fadeOutDuration) * Random.Range(0.6f, 1.4f);

        // 무작위 시간이 비슷해도 같은 순간에 끝나지 않도록 최소 간격을 확보합니다.
        const float minDisappearSpacing = 0.15f;
        for (int attempt = 0; attempt < _echoList.Count; attempt++)
        {
            bool adjusted = false;
            foreach (Echo other in _echoList)
            {
                if (other == echo || other.Text == null || !other.Text.gameObject.activeSelf)
                    continue;

                if (Mathf.Abs(echo.Lifetime - other.Lifetime) < minDisappearSpacing)
                {
                    echo.HoldDuration += other.Lifetime + minDisappearSpacing - echo.Lifetime;
                    adjusted = true;
                }
            }

            if (!adjusted)
                break;
        }
    }

    private IEnumerator AnimateEchoes()
    {
        while (true)
        {
            bool hasActiveEcho = false;
            foreach (Echo echo in _echoList)
            {
                if (echo.Text == null || !echo.Text.gameObject.activeSelf)
                    continue;

                echo.ElapsedTime += Time.unscaledDeltaTime;
                if (echo.ElapsedTime >= echo.Lifetime)
                {
                    HideEchoText(echo.Text);
                    continue;
                }

                hasActiveEcho = true;
                if (echo.ElapsedTime < echo.FadeInDuration)
                {
                    float progress = Mathf.SmoothStep(0f, 1f, echo.ElapsedTime / echo.FadeInDuration);
                    echo.Text.alpha = Mathf.Lerp(0f, echoColor.a, progress);
                    echo.Text.rectTransform.anchoredPosition = Vector2.Lerp(echo.Position + echo.Slide, echo.Position, progress);
                }
                else if (echo.ElapsedTime < echo.FadeInDuration + echo.HoldDuration)
                {
                    echo.Text.alpha = echoColor.a;
                    echo.Text.rectTransform.anchoredPosition = echo.Position;
                }
                else
                {
                    float fadeElapsed = echo.ElapsedTime - echo.FadeInDuration - echo.HoldDuration;
                    float progress = Mathf.SmoothStep(0f, 1f, fadeElapsed / echo.FadeOutDuration);
                    echo.Text.alpha = Mathf.Lerp(echoColor.a, 0f, progress);
                    echo.Text.rectTransform.anchoredPosition = Vector2.Lerp(echo.Position, echo.Position - echo.Slide * 0.5f, progress);
                }
            }

            if (!hasActiveEcho)
                yield break;

            yield return null;
        }
    }
}
