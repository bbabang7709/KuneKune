using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private SoundDatabase soundDatabase;

    [Header("Audio Mixers")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioSource _footstepSource;

    private SoundData _currentBGMData;
    private Coroutine _volumeCoroutine;
    private Coroutine _crossFadeCoroutine;
    private float _bgmVolumeMultiplier = 1f;
    private readonly HashSet<Object> _bgmPlaybackBlockers = new();

    private sealed class LoopSFX
    {
        public SoundData data;
        public float volume;
        public float pitch = 1f;
    }

    private readonly Dictionary<AudioSource, LoopSFX> _loopSFX = new();
    private bool _loopsPaused;

    [Header("반복 효과음")]
    [Tooltip("목표 음량까지 부드럽게 바뀌는 시간입니다.")]
    [SerializeField, Min(0.05f)] private float loopFadeSeconds = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        soundDatabase?.Initialize();
        CreateAudioSources();
    }

    private void CreateAudioSources()
    {
        // BGM용 AudioSource 세팅
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        // SFX용 AudioSource 세팅
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;

        // 발걸음 전용 AudioSource 세팅
        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.outputAudioMixerGroup = sfxMixerGroup;
        _footstepSource.loop = false;
        _footstepSource.playOnAwake = false;
    }

    public void PlaySFX(string sfxID, float volumeMultiplier = 1f)
    {
        SoundData data = soundDatabase?.GetSFX(sfxID);
        if (data != null && data.clip != null)
        {
            _sfxSource.PlayOneShot(data.clip, data.volume * Mathf.Max(0f, volumeMultiplier));
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX '{sfxID}'를 찾을 수 없습니다.");
        }
    }

    public void PlayFootstepSFX(string sfxID)
    {
        SoundData data = soundDatabase?.GetSFX(sfxID);
        if (data != null && data.clip != null)
        {
            _footstepSource.clip = data.clip;
            _footstepSource.volume = data.volume;

            _footstepSource.pitch = Random.Range(0.85f, 1.15f);

            _footstepSource.Play();
        }
    }

    /// <summary>
    /// 특정 오브젝트가 따로 재생하고 멈출 수 있는 효과음 채널을 만듭니다.
    /// </summary>
    public AudioSource CreateSFXSource(Transform owner)
    {
        AudioSource source = owner.gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = sfxMixerGroup;
        return source;
    }

    /// <summary>
    /// 지정한 채널에서 재생합니다. 같은 채널의 이전 효과음은 멈춥니다.
    /// </summary>
    public bool PlaySFXOnSource(AudioSource source, string sfxID, float volume = 1f, bool loop = false)
    {
        if (source == null)
            return false;

        source.Stop();
        SoundData data = soundDatabase?.GetSFX(sfxID);
        if (data == null || data.clip == null)
        {
            source.clip = null;
            Debug.LogWarning($"[SoundManager] SFX '{sfxID}'를 찾을 수 없습니다.", this);
            return false;
        }

        source.clip = data.clip;
        source.volume = data.volume * Mathf.Clamp01(volume);
        source.pitch = 1f;
        source.loop = loop;
        source.Play();
        return true;
    }

    /// <summary>
    /// 반복 효과음을 준비합니다. 반환된 소리로 음량을 조절하고 사용이 끝나면 정지합니다.
    /// </summary>
    public AudioSource PlayLoopSFX(string sfxID)
    {
        SoundData data = soundDatabase?.GetSFX(sfxID);
        if (data == null || data.clip == null)
        {
            Debug.LogWarning($"[SoundManager] 반복 효과음 '{sfxID}'를 찾을 수 없습니다.", this);
            return null;
        }

        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.clip = data.clip;
        source.volume = 0f;
        _loopSFX.Add(source, new LoopSFX { data = data });
        return source;
    }

    /// <summary>
    /// 데이터베이스의 기본 음량에 곱할 비율과 재생 속도를 지정합니다.
    /// </summary>
    public void SetLoopSFX(AudioSource source, float volume, float pitch = 1f)
    {
        if (source == null || !_loopSFX.TryGetValue(source, out LoopSFX loop))
            return;

        loop.volume = Mathf.Clamp01(volume);
        loop.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
    }

    public void StopLoopSFX(AudioSource source)
    {
        if (source == null || !_loopSFX.Remove(source))
            return;

        source.Stop();
        Destroy(source);
    }

    private void LateUpdate()
    {
        bool paused = Time.timeScale <= 0f || AudioListener.pause;
        float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, loopFadeSeconds);
        foreach (var entry in _loopSFX)
        {
            AudioSource source = entry.Key;
            if (source == null)
                continue;

            if (paused != _loopsPaused)
            {
                if (paused) source.Pause();
                else source.UnPause();
            }
            if (paused)
                continue;

            LoopSFX loop = entry.Value;
            float target = loop.volume * loop.data.volume;
            source.volume = Mathf.MoveTowards(source.volume, target, step);
            source.pitch = Mathf.MoveTowards(source.pitch, loop.pitch, step);
            if (source.volume <= 0f)
            {
                if (source.isPlaying) source.Stop();
            }
            else if (!source.isPlaying)
            {
                source.Play();
            }
        }
        _loopsPaused = paused;
    }

    private void OnDisable()
    {
        foreach (AudioSource source in _loopSFX.Keys)
            if (source != null) source.Pause();
        _loopsPaused = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    /// <summary>
    /// 지정한 연출이 진행되는 동안 배경음 재생을 막습니다. 해제 시 자동 재생하지 않습니다.
    /// </summary>
    public void SetBGMPlaybackBlocked(Object owner, bool blocked)
    {
        if (owner == null) return;

        if (blocked)
        {
            _bgmPlaybackBlockers.Add(owner);
            StopBGM(0f);
        }
        else
        {
            _bgmPlaybackBlockers.Remove(owner);
        }
    }

    public void PlayBGM(string bgmID, float fadeDuration = 1f, float volumeMultiplier = 1f,
        Object playbackOwner = null)
    {
        // 씬 전환으로 소유자가 사라지면 다음 씬의 음악을 막지 않습니다.
        _bgmPlaybackBlockers.RemoveWhere(owner => owner == null);
        // 차단을 소유한 컷씬은 지정한 연출 음악만 직접 재생할 수 있습니다.
        if (_bgmPlaybackBlockers.Count > 0 &&
            (playbackOwner == null || _bgmPlaybackBlockers.Count != 1 ||
             !_bgmPlaybackBlockers.Contains(playbackOwner))) return;

        SoundData data = soundDatabase?.GetBGM(bgmID);
        if (data == null || data.clip == null) return;

        // 이미 같은 곡이 재생 중이거나 전환 중이면 곡은 유지하고 배율만 맞춥니다.
        // (전환 중에는 clip이 아직 이전 곡이므로 목표 곡 기준으로 비교합니다.
        //  StopBGM 중에는 _currentBGMData가 null이라 같은 곡이어도 다시 재생됩니다.)
        if (_currentBGMData == data && _bgmSource.isPlaying)
        {
            FadeBGMVolume(volumeMultiplier, fadeDuration);
            return;
        }

        CancelBGMRoutines();
        _currentBGMData = data;
        _bgmVolumeMultiplier = Mathf.Max(0f, volumeMultiplier);
        _crossFadeCoroutine = StartCoroutine(CrossFadeBGM(data, fadeDuration, volumeMultiplier));
    } 

    public void FadeBGMVolume(float multiplier, float fadeDuration = 1f)
    {
        if (_currentBGMData == null || !_bgmSource.isPlaying) return;

        _bgmVolumeMultiplier = multiplier;

        // 곡 전환 중이면 전환이 끝날 때 이 배율로 맞춰지므로 따로 페이드하지 않습니다.
        if (_crossFadeCoroutine != null) return;

        if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);

        float targetVolume = _currentBGMData.volume * multiplier;
        _volumeCoroutine = StartCoroutine(VolumeFadeRoutine(targetVolume, fadeDuration));
    }

    private void CancelBGMRoutines()
    {
        if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);
        if (_crossFadeCoroutine != null) StopCoroutine(_crossFadeCoroutine);
        _volumeCoroutine = null;
        _crossFadeCoroutine = null;
    }

    private IEnumerator VolumeFadeRoutine(float targetVolume, float duration)
    {
        float currentTime = 0f;
        float startVol = _bgmSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVol, targetVolume, currentTime / duration);
            yield return null;
        }

        _bgmSource.volume = targetVolume;
    }

    private IEnumerator CrossFadeBGM(SoundData nextData, float duration, float volumeMultiplier)
    {
        float targetVolume = nextData.volume * Mathf.Max(0f, volumeMultiplier);
        float currentTime = 0f;
        float startVol = _bgmSource.volume;

        if (_bgmSource.isPlaying)
        {
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, currentTime / duration);
                yield return null;
            }
        }

        _bgmSource.clip = nextData.clip;
        _bgmSource.Play();
        currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, nextData.volume * _bgmVolumeMultiplier, currentTime / duration);
            yield return null;
        }

        _bgmSource.volume = nextData.volume * _bgmVolumeMultiplier;
        _crossFadeCoroutine = null;
    }

    public void SetBGMVolume(float level)
    {
        if (level < 0f || level > 1f) return;

        _bgmSource.volume = level;
    }

    public void StopBGM(float fadeDuration = 1f)
    {
        CancelBGMRoutines();

        _currentBGMData = null;

        if (!_bgmSource.isPlaying) return;

        if (fadeDuration <= 0f)
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
            return;
        }

        _volumeCoroutine = StartCoroutine(StopBGMRoutine(fadeDuration));
    }

    private IEnumerator StopBGMRoutine(float duration)
    {
        float currentTime = 0f;
        float startVol = _bgmSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVol, 0f, currentTime / duration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = null;
        _bgmSource.volume = 0f;
    }
}
