using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class KuneKuneHand : MonoBehaviour
{
    private const int DoorCount = 5;

    [SerializeField] private SunlightAreaRoot _sunlightAreaRoot;

    [Header("문과 손 이미지")]
    [Tooltip("손이 가장 적게 나온 이미지부터 가장 많이 나온 이미지 순서입니다.")]
    [SerializeField] private Sprite[] _handSprites;
    [SerializeField] private Sprite _openedDoorSprite;
    [SerializeField] private Sprite _closedDoorSprite;

    [Header("손을 밀어 올리는 난이도")]
    [Tooltip("손이 완전히 내려오는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float _secondsToFullyLower = 5f;
    [Tooltip("한 번 누를 때 밀어 올리는 양입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float _pushPerPress = 0.08f;

    [Header("문을 밀어 올리는 연출")]
    [Tooltip("밀어 올린 뒤 손이 다시 내려오기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float _pushHoldSeconds = 0.16f;
    [Tooltip("문이 흔들린 뒤 제자리로 돌아오는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float _shakeSeconds = 0.18f;
    [Tooltip("문이 좌우로 흔들리는 거리입니다.")]
    [SerializeField, Min(0f)] private float _shakeDistance = 0.045f;
    [Tooltip("누르는 순간 문이 위로 들리는 거리입니다.")]
    [SerializeField, Min(0f)] private float _pushLiftDistance = 0.06f;

    [Header("F 키 안내 연출")]
    [Tooltip("마지막으로 문을 두드린 뒤 안내가 다시 나타나기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float _handPromptDelay = 0.3f;
    [Tooltip("손이 끝까지 내려왔을 때 안내가 좌우로 움직이는 최대 거리입니다.")]
    [SerializeField, Min(0f)] private float _handPromptShakeDistance = 0.045f;
    private float _handPromptHideRemaining;
    private float _handPromptShakePhase;
    
    [Header("선택된 문의 전등")]
    [SerializeField, Min(0f)] private float _intensity = 1f;
    [Tooltip("불이 안정적으로 켜져 있는 평균 시간입니다. 가끔 짧게 떨린 뒤 다시 안정됩니다.")]
    [SerializeField, Min(0.01f)] private float _lightOnSeconds = 1f;

    private AudioSource _handScratchSource;

    [Header("획득 아이템")]
    [Tooltip("이벤트를 완료했을 때 얻을 아이템과 다이얼로그 등록하기")]
    [SerializeField] private DialogData finishDialog;
    [SerializeField] private ItemData rewardItem_key;
    [SerializeField] private DialogData keyDialog;
    [SerializeField] private ItemData rewardItem_dollArm;
    [SerializeField] private DialogData dollArmDialog;

    #region 형광등 효과음
    /// <summary>
    /// 평소 켜져 있을 때의 작은 웅웅거림
    /// </summary>
    private AudioSource _lightHumSource;
    private float _lightHumBaseVolume;
    /// <summary>
    /// 켜짐, 깜빡임, 소등 효과음
    /// </summary>
    private AudioSource _lightEventSource;
    /// <summary>
    /// 손과 형광등 소리를 일시정지했는지 기억합니다.
    /// </summary>
    private bool _audioPaused;
    /// <summary>
    /// 블랙아웃 효과가 들어갔는지 확인합니다.
    /// </summary>
    private bool _isPlayingLightCue;
    #endregion

    private Light2D[] _lights = new Light2D[DoorCount];
    private Coroutine _coFlicker = null;

    #region Door
    private List<int> _remainingDoors = new List<int>();
    private SpriteRenderer[] _doorSprites = new SpriteRenderer[DoorCount];
    private Collider2D[] _doorColliders = new Collider2D[DoorCount];
    
    private Vector3[] _originalPositions = new Vector3[DoorCount];
    private Vector3[] _originalSpriteMin = new Vector3[DoorCount];
    private Vector2[] _originalColliderOffsets = new Vector2[DoorCount];

    private int _shakingDoorIndex = -1;
    private float _shakeTimeRemaining;
    private float _shakeDirection = 1f;

    public int OpenedDoorCount { get; private set; } = 0;
    public int CurrentDoorIndex { get; private set; } = -1;
    #endregion

    private float _handDepth;
    private float _nextPushSoundTime;
    private float _pushHoldRemaining;   // 손이 다시 내려오기 전까지 남은 대기시간

    private Light2D _globalLight;

    private Collider2D _collider;
    private Collider2D _playerCollider;
    private PlayerMovement _playerMovement;
    private float _heatBeforeEvent;

    public static bool IsPlaying { get; private set; } = false;
    public static bool IsCleared { get; private set; } = false;
    public static bool IsGameOver { get; private set; } = false;

    // 손이 나온 문에 닿아 있고 조작할 수 있을 때만 키 안내를 표시합니다.
    public bool CanPushHand => isActiveAndEnabled && IsPlaying && !IsPaused &&
        CurrentDoorIndex >= 0 && IsPlayerAtCurrentDoor();

    // 플레이어의 UI가 먼저 갱신되어도 두드린 프레임에는 안내를 숨깁니다.
    public bool ShowHandPrompt => CanPushHand && _handPromptHideRemaining <= 0f &&
        (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame);

    public Vector3 HandPromptPosition
    {
        get
        {
            if (CurrentDoorIndex < 0 || _playerCollider == null)
                return transform.position;

            Bounds bounds = _playerCollider.bounds;

            // 문 그림 대신 고정된 충돌 범위를 기준으로 잡아 문 흔들림을 따라가지 않습니다.
            float x = _doorColliders[CurrentDoorIndex].bounds.center.x;
            float depth = Mathf.Clamp01(_handDepth);

            // 손이 절반까지 내려오기 전에는 멈춰 있고, 이후부터 떨림이 서서히 커집니다.
            float shakeStrength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, depth));
            float shakeX = Mathf.Sin(_handPromptShakePhase) * _handPromptShakeDistance * shakeStrength;

            // 손이 내려올수록 안내만 더 크게 떨립니다. 높이는 플레이어 머리 위로 유지합니다.
            return new Vector3(x + shakeX, bounds.max.y + 0.35f, 0f);
        }
    }

    public bool IsPaused => Time.timeScale <= 0f || AudioListener.pause ||
        (IsPlaying && ((ControlLockManager.Instance != null && ControlLockManager.Instance.IsInputLocked) ||
            (DialogSystem.Instance != null && DialogSystem.Instance.IsDialogPlaying)));
    
    private void Start()
    {
        Init();
    }

    public void Init()
    {
        // 정적 변수 초기화 (씬 재로드 시 정상 작동을 위함)
        IsPlaying = false;
        IsCleared = false;
        IsGameOver = false;
        
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

        _sunlightAreaRoot.gameObject.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerCollider = player.GetComponent<Collider2D>();
            _playerMovement = player.GetComponent<PlayerMovement>();
        }

        _collider = transform.GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _collider.excludeLayers = ~LayerMask.GetMask("Player");
        _collider.enabled = false;

        _globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None)
        .FirstOrDefault(light => light.lightType == Light2D.LightType.Global);

        if (_handScratchSource == null)
            _handScratchSource = SoundManager.Instance.CreateSFXSource(transform);

        // 새 문에서 손이 나타날 때마다 처음부터 반복 재생합니다.
        SoundManager.Instance.PlaySFXOnSource(_handScratchSource, "KuneKuneHand_Scratch", loop: true);
        _handScratchSource.Stop();

        StopFlicker();
    }

    public void Play()
    {
        // 진행 중이거나 이미 끝났다면 다시 시작하지 않습니다.
        if (IsPlaying || IsCleared || IsGameOver)
            return;

        // 클리어 시 복구할 더위 값은 실제 이벤트 시작 직전에 기록합니다.
        if (_playerMovement != null)
            _heatBeforeEvent = _playerMovement.CurrentHeat;

        _sunlightAreaRoot.gameObject.SetActive(true);
        _collider.enabled = false;
        _globalLight.color = new Color32(19, 12, 12, 255);

        _remainingDoors.Clear();
        for (int i = 0; i < DoorCount; i++)
            _remainingDoors.Add(i);

        IsPlaying = true;
        _handPromptHideRemaining = 0f;
        _handPromptShakePhase = 0f;
        SelectNextDoor();
    }

    private bool GameOver()
    {
        if (_handDepth < 1f)
            return false;

        // 손이 끝까지 내려왔다
        _sunlightAreaRoot.gameObject.SetActive(false);
        UpdateSprite();
        IsPlaying = false;
        IsGameOver = true;
        _playerMovement.SetCurrentHeat(_heatBeforeEvent);

        _handScratchSource?.Stop();
        StartBlackout();
        ResetDoorShake();
        StartCoroutine(GameOverSequence());

        return true;
    }

    private void Update()
    {
        // 대화나 컷신으로 조작할 수 없는 동안에는 손도 내려오지 않습니다.
        SetAudioPaused(IsPaused);
        if (IsPaused)
            return;

        // 웅웅거림은 불이 어두워지는 만큼 작아지고, 다시 밝아지면 원래 음량으로 돌아갑니다.
        if (!_isPlayingLightCue && _lightHumSource != null && CurrentDoorIndex >= 0)
        {
            float brightness = _lights[CurrentDoorIndex].intensity / Mathf.Max(0.001f, _intensity);
            _lightHumSource.volume = _lightHumBaseVolume * Mathf.Clamp01(brightness);
        }

        // 마지막 문을 열었어도 남은 흔들림은 자연스럽게 끝냅니다.
        UpdateDoorShake();

        if (IsPlaying == false)
            return;

        _handPromptHideRemaining = Mathf.Max(0f, _handPromptHideRemaining - Time.deltaTime);
        _handPromptShakePhase = Mathf.Repeat(_handPromptShakePhase + Time.deltaTime * Mathf.Lerp(4f, 10f, Mathf.Clamp01(_handDepth)) * Mathf.PI * 2f, 
            Mathf.PI * 2f);

        // 잠깐 버티는 시간이 끝난 뒤부터 손이 다시 내려옵니다.
        float lowerTime = Mathf.Max(0f, Time.deltaTime - _pushHoldRemaining);
        _pushHoldRemaining = Mathf.Max(0f, _pushHoldRemaining - Time.deltaTime);
        _handDepth = Mathf.Min(1f, _handDepth + lowerTime / Mathf.Max(0.1f, _secondsToFullyLower));

        if (GameOver())
            return;

        // 누르고 있는 시간과 관계없이, 새로 누른 순간에만 한 번 밀어 올립니다.
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && IsPlayerAtCurrentDoor())
        {
            _handPromptHideRemaining = _handPromptDelay;
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

        return door.IsTouching(_playerCollider);
    }

    private void SelectNextDoor()
    {
        // 첫 손은 2번째 문에서 나오고, 이후에는 남은 문 중에서 무작위로 선택합니다.
        int index = OpenedDoorCount == 0 ? _remainingDoors.IndexOf(1) : Random.Range(0, _remainingDoors.Count);
        CurrentDoorIndex = _remainingDoors[index];
        _remainingDoors.RemoveAt(index);
        
        _handDepth = 0.1f;  // 처음 손 위치
        _pushHoldRemaining = 0f;
        UpdateSprite();
        StartFlicker();
        _handScratchSource?.Play();
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
        _handScratchSource?.Stop();
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
        _sunlightAreaRoot.gameObject.SetActive(false);
        IsPlaying = false;
        IsCleared = true;
        CurrentDoorIndex = -1;

        Debug.Log(_heatBeforeEvent);
        _playerMovement.SetCurrentHeat(_heatBeforeEvent);

        // TODO: 문을 모두 열었을 때의 이벤트 처리 => 인형의 오른팔과 교실 열쇠 획득
        if (DialogSystem.Instance != null && finishDialog != null)
        {
            DialogSystem.Instance.PlayDialog(finishDialog, () => 
                StartCoroutine(RewardRoutine())
            );
        }

        _collider.enabled = true;
    }

    private IEnumerator RewardRoutine()
    {
        if (rewardItem_key == null || rewardItem_dollArm == null) yield break;
        
        InventoryManager.Instance?.AcquireItem(rewardItem_dollArm, dollArmDialog);
        yield return new WaitUntil(() => DialogSystem.Instance.IsDialogPlaying == false);

        InventoryManager.Instance?.AcquireItem(rewardItem_key, keyDialog);
    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("손이 끝까지 내려와 게임 오버되었습니다.", this);
        ControlLockManager.Instance?.AddInputLock(this);
        ControlLockManager.Instance?.AddMovementLock(this);

        // 무서워서 꺼버렸다. 나중에 알아서 켜주세요.
        SoundManager.Instance?.SetBGMVolume(0f);
        SoundManager.Instance?.PlaySFX("KuneKune_Jumpscare");

        if (UIManager.Instance != null)
           yield return UIManager.Instance.ShowJumpscareRoutine(0.05f);

        yield return new WaitForSecondsRealtime(1.5f);

        if (UIManager.Instance != null)
           yield return UIManager.Instance.FadeOut(0.2f);

        yield return new WaitForSecondsRealtime(1.5f);

        if (_coFlicker != null)
            yield return _coFlicker;

        SaveManager.Instance?.Load(true);
    }

    private void OnDisable()
    {
        _handScratchSource?.Stop();
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
        light.intensity = 0f;

        // 형광등 소리 셋팅
        if (SoundManager.Instance != null)
        {
            if (_lightHumSource == null)
                _lightHumSource = SoundManager.Instance.CreateSFXSource(transform);
            if (_lightEventSource == null)
                _lightEventSource = SoundManager.Instance.CreateSFXSource(transform);

            // 전등 자체의 웅웅거림 소리 셋팅
            SoundManager.Instance.PlaySFXOnSource(_lightHumSource, "Fluorescent_Idle", loop: true);
            _lightHumBaseVolume = _lightHumSource.volume;
            //_lightHumSource.Pause();
        }

        _coFlicker = StartCoroutine(CoFlicker(CurrentDoorIndex));
    }

    private void StopFlicker()
    {
        if (_coFlicker != null)
        {
            StopCoroutine(_coFlicker);
            _coFlicker = null;
        }

        if (_lightHumSource != null)
            _lightHumSource.Stop();
        if (_lightEventSource != null)
            _lightEventSource.Stop();

        _isPlayingLightCue = false;
        _audioPaused = false;

        foreach (Light2D light in _lights)
            if (light != null)
                light.enabled = false;
    }

    private IEnumerator CoFlicker(int index)
    {
        Light2D light = _lights[index];
        float noiseOffset = Random.Range(0f, 100f);
        float noiseTime = 0f;

        while (IsPlaying && CurrentDoorIndex == index && light != null)
        {
            float elapsed = 0f;
            float steadyTime = Mathf.Max(0.5f, _lightOnSeconds) * Random.Range(0.75f, 1.75f);

            // 평소에는 거의 일정하게 켜져 있고, 밝기만 아주 조금 흔들립니다.
            while (elapsed < steadyTime && light != null)
            {
                if (IsPaused)
                {
                    SetAudioPaused(true);
                    yield return null;
                    continue;
                }

                SetAudioPaused(false);

                elapsed += Time.deltaTime;
                noiseTime += Time.deltaTime;
            
                float noise = Mathf.PerlinNoise(noiseOffset, noiseTime * 1.7f);
                light.intensity = _intensity * Mathf.Lerp(0.94f, 1f, noise);
                yield return null;
            }

            if (light == null)
                break;

            // 깜빡깜빡
            int flickerCount = Random.value < 0.3f ? 1 : Random.Range(2, 4);
            for (int i = 0; i < flickerCount && light != null; i++)
            {
                // 깜빡이는 순간 짧은 전기 잡음을 더합니다. 같은 채널을 써서 소리가 겹쳐 쌓이지 않게 합니다.
                SoundManager.Instance.PlaySFXOnSource(_lightEventSource, "Fluorescent_Flicker", loop: false);
                if (_lightEventSource != null)
                    _lightEventSource.pitch = Random.Range(0.96f, 1.04f);

                float dimRatio = Random.value < 0.12f ? 0f : Random.Range(0.2f, 0.65f);
                yield return FadeLight(light, _intensity * dimRatio, Random.Range(0.015f, 0.035f));
                yield return FadeLight(light, _intensity * dimRatio, Random.Range(0.03f, 0.1f));
                yield return FadeLight(light, _intensity * Random.Range(0.78f, 0.98f), Random.Range(0.03f, 0.09f));

                if (i < flickerCount - 1)
                    yield return FadeLight(light, light.intensity, Random.Range(0.045f, 0.16f));
            }

            yield return FadeLight(light, _intensity * 0.97f, Random.Range(0.15f, 0.3f));
        }

        if (light != null)
            light.enabled = false;

        _coFlicker = null;
    }

    /// <summary>
    /// 일시정지했던 소리를 이어서 재생하거나 재생 중인 소리를 일시정지합니다.
    /// </summary>
    /// <param name="paused">true: 일시정지, false: 이어서 재생</param>
    private void SetAudioPaused(bool paused)
    {
        // 중복 방지
        if (_audioPaused == paused)
            return;

        if (IsPaused)
            _handScratchSource?.Pause();
        else
            _handScratchSource?.UnPause();

        _audioPaused = paused;
        if (paused)
        {
            _lightHumSource?.Pause();
            _lightEventSource?.Pause();
        }
        else
        {
            _lightEventSource?.UnPause();
            if (_isPlayingLightCue == false)
                _lightHumSource?.UnPause();
        }
    }

    private void StartBlackout()
    {
        StopFlicker();
        Light2D light = _lights[CurrentDoorIndex];
        if (light == null)
            return;

        // 정상 진행 때 쓰던 채널이 없더라도 소등 효과음을 재생할 수 있게 준비합니다.
        if (_lightEventSource == null && SoundManager.Instance != null)
            _lightEventSource = SoundManager.Instance.CreateSFXSource(transform);
        
        light.enabled = true;
        _coFlicker = StartCoroutine(CoBlackout(light));
    }

    private IEnumerator CoBlackout(Light2D light)
    {
        // 게임이 멈췄는지 확인
        while (IsPaused)
        {
            SetAudioPaused(true);
            yield return null;
        }

        _audioPaused = false;
        _lightHumSource?.Pause();   // 웅웅 소리 멈춤

        _isPlayingLightCue = true;
        bool hasSound = SoundManager.Instance != null &&
            SoundManager.Instance.PlaySFXOnSource(_lightEventSource, "Fluorescent_Blackout", loop: false);

        float initialIntensity = light.intensity;
        float elapsed = 0f;
        float seconds = hasSound ? _lightEventSource.clip.length : 1.2f;
        while (elapsed < seconds && light != null)
        {
            if (IsPaused)
            {
                SetAudioPaused(true);
                yield return null;
                continue;
            }

            SetAudioPaused(false);

            // 화면 그리기가 늦어져도 음원의 현재 재생 위치에 밝기를 맞춥니다.
            elapsed = hasSound
                ? (_lightEventSource.isPlaying ? _lightEventSource.time : seconds)
                : elapsed + Time.deltaTime;
            light.intensity = Mathf.Lerp(initialIntensity, 0f, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }

        if (light != null)
            light.intensity = 0f;

        _lightEventSource?.Stop();
        _isPlayingLightCue = false;

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
            if (IsPaused)
            {
                SetAudioPaused(true);
                yield return null;
                continue;
            }

            SetAudioPaused(false);

            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
            light.intensity = Mathf.Lerp(initial, intensity, progress);
            yield return null;
        }

        if (light != null)
            light.intensity = intensity;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsCleared)
        {
            // 5개의 문이 쾅 닫힘
            for (int i = 0; i < DoorCount; ++i)
                SetSprite(i, _closedDoorSprite);

            SoundManager.Instance?.PlaySFX("StallDoor_Open", 8f);  // 기본 음량의 8배로 재생합니다.
            _collider.enabled = false;
        }
    }
}
