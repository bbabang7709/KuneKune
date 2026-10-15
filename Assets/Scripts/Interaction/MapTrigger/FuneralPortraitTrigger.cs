using UnityEngine;

public class FuneralPortraitTrigger : MonoBehaviour
{
    private FuneralPortrait portrait;

    void Awake()
    {
        portrait = GetComponentInParent<FuneralPortrait>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {        
        if (collision.CompareTag("Player"))
        {
            if (portrait.HasSpriteChanged)
            {
                portrait?.RecoverSprite();
                gameObject.SetActive(false);
            }
        }
    }
}
