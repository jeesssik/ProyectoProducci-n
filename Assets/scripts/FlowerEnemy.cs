using UnityEngine;

public class FlowerEnemy : MonoBehaviour
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

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;

    [Header("Attack Damage")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float damageRange = 1.5f;
    [SerializeField] private bool applyKnockbackToPlayer = true;

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

    public void ApplyFlowerAttackDamage()
{
    Debug.Log("EVENTO DE DAÑO DE LA FLOR EJECUTADO");

    if (state == FlowerState.Dead) return;

    if (player == null)
    {
        Debug.LogWarning("La flor no tiene referencia al Player");
        return;
    }

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);

    Debug.Log("La flor intenta hacer daño. Distancia: " + distanceToPlayer + " / Rango de daño: " + damageRange);

    if (distanceToPlayer > damageRange)
    {
        Debug.Log("El jugador salió del rango de daño de la flor");
        return;
    }

    PlayerController playerController = player.GetComponentInParent<PlayerController>();

    if (playerController == null)
    {
        Debug.LogWarning("El objeto Player no tiene PlayerController");
        return;
    }

    if (playerController.IsDead())
    {
        Debug.Log("El jugador ya está muerto");
        return;
    }

    Debug.Log("LA FLOR GOLPEÓ AL PLAYER");

    playerController.TakeDamage(attackDamage);

    if (applyKnockbackToPlayer)
    {
        playerController.ApplyKnockback(transform.position);
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

        Debug.Log("La flor se está abriendo");

        state = FlowerState.Opening;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Open");
    }

    public void FinishOpening()
    {
        if (state == FlowerState.Dead) return;

        Debug.Log("La flor quedó activa");

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

    Debug.Log("La flor ataca");

    animator.ResetTrigger("Attack");
    animator.SetTrigger("Attack");

    // PRUEBA TEMPORAL
    ApplyFlowerAttackDamage();
}
    public void TakeDamage(int damage)
    {
        if (state == FlowerState.Dead) return;

        currentHealth -= damage;

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

        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Hurt");
    }

    private void Die()
    {
        if (state == FlowerState.Dead) return;

        state = FlowerState.Dead;

        Debug.Log("La flor murió");

        animator.SetBool("IsDead", true);

        Destroy(gameObject, 1.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, damageRange);
    }
}