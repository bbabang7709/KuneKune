using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingEventManager : MonoBehaviour
{
    public static LightingEventManager Instance { get; private set; }

    [Header("Global Lighting")]
    [Tooltip("Global Light 2D 오브젝트")]
    [SerializeField] private Light2D globalLight;

    private Coroutine _lightCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetBlackout(float targetIntensity = 0.05f, float duration = 1.5f)
    {
        if (globalLight == null) return;

        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LerpLightIntensity(targetIntensity, duration));
    }

    public void RestoreLight(float targetIntensity = 1f, float duration = 0.5f)
    {
        if (globalLight == null) return;
        
        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LerpLightIntensity(targetIntensity, duration));
    }

    public IEnumerator FlickerRoutine()
    {
        if (globalLight == null) yield break;

        float originalIntensity = globalLight.intensity;

        globalLight.intensity = 0f;
        yield return new WaitForSeconds(0.1f);

        globalLight.intensity = originalIntensity * 0.5f;
        yield return new WaitForSeconds(0.05f);

        globalLight.intensity = 0f;
        yield return new WaitForSeconds(0.2f);

        globalLight.intensity = originalIntensity;
    }

    private IEnumerator LerpLightIntensity(float targetIntensity, float duration)
    {
        float elapsed = 0f;
        float startIntensity = globalLight.intensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
        globalLight.intensity = targetIntensity;
        _lightCoroutine = null;
    }
}
