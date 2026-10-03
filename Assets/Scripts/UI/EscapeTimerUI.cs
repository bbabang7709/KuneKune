using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class EscapeTimerUI : MonoBehaviour
{
    public static EscapeTimerUI Instance { get; private set; }
    
    [Header("UI Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] TextMeshProUGUI timerText;

    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [Tooltip("몇 초 남았을 때부터 warningColor로 깜빡일지 설정")]
    [SerializeField] private float warningTime = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 시작할 때는 숨겨두기
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowTimer()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (timerText != null) timerText.color = normalColor;
    }

    public void HideTimer()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void UpdateTime(float timeLeft)
    {
        if (timerText == null) return;

        timerText.text = timeLeft.ToString("F2");

        if (timeLeft <= warningTime)
        {
            timerText.color = warningColor;

            float scale = 1f + Mathf.PingPong(Time.time * 5f, 0.2f);
            timerText.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
