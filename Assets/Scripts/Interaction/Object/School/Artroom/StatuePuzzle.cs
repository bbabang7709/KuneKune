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
            // InventoryManager.Instance.AddItem(rewardItem);
            DialogSystem.Instance.PlayDialog(solveDialog, () => GetReward());
        }

        // TODO: 퍼즐 해결 연출
    }

    private void GetReward()
    {
        if (DialogSystem.Instance != null)
        {
            DialogSystem.Instance.PlayIllustrationDialog(
                rewardItem.Illustration, 
                rewardDialog, 
                () => InventoryManager.Instance.AddItem(rewardItem)
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
}
