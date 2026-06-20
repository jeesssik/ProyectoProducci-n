using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI Health Bar")]
    [SerializeField] private Image healthBarFill;
    [Tooltip("El objeto raíz de la barra de vida (el Canvas o el Fondo) para ocultarlo/mostrarlo por completo.")]
    [SerializeField] private GameObject healthBarObject; // CAMBIO: UI - Referencia al contenedor contenedor completo

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

        UpdateHealthBar();

        // CAMBIO: UI - Asegura que la barra empiece completamente oculta al iniciar el nivel
        SetHealthBarVisible(false);
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


        state = FlowerState.Active;

        SetHealthBarVisible(true);

        animator.ResetTrigger("Open");
        animator.SetTrigger("Open");

        // Inicializamos el contador de ataque
        attackTimer = timeBetweenAttacks;
    }

    public void FinishOpening()
    {
        if (state == FlowerState.Dead) return;

        state = FlowerState.Active;
        attackTimer = timeBetweenAttacks;
    }

    private void HandleAttack(float distanceToPlayer)
    {
        // El temporizador de ataque debe correr SIEMPRE que estemos en estado Activo
        // para que la flor mantenga su ritmo de ataque, esté o no el jugador pegado en ese instante.
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // Si el temporizador ya llegó a cero Y el jugador está dentro del rango de ataque...
        if (attackTimer <= 0f && distanceToPlayer <= attackRange)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (state != FlowerState.Active) return;

        Debug.Log("La flor ataca");
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        // CAMBIO CLAVE: El temporizador se reinicia ACÁ, justo cuando el ataque se dispara.
        // Esto garantiza que la animación corra completa y su Animation Event (ApplyFlowerAttackDamage)
        // se ejecute correctamente frame a frame.
        attackTimer = timeBetweenAttacks;
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

        // CAMBIO: UI - Ocultamos la barra de vida al morir
        SetHealthBarVisible(false);

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");

        animator.SetBool("IsDead", true);

        Destroy(gameObject, 1.2f);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    // CAMBIO: UI - Función auxiliar para controlar la visibilidad sin romper nada si nos olvidamos de asignar el objeto
    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(visible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.DrawWireSphere(transform.position, damageRange);
    }
}