using System.Collections;
using UnityEngine;

public class CleaningCutsceneController : CutsceneControllerBase
{
    [SerializeField] private Collider2D brokenFloorCollider;
    [SerializeField] private Collider2D triggerCollider;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(WaitAndPlayCutscene());
        }
    }
    protected override void OnCutsceneEndSetup()
    {
        if (brokenFloorCollider != null) brokenFloorCollider.enabled = true;
        if (triggerCollider != null) triggerCollider.enabled = true;
    }

    private IEnumerator WaitAndPlayCutscene()
    {
        if (SaveManager.Instance != null)
        {
            yield return new WaitUntil(() => SaveManager.Instance.IsLoading == false);
        }

        PlayCutscene();
    }
}
