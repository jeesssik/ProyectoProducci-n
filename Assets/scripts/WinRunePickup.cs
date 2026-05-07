using UnityEngine;

[DisallowMultipleComponent]
public class WinRunePickup : MonoBehaviour
{
    [Header("Tag")]
    [SerializeField] private string winTag = "Condition";

    [Header("References (optional)")]
    [SerializeField] private WinManager winManager;

    [Tooltip("Desactiva el item cuando se recoge.")]
    [SerializeField] private bool disablePickedObject = true;

    private void Awake()
    {
        if (winManager == null)
            winManager = FindFirstObjectByType<WinManager>();

        if (winManager == null)
            winManager = new GameObject("WinManager").AddComponent<WinManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(winTag)) return;

        if (disablePickedObject)
            other.gameObject.SetActive(false);

        winManager.ShowWin();
    }
}

