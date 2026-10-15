using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLight : MonoBehaviour
{
    [SerializeField] private Light2D playerLight;
    [SerializeField] private float onIntensity = 1f;
    [SerializeField] private float offIntensity = 0f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private bool startOn = true;

    private Coroutine fadeRoutine;

    public bool IsOn { get; private set; }

    private void Awake()
    {
        if (playerLight == null)
            playerLight = GetComponentInChildren<Light2D>();
    }

    private void Start()
    {
        IsOn = startOn;
        SetIntensity(startOn ? onIntensity : offIntensity);
    }

    // 즉시 intensity 설정 (진행 중인 페이드는 중단)
    public void SetIntensity(float level)
    {
        if (level < 0 || playerLight == null) return;

        StopFade();
        playerLight.intensity = level;
    }

    public void TurnOn(bool fade = true) => SetState(true, fade);

    public void TurnOff(bool fade = true) => SetState(false, fade);

    public void Toggle(bool fade = true) => SetState(!IsOn, fade);

    private void SetState(bool on, bool fade)
    {
        if (playerLight == null) return;

        IsOn = on;
        float target = on ? onIntensity : offIntensity;

        if (!fade || fadeDuration <= 0f || !isActiveAndEnabled)
        {
            SetIntensity(target);
            return;
        }

        StopFade();
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float start = playerLight.intensity;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            playerLight.intensity = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        playerLight.intensity = target;
        fadeRoutine = null;
    }

    private void StopFade()
    {
        if (fadeRoutine == null) return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }
}
