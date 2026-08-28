using UnityEngine;

public class Shadow : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField] private float Alpah = 0.5f;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        GameObject shadow = new GameObject("Shadow");

        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        float yOffset = sr.sprite.bounds.min.y * 2;
     
        shadow.transform.parent = transform;
        shadow.transform.localPosition = new Vector3(0, yOffset, 0);

        SpriteRenderer shadowSpriteRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowSpriteRenderer.sprite = sr.sprite;
        shadowSpriteRenderer.material = sr.material;
        shadowSpriteRenderer.sortingLayerID = sr.sortingLayerID;
        // 그림자가 원본과 같은 순서에 있으면 SpriteMask의 앞쪽 경계와 겹칠 수 있으므로 한 단계 뒤에 그린다.
        shadowSpriteRenderer.sortingOrder = sr.sortingOrder - 1;
        shadowSpriteRenderer.flipX = sr.flipX;
        shadowSpriteRenderer.flipY = true;
        shadowSpriteRenderer.color = new Color(0, 0, 0, Alpah);

        shadowSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
}
