using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingEventManager : MonoBehaviour
{
    public static LightingEventManager Instance { get; private set; }

    private Light2D _globalLight;
    private static readonly Color HouseLightColor = new Color32(0x33, 0x33, 0x33, 0xFF);
    private bool _isHouseLightingActive;
    private Color _colorBeforeHouse;

    private Coroutine _lightCoroutine;
    private Coroutine _fadeCoroutine;
    private bool _isPowerOutageActive;
    private float _intensityBeforePowerOutage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindGlobalLight();
    }

    private void FindGlobalLight()
    {
        _globalLight = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None)
            .FirstOrDefault(light => light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
    }

    /// <summary>Awake 시점에 글로벌 라이트가 없었다면 사용 직전에 다시 찾습니다.</summary>
    private bool EnsureGlobalLight()
    {
        if (_globalLight == null) FindGlobalLight();
        return _globalLight != null;
    }

    public void FadeGlobalLight(Color targetColor, float targetIntensity, float duration = 1.0f)
    {
        if (!EnsureGlobalLight()) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CoFadeGlobalLight(targetColor, targetIntensity, duration));
    }

    /// <summary>글로벌 라이트의 색상만 즉시 바꿉니다. 밝기 코루틴에는 영향을 주지 않습니다.</summary>
    public void SetGlobalLightColor(Color color)
    {
        if (!EnsureGlobalLight()) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        ApplyGlobalLightColor(color);
    }

    public void SaveCheckpointLighting(SaveData data)
    {
        data.hasCheckpointLighting = EnsureGlobalLight();
        if (!data.hasCheckpointLighting) return;

        data.checkpointLightColor = _globalLight.color;
        data.checkpointLightIntensity = _globalLight.intensity;
    }

    public void RestoreCheckpointLighting(SaveData data)
    {
        Color color;
        float intensity;
        if (data.hasCheckpointLighting)
        {
            color = data.checkpointLightColor;
            intensity = data.checkpointLightIntensity;
        }
        else if (data.currentChapter == EChapter.Chapter3_School &&
            data.GetFlag(KuneKuneHand.ExitCheckpointFlag) && !data.GetFlag(DirtyDeskCutsceneController.CompletedFlag))
        {
            // 조명 필드가 없던 버전에서 저장한 여자 화장실 출구 체크포인트도 복구합니다.
            color = KuneKuneHand.EventLightColor;
            intensity = 1f;
        }
        else
        {
            return;
        }

        if (!EnsureGlobalLight()) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        _fadeCoroutine = null;
        _lightCoroutine = null;
        _isPowerOutageActive = false;
        ApplyGlobalLightColor(color);
        _globalLight.intensity = intensity;
    }

    /// <summary>맵 전환의 암전 중 집 조명을 적용하거나 원래 색으로 복구합니다.</summary>
    public void SetHouseLighting(bool isInsideHouse)
    {
        if (!EnsureGlobalLight() || _isHouseLightingActive == isInsideHouse) return;

        if (isInsideHouse)
            _colorBeforeHouse = _globalLight.color;

        _isHouseLightingActive = isInsideHouse;
        _globalLight.color = isInsideHouse ? HouseLightColor : _colorBeforeHouse;
    }

    private void ApplyGlobalLightColor(Color color)
    {
        // 집 안에서 챕터 조명이 갱신돼도 집 색은 유지하고, 나갈 때 복구할 색만 갱신합니다.
        if (_isHouseLightingActive)
            _colorBeforeHouse = color;

        _globalLight.color = _isHouseLightingActive ? HouseLightColor : color;
    }

    public IEnumerator CoFadeGlobalLight(Color targetColor, float targetIntensity, float duration)
    {
        Color startColor = _isHouseLightingActive ? _colorBeforeHouse : _globalLight.color;
        float startIntensity = _globalLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 맵 전환 중 timeScale이 0이어도 배경 조명이 멈추지 않게 합니다.
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            ApplyGlobalLightColor(Color.Lerp(startColor, targetColor, t));
            _globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        ApplyGlobalLightColor(targetColor);
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

    /// <summary>마지막 정전 구간에서 대상을 배치한 뒤 원래 밝기로 켭니다.</summary>
    public IEnumerator PowerOutageRoutine(Action onDark)
    {
        if (_globalLight == null)
        {
            onDark?.Invoke();
            yield break;
        }

        RestoreAfterPowerOutage();
        if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _lightCoroutine = null;
        _fadeCoroutine = null;
        _intensityBeforePowerOutage = _globalLight.intensity;
        _isPowerOutageActive = true;

        try
        {
            _globalLight.intensity = 0f;
            yield return new WaitForSeconds(0.1f);
            _globalLight.intensity = _intensityBeforePowerOutage * 0.5f;
            yield return new WaitForSeconds(0.05f);
            _globalLight.intensity = 0f;
            onDark?.Invoke();
            yield return new WaitForSeconds(0.2f);
        }
        finally
        {
            RestoreAfterPowerOutage();
        }
    }

    public void RestoreAfterPowerOutage()
    {
        if (!_isPowerOutageActive) return;
        if (_globalLight != null) _globalLight.intensity = _intensityBeforePowerOutage;
        _isPowerOutageActive = false;
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
