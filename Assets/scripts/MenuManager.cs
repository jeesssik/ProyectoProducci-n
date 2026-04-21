using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject optionsCanvas;
    public GameObject creditsCanvas;
    public GameObject soundOptionsCanvas;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    void Start()
    {
        ShowMainMenu();
    }

    // ------------------------
    // BOTONES PRINCIPALES
    // ------------------------

    public void OnStartButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnOptionsButton()
    {
        mainMenuCanvas.SetActive(false);
        optionsCanvas.SetActive(true);
        creditsCanvas.SetActive(false);
    }

    public void OnCreditsButton()
    {
        mainMenuCanvas.SetActive(false);
        optionsCanvas.SetActive(false);
        creditsCanvas.SetActive(true);
    }

    public void OnExitButton()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ------------------------
    // VOLVER AL MENÚ
    // ------------------------

    public void OnBackButton()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuCanvas.SetActive(true);
        optionsCanvas.SetActive(false);
        creditsCanvas.SetActive(false);
    }

    //------------------------
    //BOTONES DE OPCIONES
    //------------------------

    public void onSoundButton()
    {
        
        //abrir el menú de opciones de sonido
        mainMenuCanvas.SetActive(false);
        optionsCanvas.SetActive(false);
        soundOptionsCanvas.SetActive(true);


    }
}