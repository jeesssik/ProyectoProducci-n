using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    public enum VerticalFollowMode
    {
        /// <summary>Hace seguimiento suave vertical arriba y abajo.</summary>
        Full,
        /// <summary>No mueve la cámara en Y hasta que el jugador sube por encima de la última altura útil.</summary>
        UpOnly
    }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [Tooltip("Offset respecto al jugador. Mantener Z = -10 en 2D.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Bounds (optional)")]
    [Tooltip("Límite izquierdo en mundo. La cámara no puede ir más atrás que este X.")]
    [SerializeField] private Transform leftBound;

    [Tooltip("Límite derecho en mundo. La cámara no puede avanzar más allá que este X.")]
    [SerializeField] private Transform rightBound;

    [Tooltip("Límite superior en mundo. La cámara no puede subir por encima de este Y.")]
    [SerializeField] private Transform topBound;

    [Tooltip("Opcional: BoxCollider2D del borde izquierdo (recomendado si el collider tiene Offset).")]
    [SerializeField] private BoxCollider2D leftBoundBox;

    [Tooltip("Opcional: BoxCollider2D del borde derecho (recomendado si el collider tiene Offset).")]
    [SerializeField] private BoxCollider2D rightBoundBox;

    [Tooltip("Si está activo, se toma el borde INTERIOR de cada collider como límite (Left=max.x, Right=min.x). Si no, usa el borde exterior.")]
    [SerializeField] private bool useInnerColliderEdgeAsLimit = true;

    [Tooltip("Si está activado, la cámara NO se mueve en X hasta que el jugador cruce por primera vez el centro horizontal de la pantalla (línea en el medio del encuadre).")]
    [SerializeField] private bool deferHorizontalUntilFirstCenterCross = true;

    [SerializeField] private VerticalFollowMode verticalMode = VerticalFollowMode.UpOnly;

    [Tooltip("Tiempo suave horizontal (solo eje X).")]
    [SerializeField] private float horizontalSmoothTime = 0.12f;

    [Tooltip("Tiempo suave cuando la cámara sube verticalmente.")]
    [SerializeField] private float verticalSmoothTimeUp = 0.18f;

    [Tooltip("Solo modo Full: tiempo suave al bajar.")]
    [SerializeField] private float verticalSmoothTimeDown = 0.2f;

    private float _velX;
    private float _velY;

    private float _cameraXWhileLocked;
    private float _screenCenterWorldX;
    private float _lastTargetWorldXForCross;
    private bool _horizontalFollowUnlocked;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        ResetSmoothState();
    }

    private void Start()
    {
        InitHorizontalDeferState();
    }

    private void OnEnable()
    {
        ResetSmoothState();
        InitHorizontalDeferState();
    }

    private void ResetSmoothState()
    {
        _velX = 0f;
        _velY = 0f;
    }

    private void InitHorizontalDeferState()
    {
        if (target != null)
            _lastTargetWorldXForCross = target.position.x;

        _cameraXWhileLocked = transform.position.x;
        _screenCenterWorldX = transform.position.x;
        _horizontalFollowUnlocked = !deferHorizontalUntilFirstCenterCross;
        _velX = 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Camera cam = GetComponent<Camera>();
        Vector3 current = transform.position;

        TryUnlockHorizontalFollow();

        float targetX = target.position.x + offset.x;
        float desiredAnchorY = target.position.y + offset.y;

        float newX = current.x;
        if (!_horizontalFollowUnlocked)
            newX = _cameraXWhileLocked;
        else if (horizontalSmoothTime <= 0f)
            newX = targetX;
        else
            newX = Mathf.SmoothDamp(current.x, targetX, ref _velX, horizontalSmoothTime);

        float newY = current.y;
        switch (verticalMode)
        {
            case VerticalFollowMode.Full:
                {
                    float chosenT = desiredAnchorY >= current.y ? verticalSmoothTimeUp : verticalSmoothTimeDown;
                    if (chosenT <= 0f)
                        newY = desiredAnchorY;
                    else
                        newY = Mathf.SmoothDamp(current.y, desiredAnchorY, ref _velY, Mathf.Max(0.0001f, chosenT));
                    break;
                }

            case VerticalFollowMode.UpOnly:
                // Solo corre Y cuando el ancla del jugador queda más arriba que la cámara.
                if (desiredAnchorY > current.y)
                {
                    if (verticalSmoothTimeUp <= 0f)
                        newY = desiredAnchorY;
                    else
                        newY = Mathf.SmoothDamp(current.y, desiredAnchorY, ref _velY, verticalSmoothTimeUp);
                }
                break;
        }

        // Clamp using visible extents (not camera center).
        // For orthographic camera: visible half-height = orthographicSize, half-width = orthographicSize * aspect.
        if (cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // Choose which side of each border collider is the limit line.
            // Inner edge faces INTO the level: Left=max.x, Right=min.x.
            // Outer edge is the opposite: Left=min.x, Right=max.x.
            float? leftEdge = null;
            if (leftBoundBox != null)
                leftEdge = useInnerColliderEdgeAsLimit ? leftBoundBox.bounds.max.x : leftBoundBox.bounds.min.x;
            else if (leftBound != null)
                leftEdge = leftBound.position.x;

            float? rightEdge = null;
            if (rightBoundBox != null)
                rightEdge = useInnerColliderEdgeAsLimit ? rightBoundBox.bounds.min.x : rightBoundBox.bounds.max.x;
            else if (rightBound != null)
                rightEdge = rightBound.position.x;

            float? leftLimit = leftEdge.HasValue ? leftEdge.Value + halfW : null;
            float? rightLimit = rightEdge.HasValue ? rightEdge.Value - halfW : null;

            // Auto-fix if Left/Right got swapped.
            if (leftLimit.HasValue && rightLimit.HasValue && leftLimit.Value > rightLimit.Value)
                (leftLimit, rightLimit) = (rightLimit, leftLimit);

            if (leftLimit.HasValue) newX = Mathf.Max(newX, leftLimit.Value);
            if (rightLimit.HasValue) newX = Mathf.Min(newX, rightLimit.Value);

            if (topBound != null)
                newY = Mathf.Min(newY, topBound.position.y - halfH);

            // If we are pinned to a horizontal border, re-lock X follow until the player crosses the screen center again.
            // This matches the "walk to the edge without camera follow" behavior at BOTH ends.
            const float edgeEpsilon = 0.0005f;
            if (_horizontalFollowUnlocked && deferHorizontalUntilFirstCenterCross)
            {
                bool pinnedLeft = leftLimit.HasValue && Mathf.Abs(newX - leftLimit.Value) <= edgeEpsilon;
                bool pinnedRight = rightLimit.HasValue && Mathf.Abs(newX - rightLimit.Value) <= edgeEpsilon;

                if (pinnedLeft || pinnedRight)
                {
                    _horizontalFollowUnlocked = false;
                    _cameraXWhileLocked = newX;
                    _screenCenterWorldX = newX;
                    _velX = 0f;
                }
            }
        }
        else
        {
            // Fallback: clamp by camera center.
            if (leftBound != null)
                newX = Mathf.Max(newX, leftBound.position.x);

            if (rightBound != null)
                newX = Mathf.Min(newX, rightBound.position.x);

            if (topBound != null)
                newY = Mathf.Min(newY, topBound.position.y);
        }

        transform.position = new Vector3(newX, newY, target.position.z + offset.z);

        _lastTargetWorldXForCross = target.position.x;
    }

    private void TryUnlockHorizontalFollow()
    {
        if (_horizontalFollowUnlocked || !deferHorizontalUntilFirstCenterCross)
            return;

        float px = target.position.x;

        float line = _screenCenterWorldX;

        bool crossedFromLeftToRight = _lastTargetWorldXForCross < line && px >= line;
        bool crossedFromRightToLeft = _lastTargetWorldXForCross > line && px <= line;

        if (crossedFromLeftToRight || crossedFromRightToLeft)
        {
            _horizontalFollowUnlocked = true;
            _velX = 0f;
        }
    }
}
