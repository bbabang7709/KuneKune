using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToiletMirror : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("첫 상호작용 이벤트 설정")]
    [SerializeField] private Sprite startSprite;
    [SerializeField] private GameObject imageRoot;
    [SerializeField] private Animator imageAnim;
    [SerializeField] private Image displayImage;

    [Tooltip("첫 상호작용 이후 상호작용 시 띄울 다이얼로그")]
    [SerializeField] private DialogData dialogAfterFirstInteraction;

    [SerializeField] private float delayTime = 1f;

    private bool _isWatching = false;
    private bool _hasInteracted = false;
    
    public void OnInteractPressed()
    {
        if (displayImage == null) return;
        if (_isWatching) return;

        if (!_hasInteracted)
        {
            _hasInteracted = true;
            if (ControlLockManager.Instance != null)
            {
                ControlLockManager.Instance.AddInputLock(this);
                ControlLockManager.Instance.AddMovementLock(this);
            }

            StartCoroutine(InteractionRoutine());
        }
        else
        {
            if (DialogSystem.Instance != null)
                DialogSystem.Instance.PlayDialog(dialogAfterFirstInteraction);
        }
    }

    private IEnumerator InteractionRoutine()
    {
        _isWatching = true;
        imageRoot.SetActive(true);

        if (imageAnim != null) imageAnim.enabled = false;

        displayImage.sprite = startSprite;

        yield return new WaitForSeconds(delayTime);

        if (imageAnim != null)
        {
            // 애니메이션 이벤트는 Animator와 같은 오브젝트의 컴포넌트로 전달됩니다.
            if (imageAnim.GetComponent<MirrorHandSoundEmitter>() == null)
                imageAnim.gameObject.AddComponent<MirrorHandSoundEmitter>();

            if (imageAnim != null) imageAnim.enabled = true;
            imageAnim?.SetTrigger("Mirror");
        }
        yield return new WaitForSeconds(3f);

        StartCoroutine(EndCutsceneRoutine());
    }

    private IEnumerator EndCutsceneRoutine()
    {
        if (UIManager.Instance != null)
        {
            _isFadedOut = true;
            yield return UIManager.Instance.FadeOut(0.5f);
        }

        imageAnim?.SetTrigger("Reset");
        imageRoot.SetActive(false);
        _isWatching = false;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.InitMap(EMapType.School_WomanToilet, "school_woman_toilet_1");
        }

        yield return new WaitForSeconds(delayTime);

        if (UIManager.Instance != null) 
            yield return UIManager.Instance.FadeIn(0.5f);
        _isFadedOut = false;

        if (ControlLockManager.Instance != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }

    private bool _isFadedOut;

    private void OnDisable()
    {
        // 암전 중 오브젝트가 꺼져 코루틴이 끊겨도 화면이 검게 남지 않게 합니다.
        if (_isFadedOut)
        {
            _isFadedOut = false;
            UIManager.Instance?.EnsureFadedIn();
        }
    }
}
