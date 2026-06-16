using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RuneSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;

    private Sprite[] _validFrames;
    private int _frameIndex;
    private float _timer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        _validFrames = BuildValidFrames();

        if (_validFrames.Length > 0)
            spriteRenderer.sprite = _validFrames[0];
    }

    private void Update()
    {
        if (_validFrames == null || _validFrames.Length <= 1)
            return;

        _timer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(framesPerSecond, 0.01f);

        if (_timer < frameTime)
            return;

        _timer = 0f;
        _frameIndex = (_frameIndex + 1) % _validFrames.Length;
        spriteRenderer.sprite = _validFrames[_frameIndex];
    }

    private Sprite[] BuildValidFrames()
    {
        if (frames == null || frames.Length == 0)
            return System.Array.Empty<Sprite>();

        int count = 0;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                count++;
        }

        if (count == 0)
            return System.Array.Empty<Sprite>();

        Sprite[] result = new Sprite[count];
        int index = 0;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null)
                continue;

            result[index++] = frames[i];
        }

        return result;
    }
}
