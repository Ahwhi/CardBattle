using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Enums;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button closeButton;

    private SoundManager _sound;

    void Awake()
    {
        _sound = AppDirector.I.Sound;
    }

    void Start()
    {
        closeButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); Close(); })
            .AddTo(this);

        bgmSlider?.OnValueChangedAsObservable()
            .Subscribe(v => _sound.BgmVolume = v)
            .AddTo(this);

        sfxSlider?.OnValueChangedAsObservable()
            .Subscribe(v => _sound.SfxVolume = v)
            .AddTo(this);

        SyncSliders();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        SyncSliders();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void SyncSliders()
    {
        if (_sound == null) return;
        if (bgmSlider != null) bgmSlider.value = _sound.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = _sound.SfxVolume;
    }
}
