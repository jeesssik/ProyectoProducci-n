using UnityEngine;
using UnityEngine.UI;

public class RangedPatrolEnemy : MonoBehaviour, IDamageable
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Patrulla")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f; // Rango de visión amplio
    [SerializeField] private float attackRange = 4.5f;   // Distancia desde la que empezará a disparar

    [Header("Bordes / suelo")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float ledgeLookAhead = 0.55f;
    [SerializeField] private float groundCheckDistance = 0.75f;

    [Header("Ataque a Distancia")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private GameObject dardoPrefab; // El proyectil a instanciar
    [SerializeField] private Transform firePoint;     // De dónde nace el dardo

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 6;

    [Header("UI Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;

    [Header("Loot al morir")]
    [SerializeField] private GameObject lootPrefabFallback;

    [Header("Victoria")]
    [SerializeField] private bool triggersWinOnDeath;

    [Header("Comportamiento especial")]
    [SerializeField] private bool preventFall = true;
    [SerializeField] private float maxChaseVerticalDrop = 1.25f;

    public bool TriggersWinOnDeath => triggersWinOnDeath;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;

    private float leftLimit;
    private float rightLimit;
    private int patrolDirection = 1;

    private bool canAttack = true;
    private bool canTakeDamage = true;
    private bool isChasing;

    private int currentHealth;
    private float desiredVelocityX;
    private float lastGroundedY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");

        TryWireHealthBarReferences();
        UpdateHealthBar();
        SetHealthBarVisible(false);
        lastGroundedY = transform.position.y;
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        CachePatrolLimits();
        InitPatrolDirection();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        UpdateAnimator();

        if (currentHealth <= 0) return;

        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            float verticalDelta = player.position.y - transform.position.y;
            bool playerTooFarBelow = preventFall && verticalDelta < -maxChaseVerticalDrop;

            if (!playerTooFarBelow)
            {
                isChasing = true;
                LookAtPlayer();

                float distanceToPlayerX = Mathf.Abs(player.position.x - transform.position.x);

                // 🔥 MODIFICADO: Si está en rango de ataque, frena, dispara y RECIÉN ACÁ muestra la barra de vida
                if (distanceToPlayerX <= attackRange)
                {
                    SetHealthBarVisible(true); // <-- Se hace visible en rango de tiro

                    desiredVelocityX = 0f; // Se planta firme
                    if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                    
                    TryAttack();
                }
                else
                {
                    // Si lo ve pero está lejos, avanza hacia él ocultando la barra de vida
                    SetHealthBarVisible(false); // <-- Se oculta si sale del rango de tiro

                    float directionX = Mathf.Sign(player.position.x - transform.position.x);
                    if (directionX == 0f) directionX = patrolDirection;

                    if (HasGroundAhead(directionX))
                        desiredVelocityX = directionX * moveSpeed;
                    else
                        desiredVelocityX = 0f; 
                }

                return;
            }
        }

        // Si no detecta al jugador, vuelve al estado normal de patrulla y apaga la barra
        isChasing = false;
        SetHealthBarVisible(false); // <-- Se oculta patrullando normalmente

        desiredVelocityX = patrolDirection * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (currentHealth <= 0) return;

        bool grounded = IsGrounded();
        if (grounded)
            lastGroundedY = transform.position.y;

        if (!isChasing)
            UpdatePatrolDirection();

        if (preventFall && !grounded)
        {
            RecoverFromFall();
            return;
        }

        if (rb != null)
        {
            if (rb.IsSleeping())
                rb.WakeUp();

            rb.velocity = new Vector2(desiredVelocityX, rb.velocity.y);
        }
    }

    private void TryAttack()
    {
        if (!canAttack || animator == null) return;

        canAttack = false;
        
        // Disparar animación
        animator.SetTrigger("Attack");
        
        // Tiempo de espera basado en tu cooldown
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    // =========================================================================
    // 🏹 LLAMAR A ESTA FUNCIÓN MEDIANTE UN ANIMATION EVENT EN TU CLIP "ATTACK"
    // =========================================================================
    public void ApplyEnemyAttackDamage()
    {
        if (currentHealth <= 0) return;

        if (dardoPrefab != null && firePoint != null)
        {
            Debug.Log($"{name} dispara un dardo.");
            // Instancia el dardo en la posición del firePoint y hereda su rotación
            Instantiate(dardoPrefab, firePoint.position, firePoint.transform.rotation);
        }
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void RecoverFromFall()
    {
        desiredVelocityX = 0f;
        if (rb != null) rb.velocity = Vector2.zero;
        transform.position = new Vector3(transform.position.x, lastGroundedY, transform.position.z);
    }

    private void UpdatePatrolDirection()
    {
        float x = transform.position.x;

        if (x >= rightLimit - 0.05f)
        {
            patrolDirection = -1;
            LookTowardsDirection(patrolDirection);
        }
        else if (x <= leftLimit + 0.05f)
        {
            patrolDirection = 1;
            LookTowardsDirection(patrolDirection);
        }

        if (!HasGroundAhead(patrolDirection))
        {
            patrolDirection *= -1;
            LookTowardsDirection(patrolDirection);
            desiredVelocityX = patrolDirection * moveSpeed;
        }
    }

    private void CachePatrolLimits()
    {
        if (pointA == null || pointB == null)
        {
            float half = 2f;
            leftLimit = transform.position.x - half;
            rightLimit = transform.position.x + half;
            return;
        }

        leftLimit = Mathf.Min(pointA.position.x, pointB.position.x);
        rightLimit = Mathf.Max(pointA.position.x, pointB.position.x);

        if (rightLimit - leftLimit < 0.25f)
        {
            float mid = (leftLimit + rightLimit) * 0.5f;
            leftLimit = mid - 1f;
            rightLimit = mid + 1f;
        }
    }

    private void InitPatrolDirection()
    {
        float x = transform.position.x;
        float distToRight = rightLimit - x;
        float distToLeft = x - leftLimit;

        patrolDirection = distToRight >= distToLeft ? 1 : -1;
        LookTowardsDirection(patrolDirection);
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            player = playerGo.transform;
    }

    private bool IsGrounded() => HasGroundBelow(GetFeetPosition());

    private bool HasGroundAhead(float direction)
    {
        if (direction == 0f) return false;

        Vector2 feet = GetFeetPosition();
        float aheadX = feet.x + direction * GetProbeForward();
        Vector2 probe = new Vector2(aheadX, feet.y + 0.05f);
        return HasGroundBelow(probe);
    }

    private bool HasGroundBelow(Vector2 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
        return hit.collider != null && !IsSelfCollider(hit.collider);
    }

    private bool IsSelfCollider(Collider2D col)
    {
        return col.transform == transform || col.transform.IsChildOf(transform);
    }

    private Vector2 GetFeetPosition()
    {
        if (bodyCollider != null)
            return new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);

        return transform.position;
    }

    private float GetProbeForward()
    {
        float halfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0.25f;
        return halfWidth + ledgeLookAhead;
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        int direction = player.position.x < transform.position.x ? -1 : 1;
        LookTowardsDirection(direction);
    }

    private void LookTowardsDirection(int direction)
    {
       if (direction == 0) return;
    
    float absX = Mathf.Abs(transform.localScale.x);
     transform.localScale = new Vector3(direction > 0 ? -absX : absX, transform.localScale.y, transform.localScale.z);
}

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = rb != null ? Mathf.Abs(rb.velocity.x) : Mathf.Abs(desiredVelocityX);
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", IsGrounded());
        animator.SetBool("IsFalling", rb != null && rb.velocity.y < -0.15f && !IsGrounded());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack"))
        {
            TakeDamage(2);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage || currentHealth <= 0) return;

        canTakeDamage = false;
        currentHealth -= damage;

        UpdateHealthBar();
        SetHealthBarVisible(true);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
            Invoke(nameof(ResetDamage), 0.3f);
        }
        else
        {
            Invoke(nameof(ResetDamage), 0.3f);
        }
    }

    private void ResetDamage()
    {
        canTakeDamage = true;
    }

    private void Die()
    {
        SetHealthBarVisible(false);

        bool dropped = false;
        EnemyLootDrop loot = GetComponent<EnemyLootDrop>();
        if (loot != null)
            dropped = loot.DropLoot();

        if (!dropped && lootPrefabFallback != null)
        {
            Vector3 pos = transform.position + new Vector3(0f, 0.35f, 0f);
            GameObject rune = Instantiate(lootPrefabFallback, pos, Quaternion.identity);
            RuneDropLaunch launch = rune.GetComponent<RuneDropLaunch>();
            if (launch != null)
                launch.BeginDrop(pos);

            dropped = true;
        }

        if (triggersWinOnDeath)
        {
            WinManager winManager = FindFirstObjectByType<WinManager>();
            if (winManager != null)
                winManager.ShowWin();
        }

        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) TryWireHealthBarReferences();

        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthBarFill.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
        }
    }

    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarObject == null) TryWireHealthBarReferences();
        if (healthBarObject != null) healthBarObject.SetActive(visible);
    }

    
