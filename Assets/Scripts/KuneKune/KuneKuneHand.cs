using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class KuneKuneHand : MonoBehaviour
{
    private const int DoorCount = 5;

    [Header("문과 손 이미지")]
    [Tooltip("손이 가장 적게 나온 이미지부터 가장 많이 나온 이미지 순서입니다.")]
    [SerializeField] private Sprite[] _handSprites;
    [SerializeField] private Sprite _openedDoorSprite;
    [SerializeField] private Sprite _closedDoorSprite;

    [Header("손을 밀어 올리는 난이도")]
    [Tooltip("손이 완전히 내려오는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float _secondsToFullyLower = 4f;
    [Tooltip("한 번 누를 때 밀어 올리는 양입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float _pushPerPress = 0.1f;

    [Header("문을 밀어 올리는 연출")]
    [Tooltip("밀어 올린 뒤 손이 다시 내려오기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float _pushHoldSeconds = 0.16f;
    [Tooltip("문이 흔들린 뒤 제자리로 돌아오는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float _shakeSeconds = 0.18f;
    [Tooltip("문이 좌우로 흔들리는 거리입니다.")]
    [SerializeField, Min(0f)] private float _shakeDistance = 0.045f;
    [Tooltip("누르는 순간 문이 위로 들리는 거리입니다.")]
    [SerializeField, Min(0f)] private float _pushLiftDistance = 0.06f;

    [Header("선택된 문의 전등")]
    [SerializeField, Min(0f)] private float _intensity = 1f;
    [Tooltip("불이 안정적으로 켜져 있는 평균 시간입니다. 가끔 짧게 떨린 뒤 다시 안정됩니다.")]
    [SerializeField, Min(0.01f)] private float _lightOnSeconds = 1f;
    [Tooltip("불이 떨릴 때 어두워지는 시간입니다. 대부분 완전히 꺼지지 않고 희미해집니다.")]
    [SerializeField, Min(0.01f)] private float _lightOffSeconds = 0.08f;

    private float _nextPushSoundTime;
    private float _pushHoldRemaining;   // 손이 다시 내려오기 전까지 남은 대기시간

    private int _shakingDoorIndex = -1;
    private float _shakeTimeRemaining;
    private float _shakeDirection = 1f;

    #region Door
    private List<int> _remainingDoors = new List<int>();
    private SpriteRenderer[] _doorSprites = new SpriteRenderer[DoorCount];
    private Collider2D[] _doorColliders = new Collider2D[DoorCount];
    private Light2D[] _lights = new Light2D[DoorCount];
    private Coroutine _coFlicker = null;
    private bool _isInitialized;
    private Vector3[] _originalPositions = new Vector3[DoorCount];
    private Vector3[] _originalSpriteMin = new Vector3[DoorCount];
    private Vector2[] _originalColliderOffsets = new Vector2[DoorCount];
    #endregion

    private float _handDepth;

    private Collider2D _playerCollider;

    public bool IsPlaying { get; private set; } = false;
    public bool IsCleared { get; private set; } = false;
    public bool IsGameOver { get; private set; } = false;
    public int OpenedDoorCount { get; private set; } = 0;
    public int CurrentDoorIndex { get; private set; } = -1;

    private void Start()
    {
        Init();
        Play();
    }

    private void Init()
    {
        if (transform.childCount < DoorCount)
        {
            Debug.LogError("KuneKuneHand의 자식으로 문 5개를 배치해주세요.", this);
            return;
        }

        for (int i = 0; i < DoorCount; i++)
        {
            Transform door = transform.GetChild(i);
            if (door == null)
            {
                Debug.LogWarning($"문 {i}이 존재하지 않습니다. KuneKuneHand 오브젝트의 자식으로 5개의 문을 배치해야 합니다.", this);
                return;
            }

            if (door.TryGetComponent(out _doorSprites[i]) == false)
                _doorSprites[i] = door.gameObject.AddComponent<SpriteRenderer>();
            if (_doorSprites[i].sprite == null)
                _doorSprites[i].sprite = _closedDoorSprite;
            if (_doorSprites[i].sprite == null)
            {
                Debug.LogError($"문 {i}의 닫힌 문 이미지를 지정해주세요.", this);
                return;
            }
            if (door.TryGetComponent(out _doorColliders[i]) == false)
                _doorColliders[i] = door.gameObject.AddComponent<BoxCollider2D>();

            _lights[i] = door.GetComponentInChildren<Light2D>(true);
            if (_lights[i] == null)
            {
                GameObject lightObj = new GameObject("Light");
                lightObj.transform.SetParent(door, false);
                _lights[i] = lightObj.AddComponent<Light2D>();
                _lights[i].lightType = Light2D.LightType.Point;
            }

            _doorSprites[i].sortingLayerName = "Middleground";
            _doorSprites[i].sortingOrder = 1;
            _doorColliders[i].isTrigger = true;
            _doorColliders[i].excludeLayers = ~LayerMask.GetMask("Player");

            _lights[i].intensity = 0f;  // 처음에는 꺼져 있음
            _lights[i].color = Color.white;
            _lights[i].pointLightInnerRadius = 0f;
            _lights[i].pointLightOuterRadius = 5f;
            _lights[i].falloffIntensity = 0.7f;
            _lights[i].shadowsEnabled = false;
            _lights[i].transform.localPosition = new Vector3(1.15f, 3.51f, 0f);

            _originalPositions[i] = door.localPosition;
            _originalSpriteMin[i] = _doorSprites[i].sprite.bounds.min;
            _originalColliderOffsets[i] = _doorColliders[i].offset;
        }

        if (_playerCollider == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerCollider = player.GetComponent<Collider2D>();
        }

        _isInitialized = true;
    }

    public void Play()
    {
        // 진행 중이거나 이미 끝났다면 다시 시작하지 않습니다.
        if (!_isInitialized || IsPlaying || IsCleared || IsGameOver)
            return;

        _remainingDoors.Clear();
        for (int i = 0; i < DoorCount; i++)
            _remainingDoors.Add(i);

        IsPlaying = true;
        SelectNextDoor();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        // 대화나 컷신으로 조작할 수 없는 동안에는 손도 내려오지 않습니다.
        if ((ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked) ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying) || AudioListener.pause)
            return;

        // 마지막 문을 열었어도 남은 흔들림은 자연스럽게 끝냅니다.
        UpdateDoorShake();

        if (IsPlaying == false)
            return;

        // 잠깐 버티는 시간이 끝난 뒤부터 손이 다시 내려옵니다.
        float lowerTime = Mathf.Max(0f, Time.deltaTime - _pushHoldRemaining);
        _pushHoldRemaining = Mathf.Max(0f, _pushHoldRemaining - Time.deltaTime);
        _handDepth = Mathf.Min(1f, _handDepth + lowerTime / Mathf.Max(0.1f, _secondsToFullyLower));

        // 손이 끝까지 내려왔다
        if (_handDepth >= 1f)
        {
            UpdateSprite();
            IsPlaying = false;
            IsGameOver = true;

            StopFlicker();
            ResetDoorShake();
            StartCoroutine(GameOverSequence());
            return;
        }

        // 누르고 있는 시간과 관계없이, 새로 누른 순간에만 한 번 밀어 올립니다.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && IsPlayerAtCurrentDoor())
        {
            _handDepth = Mathf.Max(0f, _handDepth - Mathf.Clamp(_pushPerPress, 0.01f, 1f));
            _pushHoldRemaining = _pushHoldSeconds;
            BeginDoorShake();
            PlayPushSound();
        }

        // 손이 모두 들어간 상태
        if (_handDepth <= 0f)
        {
            OpenCurrentDoor();
            return;
        }

        UpdateSprite();
    }

    private bool IsPlayerAtCurrentDoor()
    {
        Collider2D door = _doorColliders[CurrentDoorIndex];
        if (_playerCollider == null || _playerCollider.isActiveAndEnabled == false || door.isActiveAndEnabled == false)
            return false;

        return door.bounds.Intersects(_playerCollider.bounds);
    }

    private void SelectNextDoor()
    {
        int randomIndex = Random.Range(0, _remainingDoors.Count);
        CurrentDoorIndex = _remainingDoors[randomIndex];
        _remainingDoors.RemoveAt(randomIndex);
        
        _handDepth = 0.1f;  // 처음 손 위치
        _pushHoldRemaining = 0f;
        UpdateSprite();
        StartFlicker();
    }

    private void UpdateSprite()
    {
        // 손이 내려온 정도에 따라 적절한 이미지를 선택합니다. (0 ~ 6 스프라이트)
        // 마지막 이미지는 실제로 끝까지 내려와 게임 오버가 되는 순간에 표시합니다.
        int frame = Mathf.FloorToInt(Mathf.Clamp01(_handDepth) * (_handSprites.Length - 1));
        SetSprite(CurrentDoorIndex, _handSprites[frame]);
    }

    private void OpenCurrentDoor()
    {
        SoundManager.Instance?.PlaySFX("StallDoor_Open");

        StopFlicker();
        SetSprite(CurrentDoorIndex, _openedDoorSprite);
        OpenedDoorCount++;

        if (_remainingDoors.Count > 0)
        {
            SelectNextDoor();
            return;
        }

        // 모든 문을 열었다
        IsPlaying = false;
        IsCleared = true;
        CurrentDoorIndex = -1;

        // TODO: 문을 모두 열었을 때의 이벤트 처리 => 인형의 오른팔과 교실 열쇠 획득
    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("손이 끝까지 내려와 게임 오버되었습니다.", this);
        //ControlLockManager.Instance?.AddInputLock(this);
        //ControlLockManager.Instance?.AddMovementLock(this);

        // 무서워서 꺼버렸다. 나중에 알아서 켜주세요.
        //SoundManager.Instance?.SetBGMVolume(0f);
        //SoundManager.Instance?.PlaySFX("KuneKune_Jumpscare");

        //if (UIManager.Instance != null)
        //    yield return UIManager.Instance.ShowJumpscareRoutine(0.05f);

        //yield return new WaitForSecondsRealtime(1.5f);

        //if (UIManager.Instance != null)
        //    yield return UIManager.Instance.FadeOut(0.2f);

        yield return new WaitForSecondsRealtime(1.5f);

        SaveManager.Instance?.Load(true);
    }

    private void OnEnable()
    {
        if (_isInitialized && IsPlaying)
            StartFlicker();
    }

    private void OnDisable()
    {
        StopFlicker();
        ResetDoorShake();
        StopAllCoroutines();
        ControlLockManager.Instance?.RemoveInputLock(this);
        ControlLockManager.Instance?.RemoveMovementLock(this);
    }

    private void PlayPushSound()
    {
        if (SoundManager.Instance == null || Time.time < _nextPushSoundTime)
            return;

        // 충격 뒤에 긁는 소리가 이어집니다. 너무 빠른 입력으로 소리가 쌓이는 것도 막습니다.
        SoundManager.Instance.PlaySFX("DoorPush");
        _nextPushSoundTime = Time.time + 0.08f;
    }

    private void SetSprite(int index, Sprite sprite)
    {
        SpriteRenderer door = _doorSprites[index];
        door.sprite = sprite;
        ApplyDoorPose(index);
    }

    private void BeginDoorShake()
    {
        ResetDoorShake();
        _shakingDoorIndex = CurrentDoorIndex;
        _shakeTimeRemaining = _shakeSeconds;
        _shakeDirection = -_shakeDirection;
        ApplyDoorPose(_shakingDoorIndex);
    }

    private void UpdateDoorShake()
    {
        if (_shakingDoorIndex < 0)
            return;

        _shakeTimeRemaining = Mathf.Max(0f, _shakeTimeRemaining - Time.deltaTime);
        if (_shakeTimeRemaining <= 0f)
            ResetDoorShake();
        else
            ApplyDoorPose(_shakingDoorIndex);
    }

    private void ResetDoorShake()
    {
        int previousDoor = _shakingDoorIndex;
        _shakingDoorIndex = -1;
        _shakeTimeRemaining = 0f;
        if (previousDoor >= 0 && _doorSprites[previousDoor] != null)
            ApplyDoorPose(previousDoor);
    }

    private void ApplyDoorPose(int index)
    {
        SpriteRenderer door = _doorSprites[index];
        Vector3 shakeOffset = Vector3.zero;
        if (index == _shakingDoorIndex)
        {
            float progress = 1f - _shakeTimeRemaining / Mathf.Max(0.01f, _shakeSeconds);
            float strength = (1f - progress) * (1f - progress);

            // 좌우로 흔들리면서, 누르는 순간 위로 들리는 효과를 줍니다.
            shakeOffset.x = Mathf.Cos(progress * Mathf.PI * 6f) * _shakeDistance * strength * _shakeDirection;  // PI * 6 => 3번 왕복
            shakeOffset.y = _pushLiftDistance * strength;
        }

        // 이미지마다 기준점이 달라도 문의 왼쪽 아래 위치는 유지합니다.
        Vector3 spriteOffset = _originalSpriteMin[index] - door.sprite.bounds.min;
        Vector3 visualOffset = spriteOffset + shakeOffset;
        Vector3 offset = Vector3.Scale(visualOffset, door.transform.localScale);
        door.transform.localPosition = _originalPositions[index] + door.transform.localRotation * offset;
        
        // 그림만 흔들고, 플레이어가 문 앞에 있는지 확인하는 범위는 고정합니다.
        _doorColliders[index].offset = _originalColliderOffsets[index] - (Vector2)visualOffset;
    }

    private void StartFlicker()
    {
        StopFlicker();
        if (!isActiveAndEnabled || !IsPlaying || CurrentDoorIndex < 0)
            return;

        Light2D light = _lights[CurrentDoorIndex];
        if (light == null)
            return;

        light.enabled = true;
        _coFlicker = StartCoroutine(CoFlicker(CurrentDoorIndex));
    }

    private void StopFlicker()
    {
        if (_coFlicker != null)
        {
            StopCoroutine(_coFlicker);
            _coFlicker = null;
        }

        foreach (Light2D light in _lights)
        {
            if (light != null)
                light.enabled = false;
        }
    }

    private IEnumerator CoFlicker(int index)
    {
        Light2D light = _lights[index];
        float noiseOffset = Random.Range(0f, 100f);
        float noiseTime = 0f;
        bool justTurnedOn = true;

        while (IsPlaying && CurrentDoorIndex == index && light != null)
        {
            // 평소에는 거의 일정하게 켜져 있고, 밝기만 아주 조금 흔들립니다.
            // 처음 켜졌을 때는 일찍 한 번 떨리고, 이후에는 긴 간격을 둡니다.
            float steadyTime = justTurnedOn
                ? Random.Range(0.4f, 1f)
                : Mathf.Max(0.5f, _lightOnSeconds) * Random.Range(0.75f, 1.75f);
            justTurnedOn = false;

            float elapsed = 0f;
            while (elapsed < steadyTime && light != null)
            {
                elapsed += Time.deltaTime;
                noiseTime += Time.deltaTime;
            
                float noise = Mathf.PerlinNoise(noiseOffset, noiseTime * 1.7f);
                light.intensity = _intensity * Mathf.Lerp(0.94f, 1f, noise);
                yield return null;
            }

            if (light == null)
                break;

            // 접촉이 불안정한 듯 한두 번 또는 세 번 연달아 떨립니다.
            int flickerCount = Random.value < 0.3f ? 1 : Random.Range(2, 4);
            for (int i = 0; i < flickerCount && light != null; i++)
            {
                float dimRatio = Random.value < 0.12f ? 0f : Random.Range(0.2f, 0.65f);
                yield return FadeLight(light, _intensity * dimRatio, Random.Range(0.015f, 0.035f));
                yield return new WaitForSeconds(Mathf.Max(0.01f, _lightOffSeconds) * Random.Range(0.4f, 1.2f));

                // 매번 똑같은 밝기로 번쩍이지 않고, 힘겹게 밝기를 되찾습니다.
                yield return FadeLight(light, _intensity * Random.Range(0.78f, 0.98f), Random.Range(0.03f, 0.09f));
                if (i < flickerCount - 1)
                    yield return new WaitForSeconds(Random.Range(0.045f, 0.16f));
            }

            yield return FadeLight(light, _intensity * 0.97f, Random.Range(0.15f, 0.3f));
        }

        if (light != null)
            light.enabled = false;
        _coFlicker = null;
    }

    private IEnumerator FadeLight(Light2D light, float intensity, float seconds)
    {
        if (light == null)
            yield break;

        float initial = light.intensity;
        float elapsed = 0f;
        while (elapsed < seconds && light != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
            light.intensity = Mathf.Lerp(initial, intensity, progress);
            yield return null;
        }

        if (light != null)
            light.intensity = intensity;
    }
}
