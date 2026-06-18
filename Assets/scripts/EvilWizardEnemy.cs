using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class EvilWizardEnemy : MonoBehaviour, IDamageable
{
    private enum WizardState
    {
        Patrolling,
        Engaging,
        Dead
    }

    [Header("Patrulla")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 1.6f;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 5.5f;
    [SerializeField] private float rangedAttackRange = 7f;

    [Header("Ataque a distancia")]
    [SerializeField] private WizardProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 1;

    [Header("Suelo / bordes")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float ledgeLookAhead = 0.55f;
    [SerializeField] private float groundCheckDistance = 0.75f;
    [SerializeField] private bool useLedgeChecksDuringPatrol;
    [SerializeField] private float patrolDirectionCooldown = 0.35f;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 7;

    [Header("UI Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Boss")]
    [SerializeField] private bool isBoss;
    [SerializeField] private int bossBonusHealth = 8;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private WizardState state = WizardState.Patrolling;

    private float leftLimit;
    private float rightLimit;
    private int patrolDirection = 1;
    private float desiredVelocityX;
    private float attackTimer;
    private bool canAttack = true;
    private bool canTakeDamage = true;
    private bool isEngaging;
    private int currentHealth;
    private float patrolFlipTimer;
    private float lastAnimatorPosX;
    private bool hasPatrolPoints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");

        if (isBoss)
            maxHealth += bossBonusHealth;

        currentHealth = maxHealth;

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0.35f, 0.9f, 0f);
            firePoint = fp.transform;
        }

        TryWireHealthBarReferences();
        UpdateHealthBar();
        SetHealthBarVisible(false);
        attackTimer = attackCooldown * 0.5f;
    }

    private void Start()
    {
        CachePatrolLimits();
        InitPatrolDirection();
        lastAnimatorPosX = transform.position.x;
    }

    private void Update()
    {
        if (state == WizardState.Dead) return;

        FindPlayerIfNeeded();
        UpdateAnimator();

        if (player == null)
        {
            PatrolUpdate();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            isEngaging = true;
            state = WizardState.Engaging;
            SetHealthBarVisible(true);
            FacePlayer();

            if (distance <= rangedAttackRange)
            {
                TryRangedAttack(distance);
            }
            else
            {
                ChasePlayer(distance);
            }

            return;
        }

        isEngaging = false;
        state = WizardState.Patrolling;

        if (currentHealth >= maxHealth)
            SetHealthBarVisible(false);

        PatrolUpdate();
    }

    private void FixedUpdate()
    {
        if (state == WizardState.Dead || rb == null) return;

        if (!isEngaging)
            UpdatePatrolDirection();

        if (rb.IsSleeping())
            rb.WakeUp();

        rb.velocity = new Vector2(desiredVelocityX, rb.velocity.y);
    }

    private void PatrolUpdate()
    {
        desiredVelocityX = patrolDirection * moveSpeed;
    }

    private void ChasePlayer(float distance)
    {
        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        if (directionX == 0f)
            directionX = patrolDirection;

        if (HasGroundAhead(directionX) && distance > 2f)
            desiredVelocityX = directionX * moveSpeed;
        else
            desiredVelocityX = 0f;
    }

    private void TryRangedAttack(float distance)
    {
        desiredVelocityX = 0f;

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (!canAttack || attackTimer > 0f) return;
        if (distance > rangedAttackRange) return;

        canAttack = false;
        attackTimer = attackCooldown;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
            Invoke(nameof(ResetAttack), attackCooldown);
        }
        else
        {
            FireProjectile();
            Invoke(nameof(ResetAttack), attackCooldown);
        }
    }

    public void FireProjectile()
    {
        if (state == WizardState.Dead || projectilePrefab == null) return;

        Vector2 direction = Vector2.right;
        if (player != null)
            direction = (player.position - firePoint.position).normalized;
        else if (spriteRenderer != null && spriteRenderer.flipX)
            direction = Vector2.left;

        WizardProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Launch(direction);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void UpdatePatrolDirection()
    {
        if (patrolFlipTimer > 0f)
            patrolFlipTimer -= Time.fixedDeltaTime;

        float x = transform.position.x;
        int newDirection = patrolDirection;

        if (x >= rightLimit - 0.05f)
            newDirection = -1;
        else if (x <= leftLimit + 0.05f)
            newDirection = 1;
        else if (useLedgeChecksDuringPatrol && !hasPatrolPoints && !HasGroundAhead(patrolDirection))
            newDirection = -patrolDirection;

        if (newDirection == patrolDirection || patrolFlipTimer > 0f)
            return;

        patrolDirection = newDirection;
        patrolFlipTimer = patrolDirectionCooldown;
        FaceDirection(patrolDirection);
    }

    private void CachePatrolLimits()
    {
        hasPatrolPoints = pointA != null && pointB != null;

        if (!hasPatrolPoints)
        {
            float half = 2.5f;
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
        patrolDirection = (rightLimit - x) >= (x - leftLimit) ? 1 : -1;
        FaceDirection(patrolDirection);
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            player = playerGo.transform;
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || player == null) return;
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    private void FaceDirection(int direction)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = direction < 0;
    }

    private bool IsGrounded()
    {
        return HasGroundBelow(GetFeetPosition());
    }

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

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = rb != null ? Mathf.Abs(rb.velocity.x) : Mathf.Abs(desiredVelocityX);
        if (speed < 0.08f)
            speed = 0f;

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", IsGrounded());
    }

    public void TakeDamage(int damage)
    {
        if (state == WizardState.Dead || !canTakeDamage) return;

        canTakeDamage = false;
        currentHealth -= damage;
        UpdateHealthBar();
        SetHealthBarVisible(true);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }

        Invoke(nameof(ResetDamage), 0.35f);
    }

    private void ResetDamage()
    {
        canTakeDamage = true;
    }

    private void Die()
    {
        if (state == WizardState.Dead) return;

        state = WizardState.Dead;
        desiredVelocityX = 0f;
        DisableBodyColliders();

        if (rb != null)
            rb.velocity = Vector2.zero;

        SetHealthBarVisible(false);

        if (animator != null)
            animator.SetBool("IsDead", true);

        EnemyLootDrop loot = GetComponent<EnemyLootDrop>();
        if (loot != null)
            loot.DropLoot();

        Destroy(gameObject, 1.6f);
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
        if (healthBarFill == null)
            TryWireHealthBarReferences();

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
        if (healthBarObject == null)
            TryWireHealthBarReferences();

        if (healthBarObject != null)
            healthBarObject.SetActive(visible);
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
            foreach (Image img in images)
            {
                if (img.gameObject.name.Equals("barra", System.StringComparison.OrdinalIgnoreCase))
                {
                    healthBarFill = img;
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Bounds b = col.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangedAttackRange);

        if (pointA != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pointA.position, 0.12f);
        }

        if (pointB != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(pointB.position, 0.12f);
        }
    }
}
