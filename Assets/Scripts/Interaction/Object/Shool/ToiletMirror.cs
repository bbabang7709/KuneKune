using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToiletMirror : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    [Header("Interaction Settings")]
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite horrorSprite;
    [SerializeField] private Image displayImage;
    [SerializeField] private float displayTime = 0.3f;
    
    
    public void OnInteractPressed()
    {
        if (displayImage == null || horrorSprite == null || originalSprite == null) return;

        StartCoroutine(InteractionRoutine());
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}

    private IEnumerator InteractionRoutine()
    {
        displayImage.sprite = originalSprite;
        displayImage.gameObject.SetActive(true);

        Time.timeScale = 0f;

        float delay = Random.Range(1f, 2f);
        yield return new WaitForSecondsRealtime(delay);
        
        displayImage.sprite = horrorSprite;
        yield return new WaitForSecondsRealtime(displayTime);
        displayImage.sprite = originalSprite;

        yield return new WaitForSecondsRealtime(1f);
        displayImage.gameObject.SetActive(false);

        Time.timeScale = 1f;
    }

}
