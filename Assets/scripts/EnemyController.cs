
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;
    [Header("Detección")]
    [SerializeField] private float attackRange = 1f;
    [Header("Patrulla")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 4f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;

    private float leftLimit;
    private float rightLimit;
    private int patrolDirection = 1;

    private bool canAttack = true;
    private bool isTouchingPlayer = false;

    private bool canTakeDamage = true;


    private int currentHealth;
    private int maxHealth = 6;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        leftLimit = Mathf.Min(pointA.position.x, pointB.position.x);
        rightLimit = Mathf.Max(pointA.position.x, pointB.position.x);
    }
    private void StopMovement()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void Update()
    {

        UpdateAnimator();
        
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
            Patrol();
        }

        UpdateAnimator();
    }

    public void ApplyEnemyAttackDamage()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (playerController != null && !playerController.IsDead())
            {
                playerController.TakeDamage(1);
                playerController.ApplyKnockback(transform.position);
            }
        }
    }
    private void Patrol()
    {
        rb.velocity = new Vector2(patrolDirection * moveSpeed, rb.velocity.y);

        if (transform.position.x >= rightLimit)
        {
            patrolDirection = -1;
            LookTowardsDirection(patrolDirection);
        }
        else if (transform.position.x <= leftLimit)
        {
            patrolDirection = 1;
            LookTowardsDirection(patrolDirection);
        }
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
        spriteRenderer.flipX = playerIsOnLeft;
    }

    private void LookTowardsDirection(int direction)
    {
        spriteRenderer.flipX = direction < 0;
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

    private void OnDrawGizmosSelected()
    {
        if (pointA != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pointA.position, 0.12f);
        }

        if (pointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pointB.position, 0.12f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("Enemy detectó trigger con: " + other.name);

        if (other.CompareTag("PlayerAttack"))
        {
            Debug.Log("ENEMIGO RECIBIÓ IMPACTO DEL ATAQUE DEL JUGADOR");

            TakeDamage(2);
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                //Debug.Log("Enemy tocó al Player y le hace daño");
                player.TakeDamage(1);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage) return;

        canTakeDamage = false;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " recibió " + damage + " de daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            EnemyLootDrop loot = GetComponent<EnemyLootDrop>();
            if (loot != null)
                loot.DropLoot();

            Destroy(gameObject);
        }
        else
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
            Invoke(nameof(ResetDamage), 0.3f);
        }
    }

    private void ResetDamage()
{
    canTakeDamage = true;
}
    private void HandleBehaviour()
    {
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (playerController != null && playerController.IsDead())
            {
                StopMovement();
                Patrol();
                return;
            }

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                StopMovement();
                TryAttack();
                return;
            }

            if (distance <= detectionRange)
            {
                ChasePlayer();
                return;
            }
        }

        Patrol();
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        float directionX = player.position.x > transform.position.x ? 1f : -1f;

        rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);

        if (directionX > 0)
        {
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.flipX = true;
        }
    }
}