using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RuneGlowPulse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.7f;
    [SerializeField] private float minScaleMultiplier = 1.15f;
    [SerializeField] private float maxScaleMultiplier = 1.5f;

    private Vector3 _baseScale;
    private Color _baseColor;

    private void Awake()
    {
        if (glowRenderer == null)
            glowRenderer = GetComponent<SpriteRenderer>();

        if (glowRenderer.sprite == null && transform.parent != null)
        {
            Transform visualTransform = transform.parent.Find("RuneVisual");
            if (visualTransform != null)
            {
                SpriteRenderer visual = visualTransform.GetComponent<SpriteRenderer>();
                if (visual != null && visual.sprite != null)
                    glowRenderer.sprite = visual.sprite;
            }
        }

        _baseScale = transform.localScale;
        _baseColor = glowRenderer.color;
    }

    private void Update()
    {
        if (glowRenderer.sprite == null)
            return;

        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        Color c = _baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        glowRenderer.color = c;

        float scale = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, wave);
        transform.localScale = _baseScale * scale;
    }
}
