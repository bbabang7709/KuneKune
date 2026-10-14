using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FloatingDialog : MonoBehaviour
{
    [Header("References")]
    [Tooltip("머리 위에 띄울 TextMeshPro (3D) 컴포넌트")]
    [SerializeField] private TMP_Text dialogText;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float defaultDisplayTime = 2.0f;

    private Coroutine _dialogRoutine;

    private void Start()
    {
        if (dialogText == null) dialogText = GetComponentInChildren<TMP_Text>(true);
        if (dialogText == null) return;
        Color c = dialogText.color;
        c.a = 0f;
        dialogText.color = c;    
    }

    public void Speak(string message)
    {
        if (dialogText == null) return;

        if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
        _dialogRoutine = StartCoroutine(DialogRoutine(message, defaultDisplayTime));
    }

    public void SpeakWithDuration(string message, float displayTime)
    {
        if (dialogText == null) return;

        if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
        _dialogRoutine = StartCoroutine(DialogRoutine(message, displayTime));
    }

    private IEnumerator DialogRoutine(string message, float displayTime)
    {
        dialogText.text = message;
        Color c = dialogText.color;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            dialogText.color = c;
            yield return null;
        }

        c.a = 1f;
        dialogText.color = c;


        yield return new WaitForSeconds(displayTime);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            dialogText.color = c;
            yield return null;
        }

        c.a = 0f;
        dialogText.color = c;

        _dialogRoutine = null;
    }
}
