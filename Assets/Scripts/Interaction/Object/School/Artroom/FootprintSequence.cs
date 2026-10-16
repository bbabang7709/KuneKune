using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FootprintSequence : MonoBehaviour
{
    [Header("Footprint Settings")]
    [Tooltip("미리 배치한 발자국을 나타날 순서대로 넣기")]
    [SerializeField] private List<SpriteRenderer> footprints;
    
    [Tooltip("다음 Footprints가 나타날 때까지 대기 시간")]
    [SerializeField] private float delayBetweenPrints = 0.15f;

    [Tooltip("하나의 발자국이 완전히 진해지는데 걸리는 시간")]
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        SetFootprints(0f, true);
    }

    private void SetFootprints(float alpha, bool active)
    {
        foreach (var fp in footprints)
        {
            if (fp == null) continue;
            Color c = fp.color;
            c.a = alpha;
            fp.color = c;
            fp.gameObject.SetActive(active);
        }
    }

    public void PlayFootprintSequence()
    {
        StopAllCoroutines();
        SetFootprints(0f, true);
        StartCoroutine(SequenceRoutine());
    }

    public void ShowFootprints()
    {
        StopAllCoroutines();
        SetFootprints(1f, true);
    }

    public void HideFootprint()
    {
        StopAllCoroutines();
        SetFootprints(0f, false);
    }

    private IEnumerator SequenceRoutine()
    {
        foreach (var fp in footprints)
        {
            if (fp == null) continue;

            StartCoroutine(FadeInFootprint(fp, fadeDuration));

            // TODO: 발자국 소리 재생

            yield return new WaitForSeconds(delayBetweenPrints);
        }
    } 

    private IEnumerator FadeInFootprint(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color c = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        c.a = 1f;
        sr.color = c;
    }
}
