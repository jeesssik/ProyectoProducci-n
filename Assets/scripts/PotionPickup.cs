using UnityEngine;

[DisallowMultipleComponent]
public class PotionPickup : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pickupRadius = 1.2f;

    private void Update()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(playerTag)) continue;

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player == null)
                player = hit.GetComponentInParent<PlayerController>();

            if (player == null || player.IsDead()) continue;

            if (!Input.GetKeyDown(pickupKey)) return;

            player.RestoreFullHealth();
            Destroy(gameObject);
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
