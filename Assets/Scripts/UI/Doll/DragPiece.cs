using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DragPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string pieceID; 
    public bool isLocked = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 startPosition;
    private Canvas parentCanvas;

    private void Awake() 
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData) 
    {
        if (isLocked) return;
        startPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) 
    {
        if (isLocked) return;
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) 
    {
        if (isLocked) return;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // 정답 슬롯에 안 들어갔다면(isLocked가 false라면) 원래 자리로 튕겨 돌아감
        if (!isLocked) 
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }
}