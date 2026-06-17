using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Patrulla")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Detección")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float attackRange = 1f;

    [Header("Bordes / suelo")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float ledgeLookAhead = 0.55f;
    [SerializeField] private float groundCheckDistance = 0.75f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 6;

    [Header("UI Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;

    [Header("Loot al morir (respaldo si falta Enemy Loot Drop)")]
    [SerializeField] private GameObject lootPrefabFallback;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;

    private float leftLimit;
    private float rightLimit;
    private int patrolDirection = 1;

    private bool canAttack = true;
    private bool isTouchingPlayer;
    private bool canTakeDamage = true;
    private bool isChasing;

    private int currentHealth;
    private float desiredVelocityX;

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

        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            isChasing = true;
            SetHealthBarVisible(true);
            LookAtPlayer();

            if (isTouchingPlayer)
            {
                desiredVelocityX = 0f;
                TryAttack();
            }
            else
            {
                float directionX = Mathf.Sign(player.position.x - transform.position.x);
                if (directionX == 0f)
                    directionX = patrolDirection;

                if (HasGroundAhead(directionX))
                    desiredVelocityX = directionX * moveSpeed;
                else
                    desiredVelocityX = 0f;
            }
        }
        else
        {
            isChasing = false;

            if (currentHealth >= maxHealth)
                SetHealthBarVisible(false);

            desiredVelocityX = patrolDirection * moveSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (!isChasing)
            UpdatePatrolDirection();

        if (rb != null)
        {
            if (rb.IsSleeping())
                rb.WakeUp();

            rb.velocity = new Vector2(desiredVelocityX, rb.velocity.y);
        }
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
        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);
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

    public void ApplyEnemyAttackDamage()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && !playerController.IsDead())
        {
            playerController.TakeDamage(1);
            playerController.ApplyKnockback(transform.position);
        }
    }

    private void LookAtPlayer()
    {
        if (spriteRenderer == null || player == null) return;
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    private void LookTowardsDirection(int direction)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = direction < 0;
    }

    private void TryAttack()
    {
        if (!canAttack || animator == null) return;

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
        if (animator == null) return;

        float speed = rb != null ? Mathf.Abs(rb.velocity.x) : Mathf.Abs(desiredVelocityX);
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", IsGrounded());
        animator.SetBool("IsFalling", rb != null && rb.velocity.y < -0.15f && !IsGrounded());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            isTouchingPlayer = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            isTouchingPlayer = false;
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

        if (Application.isPlaying)
        {
            Vector2 feet = GetFeetPosition();
            Gizmos.color = Color.green;
            Gizmos.DrawLine(feet, feet + Vector2.down * groundCheckDistance);

            float dir = patrolDirection;
            Vector2 ahead = new Vector2(feet.x + dir * GetProbeForward(), feet.y + 0.05f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ahead, ahead + Vector2.down * groundCheckDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack"))
        {
            TakeDamage(2);
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
                pc.TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage) return;

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
        }

        if (!dropped)
            Debug.LogWarning($"{name}: murió sin loot. Agregá Enemy Loot Drop o Loot Prefab Fallback.");

        Destroy(gameObject);
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

        if (healthBarObject == null && healthBarFill != null)
        {
            Canvas canvas = healthBarFill.GetComponentInParent<Canvas>();
            if (canvas != null)
                healthBarObject = canvas.gameObject;
        }
    }
}
