using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image 컴포넌트를 제어하기 위해 추가

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Tooltip("이 자리에 들어와야 할 조각의 ID (예: Head)")]
    public string requiredPieceID;

    [Tooltip("조각이 맞춰졌을 때 나타날 '봉합이 완료된' 부위의 이미지")]
    public Image completedImage;

    private void Awake()
    {
        // 게임 시작 시, 인스펙터에서 깜빡하고 투명도를 안 뺐을 경우를 대비해 확실하게 투명(Alpha 0) 처리
        if (completedImage != null)
        {
            Color c = completedImage.color;
            c.a = 0f;
            completedImage.color = c;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DragPiece piece = eventData.pointerDrag.GetComponent<DragPiece>();
            
            // 끌고 온 조각의 ID가 일치하고 아직 처리되지 않은 조각이라면
            if (piece != null && piece.pieceID == requiredPieceID && !piece.isLocked)
            {
                // 1. 상태 잠금 (중복 실행 방지)
                piece.isLocked = true;

                // 2. 마우스로 드래그하던 파편 조각은 화면에서 즉시 비활성화 (삭제)
                piece.gameObject.SetActive(false); 

                // 3. 슬롯에 미리 대기시켜둔 '완성된 이미지'의 알파(투명도)를 1로 켜기
                if (completedImage != null)
                {
                    Color c = completedImage.color;
                    c.a = 1f;
                    completedImage.color = c;
                }

                // (선택) 꿰매는 소리 재생
                // SoundManager.Instance?.PlaySFX("Sewing_Success");

                // 4. 매니저에 조각이 맞춰졌음을 알림
                DollPuzzleManager.Instance.OnPieceMatched();
            }
        }
    }
}