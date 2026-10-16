using System.Collections;
using UnityEngine;

public class Blackboard : InteractableBase
{
    [SerializeField] private DialogData dialog1;
    [SerializeField] private DialogData dialog2;
    [SerializeField] private DialogData acquireDialog;
    [SerializeField] private KeyItemData artroomKey;

    [SerializeField] private GameObject[] glowingEyes;

    private bool _dialogPlayed = false;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        foreach (GameObject glowingEye in glowingEyes)
        {
            if (glowingEye != null) glowingEye.SetActive(false);
        }
    }
    
    public override void OnInteractPressed()
    {
        if (DialogSystem.Instance != null)
        {
            // 체크포인트에서 인벤토리를 복원한 뒤에도 열쇠 지급과 초상화 힌트를 반복하지 않습니다.
            bool hasReceivedKey = _dialogPlayed || (artroomKey != null &&
                InventoryManager.Instance != null && InventoryManager.Instance.HasKey(artroomKey.ItemID));
            DialogSystem.Instance.PlayDialog(
                dialog1, 
                hasReceivedKey ? null
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

        foreach (GameObject glowingEye in glowingEyes)
        {
            if (glowingEye != null)
            {
                glowingEye.SetActive(true);
            }
        }

        if (boxCollider != null) boxCollider.enabled = true;
    }
}
