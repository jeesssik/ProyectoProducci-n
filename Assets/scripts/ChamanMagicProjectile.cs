using UnityEngine;

public class ChamanMagicProjectile : WizardProjectile
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer coreRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;

    [Header("Brillo")]
    [SerializeField] private float glowPulseSpeed = 14f;
    [SerializeField] private float glowMinAlpha = 0.22f;
    [SerializeField] private float glowMaxAlpha = 0.55f;
    [SerializeField] private float glowMinScale = 0.92f;
    [SerializeField] private float glowMaxScale = 1.12f;

    [Header("Estela")]
    [SerializeField] private float trailSpawnInterval = 0.04f;
    [SerializeField] private float afterimageLifetime = 0.42f;
    [SerializeField] private float afterimageAlpha = 0.48f;
    [SerializeField] private float afterimageScaleMultiplier = 0.9f;

    private float trailTimer;
    private Vector3 glowBaseScale;
    private Color glowBaseColor;
    private bool visualsReady;

    protected override void Awake()
    {
        base.Awake();

        if (coreRenderer == null)
        {
            Transform core = transform.Find("Core");
            if (core != null)
                coreRenderer = core.GetComponent<SpriteRenderer>();
        }

        if (glowRenderer == null)
        {
            Transform glow = transform.Find("Glow");
            if (glow != null)
                glowRenderer = glow.GetComponent<SpriteRenderer>();
        }

        if (glowRenderer != null)
        {
            glowBaseScale = glowRenderer.transform.localScale;
            glowBaseColor = glowRenderer.color;
            visualsReady = glowRenderer.sprite != null;
        }
    }

    protected override void OnLaunched(Vector2 direction)
    {
        trailTimer = 0f;
    }

    private void Update()
    {
        if (HasHit)
            return;

        AnimateGlow();
        UpdateTrail();
    }

    private void AnimateGlow()
    {
        if (!visualsReady || glowRenderer == null)
            return;

        float wave = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;

        Color c = glowBaseColor;
        c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, wave);
        glowRenderer.color = c;

        float scale = Mathf.Lerp(glowMinScale, glowMaxScale, wave);
        glowRenderer.transform.localScale = glowBaseScale * scale;
    }

    private void UpdateTrail()
    {
        if (coreRenderer == null || coreRenderer.sprite == null)
            return;

        trailTimer -= Time.deltaTime;
        if (trailTimer > 0f)
            return;

        trailTimer = trailSpawnInterval;
        SpawnAfterimage();
    }

    private void SpawnAfterimage()
    {
        Transform visual = coreRenderer.transform;

        GameObject ghost = new GameObject("MagicAfterimage");
        ghost.transform.SetPositionAndRotation(visual.position, transform.rotation);
        ghost.transform.localScale = visual.lossyScale * afterimageScaleMultiplier;

        SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = coreRenderer.sprite;
        ghostRenderer.flipX = coreRenderer.flipX;
        ghostRenderer.flipY = coreRenderer.flipY;
        ghostRenderer.sortingLayerID = coreRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = coreRenderer.sortingOrder - 1;

        Color c = coreRenderer.color;
        c.a = afterimageAlpha;
        ghostRenderer.color = c;

        MagicProjectileAfterimage fade = ghost.AddComponent<MagicProjectileAfterimage>();
        fade.Begin(afterimageLifetime);
    }
}
