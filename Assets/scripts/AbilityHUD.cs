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

    [Header("Iconos (opcional, se autodetectan en Play)")]
    [SerializeField] private Sprite yellowRuneIcon;
    [SerializeField] private Sprite greenRuneIcon;
    [SerializeField] private Sprite celesteRuneIcon;

    [Header("Layout")]
    [SerializeField] private Vector2 panelPosition = new Vector2(24f, 24f);
    [SerializeField] private float slotSize = 56f;
    [SerializeField] private float slotSpacing = 12f;

    private readonly List<SlotRefs> _slots = new List<SlotRefs>();
    private RectTransform _panelRoot;
    private bool _built;

    private void Awake()
    {
        AutoLoadIcons();
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
    }

    public void RefreshSlots()
    {
        BuildIfNeeded();

        foreach (SlotRefs slot in _slots)
        {
            if (slot.root != null)
                slot.root.SetActive(RuneProgress.IsUnlocked(slot.runeType));
        }
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

        GameObject canvasGo = new GameObject("AbilityHUDCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = new GameObject("AbilityPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);

        _panelRoot = panelGo.AddComponent<RectTransform>();
        _panelRoot.anchorMin = new Vector2(0f, 0f);
        _panelRoot.anchorMax = new Vector2(0f, 0f);
        _panelRoot.pivot = new Vector2(0f, 0f);
        _panelRoot.anchoredPosition = panelPosition;
        _panelRoot.sizeDelta = new Vector2(220f, slotSize);

        HorizontalLayoutGroup layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateSlot(RuneType.Yellow, GetIcon(RuneType.Yellow));
        CreateSlot(RuneType.Green, GetIcon(RuneType.Green));
        CreateSlot(RuneType.Celeste, GetIcon(RuneType.Celeste));
    }

    private void CreateSlot(RuneType runeType, Sprite iconSprite)
    {
        GameObject slotGo = new GameObject($"Slot_{runeType}");
        slotGo.transform.SetParent(_panelRoot, false);

        RectTransform slotRt = slotGo.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement layout = slotGo.AddComponent<LayoutElement>();
        layout.preferredWidth = slotSize;
        layout.preferredHeight = slotSize;

        Image bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        RectTransform iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(6f, 6f);
        iconRt.offsetMax = new Vector2(-6f, -6f);

        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = iconSprite;
        icon.preserveAspect = true;

        GameObject cdGo = new GameObject("Cooldown");
        cdGo.transform.SetParent(slotGo.transform, false);
        RectTransform cdRt = cdGo.AddComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;

        Image cooldown = cdGo.AddComponent<Image>();
        cooldown.sprite = iconSprite;
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Top;
        cooldown.color = new Color(0f, 0f, 0f, 0.72f);
        cooldown.fillAmount = 0f;

        _slots.Add(new SlotRefs
        {
            runeType = runeType,
            root = slotGo,
            icon = icon,
            cooldownOverlay = cooldown
        });

        slotGo.SetActive(false);
    }

    private Sprite GetIcon(RuneType runeType)
    {
        switch (runeType)
        {
            case RuneType.Yellow: return yellowRuneIcon;
            case RuneType.Green: return greenRuneIcon;
            case RuneType.Celeste: return celesteRuneIcon;
            default: return null;
        }
    }

    private void AutoLoadIcons()
    {
        if (yellowRuneIcon != null && greenRuneIcon != null && celesteRuneIcon != null)
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
        }
    }
}
