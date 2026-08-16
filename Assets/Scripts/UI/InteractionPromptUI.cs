using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Interaction Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Image promptFillImage;

    void Awake()
    {
        Hide();
    }

    public void Show()
    {
        promptFillImage.fillAmount = 1;
        promptRoot?.SetActive(true);
    }

    public void ShowHold(float progress)
    {
        promptRoot?.SetActive(true);
        promptFillImage.fillAmount = Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        promptRoot?.SetActive(false);
        promptFillImage.fillAmount = 0f;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
}
