using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("X축 따라가기: 1 = 카메라와 1:1로 이동, 0 = 이동 안 함")]
    [SerializeField] private float parallaxEffectX = 1f;
    [SerializeField] private float parallaxEffectY = 1f;

    [Header("Cloud / Infinite Settings")]
    [Tooltip("구름이 스스로 흘러가는 속도")]
    [SerializeField] private float autoScrollSpeedX = 0f;
    [Tooltip("체크 해제 시 양옆으로 복제하지 않습니다. (해/달)")]
    [SerializeField] private bool enableInfiniteClones = true;

    private Transform cam;
    private float startPosX;
    private float startPosY;
    private float spriteLength;
    private float currentAutoScroll;
    private SpriteRenderer mySr;

    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;

        startPosX = transform.position.x;
        startPosY = transform.position.y;

        mySr = GetComponent<SpriteRenderer>();
        if (mySr.sprite != null) spriteLength = mySr.sprite.bounds.size.x;

        if (enableInfiniteClones)
        {
            CreateClone(1);
            CreateClone(-1);
        }
    }

    private void CreateClone(int direction)
    {
        GameObject clone = new GameObject(gameObject.name + "_Clone");
        clone.transform.SetParent(transform);

        float localOffsetX = spriteLength * direction / transform.localScale.x;
        clone.transform.localPosition = new Vector3(localOffsetX, 0, 0);
        clone.transform.localScale = Vector3.one;

        SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
        cloneSr.sprite = mySr.sprite;
        cloneSr.color = mySr.color;
        cloneSr.sortingLayerID = mySr.sortingLayerID;
        cloneSr.sortingLayerName = mySr.sortingLayerName;
        cloneSr.sortingOrder = mySr.sortingOrder;
    }

    /// <summary>
    /// 스프라이트나 스케일이 바뀐 뒤 타일 길이와 복제본 위치를 현재 크기에 맞춥니다.
    /// </summary>
    public void RefreshLayout()
    {
        if (mySr == null) mySr = GetComponent<SpriteRenderer>();
        if (mySr == null || mySr.sprite == null) return;

        float localWidth = mySr.sprite.bounds.size.x;
        spriteLength = localWidth * Mathf.Abs(transform.lossyScale.x);

        foreach (Transform child in transform)
        {
            if (!child.name.Contains("_Clone")) continue;

            // 복제본은 부모 스케일을 그대로 물려받으므로 로컬 폭만큼 떨어뜨리면 이음새가 맞습니다.
            float direction = Mathf.Sign(child.localPosition.x == 0f ? 1f : child.localPosition.x);
            child.localPosition = new Vector3(localWidth * direction, 0f, 0f);
        }
    }

    public void ChangeSprite(Sprite newSprite)
    {
        if (mySr == null) mySr = GetComponent<SpriteRenderer>();
        mySr.sprite = newSprite;

        if (enableInfiniteClones)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("_Clone"))
                {
                    SpriteRenderer cloneSr = child.GetComponent<SpriteRenderer>();
                    if (cloneSr != null) cloneSr.sprite = newSprite;
                }
            }
        }

        RefreshLayout();
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        currentAutoScroll += autoScrollSpeedX * Time.deltaTime;

        float tempX = cam.position.x * (1 - parallaxEffectX);
        float distX = (cam.position.x * parallaxEffectX) + currentAutoScroll;
        float distY = (cam.position.y - startPosY) * parallaxEffectY;

        transform.position = new Vector3(startPosX + distX, startPosY + distY, transform.position.z);
        
        if (enableInfiniteClones)
        {
            if (tempX > startPosX + spriteLength)
                startPosX += spriteLength;
            else if (tempX < startPosX - spriteLength)
                startPosX -= spriteLength;
        }
    }
}
