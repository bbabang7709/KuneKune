using System.Collections;
using UnityEngine;

public class ScarecrowObstacle : InteractableBase
{
    [Header("References")]
    [SerializeField] private Collider2D _blockCollider;
    [SerializeField] private SpriteRenderer _sr;

    [Header("Settings")]
    [SerializeField] private float squashDuration = 0.1f;
    [SerializeField] private float targetSquashRatio = 0.95f;
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] private int maxInteractCount = 5;
    private int _interactCount = 0;

    public override void OnInteractPressed()
    {
        if (_interactCount >= maxInteractCount) return;

        _interactCount++;
        StartCoroutine(SquashRoutine());

        if (_interactCount == maxInteractCount)
        {
            if (_blockCollider != null) _blockCollider.enabled = false;

            KuneKune kunekune = FindFirstObjectByType<KuneKune>();
            if (kunekune != null)
            {
                // TODO: 쿠네쿠네가 살짝 멈칫하게 만들기
            }

            // TODO: 기괴한 소리 재생
            StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        Color originalColor = _sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            transform.localScale = new Vector3(1f + Random.Range(-0.2f, 0.2f), 1f, 1f);
            
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            _sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator SquashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < squashDuration)
        {
            elapsed += Time.deltaTime;
            float y = Mathf.Lerp(1f, targetSquashRatio, elapsed / squashDuration);

            transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
            yield return null;
        }
        transform.localScale = new Vector3(transform.localScale.x, targetSquashRatio, transform.localScale.z);

        elapsed = 0f;
        while (elapsed < squashDuration)
        {
            elapsed += Time.deltaTime;
            float y = Mathf.Lerp(targetSquashRatio, 1f, elapsed / squashDuration);

            transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
            yield return null;
        }

        transform.localScale = new Vector3(transform.localScale.x, 1f, transform.localScale.z);
    }
}
