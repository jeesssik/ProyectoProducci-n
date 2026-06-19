using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject gameOverCanvas;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    private void Awake()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void ShowGameOver()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        AbilityHUD.SetAllHidden(true);
        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
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