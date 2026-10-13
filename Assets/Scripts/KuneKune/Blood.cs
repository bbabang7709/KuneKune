using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Blood : MonoBehaviour
{
    private const float FadeDuration = 2f;

    [SerializeField] private Collider2D _kuneKune;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        SetAlpha(0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActiveAndEnabled || _kuneKune == null || collision != _kuneKune)
            return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        SetAlpha(0f);
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / FadeDuration));
        }

        SetAlpha(1f);
        _fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _spriteRenderer.color;
        color.a = alpha;
        _spriteRenderer.color = color;
    }

    private void OnDisable()
    {
        if (_fadeCoroutine == null)
            return;

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }
}
