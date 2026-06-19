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
    [SerializeField] private TextMeshProUGUI narrativeText;
    [TextArea(2, 6)]
    [SerializeField] private string winNarrativeText;

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

        RefreshNextLevelButton(winCanvas);
        ApplyNarrativeText();

        if (winCanvas != null)
            winCanvas.SetActive(true);

        AbilityHUD.SetAllHidden(true);
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

        if (SceneManager.GetActiveScene().name == "Level-2")
            return "Level-3";

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

        narrativeText = CreateNarrativeText(content.transform);
        ApplyNarrativeText();

        CreateButton(content.transform, FormatNextLevelLabel(ResolveNextSceneName()), GoToNextLevel);
        CreateButton(content.transform, "Menu", GoToMainMenu);
        CreateButton(content.transform, "Exit", ExitGame);

        root.SetActive(false);
        return root;
    }

    private void RefreshNextLevelButton(GameObject canvasRoot)
    {
        if (canvasRoot == null)
            return;

        string next = ResolveNextSceneName();
        bool hasNext = !string.IsNullOrEmpty(next) && IsSceneInBuildSettings(next);
        string label = hasNext ? FormatNextLevelLabel(next) : string.Empty;

        Button[] buttons = canvasRoot.GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            if (!IsNextLevelButton(b))
                continue;

            b.gameObject.SetActive(hasNext);
            if (!hasNext)
                continue;

            SetButtonLabel(b, label);
        }
    }

    private static bool IsNextLevelButton(Button button)
    {
        string name = button.gameObject.name.ToLowerInvariant();
        if (name.Contains("nivel") || name.Contains("level") || name.Contains("next"))
            return true;

        string label = GetButtonLabel(button).ToLowerInvariant();
        return label.Contains("nivel") || label.Contains("level") || label.Contains("next");
    }

    private static string GetButtonLabel(Button button)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            return tmp.text ?? string.Empty;

        Text uiText = button.GetComponentInChildren<Text>(true);
        return uiText != null ? uiText.text ?? string.Empty : string.Empty;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = label;
    }

    private static string FormatNextLevelLabel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "Siguiente nivel";

        const string prefix = "Level-";
        if (sceneName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
            && int.TryParse(sceneName.Substring(prefix.Length), out int levelNumber))
        {
            return $"Nivel {levelNumber}";
        }

        return $"Ir a {sceneName}";
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
            string label = GetButtonLabel(b);

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

    private void ApplyNarrativeText()
    {
        if (string.IsNullOrWhiteSpace(winNarrativeText))
        {
            if (narrativeText != null)
                narrativeText.gameObject.SetActive(false);
            return;
        }

        if (narrativeText == null && winCanvas != null)
            narrativeText = FindNarrativeText(winCanvas.transform);

        if (narrativeText == null && winCanvas != null)
            narrativeText = CreateNarrativeText(winCanvas.transform);

        if (narrativeText == null)
            return;

        narrativeText.text = winNarrativeText.Trim();
        narrativeText.gameObject.SetActive(true);
    }

    private static TextMeshProUGUI FindNarrativeText(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name != "WinNarrative" && child.name != "NarrativeText")
                continue;

            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                return tmp;
        }

        return null;
    }

    private static TextMeshProUGUI CreateNarrativeText(Transform parent)
    {
        GameObject textGo = new GameObject("WinNarrative");
        textGo.transform.SetParent(parent, false);
        textGo.layer = parent.gameObject.layer;

        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -90f);
        rt.sizeDelta = new Vector2(920f, 180f);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.fontSize = 24f;
        tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        tmp.enableWordWrapping = true;
        tmp.richText = true;

        return tmp;
    }
}

