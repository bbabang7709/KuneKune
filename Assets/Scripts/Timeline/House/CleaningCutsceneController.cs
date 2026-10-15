using System.Collections;
using UnityEngine;

public class CleaningCutsceneController : CutsceneControllerBase
{
    [SerializeField] private Collider2D brokenFloorCollider;
    [SerializeField] private Collider2D triggerCollider;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !(playOnlyOnce && hasPlayed))
        {
            // 대기 코루틴이 PlayCutscene을 호출할 때까지의 프레임 틈에 조작이 새지 않게 먼저 잠급니다.
            HoldControls();
            StartCoroutine(WaitAndPlayCutscene());
        }
    }
    protected override void OnCutsceneEndSetup()
    {
        if (brokenFloorCollider != null) brokenFloorCollider.enabled = true;
        if (triggerCollider != null) triggerCollider.enabled = true;
    }

    // 타임라인의 바닥 붕괴 Signal에서 호출합니다.
    public void PlayFloorCollapseSound()
    {
        SoundManager.Instance?.PlaySFX("BrokenFloor");
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
            // 로드가 끝나며 잠금이 모두 지워질 수 있으므로 다시 잠급니다.
            HoldControls();
        }

        PlayCutscene();
    }
}
