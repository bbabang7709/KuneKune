using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChapterBackgroundData
{
    public EChapter chapter; //[cite: 1]
    
    [Header("Static Backgrounds")]
    public Sprite skySprite;
    public Sprite sunMoonSprite;
    
    [Header("Dynamic Clouds")]
    [Tooltip("이 챕터에 사용될 2~3개의 구름 이미지를 등록하세요")]
    public Sprite[] cloudSprites; 

    [Header("Lighting")]
    public Color globalLightColor = Color.white;
    public float globalLightIntensity = 1f;
}

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("카메라를 따라다니는 단일 하늘 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer skyRenderer;
    [Tooltip("카메라를 따라다니는 단일 해/달 스프라이트 렌더러")]
    [SerializeField] private SpriteRenderer sunMoonRenderer;
    [Tooltip("방금 만든 동적 구름 시스템 오브젝트")]
    [SerializeField] private DynamicCloudSystem cloudSystem;

    [Header("Database")]
    [SerializeField] private List<ChapterBackgroundData> backgroundDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetBackgroundForChapter(EChapter currentChapter) //[cite: 1]
    {
        var bgData = backgroundDatabase.Find(x => x.chapter == currentChapter);
        if (bgData == null) return;

        // 1. 단일 이미지 교체 (하늘, 달)
        if (skyRenderer != null) skyRenderer.sprite = bgData.skySprite;
        if (sunMoonRenderer != null) sunMoonRenderer.sprite = bgData.sunMoonSprite;

        // 2. 동적 구름 시스템에 배열 전달 (알아서 랜덤 생성/리스폰 시작)
        if (cloudSystem != null) cloudSystem.SetCloudSprites(bgData.cloudSprites);

        // 3. 글로벌 라이트 변경
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.FadeGlobalLight(bgData.globalLightColor, bgData.globalLightIntensity, 2f); 
        }
    }
}