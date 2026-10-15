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

    void Awake()
    {
        choiceButton?.onClick.AddListener(SelectChoice);
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        choiceButton?.onClick.RemoveListener(SelectChoice);
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
        promptRoot?.SetActive(false);
        promptFillImage.fillAmount = 0f;
    }

    public void ShowChoice(string text, Action onSelected)
    {
        promptRoot?.SetActive(false);
        promptFillImage.fillAmount = 0f;

        if (choiceButton == null || choiceText == null) return;

        _onChoiceSelected = onSelected;
        choiceText.text = text;
        Canvas canvas = choiceButton.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            canvas.worldCamera = Camera.main;
        choiceButton.gameObject.SetActive(true);
    }

    private void HideChoice()
    {
        _onChoiceSelected = null;
        choiceButton?.gameObject.SetActive(false);
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
