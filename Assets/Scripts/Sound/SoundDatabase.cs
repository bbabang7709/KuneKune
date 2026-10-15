using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "KuneKune/Data/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [Header("BGM List")]
    [Tooltip("BGM으로 사용할 SoundData를 넣어주세요.")]
    public List<SoundData> bgmList = new List<SoundData>();

    [Header("SFX List")]
    [Tooltip("효과음 SoundData를 모두 넣어주세요.")]
    public List<SoundData> sfxList = new List<SoundData>();

    private Dictionary<string, SoundData> _bgmDict;
    private Dictionary<string, SoundData> _sfxDict;

    public void Initialize()
    {
        // bgm 리스트 셋업
        _bgmDict = new Dictionary<string, SoundData>();
        foreach (var bgm in bgmList)
        {
            if (!string.IsNullOrEmpty(bgm.soundID)) _bgmDict[bgm.soundID] = bgm;
        }

        // sfx 리스트 셋업
        _sfxDict = new Dictionary<string, SoundData>();
        foreach (var sfx in sfxList)
        {
            if (!string.IsNullOrEmpty(sfx.soundID)) _sfxDict[sfx.soundID] = sfx;
        }
    }

    public SoundData GetBGM(string id) => 
        _bgmDict != null && _bgmDict.TryGetValue(id, out var data) ? data : null;
    public SoundData GetSFX(string id) =>
        _sfxDict != null && _sfxDict.TryGetValue(id, out var data) ? data : null;
}
