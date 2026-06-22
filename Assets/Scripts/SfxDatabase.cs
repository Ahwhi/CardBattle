using System.Collections.Generic;
using UnityEngine;
using static Enums;

[CreateAssetMenu(fileName = "SfxDatabase", menuName = "CardBattle/SfxDatabase")]
public class SfxDatabase : ScriptableObject
{
    [System.Serializable]
    public class SfxEntry
    {
        public SfxKey Key;
        public AudioClip Clip;
    }

    [System.Serializable]
    public class BgmEntry
    {
        public BgmKey Key;
        public AudioClip Clip;
    }

    [SerializeField] List<SfxEntry> sfxEntries = new();
    [SerializeField] List<BgmEntry> bgmEntries = new();

    private Dictionary<SfxKey, AudioClip> _sfxMap;
    private Dictionary<BgmKey, AudioClip> _bgmMap;

    public void Initialize()
    {
        _sfxMap = new Dictionary<SfxKey, AudioClip>();
        foreach (var e in sfxEntries)
            if (e.Clip != null) _sfxMap[e.Key] = e.Clip;

        _bgmMap = new Dictionary<BgmKey, AudioClip>();
        foreach (var e in bgmEntries)
            if (e.Clip != null) _bgmMap[e.Key] = e.Clip;
    }

    public AudioClip GetSfx(SfxKey key) =>
        _sfxMap != null && _sfxMap.TryGetValue(key, out var clip) ? clip : null;

    public AudioClip GetBgm(BgmKey key) =>
        _bgmMap != null && _bgmMap.TryGetValue(key, out var clip) ? clip : null;
}
