using UnityEngine;

public class MagicProjectileAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private Vector3 startScale;
    private float lifetime;
    private float elapsed;

    public void Begin(float duration)
    {
        lifetime = Mathf.Max(0.05f, duration);
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            startColor = spriteRenderer.color;

        startScale = transform.localScale;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
            return;

        Color c = startColor;
        c.a = startColor.a * (1f - t);
        spriteRenderer.color = c;

        float scale = Mathf.Lerp(1f, 0.55f, t);
        transform.localScale = startScale * scale;
    }
}
