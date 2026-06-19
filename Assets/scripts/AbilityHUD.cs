using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AbilityHUD : MonoBehaviour
{
    [System.Serializable]
    private class SlotRefs
    {
        public RuneType runeType;
        public GameObject root;
        public Image icon;
        public Image cooldownOverlay;
    }

    [Header("Iconos")]
    [SerializeField] private RuneIconLibrary iconLibrary;
    [SerializeField] private Sprite yellowRuneIcon;
    [SerializeField] private Sprite greenRuneIcon;
    [SerializeField] private Sprite celesteRuneIcon;
    [SerializeField] private Sprite redRuneIcon;

    [Header("Layout")]
    [SerializeField] private Vector2 panelPosition = new Vector2(-16f, -16f);
    [SerializeField] private float slotSize = 52f;
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private float panelWidth = 280f;

    [Header("Aviso al desbloquear")]
    [SerializeField] private float unlockToastDuration = 3f;

    private readonly List<SlotRefs> _slots = new List<SlotRefs>();
    private RectTransform _panelRoot;
    private RectTransform _toastRoot;
    private Text _toastTitle;
    private Text _toastDescription;
    private Canvas _canvas;
    private bool _built;
    private Coroutine _toastRoutine;
    private Font _uiFont;
    private bool _forcedHidden;

    public void SetGameplayOverlayHidden(bool hidden)
    {
        _forcedHidden = hidden;
        BuildIfNeeded();

        if (_canvas != null)
            _canvas.gameObject.SetActive(!hidden);
    }

    public static void SetAllHidden(bool hidden)
    {
        AbilityHUD[] huds = FindObjectsByType<AbilityHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AbilityHUD hud in huds)
            hud.SetGameplayOverlayHidden(hidden);
    }

    private void Awake()
    {
        ResolveIcons();
        BuildIfNeeded();
        RefreshSlots();
    }

    private void OnEnable()
    {
        RuneProgress.OnRuneUnlocked += HandleRuneUnlocked;
        RefreshSlots();
    }

    private void OnDisable()
    {
        RuneProgress.OnRuneUnlocked -= HandleRuneUnlocked;
    }

    private void HandleRuneUnlocked(RuneType rune)
    {
        RefreshSlots();
        ShowUnlockToast(rune);
    }

    public void RefreshSlots()
    {
        BuildIfNeeded();

        if (_forcedHidden)
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
            return;
        }

        bool anyUnlocked = false;

        foreach (SlotRefs slot in _slots)
        {
            if (slot.root == null)
                continue;

            Sprite icon = GetIcon(slot.runeType);
            ApplyIcon(slot, icon);

            bool unlocked = RuneProgress.IsUnlocked(slot.runeType);
            slot.root.SetActive(unlocked);
            anyUnlocked |= unlocked;
        }

        if (_panelRoot != null)
            _panelRoot.gameObject.SetActive(anyUnlocked);
    }

    public void StartCooldown(RuneType rune, float duration)
    {
        SetCooldownNormalized(rune, 1f);
    }

    public void SetCooldownNormalized(RuneType rune, float normalized)
    {
        SlotRefs slot = _slots.Find(s => s.runeType == rune);
        if (slot?.cooldownOverlay == null)
            return;

        slot.cooldownOverlay.fillAmount = Mathf.Clamp01(normalized);
    }

    private void BuildIfNeeded()
    {
        if (_built)
            return;

        _built = true;
        _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGo = new GameObject("AbilityHUDCanvas");

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = new GameObject("AbilityPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);

        _panelRoot = panelGo.AddComponent<RectTransform>();
        _panelRoot.anchorMin = new Vector2(1f, 1f);
        _panelRoot.anchorMax = new Vector2(1f, 1f);
        _panelRoot.pivot = new Vector2(1f, 1f);
        _panelRoot.anchoredPosition = panelPosition;
        _panelRoot.sizeDelta = new Vector2(panelWidth, 120f);

        Image panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.55f);

        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateSlot(RuneType.Yellow);
        CreateSlot(RuneType.Green);
        CreateSlot(RuneType.Celeste);
        CreateSlot(RuneType.Red);
        BuildToast(canvasGo.transform);
    }

    private void CreateSlot(RuneType runeType)
    {
        GameObject slotGo = new GameObject($"Slot_{runeType}");
        slotGo.transform.SetParent(_panelRoot, false);

        RectTransform slotRt = slotGo.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(panelWidth - 20f, slotSize);

        LayoutElement layout = slotGo.AddComponent<LayoutElement>();
        layout.minHeight = slotSize;
        layout.preferredHeight = slotSize;

        HorizontalLayoutGroup row = slotGo.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        GameObject iconBoxGo = new GameObject("IconBox");
        iconBoxGo.transform.SetParent(slotGo.transform, false);

        RectTransform iconBoxRt = iconBoxGo.AddComponent<RectTransform>();
        iconBoxRt.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement iconLayout = iconBoxGo.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = slotSize;
        iconLayout.preferredHeight = slotSize;

        Image bg = iconBoxGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconBoxGo.transform, false);
        RectTransform iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(6f, 6f);
        iconRt.offsetMax = new Vector2(-6f, -6f);

        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.color = Color.white;

        GameObject cdGo = new GameObject("Cooldown");
        cdGo.transform.SetParent(iconBoxGo.transform, false);
        RectTransform cdRt = cdGo.AddComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;

        Image cooldown = cdGo.AddComponent<Image>();
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Top;
        cooldown.color = new Color(0f, 0f, 0f, 0.72f);
        cooldown.fillAmount = 0f;
        cooldown.preserveAspect = true;

        GameObject textColGo = new GameObject("Texts");
        textColGo.transform.SetParent(slotGo.transform, false);

        LayoutElement textLayout = textColGo.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.minHeight = slotSize;

        VerticalLayoutGroup textCol = textColGo.AddComponent<VerticalLayoutGroup>();
        textCol.spacing = 2f;
        textCol.childAlignment = TextAnchor.MiddleLeft;
        textCol.childControlWidth = true;
        textCol.childControlHeight = true;
        textCol.childForceExpandWidth = true;
        textCol.childForceExpandHeight = false;

        Text title = CreateText(textColGo.transform, GetRuneTitle(runeType), 15, FontStyle.Bold);
        CreateText(textColGo.transform, GetRuneDescription(runeType), 12, FontStyle.Normal);

        SlotRefs slot = new SlotRefs
        {
            runeType = runeType,
            root = slotGo,
            icon = icon,
            cooldownOverlay = cooldown
        };

        ApplyIcon(slot, GetIcon(runeType));
        _slots.Add(slot);
        slotGo.SetActive(false);
    }

    private Text CreateText(Transform parent, string content, int fontSize, FontStyle style)
    {
        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(parent, false);

        Text text = textGo.AddComponent<Text>();
        text.font = _uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;

        LayoutElement layout = textGo.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;

        return text;
    }

    private void BuildToast(Transform canvasTransform)
    {
        GameObject toastGo = new GameObject("UnlockToast");
        toastGo.transform.SetParent(canvasTransform, false);

        _toastRoot = toastGo.AddComponent<RectTransform>();
        _toastRoot.anchorMin = new Vector2(0.5f, 1f);
        _toastRoot.anchorMax = new Vector2(0.5f, 1f);
        _toastRoot.pivot = new Vector2(0.5f, 1f);
        _toastRoot.anchoredPosition = new Vector2(0f, -24f);
        _toastRoot.sizeDelta = new Vector2(360f, 88f);

        Image toastBg = toastGo.AddComponent<Image>();
        toastBg.color = new Color(0f, 0f, 0f, 0.78f);

        VerticalLayoutGroup toastLayout = toastGo.AddComponent<VerticalLayoutGroup>();
        toastLayout.padding = new RectOffset(14, 14, 10, 10);
        toastLayout.spacing = 4f;
        toastLayout.childAlignment = TextAnchor.MiddleCenter;

        _toastTitle = CreateText(toastGo.transform, string.Empty, 18, FontStyle.Bold);
        _toastTitle.alignment = TextAnchor.MiddleCenter;

        _toastDescription = CreateText(toastGo.transform, string.Empty, 13, FontStyle.Normal);
        _toastDescription.alignment = TextAnchor.MiddleCenter;

        toastGo.SetActive(false);
    }

    private void ShowUnlockToast(RuneType rune)
    {
        if (_toastRoot == null)
            return;

        _toastTitle.text = $"¡{GetRuneTitle(rune)} obtenida!";
        _toastDescription.text = GetRuneDescription(rune);
        _toastRoot.gameObject.SetActive(true);

        if (_toastRoutine != null)
            StopCoroutine(_toastRoutine);

        _toastRoutine = StartCoroutine(HideToastAfterDelay());
    }

    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSecondsRealtime(unlockToastDuration);
        if (_toastRoot != null)
            _toastRoot.gameObject.SetActive(false);
        _toastRoutine = null;
    }

    private void ApplyIcon(SlotRefs slot, Sprite iconSprite)
    {
        if (slot?.icon == null || iconSprite == null)
            return;

        slot.icon.sprite = iconSprite;
        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.sprite = iconSprite;
    }

    private Sprite GetIcon(RuneType runeType)
    {
        switch (runeType)
        {
            case RuneType.Yellow: return yellowRuneIcon;
            case RuneType.Green: return greenRuneIcon;
            case RuneType.Celeste: return celesteRuneIcon;
            case RuneType.Red: return redRuneIcon;
            default: return null;
        }
    }

    private void ResolveIcons()
    {
        if (iconLibrary == null)
            iconLibrary = RuneIconLibrary.Instance;

        if (iconLibrary != null)
        {
            if (yellowRuneIcon == null) yellowRuneIcon = iconLibrary.GetIcon(RuneType.Yellow);
            if (greenRuneIcon == null) greenRuneIcon = iconLibrary.GetIcon(RuneType.Green);
            if (celesteRuneIcon == null) celesteRuneIcon = iconLibrary.GetIcon(RuneType.Celeste);
            if (redRuneIcon == null) redRuneIcon = iconLibrary.GetIcon(RuneType.Red);
        }

        if (yellowRuneIcon != null && greenRuneIcon != null && celesteRuneIcon != null && redRuneIcon != null)
            return;

        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
                continue;

            if (yellowRuneIcon == null && sprite.name == "yellow_rune_5_0")
                yellowRuneIcon = sprite;
            else if (greenRuneIcon == null && sprite.name == "green_rune_2_0")
                greenRuneIcon = sprite;
            else if (celesteRuneIcon == null && sprite.name == "blue_rune_1_0")
                celesteRuneIcon = sprite;
            else if (redRuneIcon == null && sprite.name == "red_rune_1_0")
                redRuneIcon = sprite;
        }
    }

    private static string GetRuneTitle(RuneType rune)
    {
        switch (rune)
        {
            case RuneType.Yellow: return "Runa amarilla";
            case RuneType.Green: return "Runa verde";
            case RuneType.Celeste: return "Runa celeste";
            case RuneType.Red: return "Runa roja — Represalia feroz";
            default: return "Runa";
        }
    }

    private static string GetRuneDescription(RuneType rune)
    {
        switch (rune)
        {
            case RuneType.Yellow: return "Dash en el suelo con Shift.";
            case RuneType.Green: return "Salto extra en el aire.";
            case RuneType.Celeste: return "Esquiva hacia atrás con Z.";
            case RuneType.Red: return "Tras esquivar con Z, el siguiente ataque causa daño devastador.";
            default: return string.Empty;
        }
    }
}
