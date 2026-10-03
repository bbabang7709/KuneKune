using UnityEngine;

public class MudSlowdownArea : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("둔화 비율 설정")]
    [SerializeField] private float speedMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetSpeedMultiplier(speedMultiplier);
            } 
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetSpeedMultiplier(1.0f);
            }
        }
    }
}
