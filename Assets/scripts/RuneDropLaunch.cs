using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class RuneDropLaunch : MonoBehaviour
{
    [Header("Arco al salir del enemigo")]
    [SerializeField] private float arcDuration = 0.55f;
    [SerializeField] private float arcHeight = 0.85f;
    [SerializeField] private float arcHorizontalDistance = 0.65f;
    [SerializeField] private bool randomizeHorizontalDirection = true;

    [Header("Aterrizaje")]
    [SerializeField] private float landingYOffset = 0.25f;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 4f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private Collider2D _pickupCollider;

    public bool CanPickup { get; private set; }

    private void Awake()
    {
        _pickupCollider = GetComponent<Collider2D>();
        SetPickupEnabled(false);
    }

    public void BeginDrop(Vector3 startPosition)
    {
        StopAllCoroutines();
        StartCoroutine(ArcRoutine(startPosition));
    }

    private IEnumerator ArcRoutine(Vector3 rawStart)
    {
        SetPickupEnabled(false);

        Vector3 from = SnapToGround(rawStart);
        transform.position = from;

        float direction = randomizeHorizontalDirection && Random.value > 0.5f ? 1f : -1f;
        Vector3 candidateEnd = from + new Vector3(direction * arcHorizontalDistance, 0f, 0f);
        Vector3 end = HasGroundBelow(candidateEnd) ? SnapToGround(candidateEnd) : from;

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
    }

    private bool HasGroundBelow(Vector3 worldPosition)
    {
        Vector2 origin = new Vector2(worldPosition.x, worldPosition.y + 0.25f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRayDistance, groundLayers);
        return hit.collider != null;
    }

    private Vector3 SnapToGround(Vector3 worldPosition)
    {
        Vector2 origin = new Vector2(worldPosition.x, worldPosition.y + groundRayStartHeight);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundRayStartHeight + groundRayDistance, groundLayers);

        if (hit.collider == null)
            return worldPosition;

        return new Vector3(worldPosition.x, hit.point.y + landingYOffset, worldPosition.z);
    }

    private void SetPickupEnabled(bool enabled)
    {
        CanPickup = enabled;

        if (_pickupCollider != null)
            _pickupCollider.enabled = enabled;
    }
}
