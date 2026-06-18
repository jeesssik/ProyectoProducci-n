using UnityEngine;

[DisallowMultipleComponent]
public class InfiniteSpriteTiler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("Tiling")]
    [Tooltip("Cantidad de copias a cada lado (1 = 3 sprites en total).")]
    [SerializeField] private int copiesPerSide = 1;

    [Tooltip("Si está activo, alinea las copias a la posición actual del centro (funciona bien con parallax).")]
    [SerializeField] private bool followCenterObject = true;

    private Transform[] _tiles;
    private float _tileWorldWidth;
    private Vector3 _centerStartPos;

    private void Awake()
    {
        if (sourceRenderer == null)
            sourceRenderer = GetComponent<SpriteRenderer>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            enabled = false;
            return;
        }

        _centerStartPos = transform.position;
        _tileWorldWidth = sourceRenderer.bounds.size.x;
        copiesPerSide = Mathf.Clamp(copiesPerSide, 1, 8);

        CreateTiles();
    }

    private void CreateTiles()
    {
        int total = copiesPerSide * 2 + 1;
        _tiles = new Transform[total];
        _tiles[copiesPerSide] = transform;

        for (int i = 1; i <= copiesPerSide; i++)
        {
            _tiles[copiesPerSide + i] = CreateClone(i);
            _tiles[copiesPerSide - i] = CreateClone(-i);
        }
    }

    private Transform CreateClone(int xOffsetTiles)
    {
        GameObject go = new GameObject($"{gameObject.name}_tile_{xOffsetTiles:+#;-#;0}");
        go.transform.SetParent(transform.parent, worldPositionStays: true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sourceRenderer.sprite;
        sr.flipX = sourceRenderer.flipX;
        sr.flipY = sourceRenderer.flipY;
        sr.color = sourceRenderer.color;
        sr.sortingLayerID = sourceRenderer.sortingLayerID;
        sr.sortingOrder = sourceRenderer.sortingOrder;
        sr.drawMode = sourceRenderer.drawMode;
        sr.size = sourceRenderer.size;
        sr.maskInteraction = sourceRenderer.maskInteraction;

        Vector3 pos = transform.position;
        pos.x += _tileWorldWidth * xOffsetTiles;
        go.transform.position = pos;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = transform.localScale;

        return go.transform;
    }

    private void LateUpdate()
    {
        if (_tiles == null || _tiles.Length == 0)
            return;

        if (cameraTransform == null)
            return;

        Vector3 centerPos = followCenterObject ? transform.position : _centerStartPos;

        for (int i = 0; i < _tiles.Length; i++)
        {
            int tileOffset = i - copiesPerSide;
            Transform t = _tiles[i];
            if (t == null)
                continue;

            Vector3 p = t.position;
            p.x = centerPos.x + _tileWorldWidth * tileOffset;
            p.y = centerPos.y;
            p.z = centerPos.z;
            t.position = p;
        }

        float camX = cameraTransform.position.x;
        float leftMostX = centerPos.x - _tileWorldWidth * copiesPerSide;
        float rightMostX = centerPos.x + _tileWorldWidth * copiesPerSide;

        if (camX < leftMostX)
        {
            Vector3 shift = new Vector3(-_tileWorldWidth, 0f, 0f);
            transform.position += shift;
        }
        else if (camX > rightMostX)
        {
            Vector3 shift = new Vector3(_tileWorldWidth, 0f, 0f);
            transform.position += shift;
        }
    }
}
