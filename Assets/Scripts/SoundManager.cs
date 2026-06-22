using UnityEngine;
using static Enums;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] SfxDatabase sfxDatabase;

    public float BgmVolume
    {
        get => bgmSource != null ? bgmSource.volume : DataManager.Data.BgmVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (bgmSource != null) bgmSource.volume = v;
            DataManager.Data.BgmVolume = v;
            DataManager.RequestSave();
        }
    }

    public float SfxVolume
    {
        get => sfxSource != null ? sfxSource.volume : DataManager.Data.SfxVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (sfxSource != null) sfxSource.volume = v;
            DataManager.Data.SfxVolume = v;
            DataManager.RequestSave();
        }
    }

    public void Initialize()
    {
        if (bgmSource != null) bgmSource.volume = DataManager.Data.BgmVolume;
        if (sfxSource != null) sfxSource.volume = DataManager.Data.SfxVolume;
        sfxDatabase?.Initialize();
    }

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayBgm(BgmKey key)
    {
        var clip = sfxDatabase?.GetBgm(key);
        if (clip != null) PlayBgm(clip);
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySfx(SfxKey key)
    {
        var clip = sfxDatabase?.GetSfx(key);
        if (clip != null) PlaySfx(clip);
    }
}
