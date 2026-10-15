using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChapterBackgroundData
{
    public EChapter chapter; 
    
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private float _fittedOrthoSize;
    private float _fittedAspect;
    private Sprite _fittedSprite;

    private void LateUpdate()
    {
        // 씬 재로드나 카메라 전환 직후에는 카메라 크기가 바뀔 수 있으므로 하늘을 계속 화면에 맞춥니다.
        if (skyRenderer == null || skyRenderer.sprite == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (Mathf.Approximately(_fittedOrthoSize, cam.orthographicSize) &&
            Mathf.Approximately(_fittedAspect, cam.aspect) &&
            _fittedSprite == skyRenderer.sprite)
            return;

        ScaleSkyToFitCamera();
    }

    public void SetBackgroundForChapter(EChapter currentChapter) 
    {
        var bgData = backgroundDatabase.Find(x => x.chapter == currentChapter);
        if (bgData == null) return;

        ApplyBackground(bgData.skySprite, bgData.sunMoonSprite, bgData.cloudSprites,
            bgData.globalLightColor, bgData.globalLightIntensity, 2f);
    }

    public void SetMorningBackground(Sprite morningSky, Sprite[] brightClouds)
    {
        // 밤의 달과 조명을 남기지 않고 새벽 장면으로 전환합니다.
        ApplyBackground(morningSky, null, brightClouds, Color.white, 1f, 0f, true);
    }

    private void ApplyBackground(Sprite sky, Sprite sunMoon, Sprite[] clouds,
        Color lightColor, float lightIntensity, float lightFadeDuration, bool keepCloudsInCameraView = false)
    {
        // 1. 단일 이미지 교체 (하늘, 달)
        if (skyRenderer != null) 
        {
            InfiniteParallax skyParallax = skyRenderer.GetComponent<InfiniteParallax>();
            if (skyParallax != null)
                skyParallax.ChangeSprite(sky);
            else
                skyRenderer.sprite = sky;
            ScaleSkyToFitCamera(); // 💡 스프라이트 교체 직후 카메라 화면에 맞게 스케일 조정
        }
        
        if (sunMoonRenderer != null) sunMoonRenderer.sprite = sunMoon;

        // 2. 동적 구름 시스템에 배열 전달 (알아서 랜덤 생성/리스폰 시작)
        if (cloudSystem != null) cloudSystem.SetCloudSprites(clouds, keepCloudsInCameraView);

        // 3. 글로벌 라이트 변경
        if (LightingEventManager.Instance != null)
        {
            LightingEventManager.Instance.FadeGlobalLight(lightColor, lightIntensity, lightFadeDuration);
        }
    }

    /// <summary>
    /// 현재 카메라의 화면 비율과 크기에 맞춰 하늘 스프라이트의 크기를 꽉 차게 늘립니다.
    /// </summary>
    private void ScaleSkyToFitCamera()
    {
        if (skyRenderer == null || skyRenderer.sprite == null || Camera.main == null) return;

        Camera cam = Camera.main;
        
        // 카메라의 세로/가로 월드 길이 계산 (Orthographic 카메라 기준)
        float cameraHeight = cam.orthographicSize * 2f;
        float cameraWidth = cameraHeight * cam.aspect;

        // 현재 들어간 스프라이트 원본의 월드 길이 계산
        Vector2 spriteSize = skyRenderer.sprite.bounds.size;

        // 카메라 크기 대비 스프라이트 크기 비율로 스케일 적용
        float scaleX = cameraWidth / spriteSize.x;
        float scaleY = cameraHeight / spriteSize.y;

        // 화면 밖으로 살짝 튀어나가게 여백을 주고 싶다면 (예: 카메라 흔들림 방지) scaleX, Y에 1.05f 정도를 곱해주면 됩니다.
        skyRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        _fittedOrthoSize = cam.orthographicSize;
        _fittedAspect = cam.aspect;
        _fittedSprite = skyRenderer.sprite;

        skyRenderer.GetComponent<InfiniteParallax>()?.RefreshLayout();
    }
}
