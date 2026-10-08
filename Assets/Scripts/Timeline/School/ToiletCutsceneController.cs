using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DialogTimelineController))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToiletCutsceneController : CutsceneControllerBase
{
    private Collider2D _collider;

    protected override void Awake()
    {
        base.Awake();
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _collider.excludeLayers = ~LayerMask.GetMask("Player");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            StartCoroutine(WaitAndPlayCutscene());
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
