using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingEventManager : MonoBehaviour
{
    public static LightingEventManager Instance { get; private set; }

    private Light2D _globalLight;

    private Coroutine _lightCoroutine;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _globalLight = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None)
            .FirstOrDefault(light => light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
    }

    public void FadeGlobalLight(Color targetColor, float targetIntensity, float duration = 1.0f)
    {
        if (_globalLight == null) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CoFadeGlobalLight(targetColor, targetIntensity, duration));
    }

    public IEnumerator CoFadeGlobalLight(Color targetColor, float targetIntensity, float duration)
    {
        Color startColor = _globalLight.color;
        float startIntensity = _globalLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            _globalLight.color = Color.Lerp(startColor, targetColor, t);
            _globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        _globalLight.color = targetColor;
        _globalLight.intensity = targetIntensity;
        _fadeCoroutine = null;
    }

    public void SetBlackout(float targetIntensity = 0.05f, float duration = 1.5f)
    {
        if (_globalLight == null) return;

        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LerpLightIntensity(targetIntensity, duration));
    }

    public void RestoreLight(float targetIntensity = 1f, float duration = 0.5f)
    {
        if (_globalLight == null) return;
        
        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        _lightCoroutine = StartCoroutine(LerpLightIntensity(targetIntensity, duration));
    }

    public IEnumerator FlickerRoutine()
    {
        if (_globalLight == null) yield break;

        float originalIntensity = _globalLight.intensity;

        _globalLight.intensity = 0f;
        yield return new WaitForSeconds(0.1f);

        _globalLight.intensity = originalIntensity * 0.5f;
        yield return new WaitForSeconds(0.05f);

        _globalLight.intensity = 0f;
        yield return new WaitForSeconds(0.2f);

        _globalLight.intensity = originalIntensity;
    }

    private IEnumerator LerpLightIntensity(float targetIntensity, float duration)
    {
        float elapsed = 0f;
        float startIntensity = _globalLight.intensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
        _globalLight.intensity = targetIntensity;
        _lightCoroutine = null;
    }
}
