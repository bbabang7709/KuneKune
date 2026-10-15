using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모니터 비율과 관계없이 항상 16:9 구도를 유지한다.
/// 카메라 뷰포트를 16:9로 맞추고 남는 영역은 검은 바로 채우며,
/// Overlay Canvas의 자식은 같은 16:9 프레임 안으로 옮겨 UI도 같은 위치에 놓이게 한다.
/// (Glitch Canvas는 Main Camera를 따르므로 카메라 뷰포트만 맞추면 함께 맞춰진다.)
/// </summary>
public class ScreenAspectFitter : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private const string FrameName = "Aspect Frame";
    private const float ScanInterval = 0.5f;

    private static ScreenAspectFitter _instance;

    private Camera _backgroundCamera;
    private int _screenWidth;
    private int _screenHeight;
    private float _nextScanTime;

    private Camera[] _cameraBuffer = new Camera[8];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (_instance != null) return;

        var go = new GameObject("Screen Aspect Fitter");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ScreenAspectFitter>();
    }

    private void Awake()
    {
        CreateBackgroundCamera();
    }

    private void LateUpdate()
    {
        bool sizeChanged = _screenWidth != Screen.width || _screenHeight != Screen.height;
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;

        Rect frame = CalculateFrame();

        ApplyCameraRects(frame);

        if (sizeChanged || Time.unscaledTime >= _nextScanTime)
        {
            _nextScanTime = Time.unscaledTime + ScanInterval;
            ApplyCanvases(frame);
        }
    }

    /// <summary>
    /// 화면 안에서 16:9가 차지하는 영역을 0~1 정규화 좌표로 계산한다.
    /// </summary>
    private static Rect CalculateFrame()
    {
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (screenAspect > TargetAspect)
        {
            float width = TargetAspect / screenAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        float height = screenAspect / TargetAspect;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }

    /// <summary>
    /// 카메라가 그리지 않는 바깥 영역이 검게 보이도록 가장 뒤에서 화면 전체를 검은색으로 지운다.
    /// </summary>
    private void CreateBackgroundCamera()
    {
        var go = new GameObject("Letterbox Background Camera");
        go.transform.SetParent(transform, false);

        _backgroundCamera = go.AddComponent<Camera>();
        _backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        _backgroundCamera.backgroundColor = Color.black;
        _backgroundCamera.cullingMask = 0;
        _backgroundCamera.depth = -100f;
        _backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
        _backgroundCamera.useOcclusionCulling = false;
        _backgroundCamera.allowHDR = false;
        _backgroundCamera.allowMSAA = false;
    }

    private void ApplyCameraRects(Rect frame)
    {
        int count = Camera.allCamerasCount;
        if (_cameraBuffer.Length < count) _cameraBuffer = new Camera[count];

        count = Camera.GetAllCameras(_cameraBuffer);

        for (int i = 0; i < count; i++)
        {
            Camera cam = _cameraBuffer[i];
            if (cam == _backgroundCamera || cam.targetTexture != null) continue;
            if (cam.rect == frame) continue;

            cam.rect = frame;
        }
    }

    private void ApplyCanvases(Rect frame)
    {
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        // 16:9 기준 화면이 항상 통째로 들어오도록, 가로가 더 넓으면 높이에 맞추고 더 좁으면 너비에 맞춘다.
        float match = screenAspect >= TargetAspect ? 1f : 0f;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isRootCanvas) continue;

            if (!canvas.TryGetComponent(out CanvasScaler scaler)) continue;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            if (!IsSixteenByNine(scaler.referenceResolution)) continue;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = match;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                FitOverlayCanvas(canvas, frame);
            }
        }
    }

    private static bool IsSixteenByNine(Vector2 resolution)
    {
        if (resolution.y <= 0f) return false;
        return Mathf.Abs(resolution.x / resolution.y - TargetAspect) < 0.01f;
    }

    /// <summary>
    /// Overlay Canvas의 기존 자식을 16:9 프레임 안으로 옮기고, 프레임 크기를 현재 화면에 맞춘다.
    /// </summary>
    private static void FitOverlayCanvas(Canvas canvas, Rect frame)
    {
        Transform canvasTransform = canvas.transform;
        RectTransform frameRect = canvasTransform.Find(FrameName) as RectTransform;

        if (frameRect == null)
        {
            var go = new GameObject(FrameName, typeof(RectTransform));
            go.layer = canvas.gameObject.layer;

            frameRect = (RectTransform)go.transform;
            frameRect.SetParent(canvasTransform, false);
            frameRect.localScale = Vector3.one;

            // 이동 중에도 기존 순서가 유지되도록 앞에서부터 차례로 옮긴다.
            var children = new List<Transform>(canvasTransform.childCount);
            for (int i = 0; i < canvasTransform.childCount; i++)
            {
                Transform child = canvasTransform.GetChild(i);
                if (child != frameRect) children.Add(child);
            }

            foreach (Transform child in children)
            {
                child.SetParent(frameRect, false);
            }
        }

        frameRect.anchorMin = frame.min;
        frameRect.anchorMax = frame.max;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
    }
}
