using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("Falta asignar el Player en el EnemyController");
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("Falta asignar el SpriteRenderer en el EnemyController");
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        bool playerIsOnLeft = player.position.x < transform.position.x;

        // Probá primero con esta línea
        spriteRenderer.flipX = playerIsOnLeft;

        Debug.Log("Jugador a la izquierda: " + playerIsOnLeft + " | flipX: " + spriteRenderer.flipX);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}