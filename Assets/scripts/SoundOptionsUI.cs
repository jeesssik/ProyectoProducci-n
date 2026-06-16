using UnityEngine;
using UnityEngine.UI;

public class SoundOptionsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider ambientSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Toggle ambientMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    private void OnEnable()
    {
        LoadValuesFromAudioManager();
    }

    private void Start()
    {
        LoadValuesFromAudioManager();
    }

    private void LoadValuesFromAudioManager()
    {
        if (AudioManager.Instance == null) return;

        musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
        ambientSlider.SetValueWithoutNotify(AudioManager.Instance.GetAmbientVolume());
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());

        musicMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetMusicMuted());
        ambientMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetAmbientMuted());
        sfxMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetSFXMuted());
    }

    public void OnMusicVolumeChanged(float value)
    {
        Debug.Log("Slider música cambió a: " + value);
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnAmbientVolumeChanged(float value)
    {
        AudioManager.Instance.SetAmbientVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMusicMuteChanged(bool value)
    {
        AudioManager.Instance.ToggleMusicMute(value);
    }

    public void OnAmbientMuteChanged(bool value)
    {
        AudioManager.Instance.ToggleAmbientMute(value);
    }

    public void OnSFXMuteChanged(bool value)
    {
        AudioManager.Instance.ToggleSFXMute(value);
    }
}