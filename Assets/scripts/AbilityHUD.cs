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

    [Header("Escena")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private List<SlotRefs> sceneSlots = new List<SlotRefs>();

    [Header("Iconos")]
    [SerializeField] private RuneIconLibrary iconLibrary;
    [SerializeField] private Sprite yellowRuneIcon;
    [SerializeField] private Sprite greenRuneIcon;
    [SerializeField] private Sprite celesteRuneIcon;
    [SerializeField] private Sprite redRuneIcon;

    private readonly List<SlotRefs> _slots = new List<SlotRefs>();
    private Canvas _canvas;
    private bool _bound;
    private bool _forcedHidden;

    public void SetGameplayOverlayHidden(bool hidden)
    {
        _forcedHidden = hidden;
        TryBindFromScene();

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
        EnsureVisibleScale();
        ResolveIcons();

        if (!TryBindFromScene())
        {
            Debug.LogWarning(
                $"[{nameof(AbilityHUD)}] No se encontró el panel de runas en '{name}'. " +
                "Usa el prefab RuneHUD en la escena; este componente no crea UI en runtime.",
                this);
            enabled = false;
            return;
        }

        HideUnlockToasts();
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
        if (!_bound)
            return;

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

            ApplyIcon(slot, GetIcon(slot.runeType));

            bool unlocked = RuneProgress.IsUnlocked(slot.runeType);
            slot.root.SetActive(unlocked);
            anyUnlocked |= unlocked;
        }

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(anyUnlocked);
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

    private bool TryBindFromScene()
    {
        if (_bound)
            return true;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (panelRoot == null)
            panelRoot = transform.Find("AbilityPanel") as RectTransform;

        _slots.Clear();

        if (sceneSlots != null && sceneSlots.Count > 0)
        {
            foreach (SlotRefs slot in sceneSlots)
            {
                if (slot?.root != null)
                    _slots.Add(slot);
            }
        }
        else if (panelRoot != null)
        {
            BindSlot(RuneType.Yellow);
            BindSlot(RuneType.Green);
            BindSlot(RuneType.Celeste);
            BindSlot(RuneType.Red);
        }

        _bound = _slots.Count > 0;
        return _bound;
    }

    private void BindSlot(RuneType runeType)
    {
        Transform slotTransform = panelRoot.Find($"Slot_{runeType}");
        if (slotTransform == null)
            return;

        _slots.Add(new SlotRefs
        {
            runeType = runeType,
            root = slotTransform.gameObject,
            icon = slotTransform.Find("IconBox/Icon")?.GetComponent<Image>(),
            cooldownOverlay = slotTransform.Find("IconBox/Cooldown")?.GetComponent<Image>()
        });
    }

    private void HideUnlockToasts()
    {
        Transform toast = transform.Find("UnlockToast");
        if (toast != null)
            toast.gameObject.SetActive(false);

        foreach (RuneType runeType in new[] { RuneType.Yellow, RuneType.Green, RuneType.Celeste, RuneType.Red })
        {
            Transform perRune = transform.Find($"UnlockToast_{runeType}");
            if (perRune != null)
                perRune.gameObject.SetActive(false);
        }
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

    private void EnsureVisibleScale()
    {
        if (transform.localScale == Vector3.zero)
            transform.localScale = Vector3.one;
    }
}
