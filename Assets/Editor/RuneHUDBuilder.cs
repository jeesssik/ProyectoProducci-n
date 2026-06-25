#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RuneHUDBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/RuneHUD.prefab";

    [MenuItem("Tools/UI/Create Rune HUD Prefab")]
    public static void CreatePrefab()
    {
        GameObject root = BuildHierarchy();
        EnsureFolder("Assets/Prefabs/UI");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"Rune HUD prefab creado en {PrefabPath}. Arrástralo a la escena o al Player y asigna AbilityHUD en PlayerRuneAbilities.");
    }

    [MenuItem("Tools/UI/Create Rune HUD In Scene")]
    public static void CreateInScene()
    {
        GameObject root = BuildHierarchy();
        Undo.RegisterCreatedObjectUndo(root, "Create Rune HUD");
        Selection.activeGameObject = root;
    }

    public static GameObject BuildHierarchy()
    {
        GameObject root = new GameObject("RuneHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(AbilityHUD));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.localScale = Vector3.one;
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        GameObject panel = CreatePanel(root.transform);
        CreateSlot(panel.transform, RuneType.Yellow, "Runa amarilla", "Dash en el suelo con Shift.");
        CreateSlot(panel.transform, RuneType.Green, "Runa verde", "Salto extra en el aire.");
        CreateSlot(panel.transform, RuneType.Celeste, "Runa celeste", "Esquiva hacia atrás con Z.");
        CreateSlot(panel.transform, RuneType.Red, "Runa roja — Represalia feroz", "Tras esquivar con Z, el siguiente ataque causa daño devastador.");

        AbilityHUD hud = root.GetComponent<AbilityHUD>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("panelRoot").objectReferenceValue = panel.GetComponent<RectTransform>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("AbilityPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-16f, -16f);
        rt.sizeDelta = new Vector2(280f, 120f);

        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return panel;
    }

    private static void CreateSlot(Transform parent, RuneType runeType, string title, string description)
    {
        GameObject slot = new GameObject($"Slot_{runeType}", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        slot.transform.SetParent(parent, false);
        slot.SetActive(false);

        LayoutElement slotLayout = slot.GetComponent<LayoutElement>();
        slotLayout.minHeight = 56f;
        slotLayout.preferredHeight = 56f;

        HorizontalLayoutGroup row = slot.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        CreateIconBox(slot.transform);
        CreateTexts(slot.transform, title, description);
    }

    private static void CreateIconBox(Transform parent)
    {
        GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        iconBox.transform.SetParent(parent, false);

        LayoutElement iconLayout = iconBox.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 56f;
        iconLayout.preferredHeight = 56f;

        iconBox.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = icon.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(6f, 6f);
        iconRt.offsetMax = new Vector2(-6f, -6f);
        icon.GetComponent<Image>().preserveAspect = true;

        GameObject cooldown = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
        cooldown.transform.SetParent(iconBox.transform, false);
        RectTransform cdRt = cooldown.GetComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;

        Image cdImage = cooldown.GetComponent<Image>();
        cdImage.type = Image.Type.Filled;
        cdImage.fillMethod = Image.FillMethod.Vertical;
        cdImage.fillOrigin = (int)Image.OriginVertical.Top;
        cdImage.color = new Color(0f, 0f, 0f, 0.72f);
        cdImage.fillAmount = 0f;
        cdImage.preserveAspect = true;
    }

    private static void CreateTexts(Transform parent, string title, string description)
    {
        GameObject texts = new GameObject("Texts", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        texts.transform.SetParent(parent, false);

        LayoutElement textLayout = texts.GetComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.minHeight = 56f;

        VerticalLayoutGroup col = texts.GetComponent<VerticalLayoutGroup>();
        col.spacing = 2f;
        col.childAlignment = TextAnchor.MiddleLeft;
        col.childControlWidth = true;
        col.childControlHeight = true;
        col.childForceExpandWidth = true;
        col.childForceExpandHeight = false;

        CreateTmp(texts.transform, "Title", title, 15f, FontStyles.Bold);
        CreateTmp(texts.transform, "Description", description, 12f, FontStyles.Normal);
    }

    private static void CreateTmp(Transform parent, string objectName, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject textGo = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;

        LayoutElement layout = textGo.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
