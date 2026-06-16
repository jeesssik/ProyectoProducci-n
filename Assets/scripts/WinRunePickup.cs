using UnityEngine;

[DisallowMultipleComponent]
public class WinRunePickup : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pickupRadius = 1.2f;

    [Header("Referencias (optional)")]
    [SerializeField] private WinManager winManager;

    [Tooltip("Desactiva la runa cuando el jugador la recoge.")]
    [SerializeField] private bool disablePickedObject = true;

    private RuneDropLaunch _dropLaunch;

    private void Awake()
    {
        _dropLaunch = GetComponent<RuneDropLaunch>();

        if (winManager == null)
            winManager = FindFirstObjectByType<WinManager>();

        if (winManager == null)
            winManager = new GameObject("WinManager").AddComponent<WinManager>();
    }

    private void Update()
    {
        if (_dropLaunch != null && !_dropLaunch.CanPickup)
            return;

        if (!Input.GetKeyDown(pickupKey))
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(playerTag))
                continue;

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player == null)
                player = hit.GetComponentInParent<PlayerController>();

            if (player == null || player.IsDead())
                continue;

            PickUp();
            return;
        }
    }

    private void PickUp()
    {
        if (disablePickedObject)
            gameObject.SetActive(false);

        winManager.ShowWin();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
