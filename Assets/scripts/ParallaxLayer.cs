using UnityEngine;

[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 = se queda fijo (muy lejos). 1 = se mueve igual que la cámara (pegado al gameplay).")]
    [SerializeField] private float parallaxX = 0.2f;

    [Tooltip("Normalmente 0 en platformers. Si querés parallax vertical, subilo un poco.")]
    [SerializeField] private float parallaxY = 0f;

    [Tooltip("Si está activo, conserva el Z original del layer.")]
    [SerializeField] private bool keepOriginalZ = true;

    private float _startZ;
    private Vector3 _lastCamPos;

    private void Awake()
    {
        _startZ = transform.position.z;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (cameraTransform != null)
            _lastCamPos = cameraTransform.position;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;
        Vector3 camDelta = camPos - _lastCamPos;

        Vector3 pos = transform.position;
        pos.x += camDelta.x * parallaxX;
        pos.y += camDelta.y * parallaxY;
        if (keepOriginalZ) pos.z = _startZ;
        transform.position = pos;

        _lastCamPos = camPos;
    }
}

