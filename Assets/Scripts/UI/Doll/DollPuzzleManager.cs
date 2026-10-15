using UnityEngine;
using UnityEngine.Events;

public class DollPuzzleManager : MonoBehaviour
{
    public static DollPuzzleManager Instance;
    
    [Tooltip("맞춰야 하는 총 조각 수")]
    public int totalPieces = 3;
    private int matchedPieces = 0;
    private bool hasCompleted;

    [Tooltip("인형이 완성되었을 때 실행할 이벤트 (컷씬 재생기 등 연결)")]
    public UnityEvent onPuzzleComplete;

    private void Awake() 
    {
        Instance = this;
    }

    public void OnPieceMatched()
    {
        if (hasCompleted) return;
        matchedPieces++;
        
        SoundManager.Instance?.PlaySFX("handkerchief");

        if (matchedPieces >= totalPieces)
        {
            hasCompleted = true;
            // 퍼즐 완성 시 이벤트 트리거
            onPuzzleComplete?.Invoke();
        }
    }
}
