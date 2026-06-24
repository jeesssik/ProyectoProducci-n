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
    [SerializeField] private WizardProjectile projectilePrefab;
    [SerializeField] private GameObject dardoPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpawnDelay = 0.35f;

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
    [SerializeField] private bool blockPlayerPhysics = true;

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
    private bool projectileFiredThisAttack;
    private bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (firePoint == null)
        {
            Transform existing = transform.Find("FirePoint");
            if (existing != null)
                firePoint = existing;
        }

        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");

        if (blockPlayerPhysics && rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

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
        ConfigurePlayerCollisionIgnore();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        UpdateAnimator();

        if (isDead || currentHealth <= 0) return;

        if (player != null
            && IsPlayerInHostileZone()
            && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            float verticalDelta = player.position.y - transform.position.y;
            bool playerTooFarBelow = preventFall && verticalDelta < -maxChaseVerticalDrop;

            if (!playerTooFarBelow)
            {
                isChasing = true;
                LookAtPlayer();

                float distanceToPlayerX = Mathf.Abs(player.position.x - transform.position.x);

                if (distanceToPlayerX <= attackRange)
                {
                    SetHealthBarVisible(true); 

                    desiredVelocityX = 0f; 
                    if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
                    
                    TryAttack();
                }
                else
                {
                    SetHealthBarVisible(false); 

                    // 🔥 CAMBIO AQUÍ: Si el jugador se escapó del rango de ataque, cancelamos el trigger
                    if (animator != null)
                    {
                        animator.ResetTrigger("Attack");
                    }

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

        isChasing = false;
        SetHealthBarVisible(false); 

        // 🔥 TAMBIÉN AQUÍ: Si se pierde la detección completa, nos aseguramos de limpiar el trigger
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }

        desiredVelocityX = patrolDirection * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (isDead || currentHealth <= 0) return;

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
        if (!canAttack || animator == null || !IsPlayerInHostileZone()) return;

        canAttack = false;
        projectileFiredThisAttack = false;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        CancelInvoke(nameof(ApplyEnemyAttackDamage));
        Invoke(nameof(ApplyEnemyAttackDamage), projectileSpawnDelay);
        Invoke(nameof(ResetAttack), attackCooldown);
    }


    public void ApplyEnemyAttackDamage()
    {
        if (isDead || currentHealth <= 0 || projectileFiredThisAttack) return;
        if (!IsPlayerInHostileZone()) return;

        projectileFiredThisAttack = true;
        FireProjectile();
    }

    private void FireProjectile()
    {
        if (firePoint == null) return;

        WizardProjectile prefab = projectilePrefab;
        if (prefab == null && dardoPrefab != null)
            prefab = dardoPrefab.GetComponent<WizardProjectile>();

        if (prefab == null) return;

        WizardProjectile projectile = Instantiate(prefab, firePoint.position, Quaternion.identity);
        projectile.Launch(GetFireDirection());
    }

    private Vector2 GetFireDirection()
    {
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)firePoint.position;
            if (toPlayer.sqrMagnitude > 0.0001f)
                return toPlayer.normalized;
        }

        float dirX = transform.localScale.x > 0f ? -1f : 1f;
        return new Vector2(dirX, 0f);
    }

    private void ConfigurePlayerCollisionIgnore()
    {
        if (!blockPlayerPhysics) return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0 && enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        if (player == null || bodyCollider == null) return;

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
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

    private bool IsPlayerInHostileZone()
    {
        if (player == null) return false;

        if (pointA == null || pointB == null)
            return true;

        float playerX = player.position.x;
        return playerX >= leftLimit && playerX <= rightLimit;
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
        if (HasAnimatorParameter("Speed"))
            animator.SetFloat("Speed", speed);
    }

    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
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
        if (isDead || !canTakeDamage || currentHealth <= 0) return;

        canTakeDamage = false;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        UpdateHealthBar();
        SetHealthBarVisible(true);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null && HasAnimatorParameter("Hurt"))
        {
            animator.ResetTrigger("Hurt");
            animator.SetTrigger("Hurt");
        }

        Invoke(nameof(ResetDamage), 0.3f);
    }

    private void ResetDamage()
    {
        canTakeDamage = true;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        desiredVelocityX = 0f;
        CancelInvoke();

        if (rb != null)
            rb.velocity = Vector2.zero;

        DisableBodyColliders();
        SetHealthBarVisible(false);

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hurt");
            animator.ResetTrigger("Death");
            animator.Play("death", 0, 0f);
        }

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

        Destroy(gameObject, 1.2f);
    }

    private void DisableBodyColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }
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

        if (pointA != null && pointB != null)
        {
            float minX = Mathf.Min(pointA.position.x, pointB.position.x);
            float maxX = Mathf.Max(pointA.position.x, pointB.position.x);
            float y = (pointA.position.y + pointB.position.y) * 0.5f;
            Vector3 a = new Vector3(minX, y - 1.5f, 0f);
            Vector3 b = new Vector3(maxX, y - 1.5f, 0f);
            Vector3 c = new Vector3(maxX, y + 1.5f, 0f);
            Vector3 d = new Vector3(minX, y + 1.5f, 0f);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}