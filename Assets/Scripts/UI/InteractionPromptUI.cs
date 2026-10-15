using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Interaction Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Image promptFillImage;

    [Header("Interaction Choice")]
    [SerializeField] private Button choiceButton;
    [SerializeField] private TextMeshProUGUI choiceText;

    private Action _onChoiceSelected;
    private Vector2 _choiceAnchoredPosition;

    void Awake()
    {
        if (choiceButton != null)
            _choiceAnchoredPosition = ((RectTransform)choiceButton.transform).anchoredPosition;
        choiceButton?.onClick.AddListener(SelectChoice);
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (choiceButton != null)
            choiceButton.onClick.RemoveListener(SelectChoice);
    }

    public void Show()
    {
        HideChoice();
        promptFillImage.fillAmount = 1;
        promptRoot?.SetActive(true);
    }

    public void ShowHold(float progress)
    {
        HideChoice();
        promptRoot?.SetActive(true);
        promptFillImage.fillAmount = Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        HideChoice();
        if (promptRoot != null) promptRoot.SetActive(false);
        if (promptFillImage != null) promptFillImage.fillAmount = 0f;
    }

    public void ShowChoice(string text, Action onSelected)
    {
        ShowChoiceContent(text, onSelected, false);
    }

    public void ShowHint(string text)
    {
        ShowChoiceContent(text, null, true);
    }

    private void ShowChoiceContent(string text, Action onSelected, bool isHint)
    {
        promptRoot?.SetActive(false);
        promptFillImage.fillAmount = 0f;

        if (choiceButton == null || choiceText == null) return;

        _onChoiceSelected = onSelected;
        choiceText.text = text;
        if (choiceButton.targetGraphic != null)
            choiceButton.targetGraphic.raycastTarget = !isHint;

        // 키 안내는 지정한 월드 위치에 중앙 정렬하고, 선택지는 기존 오프셋을 유지합니다.
        ((RectTransform)choiceButton.transform).anchoredPosition =
            isHint ? Vector2.zero : _choiceAnchoredPosition;
        Canvas canvas = choiceButton.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            canvas.worldCamera = Camera.main;
        choiceButton.gameObject.SetActive(true);
    }

    private void HideChoice()
    {
        _onChoiceSelected = null;
        if (choiceButton != null)
            choiceButton.gameObject.SetActive(false);
    }

    private void SelectChoice()
    {
        Action onSelected = _onChoiceSelected;
        Hide();
        onSelected?.Invoke();
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
}
