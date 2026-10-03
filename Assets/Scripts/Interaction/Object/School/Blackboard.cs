using System.Collections;
using UnityEngine;

public class Blackboard : InteractableBase
{
    [SerializeField] private DialogData dialog1;
    [SerializeField] private DialogData dialog2;
    [SerializeField] private DialogData acquireDialog;
    [SerializeField] private KeyItemData artroomKey;

    private bool _dialogPlayed = false;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }
    
    public override void OnInteractPressed()
    {
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.PlayDialog(
                dialog1, 
                _dialogPlayed ? null 
                              : () => StartCoroutine(KeyAcquireRoutine()));
        }
    }

    private IEnumerator KeyAcquireRoutine()
    {
        _dialogPlayed = true;
        if (boxCollider != null) boxCollider.enabled = false;

        yield return new WaitForSeconds(0.5f);

        DialogSystem.Instance?.PlayDialog(dialog2);

        yield return new WaitUntil(() => !DialogSystem.Instance.IsDialogPlaying);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AcquireItem(artroomKey, acquireDialog);
        }

        if (boxCollider != null) boxCollider.enabled = true;
    }
}
