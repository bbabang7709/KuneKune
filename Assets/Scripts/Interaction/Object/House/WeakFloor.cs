using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 플레이어가 밟고 지나가면 무너집니다. 무너진 뒤 상호작용할 수 있는 바닥입니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class WeakFloor : MonoBehaviour, IInteractable
{
    private enum EWeakFloorState
    {
        Idle,
        Collapsing,
        WaitingForKunekuneEncounterCutscene,
        PlayingKunekuneEncounterCutscene,
        WaitingForFallCutscene,
        PlayingFallCutscene,
        End
    }

    [Header("무너지는 연출")]
    [Tooltip("바닥이 무너지는 연출에 걸리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float _collapseDuration = 0.5f;

    [Tooltip("무너지는 동안 바닥이 좌우로 움직이는 거리입니다.")]
    [Min(0f)] [SerializeField] private float _shakeDistance = 0.01f;

    [Tooltip("무너진 바닥에 사용할 색입니다.")]
    [SerializeField] private Color _collapsedColor = new(0.08f, 0.05f, 0.03f, 1f);

    [Header("첫 번째 상호작용 컷씬")]
    [Tooltip("1층에서 돌아다니는 쿠네쿠네를 발견하고 눈이 마주치는 컷씬입니다.")]
    [SerializeField] private PlayableDirector _kunekuneEncounterCutsceneDirector;

    [Header("두 번째 상호작용 컷씬")]
    [Tooltip("쿠네쿠네가 문을 열고 들어오며 플레이어가 1층으로 떨어지는 컷씬입니다.")]
    [SerializeField] private PlayableDirector _fallCutsceneDirector;

    private SpriteRenderer _spriteRenderer;
    [SerializeField] private BoxCollider2D _trigger;
    [SerializeField] private BoxCollider2D _collapsedBarrier;
    private EWeakFloorState _state = EWeakFloorState.Idle;

    private Vector3 _initialLocalPosition;
    private Color _initialColor;

    /// <summary>
    /// 무너진 뒤 컷씬을 기다리거나, 컷씬이 끝난 뒤에만 상호작용할 수 있습니다.
    /// </summary>
    public bool CanInteract =>
        _state == EWeakFloorState.WaitingForKunekuneEncounterCutscene ||
        _state == EWeakFloorState.WaitingForFallCutscene;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _initialLocalPosition = transform.localPosition;
        _initialColor = _spriteRenderer.color;

        // 상호작용 용도
        _trigger.isTrigger = true;
        LayerMask playerMask = LayerMask.GetMask("Player");
        _trigger.excludeLayers = ~playerMask;
        _trigger.enabled = true;

        // 이동 차단 영역
        _collapsedBarrier.enabled = false;
        _collapsedBarrier.isTrigger = false;
        _collapsedBarrier.size = new Vector2(1f, 7.5f);
        _collapsedBarrier.offset = new Vector2(0f, 3f);
        _collapsedBarrier.excludeLayers = ~playerMask;

        if (_kunekuneEncounterCutsceneDirector != null)
            _kunekuneEncounterCutsceneDirector.stopped += HandleKunekuneEncounterCutsceneStopped;

        if (_fallCutsceneDirector != null)
            _fallCutsceneDirector.stopped += HandleFallCutsceneStopped;
    }

    private void OnDestroy()
    {
        if (_kunekuneEncounterCutsceneDirector != null)
            _kunekuneEncounterCutsceneDirector.stopped -= HandleKunekuneEncounterCutsceneStopped;

        if (_fallCutsceneDirector != null)
            _fallCutsceneDirector.stopped -= HandleFallCutsceneStopped;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_state != EWeakFloorState.Idle || _state == EWeakFloorState.End)
            return;

        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        _state = EWeakFloorState.Collapsing;

        float elapsedTime = 0f;
        while (elapsedTime < _collapseDuration)
        {
            elapsedTime += Time.deltaTime;

            float horizontalOffset = Mathf.Sin(elapsedTime * 50f) * _shakeDistance;
            transform.localPosition = _initialLocalPosition + Vector3.right * horizontalOffset;

            float progress = _collapseDuration <= 0f ? 1f : Mathf.Clamp01(elapsedTime / _collapseDuration);
            _spriteRenderer.color = Color.Lerp(_initialColor, _collapsedColor, progress);

            yield return null;
        }

        transform.localPosition = _initialLocalPosition;
        _spriteRenderer.color = _collapsedColor;
        _state = EWeakFloorState.WaitingForKunekuneEncounterCutscene;

        // 상호작용용 트리거는 유지하고, 이동을 막는 충돌만 추가로 켭니다.
        _collapsedBarrier.enabled = true;

        // 바닥이 무너지면 남은 청소 지점도 모두 완료한 것으로 처리합니다.
        QuestManager.Instance?.CompleteQuest("HouseCleanup");

        Debug.Log("[WeakFloor] 바닥이 무너졌습니다. 이제 상호작용할 수 있습니다.", this);
    }

    public void OnInteractPressed()
    {
        if (CanInteract == false)
            return;

        if (_state == EWeakFloorState.WaitingForKunekuneEncounterCutscene)
            PlayKunekuneEncounterCutscene();
        else if (_state == EWeakFloorState.WaitingForFallCutscene)
            PlayChaseStartCutscene();
    }

    /// <summary>
    /// 쿠네쿠네를 발견하고 눈이 마주치는 컷씬을 재생합니다.
    /// </summary>
    private void PlayKunekuneEncounterCutscene()
    {
        _state = EWeakFloorState.PlayingKunekuneEncounterCutscene;

        if (_kunekuneEncounterCutsceneDirector == null)
        {
            Debug.Log("[WeakFloor] 쿠네쿠네 발견 컷씬이 없어 완료 상태로 바로 넘어갑니다.", this);
            NotifyKunekuneEncounterCutsceneFinished();
            return;
        }

        _kunekuneEncounterCutsceneDirector.Play();
    }

    /// <summary>
    /// 쿠네쿠네가 들어오고 플레이어가 떨어지는 추격 시작 컷씬을 재생합니다.
    /// </summary>
    private void PlayChaseStartCutscene()
    {
        _state = EWeakFloorState.PlayingFallCutscene;

        if (_fallCutsceneDirector == null)
        {
            Debug.Log("[WeakFloor] 추격 시작 컷씬이 없어 완료 상태로 바로 넘어갑니다.", this);
            NotifyFallCutsceneFinished();
            return;
        }

        _fallCutsceneDirector.Play();
    }

    private void HandleKunekuneEncounterCutsceneStopped(PlayableDirector director)
    {
        NotifyKunekuneEncounterCutsceneFinished();
    }

    private void HandleFallCutsceneStopped(PlayableDirector director)
    {
        NotifyFallCutsceneFinished();
    }

    /// <summary>
    /// 쿠네쿠네 발견 컷씬이 끝났을 때 호출합니다.
    /// </summary>
    private void NotifyKunekuneEncounterCutsceneFinished()
    {
        if (_state != EWeakFloorState.PlayingKunekuneEncounterCutscene)
            return;

        _state = EWeakFloorState.WaitingForFallCutscene;
        Debug.Log("[WeakFloor] 쿠네쿠네 발견 컷씬이 끝났습니다. 다시 상호작용할 수 있습니다.", this);
    }

    /// <summary>
    /// 1층으로 떨어지는 컷씬이 끝났을 때 호출하고 실제 추격을 시작합니다.
    /// </summary>
    private void NotifyFallCutsceneFinished()
    {
        if (_state != EWeakFloorState.PlayingFallCutscene)
            return;

        _state = EWeakFloorState.End;
        HouseChase.Instance.BeginChase();
    }
}
