using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RuneSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;

    private int _frameIndex;
    private float _timer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1) return;

        _timer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(framesPerSecond, 0.01f);

        if (_timer < frameTime) return;

        _timer = 0f;
        _frameIndex = (_frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[_frameIndex];
    }
}