private void TryWireHealthBarReferences()
    {
        if (healthBarObject == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.IndexOf("HealthBar", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    healthBarObject = child.gameObject;
                    break;
                }
            }
        }

        Transform searchRoot = healthBarObject != null ? healthBarObject.transform : transform;

        if (healthBarFill == null)
        {
            Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
            
            // Intento 1: Buscar por nombre exacto
            foreach (Image img in images)
            {
                if (img.gameObject.name.Equals("barra", System.StringComparison.OrdinalIgnoreCase) || 
                    img.gameObject.name.Equals("fill", System.StringComparison.OrdinalIgnoreCase))
                {
                    healthBarFill = img;
                    break;
                }
            }

            // Intento 2: Si sigue vacía, agarra la primera que esté configurada como Filled
            if (healthBarFill == null)
            {
                foreach (Image img in images)
                {
                    if (img.type == Image.Type.Filled)
                    {
                        healthBarFill = img;
                        break;
                    }
                }
            }
        }
    }




    private void OnDrawGizmosSelected()
    {
        if (pointA != null) { Gizmos.color = Color.blue; Gizmos.DrawSphere(pointA.position, 0.12f); }
        if (pointB != null) { Gizmos.color = Color.red; Gizmos.DrawSphere(pointB.position, 0.12f); }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}