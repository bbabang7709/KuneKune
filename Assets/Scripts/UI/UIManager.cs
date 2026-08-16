using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{   
    [Header("Player Status")]
    [SerializeField] private Image staminaGuage;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    void OnEnable()
    {
        playerMovement.OnStaminaChanged += UpdateStaminaUI;
    }

    void OnDisable()
    {
        playerMovement.OnStaminaChanged -= UpdateStaminaUI;
    }

    public void UpdateStaminaUI(float current, float max)
    {
        float ratio = current / max;
        staminaGuage.fillAmount = ratio;
    }
}
