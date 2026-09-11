using System.Collections;
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

    public void PlaySFX(string sfxID)
    {
        SoundData data = soundDatabase?.GetSFX(sfxID);
        if (data != null && data.clip != null)
        {
            _sfxSource.PlayOneShot(data.clip, data.volume);
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

    public void PlayBGM(string bgmID, float fadeDuration = 1f)
    {
        SoundData data = soundDatabase?.GetBGM(bgmID);
        if (data == null || data.clip == null) return;

        // 이미 같은 곡이 재생 중이면 무시
        if (_bgmSource.clip == data.clip && _bgmSource.isPlaying) return;

        _currentBGMData = data;
        if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);
        StartCoroutine(CrossFadeBGM(data, fadeDuration));
    }

    public void FadeBGMVolume(float multiplier, float fadeDuration = 1f)
    {
        if (_currentBGMData == null || !_bgmSource.isPlaying) return;

        if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);

        float targetVolume = _currentBGMData.volume * multiplier;
        _volumeCoroutine = StartCoroutine(VolumeFadeRoutine(targetVolume, fadeDuration)); 
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

    private IEnumerator CrossFadeBGM(SoundData nextData, float duration)
    {
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
            _bgmSource.volume = Mathf.Lerp(0f, nextData.volume, currentTime / duration);
            yield return null;
        }

        _bgmSource.volume = nextData.volume;
    }

    public void SetBGMVolume(float level)
    {
        if (level < 0f || level > 1f) return;

        _bgmSource.volume = level;
    }
}
