using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class WinManager : MonoBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] private GameObject winCanvas;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Menu";
    [Tooltip("Nombre de la escena del siguiente nivel (debe estar en Build Settings).")]
    [SerializeField] private string nextLevelSceneName = "Level-2";

    private bool _shown;
    private bool _wired;

    private void Awake()
    {
        if (winCanvas == null)
        {
            // If the user created a WinScreen in the scene, use it automatically.
            GameObject byName = GameObject.Find("WinScreen") ?? GameObject.Find("Win Screen") ?? GameObject.Find("Win") ?? GameObject.Find("WinScreen (1)");
            if (byName != null) winCanvas = byName;
        }

        if (winCanvas != null)
            winCanvas.SetActive(false);

        Time.timeScale = 1f;
    }

    public void ShowWin()
    {
        if (_shown) return;
        _shown = true;

        EnsureEventSystem();

        if (winCanvas == null)
            winCanvas = BuildRuntimeWinCanvas();

        if (!_wired && winCanvas != null)
        {
            TryWireButtons(winCanvas);
            _wired = true;
        }

        if (winCanvas != null)
            winCanvas.SetActive(true);

        Time.timeScale = 0f;
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;

        string target = ResolveNextSceneName();
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("WinManager: no hay escena siguiente configurada.");
            return;
        }

        if (!IsSceneInBuildSettings(target))
        {
            Debug.LogError(
                $"WinManager: la escena \"{target}\" no está en Build Settings. " +
                "En Unity: File → Build Settings y añadí Level-2 a la lista.");
            return;
        }

        SceneManager.LoadScene(target, LoadSceneMode.Single);
    }

    private string ResolveNextSceneName()
    {
        if (!string.IsNullOrWhiteSpace(nextLevelSceneName))
            return nextLevelSceneName.Trim();

        // Fallback si el Inspector quedó vacío (evita LoadScene por índice).
        if (SceneManager.GetActiveScene().name == "Level-1")
            return "Level-2";

        return null;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
                continue;

            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return true;
        }

        return false;
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

    private GameObject BuildRuntimeWinCanvas()
    {
        EnsureEventSystem();

        GameObject root = new GameObject("WinScreen");
        root.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);

        root.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        panel.layer = root.layer;

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        content.layer = root.layer;

        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(420, 320);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 14;
        vlg.padding = new RectOffset(24, 24, 24, 24);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(content.transform, false);
        titleGo.layer = root.layer;
        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "¡Nivel completado!";
        title.fontSize = 42;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        CreateButton(content.transform, "Nivel 2", GoToNextLevel);
        CreateButton(content.transform, "Menu", GoToMainMenu);
        CreateButton(content.transform, "Exit", ExitGame);

        root.SetActive(false);
        return root;
    }

    private void TryWireButtons(GameObject canvasRoot)
    {
        // If user already wired buttons in Inspector, this does nothing harmful.
        Button[] buttons = canvasRoot.GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            string name = b.gameObject.name.ToLowerInvariant();

            // Try to detect by GameObject name first.
            if (name.Contains("nivel") || name.Contains("level") || name.Contains("next"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(GoToNextLevel);
                continue;
            }
            if (name.Contains("menu"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(GoToMainMenu);
                continue;
            }
            if (name.Contains("exit") || name.Contains("salir") || name.Contains("quit"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(ExitGame);
                continue;
            }

            // Fallback: detect by label text.
            string label = "";
            TMP_Text tmp = b.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) label = tmp.text;
            else
            {
                Text uiText = b.GetComponentInChildren<Text>(true);
                if (uiText != null) label = uiText.text;
            }

            string l = (label ?? "").ToLowerInvariant();
            if (l.Contains("nivel") || l.Contains("level") || l.Contains("next"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(GoToNextLevel);
            }
            else if (l.Contains("menu"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(GoToMainMenu);
            }
            else if (l.Contains("exit") || l.Contains("salir") || l.Contains("quit"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(ExitGame);
            }
        }
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = new GameObject(label + "Button");
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = parent.gameObject.layer;

        RectTransform rt = btnGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 54);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.92f);

        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        textGo.layer = btnGo.layer;

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}

