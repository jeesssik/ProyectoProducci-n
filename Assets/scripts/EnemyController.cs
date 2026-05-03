/*using UnityEngine;

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
}*/

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 0.8f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;

    private bool isTouchingPlayer = false;
    private bool canAttack = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            LookAtPlayer();

            if (isTouchingPlayer)
            {
                StopMoving();
                TryAttack();
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
        else
        {
            StopMoving();
        }

        UpdateAnimator();
    }

    private void MoveTowardsPlayer()
    {
        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);
    }

    private void StopMoving()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void LookAtPlayer()
    {
        bool playerIsOnLeft = player.position.x < transform.position.x;

        // Si queda al revés, cambiá esta línea por: spriteRenderer.flipX = !playerIsOnLeft;
        spriteRenderer.flipX = playerIsOnLeft;
    }

    private void TryAttack()
    {
        if (!canAttack) return;

        canAttack = false;
        animator.SetTrigger("Attack");

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }
}