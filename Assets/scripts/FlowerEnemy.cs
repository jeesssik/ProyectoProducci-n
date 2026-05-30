using UnityEngine;
using UnityEngine.UI; // <-- 1. AGREGADO: Necesario para controlar componentes de UI (Image)

public class FlowerEnemy : MonoBehaviour, IDamageable
{
    private enum FlowerState
    {
        Closed,
        Opening,
        Active,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attack")]
    [SerializeField] private float timeBetweenAttacks = 2f;
    [SerializeField] private bool useSpecialAttack = false;

    [Header("Attack Damage")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float damageRange = 2f;
    [SerializeField] private bool applyKnockbackToPlayer = true;

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    
    // <-- 2. AGREGADO: Campo para arrastrar tu objeto "barra" en el Inspector
    [SerializeField] private Image healthBarFill; 

    private int currentHealth;
    private Animator animator;
    private FlowerState state = FlowerState.Closed;
    private Vector3 initialPosition;
    private float attackTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        initialPosition = transform.position;
        attackTimer = timeBetweenAttacks;

        // <-- 3. AGREGADO: Asegura que la barra empiece llena al 100%
        UpdateHealthBar();
    }

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (state == FlowerState.Dead) return;

        transform.position = initialPosition;

        FindPlayerIfNeeded();

        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (state == FlowerState.Closed)
        {
            if (distanceToPlayer <= detectionRange)
            {
                OpenFlower();
            }

            return;
        }

        if (state == FlowerState.Opening)
        {
            return;
        }

        if (state == FlowerState.Active)
        {
            HandleAttack(distanceToPlayer);
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void OpenFlower()
    {
        if (state != FlowerState.Closed) return;

        state = FlowerState.Opening;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Open");
    }

    public void FinishOpening()
    {
        if (state == FlowerState.Dead) return;

        state = FlowerState.Active;
        attackTimer = timeBetweenAttacks;
    }

    private void HandleAttack(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange)
        {
            attackTimer = timeBetweenAttacks;
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f) return;

        Attack();

        attackTimer = timeBetweenAttacks;
    }

    private void Attack()
    {
        if (state != FlowerState.Active) return;
        
        // Corregido un pequeño "else" huerfano que tenías aquí de un if borrado
        Debug.Log("La flor ataca");
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    public void ApplyFlowerAttackDamage()
    {
        if (state == FlowerState.Dead) return;
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > damageRange)
        {
            return;
        }

        PlayerController playerController = player.GetComponentInParent<PlayerController>();

        if (playerController == null)
        {
            return;
        }

        if (playerController.IsDead()) return;

        Debug.Log("LA FLOR GOLPEÓ AL PLAYER");

        playerController.TakeDamage(attackDamage);

        if (applyKnockbackToPlayer)
        {
            playerController.ApplyKnockback(transform.position);
        }
    }

    public void TakeDamage(int damage)
    {
        if (state == FlowerState.Dead) return;

        currentHealth -= damage;

        // <-- 4. AGREGADO: Actualiza la barra visual inmediatamente al recibir daño
        UpdateHealthBar();

        Debug.Log("La flor recibió " + damage + " de daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (state == FlowerState.Closed)
        {
            OpenFlower();
            return;
        }

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        animator.SetTrigger("Hurt");
    }

    private void Die()
    {
        if (state == FlowerState.Dead) return;

        Debug.Log("FLOR MURIOOOOOOOO");

        state = FlowerState.Dead;

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        animator.SetBool("IsDead", true);

        Destroy(gameObject, 1.2f);
    }

    // <-- 5. AGREGADO: Función dedicada a calcular el porcentaje de vida y pasárselo a la imagen
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            // Convertimos a float para que la división dé decimales precisos entre 0.0 y 1.0
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.DrawWireSphere(transform.position, damageRange);
    }
}