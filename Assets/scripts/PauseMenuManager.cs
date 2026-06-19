using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Canvas & Panels")]
    [Tooltip("El Canvas principal de pausa (PauseScreen).")]
    [SerializeField] private GameObject pauseCanvas;
    
    [Tooltip("El panel de opciones generales (CanvasOpt).")]
    [SerializeField] private GameObject optionsCanvas;

    [Tooltip("El nuevo panel o canvas para el control de volúmenes de audio.")]
    [SerializeField] private GameObject audioVolumeCanvas; // <-- NUEVO

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    private bool isPaused = false;

    private void Start()
    {
        // Al empezar, nos aseguramos de que todo esté apagado
        pauseCanvas.SetActive(false);
        if (optionsCanvas != null) optionsCanvas.SetActive(false);
        if (audioVolumeCanvas != null) audioVolumeCanvas.SetActive(false); // <-- NUEVO
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            HandlePhysicalBackButton();
        }
    }

    /// <summary>Maneja la lógica del botón físico (Escape/P) como un 'Atrás' inteligente.</summary>
    private void HandlePhysicalBackButton()
    {
        if (!isPaused)
        {
            PauseGame();
            return;
        }

        // Capa 3: Si está abierto el volumen de audio, vuelve a Opciones
        if (audioVolumeCanvas != null && audioVolumeCanvas.activeSelf)
        {
            CloseAudioToOptions();
        }
        // Capa 2: Si está abierta la configuración, vuelve a Pausa
        else if (optionsCanvas != null && optionsCanvas.activeSelf)
        {
            CloseOptionsToPause();
        }
        // Capa 1: Si solo está la pausa, cierra el menú y despausa el juego
        else
        {
            ResumeGame();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        pauseCanvas.SetActive(true);
        if (optionsCanvas != null) optionsCanvas.SetActive(false);
        if (audioVolumeCanvas != null) audioVolumeCanvas.SetActive(false);
        AbilityHUD.SetAllHidden(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseCanvas.SetActive(false);
        if (optionsCanvas != null) optionsCanvas.SetActive(false);
        if (audioVolumeCanvas != null) audioVolumeCanvas.SetActive(false);
        AbilityHUD.SetAllHidden(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    // ========================================================
    // NAVEGACIÓN: IR (Pausa -> Configuración -> Volúmenes)
    // ========================================================

    /// <summary>Se asigna al botón 'OPCIONES' del menú de pausa principal.</summary>
    public void OpenOptionsFromPause()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (optionsCanvas != null) optionsCanvas.SetActive(true);
    }

    /// <summary>Se asigna al botón 'SONIDO' del menú de configuración (CanvasOpt).</summary>
    public void OpenAudioFromOptions()
    {
        if (optionsCanvas != null) optionsCanvas.SetActive(false);
        if (audioVolumeCanvas != null) audioVolumeCanvas.SetActive(true);
    }

    // ========================================================
    // NAVEGACIÓN: VOLVER (Volúmenes -> Configuración -> Pausa)
    // ========================================================

    /// <summary>Se asigna al botón 'ATRÁS' del panel de volúmenes de audio.</summary>
    public void CloseAudioToOptions()
    {
        if (audioVolumeCanvas != null) audioVolumeCanvas.SetActive(false);
        if (optionsCanvas != null) optionsCanvas.SetActive(true);
    }

    /// <summary>Se asigna al botón 'VOLVER' del CanvasOpt.</summary>
    public void CloseOptionsToPause()
    {
        if (optionsCanvas != null) optionsCanvas.SetActive(false);
        if (pauseCanvas != null) pauseCanvas.SetActive(true);
    }

    // ========================================================

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}