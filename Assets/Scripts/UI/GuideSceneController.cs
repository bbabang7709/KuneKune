using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GuideSceneController : MonoBehaviour
{
    [SerializeField] private Image guideImage;
    [SerializeField] private Sprite warningSprite;
    [SerializeField] private Sprite controlsSprite;
    [SerializeField, Min(0f)] private float warningFadeDuration = 1.5f;
    [SerializeField, Min(0f)] private float warningDisplayDuration = 3f;
    [SerializeField] private string targetSceneName = "Scene_Intro";

    private CanvasGroup guideCanvasGroup;

    private void Awake()
    {
        if (guideImage == null || warningSprite == null || controlsSprite == null)
        {
            Debug.LogError("가이드 화면의 이미지와 스프라이트를 연결해주세요.", this);
            enabled = false;
            return;
        }

        guideCanvasGroup = GetComponent<CanvasGroup>();
        guideCanvasGroup.alpha = 0f;
        guideImage.sprite = warningSprite;
        guideImage.color = Color.white;
    }

    private IEnumerator Start()
    {
        // 첫 UI 메시가 준비된 다음 프레임부터 페이드를 시작한다.
        Canvas.ForceUpdateCanvases();
        yield return null;

        float elapsed = 0f;
        while (elapsed < warningFadeDuration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            // 이미지 정점을 매번 갱신하지 않고 그룹의 투명도만 변경한다.
            guideCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f,
                elapsed / warningFadeDuration);
        }

        guideCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(warningDisplayDuration);

        guideImage.sprite = controlsSprite;

        // 조작법을 표시한 프레임의 입력으로 즉시 넘어가지 않도록 한다.
        yield return null;
        yield return new WaitUntil(() => Input.anyKeyDown);

        SceneManager.LoadScene(targetSceneName);
    }
}
