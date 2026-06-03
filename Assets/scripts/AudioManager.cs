// using UnityEngine;

// public class AudioManager : MonoBehaviour
// {
//     public static AudioManager Instance;

//     [Header("Sources")]
//     [SerializeField] private AudioSource musicSource;
//     [SerializeField] private AudioSource ambientSource;
//     [SerializeField] private AudioSource sfxSource;

//     [Header("Volumes")]
//     [Range(0f, 1f)] public float musicVolume = 1f;
//     [Range(0f, 1f)] public float ambientVolume = 1f;
//     [Range(0f, 1f)] public float sfxVolume = 1f;

//     [Header("Mute")]
//     public bool musicMuted = false;
//     public bool ambientMuted = false;
//     public bool sfxMuted = false;

//     private void Awake()
//     {
//         // SOLO PARA TESTEAR UNA VEZ
//         // Después podés borrar estas líneas
//         /*
//         PlayerPrefs.DeleteKey("MusicVolume");
//         PlayerPrefs.DeleteKey("AmbientVolume");
//         PlayerPrefs.DeleteKey("SFXVolume");

//         PlayerPrefs.DeleteKey("MusicMuted");
//         PlayerPrefs.DeleteKey("AmbientMuted");
//         PlayerPrefs.DeleteKey("SFXMuted");
//         */

//         // Singleton
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }

//         LoadAudioSettings();
//         ApplyVolumes();

//         Debug.Log("AudioManager inicializado");
//     }

//     // =====================================================
//     // GETTERS
//     // =====================================================

//     public float GetMusicVolume()
//     {
//         return musicVolume;
//     }

//     public float GetAmbientVolume()
//     {
//         return ambientVolume;
//     }

//     public float GetSFXVolume()
//     {
//         return sfxVolume;
//     }

//     public bool GetMusicMuted()
//     {
//         return musicMuted;
//     }

//     public bool GetAmbientMuted()
//     {
//         return ambientMuted;
//     }

//     public bool GetSFXMuted()
//     {
//         return sfxMuted;
//     }

//     // =====================================================
//     // MUSIC
//     // =====================================================

//     public void PlayMusic(AudioClip clip)
//     {
//         if (clip == null)
//         {
//             Debug.LogWarning("No hay clip de música asignado.");
//             return;
//         }

//         // evitar reiniciar la misma canción
//         if (musicSource.clip == clip && musicSource.isPlaying)
//             return;

//         musicSource.clip = clip;
//         musicSource.loop = true;

//         ApplyVolumes();

//         musicSource.Play();

//         Debug.Log("Reproduciendo música: " + clip.name);
//     }

//     // =====================================================
//     // SFX
//     // =====================================================

//     public void PlaySFX(AudioClip clip)
//     {
//         if (clip == null) return;
//         if (sfxMuted) return;

//         sfxSource.PlayOneShot(clip, sfxVolume);
//     }

//     // =====================================================
//     // AMBIENT
//     // =====================================================

//     public void PlayAmbient(AudioClip clip)
//     {
//         if (clip == null)
//         {
//             Debug.LogWarning("No hay clip ambiente asignado.");
//             return;
//         }

//         ambientSource.clip = clip;
//         ambientSource.loop = true;

//         ApplyVolumes();

//         ambientSource.Play();
//     }

//     // =====================================================
//     // SET VOLUMES
//     // =====================================================

//     public void SetMusicVolume(float value)
//     {
//         musicVolume = value;

//         ApplyVolumes();
//         SaveAudioSettings();

//         Debug.Log("Volumen música: " + value);
//     }

//     public void SetAmbientVolume(float value)
//     {
//         ambientVolume = value;

//         ApplyVolumes();
//         SaveAudioSettings();
//     }

//     public void SetSFXVolume(float value)
//     {
//         sfxVolume = value;

//         ApplyVolumes();
//         SaveAudioSettings();
//     }

//     // =====================================================
//     // MUTE
//     // =====================================================

//     public void ToggleMusicMute(bool muted)
//     {
//         musicMuted = muted;

//         ApplyVolumes();
//         SaveAudioSettings();
//     }

//     public void ToggleAmbientMute(bool muted)
//     {
//         ambientMuted = muted;

//         ApplyVolumes();
//         SaveAudioSettings();
//     }

//     public void ToggleSFXMute(bool muted)
//     {
//         sfxMuted = muted;

//         ApplyVolumes();
//         SaveAudioSettings();
//     }

