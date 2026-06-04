using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class RuneDropLaunch : MonoBehaviour
{
    [Header("Arco al salir del enemigo")]
    [SerializeField] private float arcDuration = 0.4f;
    [SerializeField] private float arcHeight = 0.5f;
    [SerializeField] private float arcHorizontalDistance = 0.25f;
    [SerializeField] private bool randomizeHorizontalDirection = true;

    [Header("Aterrizaje")]
    [SerializeField] private float landingYOffset = 0.2f;
    [SerializeField] private float groundRayStartHeight = 1.5f;
    [SerializeField] private float groundRayDistance = 2.5f;
    [SerializeField] private float maxSnapBelowSpawn = 1.2f;
    [SerializeField] private float maxSnapAboveSpawn = 0.4f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Visual")]
    [SerializeField] private int sortingOrder = 50;

    private Collider2D _pickupCollider;

    public bool CanPickup { get; private set; }

    private void Awake()
    {
        _pickupCollider = GetComponent<Collider2D>();
        SetPickupEnabled(false);
        BoostSortingOrder();
    }

    public void BeginDrop(Vector3 startPosition)
    {
        StopAllCoroutines();
        StartCoroutine(ArcRoutine(startPosition));
    }

    private IEnumerator ArcRoutine(Vector3 rawStart)
    {
        SetPickupEnabled(false);

        Vector3 from = SnapToGroundNear(rawStart);
        transform.position = from;

        float direction = randomizeHorizontalDirection && Random.value > 0.5f ? 1f : -1f;
        Vector3 candidateEnd = from + new Vector3(direction * arcHorizontalDistance, 0f, 0f);
        Vector3 end = HasGroundBelow(candidateEnd, from.y) ? SnapToGroundNear(candidateEnd, from.y) : from;

        float elapsed = 0f;
        while (elapsed < arcDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcDuration);
            float height = 4f * arcHeight * t * (1f - t);
            Vector3 flat = Vector3.Lerp(from, end, t);
            transform.position = flat + Vector3.up * height;
            yield return null;
        }

        transform.position = end;
        SetPickupEnabled(true);

        Debug.Log($"Runa lista para recoger en {end} (Q).");
    }

    private bool HasGroundBelow(Vector3 worldPosition, float referenceY)
    {
        Vector2 origin = new Vector2(worldPosition.x, worldPosition.y + 0.15f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRayDistance, groundLayers);

        if (hit.collider == null)
            return false;

        float snappedY = hit.point.y + landingYOffset;
        return snappedY >= referenceY - maxSnapBelowSpawn && snappedY <= referenceY + maxSnapAboveSpawn;
    }

    private Vector3 SnapToGroundNear(Vector3 worldPosition, float? referenceY = null)
    {
        float refY = referenceY ?? worldPosition.y;

        Vector2 origin = new Vector2(worldPosition.x, worldPosition.y + groundRayStartHeight);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRayStartHeight + groundRayDistance, groundLayers);

        if (hit.collider == null)
            return new Vector3(worldPosition.x, refY + landingYOffset, worldPosition.z);

        float y = hit.point.y + landingYOffset;
        y = Mathf.Clamp(y, refY - maxSnapBelowSpawn, refY + maxSnapAboveSpawn);

        return new Vector3(worldPosition.x, y, worldPosition.z);
    }

    private void BoostSortingOrder()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingOrder = sortingOrder;
        }
    }

    private void SetPickupEnabled(bool enabled)
    {
        CanPickup = enabled;

        if (_pickupCollider != null)
            _pickupCollider.enabled = enabled;
    }
}
