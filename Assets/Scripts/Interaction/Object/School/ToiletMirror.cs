using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToiletMirror : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private Sprite startSprite;
    [SerializeField] private GameObject imageRoot;
    [SerializeField] private Animator imageAnim;
    [SerializeField] private Image displayImage;

    [SerializeField] private float delayTime = 1f;

    private bool _isWatching = false;
    
    public void OnInteractPressed()
    {
        if (displayImage == null) return;
        if (_isWatching) return;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        StartCoroutine(InteractionRoutine());
    }

    public void EndCutscene()
    {
        

        
    }

    private IEnumerator InteractionRoutine()
    {
        _isWatching = true;
        imageRoot.SetActive(true);

        if (imageAnim != null) imageAnim.enabled = false;

        displayImage.sprite = startSprite;

        yield return new WaitForSeconds(delayTime);

        if (imageAnim != null)
        {
            if (imageAnim != null) imageAnim.enabled = true;
            imageAnim?.SetTrigger("Mirror");
        }
        yield return new WaitForSeconds(3f);

        StartCoroutine(EndCutsceneRoutine());
    }

    private IEnumerator EndCutsceneRoutine()
    {
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        imageAnim?.SetTrigger("Reset");
        imageRoot.SetActive(false);
        _isWatching = false;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.InitMap(EMapType.School_WomanToilet, "school_woman_toilet_1");
        }

        yield return new WaitForSeconds(delayTime);

        if (UIManager.Instance != null) 
            yield return UIManager.Instance.FadeIn(0.5f);

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }
}
