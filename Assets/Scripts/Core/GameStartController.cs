using System.Collections;
using UnityEngine;

public class GameStartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("Settings")]
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float uiFadeDuration = 1f;

    void Start()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        playerMovement.SetMovementLocked(true);

        UIManager.Instance?.SetGameplayUIAlpha(0f);
        UIManager.Instance?.SetFadeAlpha(1f);

        yield return new WaitForSeconds(0.5f);

        yield return UIManager.Instance?.FadeIn(screenFadeDuration);

        yield return UIManager.Instance?.FadeInGameplayUI(uiFadeDuration);

        playerMovement.SetMovementLocked(false);
    }
}
