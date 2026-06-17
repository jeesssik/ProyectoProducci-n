using UnityEngine;

[DisallowMultipleComponent]
public class WinRunePickup : MonoBehaviour
{
    [Header("Runa")]
    [SerializeField] private RuneType runeType = RuneType.Yellow;

    [Tooltip("Si está activo, recoger esta runa muestra la pantalla de victoria del nivel.")]
    [SerializeField] private bool triggersLevelWin = true;

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

            PickUp(player);
            return;
        }
    }

    private void PickUp(PlayerController player)
    {
        if (!RuneProgress.IsUnlocked(runeType))
            RuneProgress.Unlock(runeType);

        PlayerRuneAbilities abilities = player.GetComponent<PlayerRuneAbilities>();
        if (abilities != null)
            abilities.RefreshFromProgress();

        if (disablePickedObject)
            gameObject.SetActive(false);

        if (triggersLevelWin)
        {
            if (winManager == null)
                winManager = FindFirstObjectByType<WinManager>();

            if (winManager == null)
                winManager = new GameObject("WinManager").AddComponent<WinManager>();

            winManager.ShowWin();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
