using UnityEngine;

[DefaultExecutionOrder(1000)]
public class DynamicCloudSystem : MonoBehaviour
{
    [Header("Cloud Settings")]
    [Tooltip("화면에 동시에 떠 있을 구름의 최대 개수")]
    [SerializeField] private int maxClouds = 5;
    
    [Tooltip("바람의 속도 (음수면 왼쪽으로, 양수면 오른쪽으로 흘러감)")]
    [SerializeField] private float windSpeed = -0.5f;
    
    [Tooltip("카메라 가로 시야 크기 (이 범위를 벗어나면 반대편에서 리스폰)")]
    [SerializeField] private float screenWidth = 25f;
    
    [Tooltip("구름이 생성될 카메라 기준 Y축(높이) 랜덤 범위")]
    [SerializeField] private Vector2 heightRange = new Vector2(1f, 5f);

    private Transform _cam;
    private Camera _camera;
    private bool _keepInCameraView;
    private Sprite[] _currentSprites;
    private float _lastCamX;

    // 개별 구름의 데이터를 관리하는 클래스
    private class Cloud
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float parallaxFactor; // 멀리 있는 구름일수록 카메라를 더 잘 따라감(느림)
        public float speedOffset;    // 각 구름의 미세한 속도 차이
        public Vector2 viewportPosition;
    }
    
    private Cloud[] _clouds;

    private void Awake()
    {
        _camera = Camera.main;
        if (_camera != null) _cam = _camera.transform;
        InitializeClouds();
    }

    private void InitializeClouds()
    {
        _clouds = new Cloud[maxClouds];
        for (int i = 0; i < maxClouds; i++)
        {
            GameObject go = new GameObject($"Cloud_{i}");
            go.transform.SetParent(transform);
            
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Background"; // 환경에 맞게 레이어 수정
            sr.sortingOrder = -90 + i;          // 겹침 방지
            
            _clouds[i] = new Cloud { obj = go, sr = sr };
            go.SetActive(false);
        }
    }

    /// <summary>
    /// BackgroundManager에서 챕터별 구름 스프라이트 배열을 넘겨줄 때 호출됩니다.
    /// </summary>
    public void SetCloudSprites(Sprite[] sprites, bool keepInCameraView = false)
    {
        _currentSprites = sprites;
        _keepInCameraView = keepInCameraView;
        if (sprites == null || sprites.Length == 0)
        {
            foreach (var c in _clouds) c.obj.SetActive(false);
            return;
        }

        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null) return;
        _cam = _camera.transform;

        if (_cam != null) _lastCamX = _cam.position.x;

        // 구름 이미지가 들어오면 화면 안의 랜덤한 위치에 흩뿌려줍니다.
        foreach (var c in _clouds)
        {
            c.obj.SetActive(true);
            float randomStartX = _cam.position.x + Random.Range(-screenWidth / 2f, screenWidth / 2f);
            ResetCloud(c, randomStartX);
        }
    }

    /// <summary>
    /// 구름의 위치, 스프라이트, 속도를 랜덤하게 재설정합니다.
    /// </summary>
    private void ResetCloud(Cloud cloud, float spawnX)
    {
        // 1. 등록된 2~3개의 스프라이트 중 하나를 랜덤으로 선택
        cloud.sr.sprite = _currentSprites[Random.Range(0, _currentSprites.Length)];
        
        // 2. 투명도도 살짝 랜덤을 주면 훨씬 입체적입니다.
        cloud.sr.color = new Color(1f, 1f, 1f, Random.Range(0.7f, 1.0f));

        // 3. 개별 이동 속도와 패럴랙스(원근감) 랜덤 설정
        cloud.parallaxFactor = Random.Range(0.85f, 0.98f); 
        cloud.speedOffset = Random.Range(0.8f, 1.2f); // 기준 바람 속도의 80~120%

        if (_keepInCameraView)
        {
            // 실제 화면 크기와 구름 크기를 기준으로 상단 시야 안에 배치합니다.
            Vector2 halfSize = GetCloudViewportHalfSize(cloud);
            float marginX = Mathf.Min(0.5f, halfSize.x + 0.03f);
            float top = Mathf.Max(0.5f, 1f - halfSize.y - 0.03f);
            cloud.viewportPosition = new Vector2(
                Random.Range(marginX, 1f - marginX),
                Random.Range(Mathf.Min(0.65f, top), Mathf.Min(0.85f, top)));
            ApplyViewportPosition(cloud);
            return;
        }

        // 4. 지정된 X 위치와 랜덤한 Y 높이로 스폰
        float randY = Random.Range(heightRange.x, heightRange.y);
        cloud.obj.transform.position = new Vector3(spawnX, _cam.position.y + randY, 0f);
    }

    private void LateUpdate()
    {
        if (_cam == null || _currentSprites == null || _currentSprites.Length == 0) return;

        if (_keepInCameraView)
        {
            UpdateViewportClouds();
            return;
        }

        float camDeltaX = _cam.position.x - _lastCamX;
        _lastCamX = _cam.position.x;

        float halfScreen = screenWidth / 2f;

        foreach (var cloud in _clouds)
        {
            if (!cloud.obj.activeSelf) continue;

            // 바람에 의한 이동 + 카메라 이동에 따른 패럴랙스(원근감) 동시 적용
            float moveX = (windSpeed * cloud.speedOffset * Time.deltaTime) + (camDeltaX * cloud.parallaxFactor);
            cloud.obj.transform.position += new Vector3(moveX, 0f, 0f);

            // 화면 왼쪽 밖으로 나갔다면 -> 화면 오른쪽 끝에서 새로운 모양으로 스폰
            if (cloud.obj.transform.position.x < _cam.position.x - halfScreen)
            {
                ResetCloud(cloud, _cam.position.x + halfScreen);
            }
            // 화면 오른쪽 밖으로 나갔다면 -> 화면 왼쪽 끝에서 새로운 모양으로 스폰
            else if (cloud.obj.transform.position.x > _cam.position.x + halfScreen)
            {
                ResetCloud(cloud, _cam.position.x - halfScreen);
            }
        }
    }

    private Vector2 GetCloudViewportHalfSize(Cloud cloud)
    {
        float height = _camera.orthographicSize * 2f;
        Vector3 extents = cloud.sr.bounds.extents;
        return new Vector2(extents.x / (height * _camera.aspect), extents.y / height);
    }

    private void ApplyViewportPosition(Cloud cloud)
    {
        // 화면 좌표를 매 프레임 환산하므로 맵 이동과 카메라의 X/Y 이동을 함께 따라갑니다.
        float depth = -_cam.position.z;
        cloud.obj.transform.position = _camera.ViewportToWorldPoint(
            new Vector3(cloud.viewportPosition.x, cloud.viewportPosition.y, depth));
    }

    private void UpdateViewportClouds()
    {
        float width = _camera.orthographicSize * 2f * _camera.aspect;
        foreach (Cloud cloud in _clouds)
        {
            if (!cloud.obj.activeSelf) continue;

            cloud.viewportPosition.x += windSpeed * cloud.speedOffset * Time.deltaTime / width;
            float halfWidth = GetCloudViewportHalfSize(cloud).x;
            if (cloud.viewportPosition.x < -halfWidth || cloud.viewportPosition.x > 1f + halfWidth)
            {
                bool passedLeft = cloud.viewportPosition.x < 0f;
                ResetCloud(cloud, _cam.position.x);
                halfWidth = GetCloudViewportHalfSize(cloud).x;
                cloud.viewportPosition.x = passedLeft ? 1f + halfWidth : -halfWidth;
            }

            ApplyViewportPosition(cloud);
        }
    }
}
