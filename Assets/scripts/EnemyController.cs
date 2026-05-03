using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 1f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private bool canAttack = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            StopMoving();
            TryAttack();
        }
        else if (distanceToPlayer <= detectionRange)
        {
            MoveToPlayer();
        }
        else
        {
            StopMoving();
        }

        UpdateAnimator();
    }

    private void MoveToPlayer()
    {
        float directionX = Mathf.Sign(player.position.x - transform.position.x);

        rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = directionX < 0;
        }
    }

    private void StopMoving()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void TryAttack()
    {
        if (!canAttack) return;

        canAttack = false;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}