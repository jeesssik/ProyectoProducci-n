using UnityEngine;
using UnityEngine.UI; // <-- AGREGADO: Necesario para controlar componentes de UI (Image)

public class EnemyController : MonoBehaviour, IDamageable
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


    [Header("Vida")]
    [SerializeField] private int maxHealth = 6;

    [Header("UI Health Bar")] // <-- AGREGADO: Campos para controlar la barra de vida de forma idéntica a la flor
    [SerializeField] private Image healthBarFill; 
    [Tooltip("El objeto raíz de la barra de vida (el Canvas o el Fondo) para ocultarlo/mostrarlo por completo.")]
    [SerializeField] private GameObject healthBarObject; 

    [Header("Loot al morir (respaldo si falta Enemy Loot Drop)")]
    [SerializeField] private GameObject lootPrefabFallback;

    private int currentHealth;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        leftLimit = Mathf.Min(pointA.position.x, pointB.position.x);
        rightLimit = Mathf.Max(pointA.position.x, pointB.position.x);

        // <-- AGREGADO: Asegura que la barra empiece llena y completamente oculta al iniciar
        UpdateHealthBar();
        SetHealthBarVisible(false);
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
            // CAMBIO: Hacemos visible la barra en cuanto detecta al jugador y empieza la persecución
            SetHealthBarVisible(true);

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
            // Opcional: Podés dejar que se oculte si pierde de vista al jugador, 
            // pero para evitar que parpadee bruscamente en combate si el player se aleja un milisegundo,
            // solo la apagamos si mantiene la vida al 100%. Si ya está herido, es mejor dejarla visible.
            if (currentHealth == maxHealth)
            {
                SetHealthBarVisible(false);
            }

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
                player.TakeDamage(1);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage) return;

        canTakeDamage = false;

        currentHealth -= damage;
        
        // CAMBIO: Actualizamos el relleno y forzamos que la barra aparezca (por si lo atacamos por la espalda sin entrar en rango)
        UpdateHealthBar();
        SetHealthBarVisible(true);

        Debug.Log($"{name} recibió {damage} de daño. Vida actual: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
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

    private void Die()
    {
        // CAMBIO: Ocultamos la barra por completo en el frame de la muerte
        SetHealthBarVisible(false);

        bool dropped = false;

        EnemyLootDrop loot = GetComponent<EnemyLootDrop>();
        if (loot != null)
        {
            loot.DropLoot();
            dropped = true;
        }
        else if (lootPrefabFallback != null)
        {
            Vector3 pos = transform.position + new Vector3(0f, 0.35f, 0f);
            GameObject rune = Instantiate(lootPrefabFallback, pos, Quaternion.identity);
            RuneDropLaunch launch = rune.GetComponent<RuneDropLaunch>();
            if (launch != null)
                launch.BeginDrop(pos);

            dropped = true;
            Debug.Log($"{name}: runa instanciada (fallback).");
        }

        Debug.Log($"{name} murió. Loot dropeado={dropped}");

        if (!dropped)
            Debug.LogWarning($"{name}: murió sin loot. Agregá Enemy Loot Drop o Loot Prefab Fallback.");

        Destroy(gameObject);
    }

    // <-- AGREGADO: Función para recalcular el fill de la imagen de UI
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    // <-- AGREGADO: Función auxiliar para apagar/prender el panel contenedor de la barra
    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(visible);
        }
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