using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{   
    [SerializeField] private Image staminaGuage;
    
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
