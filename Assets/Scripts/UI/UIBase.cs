using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIBase : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowUI(float duration = 0.5f)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, duration));   
    }

    public void HideUI(float duration = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, duration, true));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool disableOnComplete = false)
    {
        float startAlpha = _canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;

        if (disableOnComplete)
        {
            gameObject.SetActive(false);
        }
    }
}
