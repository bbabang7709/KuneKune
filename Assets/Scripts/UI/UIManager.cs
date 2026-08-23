using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 내 UI를 관리하는 싱글톤 매니저.
/// 플레이어 상태 UI, 대화 UI, 이미지 연출 UI 등을 관리한다.
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
    [SerializeField] private Image heatGuage;

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

        dialogRoot.SetActive(false);
        illustrationRoot.SetActive(false);

        dialogText.text = string.Empty;
    }

    /// <summary>
    /// 플레이어 스태미나 변경 이벤트를 구독한다.
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
    /// 플레이어 스태미나 변경 이벤트 구독을 해제한다.
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

    public void UpdateHeatUI(float current, float max)
    {
        float ratio = current / max;
        heatGuage.fillAmount = ratio;
    }    
}
