using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class StatuePuzzle : MonoBehaviour
{
    [System.Serializable]
    public struct StatueSetting
    {
        public StatuePuzzlePiece statue;
        public StatuePuzzlePiece.EDirection correctDirection;
    }

    [Header("Puzzle Settings")]
    [Tooltip("퍼즐에 포함된 조각상과 각각의 정답 방향 설정")]
    [SerializeField] private StatueSetting[] statues;
    [SerializeField] private DialogData solveDialog;

    [Header("Reward")]
    [Tooltip("퍼즐 해결 시 지급할 보상 아이템")]
    [SerializeField] private KeyItemData rewardItem;
    [SerializeField] private DialogData rewardDialog;
    
    [Tooltip("발자국 연출")]
    [SerializeField] private float fadeIntensity = 0.05f;
    [SerializeField] private FootprintSequence footprintSequence;
    [Tooltip("1층 교실의 더러운 책상 활성화")]
    [SerializeField] private Collider2D deskCollider;

    private bool _isSolved = false;

    private void Start()
    {
        foreach (var setting in statues)
        {
            if (setting.statue != null)
            {
                setting.statue.OnRotated += CheckPuzzleCompletion;
            }
        }

        if (deskCollider != null) deskCollider.enabled = false;
    }

    private void CheckPuzzleCompletion()
    {
        if (_isSolved) return;

        foreach (var setting in statues)
        {
            if (setting.statue.CurrentDirection != setting.correctDirection)
            {
                return;
            }
        }

        SolvePuzzle();
    }

    private void SolvePuzzle()
    {
        _isSolved = true;

        foreach (var setting in statues)
        {
            setting.statue.enabled = false;

            Collider2D col = setting.statue.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        if (rewardItem != null && InventoryManager.Instance != null)
        {
            Debug.Log("퍼즐 해결!! 키 아이템 획득");
            DialogSystem.Instance.PlayDialog(solveDialog, () => GetReward());
        }
    }

    private void GetReward()
    {
        InventoryManager.Instance.AddItem(rewardItem);

        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.PlayItemDialog(
                rewardItem.Illustration, 
                rewardDialog, 
                () => StartCoroutine(FinishPuzzleRoutine())
            );
        }
    }

    private void OnDestroy()
    {
        foreach (var setting in statues)
        {
            if (setting.statue != null)
            {
                setting.statue.OnRotated -= CheckPuzzleCompletion;
            }
        }
    }

    private IEnumerator FinishPuzzleRoutine()
    {
        yield return new WaitForSeconds(1f);

        yield return LightingEventManager.Instance?.FlickerRoutine();
        yield return new WaitForSeconds(1f);
        LightingEventManager.Instance?.SetBlackout(fadeIntensity, 0.2f);
        yield return new WaitForSeconds(1f);

        footprintSequence.PlayFootprintSequence();
        if (deskCollider != null) deskCollider.enabled = true;
    }
}
