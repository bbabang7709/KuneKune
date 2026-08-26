using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroCutsceneController : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private PlayableDirector director;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private CinemachineCamera cutsceneCamera;

    private bool hasPlayed = false;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        // 시작 안내 문구 블링크 시작
        blinkCoroutine = StartCoroutine(TextBlinkRoutine());
    }

    private void Update()
    {
        if (!hasPlayed && Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(StartCutsceneRoutine());
        }
    }

    private IEnumerator StartCutsceneRoutine()
    {
        // 중복 입력 방지
            hasPlayed = true;

            // 블링크 중지
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            // 안내 문구 숨기기
            guideText.alpha = 0f;

            // 로고 페이드 아웃
            yield return FadeInLogo();

            yield return UIManager.Instance?.FadeOut(0.5f);

            yield return new WaitForSeconds(1f);

            // 컷씬 카메라로 전환
            CameraManager.Instance?.ChangeCamera(cutsceneCamera);

            yield return UIManager.Instance?.FadeIn(0.5f);

            // Timeline 재생
            director.Play();
    }

    /// <summary>
    /// 컷씬 종료 후 실제 게임을 시작한다.
    /// Timeline Signal에서 호출한다.
    /// </summary>
    public void StartGameplay()
    {
        StartCoroutine(StartGameplayRoutine());
    }

    private IEnumerator StartGameplayRoutine()
    {
        // 화면 페이드 아웃
        yield return UIManager.Instance.FadeOut(0.5f);

        // 페이드가 끝난 후 잠시 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 실제 게임 씬으로 이동
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 시작 안내 문구를 반복해서 페이드 인/아웃한다.
    /// </summary>
    private IEnumerator TextBlinkRoutine()
    {
        while (!hasPlayed)
        {
            // Fade In
            yield return FadeText(0f, 1f, 0.5f);

            // 잠시 유지
            yield return new WaitForSeconds(0.2f);

            // Fade Out
            yield return FadeText(1f, 0f, 0.5f);

            // 잠시 유지
            yield return new WaitForSeconds(0.2f);
        }

        // 스페이스 입력 후 확실하게 숨김
        guideText.alpha = 0f;
    }

    /// <summary>
    /// 텍스트의 알파값을 지정된 값까지 부드럽게 변경한다.
    /// </summary>
    private IEnumerator FadeText(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            guideText.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        guideText.alpha = to;
    }

    /// <summary>
    /// 로고를 페이드 아웃한다.
    /// </summary>
    private IEnumerator FadeInLogo()
    {
        float elapsed = 0f;
        float duration = 0.2f;

        logoCanvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            logoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        logoCanvasGroup.alpha = 0f;
    }
}