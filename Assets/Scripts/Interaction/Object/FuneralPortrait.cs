using UnityEngine;

public class FuneralPortrait : MonoBehaviour, IInteractable
{
    [Header("Sprite After Interaction")]
    [SerializeField] private Sprite spriteAfterInteract;

    [Header("Prompt Settings")]
    [SerializeField] private Transform promptPoint;
    public Transform PromptPoint => promptPoint;

    public bool HasSpriteChanged { get; set; }
    
    private SpriteRenderer sr;
    private Sprite originalSprite;
    

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr != null) originalSprite = sr.sprite;
    }

    public void OnInteractPressed()
    {   
        // TODO: 텍스트 연출 구현 시 이미지 띄우는 연출 추가
        
        if (!HasSpriteChanged)
        {
            HasSpriteChanged = true;
            sr.sprite = spriteAfterInteract;    
        }
    }

    public void OnInteractHeld() {}
    public void OnInteractReleased() {}

    public void RecoverSprite()
    {
        sr.sprite = originalSprite;
    }
}
