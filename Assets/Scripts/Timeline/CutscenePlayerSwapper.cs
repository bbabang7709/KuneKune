using UnityEngine;
using UnityEngine.Playables;

public class CutscenePlayerSwapper : MonoBehaviour
{
    [Header("Cutscene Actors")]
    [Tooltip("타임라인에서 연기를 담당할 더미 플레이어 객체")]
    [SerializeField] private GameObject  dummyPlayer;
    
    private PlayableDirector _director;
    private GameObject _realPlayer;
    private SpriteRenderer _realPlayerSprite;

    [SerializeField] private Vector3 offset;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        _director.played += OnCutsceneStart;
        _director.stopped += OnCutsceneEnd;
    }

    private void OnDisable()
    {
        _director.played -= OnCutsceneStart;
        _director.stopped -= OnCutsceneEnd;
    }

    private void OnCutsceneStart(PlayableDirector director)
    {
        if (_realPlayer == null)
        {
            _realPlayer = GameObject.FindGameObjectWithTag("Player");
            if (_realPlayer != null)
                _realPlayerSprite = _realPlayer.GetComponentInChildren<SpriteRenderer>();
        }

        if (_realPlayerSprite != null) _realPlayerSprite.enabled = false;
        _realPlayer.GetComponent<Collider2D>().enabled = false; // 충돌 무시 처리

        if (ControlLockManager.Instance != null && _realPlayer != null)
        {
            ControlLockManager.Instance.AddInputLock(this);
            ControlLockManager.Instance.AddMovementLock(this);
        }

        if (dummyPlayer != null) dummyPlayer.SetActive(true);
    }

    private void OnCutsceneEnd(PlayableDirector director)
    {
        if (_realPlayer != null && dummyPlayer != null)
        {
            _realPlayer.transform.position = dummyPlayer.transform.position + offset;
        }

        // 배우 퇴장
        if (dummyPlayer != null) dummyPlayer.SetActive(false);

        if (_realPlayerSprite != null) _realPlayerSprite.enabled = true;
        _realPlayer.GetComponent<Collider2D>().enabled = true;

        if (ControlLockManager.Instance != null && _realPlayer != null)
        {
            ControlLockManager.Instance.RemoveInputLock(this);
            ControlLockManager.Instance.RemoveMovementLock(this);
        }
    }
}
