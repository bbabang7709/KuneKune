using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 배경에 주인공의 생각처럼 스며드는, 소리 없는 메아리.
/// </summary>
public class PauseMenuEcho : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private TMP_Text echoText;
    [SerializeField] private Color echoColor = new Color(138f / 255f, 40f / 255f, 46f / 255f, 0.65f);
    [SerializeField] private Vector2 delayRange = new Vector2(1.5f, 3f);
    [Tooltip("반복할 때마다 동시에 표시할 메아리 수를 이 범위에서 추첨합니다. 1~8개까지 설정할 수 있습니다.")]
    [SerializeField] private Vector2Int echoCountRange = new Vector2Int(1, 6);
    [SerializeField, Min(0f)] private float fadeInDuration = 0.8f;
    [SerializeField, Min(0f)] private float holdDuration = 0.7f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 1f;
    [Tooltip("메아리 텍스트가 페이드인/페이드아웃할 때 화면 픽셀 기준으로 좌우로 얼마나 미끄러질지 결정합니다.")]
    [SerializeField, Range(2f, 5f)] private float slidePixels = 4f;

    [Header("Random Placement")]
    [Tooltip("메아리의 표시 영역이 겹쳐서는 안 되는 메뉴 버튼")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField, Min(0f)] private float buttonPaddingPixels = 16f;
    [SerializeField, Min(0f)] private float screenPaddingPixels = 12f;

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
    private Vector2 _buttonPadding;
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
    }

    private void Awake()
    {
        if (echoText != null)
        {
            echoText.raycastTarget = false;
            _echoList.Add(new Echo { Text = echoText });
        }

        if (menuButtons == null || menuButtons.Length == 0)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            menuButtons = new RectTransform[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
                menuButtons[i] = buttons[i].transform as RectTransform;
        }

        Stop();
    }

    private bool CanShowEcho()
    {
        KuneKune kuneKune = MapManager.Instance != null ? MapManager.Instance.KuneKune : null;
        return isActiveAndEnabled && PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused &&
            kuneKune != null && kuneKune.isActiveAndEnabled && kuneKune.State != ECharacterState.Unspawned &&
            (SaveManager.Instance == null || !SaveManager.Instance.IsLoading);
    }

    /// <summary>
    /// 메뉴가 열려 있는 동안 반복하고, 확인창을 열거나 메뉴를 닫으면 모두 정리합니다.
    /// </summary>
    public void Play()
    {
        Stop();

        if (!CanShowEcho() || echoText == null || phrases == null || phrases.Length == 0)
            return;

        _echoRoutine = StartCoroutine(PlayEcho());
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
        // 쿠네쿠네가 디스폰되면 즉시 중단합니다.
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

        // 메아리가 표시 중에 버튼 위치나 크기가 바뀌어도
        // 겹치는 메아리는 즉시 사라집니다.
        RectTransform backgroundRect = transform as RectTransform;
        foreach (Echo echo in _echoList)
        {
            if (echo.Text != null && echo.Text.gameObject.activeSelf &&
                (backgroundRect == null || OverlapsMenuButton(backgroundRect,
                  GetLocalBounds(backgroundRect, echo.Text.rectTransform), _buttonPadding)))
                HideEchoText(echo.Text);
        }
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

                echo.Text.rectTransform.anchoredPosition = echo.Position + echo.Slide;
                echo.Text.gameObject.SetActive(true);
            }

            yield return FadeEchoes(0f, echoColor.a, fadeInDuration, false);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdDuration));
            yield return FadeEchoes(echoColor.a, 0f, fadeOutDuration, true);
            
            HideEchoTexts();
        }

        HideEchoTexts();
        _echoRoutine = null;
    }

    private void AddEchoList(int count)
    {
        while (_echoList.Count < count)
        {
            TMP_Text text = Instantiate(echoText, echoText.transform.parent);
            text.name = "Menu Echo " + (_echoList.Count + 1);
            text.raycastTarget = false;

            // 모든 메아리를 메뉴 버튼보다 뒤에 그리며, 반복할 때 재사용합니다.
            text.transform.SetSiblingIndex(echoText.transform.GetSiblingIndex() + _echoList.Count);
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
        RectTransform backgroundRect = transform as RectTransform;
        Canvas canvas = text.GetComponentInParent<Canvas>();
        
        if (backgroundRect == null || canvas == null || menuButtons == null || menuButtons.Length == 0)
            return false;

        Canvas.ForceUpdateCanvases();   // 메뉴 레이아웃 계산
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
        _buttonPadding = pixelSize * buttonPaddingPixels;

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
        Vector2 minCenter = screenRect.min + screenPadding + (textSize * 0.5f) - motionMin; // 화면 왼쪽 아래 + 여백 + 텍스트 절반 크기 + 움직일 수 있는 왼쪽 거리
        Vector2 maxCenter = screenRect.max - screenPadding - (textSize * 0.5f) - motionMax; // 화면 오른쪽 끝 - 여백 - 텍스트 절반 크기 - 움직일 수 있는 오른쪽 거리
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
            if (OverlapsMenuButton(backgroundRect, sweptRect, _buttonPadding) || OverlapsOtherEcho(sweptRect))
                continue;

            echo.Position = center - backgroundRect.rect.center;
            echo.Slide = slide;
            echo.MovementBounds = sweptRect;
            return true;
        }

        // 안전한 영역이 없으면 버튼을 가리는 대신 이번 메아리를 생략합니다.
        return false;
    }

    private bool OverlapsOtherEcho(Rect bounds)
    {
        foreach (Echo echo in _echoList)
            if (echo.Text != null && echo.Text.gameObject.activeSelf && bounds.Overlaps(echo.MovementBounds))
                return true;
        return false;
    }

    private bool OverlapsMenuButton(RectTransform backgroundRect, Rect textBounds, Vector2 padding)
    {
        foreach (RectTransform button in menuButtons)
        {
            if (button == null)
                return true;
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

    private Rect GetLocalBounds(RectTransform backgroundRect, RectTransform target)
    {
        // RectTransform 사각형 네 모서리 좌표
        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);
    
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector3 corner in worldCorners)
        {
            Vector2 local = backgroundRect.InverseTransformPoint(corner);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private IEnumerator FadeEchoes(float startAlpha, float endAlpha, float duration, bool isFadeOut)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyEchoFade(Mathf.Lerp(startAlpha, endAlpha, progress), progress, isFadeOut);
            yield return null;
        }

        ApplyEchoFade(endAlpha, 1f, isFadeOut);
    }

    private void ApplyEchoFade(float alpha, float progress, bool isFadeOut)
    {
        foreach (Echo echo in _echoList)
        {
            if (echo.Text == null || !echo.Text.gameObject.activeSelf)
                continue;

            Vector2 start = isFadeOut ? echo.Position : echo.Position + echo.Slide;
            Vector2 end = isFadeOut ? echo.Position - echo.Slide * 0.5f : echo.Position;
            echo.Text.alpha = alpha;
            echo.Text.rectTransform.anchoredPosition = Vector2.Lerp(start, end, progress);
        }
    }
}
