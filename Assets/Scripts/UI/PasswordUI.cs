using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class PasswordUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI displayText;

    private static readonly string PASSWORD = "0724";
    private static readonly int MAX_LENGTH = 4;

    private StringBuilder input = new();
    private bool isBlinking = false;

    public event Action OnCorrectPassword;

    void Start()
    {
        Clear();
    }

    public void InputNumber(int num)
    {
        if (isBlinking || input.Length >= MAX_LENGTH) return;

        input.Append(num);
        RefreshUI();
    }

    public void DeleteNumber()
    {
        if (isBlinking || input.Length == 0) return;

        input.Remove(input.Length - 1, 1);
        RefreshUI();
    }

    public void Clear()
    {
        input.Clear();
        RefreshUI();
    }

    public void Enter()
    {
        if (isBlinking) return;

        if (input.ToString() == PASSWORD)
        {
            Debug.Log("정답");
            OnCorrectPassword?.Invoke();
        }
        else
        {
            Debug.Log("오답");
            StartCoroutine(BlinkDisplay());
        }
    }

    public void CloseUI()
    {
        StopAllCoroutines();
        isBlinking = false;
        if (displayText != null) displayText.alpha = 1f;

        Clear();
        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (displayText != null)
            displayText.text = input.ToString();
    }

    private IEnumerator BlinkDisplay()
    {
        float timer = 0f;

        displayText.alpha = 1f;

        while (timer < 0.25f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        displayText.alpha = 0f;

        timer = 0f;
        while (timer < 0.25f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        displayText.alpha = 1f;

        yield return new WaitForSeconds(0.25f);

        Clear();
    }
}
