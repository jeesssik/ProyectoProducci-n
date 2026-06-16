using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class CollapsingBridge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Tilemap tilemap;

    [Header("Tiempos")]
    [Tooltip("Instante mínimo sobre el puente antes del primer aviso.")]
    [SerializeField] private float minStandTime = 0.08f;
    [Tooltip("Pausa entre cada aviso de fade.")]
    [SerializeField] private float pauseBetweenWarnings = 0.12f;
    [Tooltip("Fade final antes de quitar el collider (tiempo para saltar).")]
    [SerializeField] private float finalFallDuration = 0.55f;

    [Header("Aviso (fade)")]
    [SerializeField] private int warningFadeCount = 2;
    [SerializeField] private float warningFadeDuration = 0.32f;
    [SerializeField] private float warningMinAlpha = 0.35f;

    [Header("Detección")]
    [SerializeField] private float feetRayDistance = 0.4f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private TilemapCollider2D _tilemapCollider;
    private CompositeCollider2D _compositeCollider;
    private TilemapRenderer _tilemapRenderer;
    private Rigidbody2D _rigidbody;

    private Color _baseColor;
    private bool _isCollapsing;
    private bool _hasCollapsed;
    private float _standTimer;

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        _tilemapCollider = GetComponent<TilemapCollider2D>();
        _compositeCollider = GetComponent<CompositeCollider2D>();
        _tilemapRenderer = GetComponent<TilemapRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();

        if (tilemap != null)
            _baseColor = tilemap.color;
        else
            _baseColor = Color.white;

        ApplyAlpha(_baseColor.a);

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (_hasCollapsed || _isCollapsing || player == null)
            return;

        if (IsPlayerStandingOnThisBridge())
        {
            _standTimer += Time.deltaTime;
            if (_standTimer >= minStandTime)
                StartCoroutine(CollapseRoutine());
        }
        else
        {
            _standTimer = 0f;
        }
    }

    private bool IsPlayerStandingOnThisBridge()
    {
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            return false;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null && controller.IsDead())
            return false;

        Bounds bounds = playerCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, feetRayDistance, groundLayers);
        if (hit.collider == null)
            return false;

        return hit.collider.gameObject == gameObject;
    }

    private IEnumerator CollapseRoutine()
    {
        _isCollapsing = true;

        for (int i = 0; i < warningFadeCount; i++)
        {
            if (!IsPlayerStandingOnThisBridge())
            {
                CancelSequence();
                yield break;
            }

            yield return WarningFadePulse();

            if (i < warningFadeCount - 1 && pauseBetweenWarnings > 0f)
                yield return new WaitForSeconds(pauseBetweenWarnings);
        }

        yield return FinalFallFade();

        HideBridge();
        _hasCollapsed = true;
        _isCollapsing = false;
    }

    private IEnumerator WarningFadePulse()
    {
        float halfDuration = warningFadeDuration * 0.5f;
        float fromAlpha = _baseColor.a;
        float toAlpha = warningMinAlpha * fromAlpha;

        yield return FadeAlpha(fromAlpha, toAlpha, halfDuration);
        yield return FadeAlpha(toAlpha, fromAlpha, halfDuration);

        ApplyAlpha(fromAlpha);
    }

    private IEnumerator FinalFallFade()
    {
        float startAlpha = _baseColor.a;
        float elapsed = 0f;

        while (elapsed < finalFallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / finalFallDuration);
            ApplyAlpha(Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        ApplyAlpha(0f);
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            ApplyAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        ApplyAlpha(to);
    }

    private void ApplyAlpha(float alpha)
    {
        if (tilemap == null)
            return;

        Color c = _baseColor;
        c.a = alpha;
        tilemap.color = c;
    }

    private void CancelSequence()
    {
        StopAllCoroutines();
        ApplyAlpha(_baseColor.a);
        _isCollapsing = false;
        _standTimer = 0f;
    }

    private void HideBridge()
    {
        if (tilemap != null)
            tilemap.ClearAllTiles();

        if (_tilemapRenderer != null)
            _tilemapRenderer.enabled = false;

        if (_tilemapCollider != null)
            _tilemapCollider.enabled = false;

        if (_compositeCollider != null)
            _compositeCollider.enabled = false;

        if (_rigidbody != null)
            _rigidbody.simulated = false;
    }
}