//     // =====================================================
//     // APPLY
//     // =====================================================

//     private void ApplyVolumes()
//     {
//         if (musicSource != null)
//             musicSource.volume = musicMuted ? 0f : musicVolume;

//         if (ambientSource != null)
//             ambientSource.volume = ambientMuted ? 0f : ambientVolume;

//         if (sfxSource != null)
//             sfxSource.volume = sfxMuted ? 0f : sfxVolume;
//     }

//     // =====================================================
//     // SAVE
//     // =====================================================

//     private void SaveAudioSettings()
//     {
//         PlayerPrefs.SetFloat("MusicVolume", musicVolume);
//         PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
//         PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

//         PlayerPrefs.SetInt("MusicMuted", musicMuted ? 1 : 0);
//         PlayerPrefs.SetInt("AmbientMuted", ambientMuted ? 1 : 0);
//         PlayerPrefs.SetInt("SFXMuted", sfxMuted ? 1 : 0);

//         PlayerPrefs.Save();
//     }

//     // =====================================================
//     // LOAD
//     // =====================================================

//     private void LoadAudioSettings()
//     {
//         musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
//         ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
//         sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

//         musicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
//         ambientMuted = PlayerPrefs.GetInt("AmbientMuted", 0) == 1;
//         sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
//     }
// }

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Mute")]
    public bool musicMuted = false;
    public bool ambientMuted = false;
    public bool sfxMuted = false;

    // =====================================================
    // LISTA COMPLETA DE AUDIO PARA EL JUGADOR
    // =====================================================
    [Header("Player SFX Clips")]
    public AudioClip playerWalkStep;     // Pasos al caminar (se repite)
    public AudioClip playerJump;         // Al presionar saltar
    public AudioClip playerLand;         // Al tocar el suelo después de caer
    public AudioClip playerAttack;       // El espadazo al aire
    public AudioClip playerHurt;         // Al recibir un golpe de la flor u otro enemigo
    public AudioClip playerDeath;        // Al quedarse sin vida
    public AudioClip playerDash;         // El dash de tus runas

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadAudioSettings();
        ApplyVolumes();

        Debug.Log("AudioManager inicializado con todos los SFX del Player.");
    }

    // =====================================================
    // GETTERS
    // =====================================================

    public float GetMusicVolume() { return musicVolume; }
    public float GetAmbientVolume() { return ambientVolume; }
    public float GetSFXVolume() { return sfxVolume; }
    public bool GetMusicMuted() { return musicMuted; }
    public bool GetAmbientMuted() { return ambientMuted; }
    public bool GetSFXMuted() { return sfxMuted; }

    // =====================================================
    // REPRODUCCIÓN DE SFX
    // =====================================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxMuted) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // =====================================================
    // MUSIC & AMBIENT (Se mantienen igual)
    // =====================================================

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        ApplyVolumes();
        musicSource.Play();
    }

    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;
        ambientSource.clip = clip;
        ambientSource.loop = true;
        ApplyVolumes();
        ambientSource.Play();
    }

    // =====================================================
    // SET VOLUMES & MUTE (Se mantienen igual)
    // =====================================================

    public void SetMusicVolume(float value) { musicVolume = value; ApplyVolumes(); SaveAudioSettings(); }
    public void SetAmbientVolume(float value) { ambientVolume = value; ApplyVolumes(); SaveAudioSettings(); }
    public void SetSFXVolume(float value) { sfxVolume = value; ApplyVolumes(); SaveAudioSettings(); }
    public void ToggleMusicMute(bool muted) { musicMuted = muted; ApplyVolumes(); SaveAudioSettings(); }
    public void ToggleAmbientMute(bool muted) { ambientMuted = muted; ApplyVolumes(); SaveAudioSettings(); }
    public void ToggleSFXMute(bool muted) { sfxMuted = muted; ApplyVolumes(); SaveAudioSettings(); }

    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicMuted ? 0f : musicVolume;
        if (ambientSource != null) ambientSource.volume = ambientMuted ? 0f : ambientVolume;
        if (sfxSource != null) sfxSource.volume = sfxMuted ? 0f : sfxVolume;
    }

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicMuted", musicMuted ? 1 : 0);
        PlayerPrefs.SetInt("AmbientMuted", ambientMuted ? 1 : 0);
        PlayerPrefs.SetInt("SFXMuted", sfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        ambientMuted = PlayerPrefs.GetInt("AmbientMuted", 0) == 1;
        sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
    }
}