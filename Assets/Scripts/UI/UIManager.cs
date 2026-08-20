using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{   
    [Header("Player Status")]
    [SerializeField] private Image staminaGuage;
    [SerializeField] private Image heatGuage;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    void OnEnable()
    {
        playerMovement.OnStaminaChanged += UpdateStaminaUI;
        playerMovement.OnHeatChanged += UpdateHeatUI;
    }

    void OnDisable()
    {
        playerMovement.OnStaminaChanged -= UpdateStaminaUI;
        playerMovement.OnHeatChanged -= UpdateHeatUI;
    }

    public void UpdateStaminaUI(float current, float max)
    {
        float ratio = current / max;
        staminaGuage.fillAmount = ratio;
    }

    public void UpdateHeatUI(float current, float max)
    {
        float ratio = current / max;
        heatGuage.fillAmount = ratio;
    }    
}
