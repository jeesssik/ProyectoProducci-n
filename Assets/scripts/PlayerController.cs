using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("Velocidad máxima corriendo (x).")]
    [SerializeField] private float maxRunSpeed = 7.5f;

    [Tooltip("Aceleración en el suelo (x).")]
    [SerializeField] private float groundAcceleration = 70f;

    [Tooltip("Frenado en el suelo (x) cuando no hay input o invertís dirección.")]
    [SerializeField] private float groundDeceleration = 95f;

    [Tooltip("Control en el aire (0..1). 1 = igual que en suelo.")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.65f;

    [Header("Jump Feel")]
    [Tooltip("Tiempo (seg) durante el cual mantener Jump sostiene el salto (más 'flotado').")]
    [SerializeField] private float jumpHoldTime = 0.12f;

    [Tooltip("Empuje extra hacia arriba mientras se mantiene Jump (unidades/seg^2).")]
    [SerializeField] private float jumpHoldForce = 18f;

    [Tooltip("Velocidad vertical máxima mientras se sostiene el salto (cap al 'boost').")]
    [SerializeField] private float maxUpwardVelocityWhileHolding = 12f;

    [Tooltip("Permite saltar un poquito después de despegar (seg).")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("Guarda el input de salto antes de tocar el suelo (seg).")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Momentum Jump")]
    [Tooltip("Bonus de salto (velocidad vertical) cuando venís con impulso horizontal (caminar con envión).")]
    [SerializeField] private float runJumpBonus = 2.2f;

    [Tooltip("Velocidad horizontal a la que el bonus llega al máximo. Más bajo = el envión influye antes.")]
    [SerializeField] private float runJumpBonusAtSpeed = 7.0f;

    [Tooltip("Cap final de velocidad vertical inicial al saltar.")]
    [SerializeField] private float maxInitialJumpVelocity = 13.5f;

    [Tooltip("Gravedad extra al caer. >1 = caídas más rápidas y saltos más 'precisos'.")]
    [SerializeField] private float fallGravityMultiplier = 2.4f;

    [Tooltip("Gravedad extra si soltás Jump temprano. >1 = salto más corto si soltás rápido.")]
    [SerializeField] private float jumpCutGravityMultiplier = 2.6f;

    [Header("Doble salto (Runa verde)")]
    [Tooltip("Impulso vertical del segundo salto en el aire.")]
    [SerializeField] private float airJumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Si el overlap no encuentra el suelo contra el LayerMask, prueba overlaps sueltos (Tilemaps en Default, tag Ground).")]
    [SerializeField] private bool relaxedGroundFallback = true;

    [Tooltip("Ray muy corto hacia abajo para captar huecos entre tiles y líneas finas.")]
    [SerializeField] private float groundProbeRayDistance = 0.08f;

    private static readonly Collider2D[] OverlapScratch = new Collider2D[24];

    [Header("Combate e Inputs Nuevos")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointOffsetX = 0.5f;
    [SerializeField] private float attackCooldown = 0.45f;
    [Tooltip("Tecla dedicada para ejecutar el ataque del jugador (Q por defecto).")]
    [SerializeField] private KeyCode attackKey = KeyCode.Q; // <-- MODIFICADO: Q por defecto

    [Tooltip("Tecla dedicada para consumir pociones (E por defecto).")]
    [SerializeField] private KeyCode potionKey = KeyCode.E; // <-- NUEVO: Mapeado junto al ataque

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityTime = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float attackRadius = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private GameOverManager gameOverManager;

    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private PlayerHealthUI playerHealthUI;
    
    private bool isFacingRight = true;
    private static readonly ContactPoint2D[] ContactScratch = new ContactPoint2D[24];
    private bool isKnockbacked = false;

    private Rigidbody2D rb;
    private PlayerRuneAbilities _runeAbilities;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsule;

    private float horizontalInput;
    private bool isGrounded;
    private bool wasGroundedLastFrame; 
    private bool canAttack = true;
    private bool isDead = false;
    private bool isInvulnerable = false;

    private int currentHealth;

    private float _baseGravityScale;
    private float _jumpHoldTimer;
    private bool _isHoldingJump;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isGroundedForJump;
    private int _jumpCount;

    public bool IsAttackDamageActive { get; private set; }
    public bool IsGrounded => isGrounded;
    public float HorizontalInput => horizontalInput;
    public float FacingDirection => isFacingRight ? 1f : -1f;
    public bool IsRuneDodgeInvulnerable { get; private set; }

    private void Awake()
    {
        _runeAbilities = GetComponent<PlayerRuneAbilities>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsule = GetComponent<CapsuleCollider2D>();
        currentHealth = maxHealth;
        _baseGravityScale = rb.gravityScale;
        
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }

        if (playerHealthUI != null)
        {
            playerHealthUI.UpdateLifeBar(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        if (isDead) return;

        ReadInput();
        CheckGround();
        UpdateJumpTimers();
        HandleJump();
        HandleAttack();
        HandlePotion(); // <-- NUEVO: Escucha el consumo de pociones
        FlipCharacter();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead || isKnockbacked) return;

        if (_runeAbilities != null && _runeAbilities.IsInRuneMovement)
        {
            wasGroundedLastFrame = isGrounded;
            CheckGround();
            return;
        }

        wasGroundedLastFrame = isGrounded;

        Move();
        ApplyBetterJumpPhysics();
        CheckGround(); 
    }

    public void ApplyKnockback(Vector2 enemyPosition)
    {
        isKnockbacked = true;
        float direction = transform.position.x < enemyPosition.x ? -1f : 1f;
        rb.velocity = new Vector2(direction * knockbackForceX, knockbackForceY);
        Invoke(nameof(EndKnockback), knockbackDuration);
    }

    private void EndKnockback()
    {
        isKnockbacked = false;
    }

    private void UpdateAttackHitboxPosition()
    {
        if (attackHitbox == null) return;

        bool attackToRight = !isFacingRight;
        Vector3 hitboxPos = attackHitbox.transform.localPosition;

        hitboxPos.x = attackToRight ? Mathf.Abs(hitboxPos.x) : -Mathf.Abs(hitboxPos.x);
        attackHitbox.transform.localPosition = hitboxPos;

        BoxCollider2D box = attackHitbox.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector2 offset = box.offset;
            offset.x = attackToRight ? -Mathf.Abs(offset.x) : Mathf.Abs(offset.x);
            box.offset = offset;
        }
    }

    private void ReadInput()
    {
        horizontalInput = 0f;
        try
        {
            // Lee los mapeos del Input Manager nativo (por defecto WASD y Flechas en horizontal)
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        catch { }

        // Salvaguarda: Forzar la lectura explícita si el Input Manager no estuviese configurado
        if (Mathf.Abs(horizontalInput) < 0.01f)
        {
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            
            if (left && !right) horizontalInput = -1f;
            else if (right && !left) horizontalInput = 1f;
        }
    }

    private void Move()
    {
        float targetSpeed = horizontalInput * maxRunSpeed;
        float accel = groundAcceleration;
        float decel = groundDeceleration;

        if (!isGrounded)
        {
            accel *= airControl;
            decel *= airControl;
        }

        float speedDiff = targetSpeed - rb.velocity.x;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;

        float movement = Mathf.Clamp(speedDiff * rate * Time.fixedDeltaTime, -Mathf.Abs(speedDiff), Mathf.Abs(speedDiff));
        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);
    }

    private void HandleJump()
    {
        if (GetJumpDown())
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        bool bufferedJump = _jumpBufferTimer > 0f;
        bool canUseCoyote = _coyoteTimer > 0f;
        int maxJumps = RuneProgress.IsUnlocked(RuneType.Green) ? 2 : 1;

        bool wantsGroundJump = _jumpCount == 0 && (_isGroundedForJump || canUseCoyote);
        bool wantsExtraAirJump = _jumpCount > 0 && !isGrounded;
        bool wantsAirRecoveryJump = _jumpCount == 0 && !isGrounded && RuneProgress.IsUnlocked(RuneType.Green);

        if (bufferedJump && _jumpCount < maxJumps && (wantsGroundJump || wantsExtraAirJump || wantsAirRecoveryJump))
        {
            bool isAirJump = !wantsGroundJump;
            float bonus = 0f;

            if (!isAirJump)
            {
                float speedAbs = Mathf.Abs(rb.velocity.x);
                if (runJumpBonusAtSpeed > 0.01f)
                {
                    float t = Mathf.Clamp01(speedAbs / runJumpBonusAtSpeed);
                    bonus = runJumpBonus * t;
                }
            }

            float baseForce = isAirJump ? airJumpForce : jumpForce;
            float initialVy = Mathf.Min(baseForce + bonus, maxInitialJumpVelocity);
            rb.velocity = new Vector2(rb.velocity.x, initialVy);

            animator.SetTrigger("Jump");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerJump);
            }

            _jumpHoldTimer = jumpHoldTime;
            _isHoldingJump = true;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _jumpCount++;
        }

        if (GetJumpUp())
        {
            _isHoldingJump = false;
        }
    }

    private bool GetJumpDown()
    {
        // MODIFICADO: Solo se salta con la Barra Espaciadora (Mapeo Arcade Limpio)
        return Input.GetKeyDown(KeyCode.Space);
    }

    private bool GetJumpUp()
    {
        return Input.GetKeyUp(KeyCode.Space);
    }

    private void UpdateJumpTimers()
    {
        if (_isGroundedForJump)
            _coyoteTimer = coyoteTime;
        else
            _coyoteTimer -= Time.deltaTime;

        _jumpBufferTimer -= Time.deltaTime;
        if (_jumpBufferTimer < 0f) _jumpBufferTimer = 0f;
        if (_coyoteTimer < 0f) _coyoteTimer = 0f;
    }

    private void ApplyBetterJumpPhysics()
    {
        if (_isHoldingJump && _jumpHoldTimer > 0f && rb.velocity.y > 0f)
        {
            if (maxUpwardVelocityWhileHolding <= 0f || rb.velocity.y < maxUpwardVelocityWhileHolding)
                rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
            _jumpHoldTimer -= Time.fixedDeltaTime;
        }

        float grav = _baseGravityScale;

        if (rb.velocity.y < -0.01f)
        {
            grav *= Mathf.Max(1f, fallGravityMultiplier);
        }
        else if (rb.velocity.y > 0.01f && !_isHoldingJump)
        {
            grav *= Mathf.Max(1f, jumpCutGravityMultiplier);
        }

        rb.gravityScale = grav;

        if (isGrounded && rb.velocity.y <= 0.01f)
        {
            _jumpHoldTimer = 0f;
            _isHoldingJump = false;
            rb.gravityScale = _baseGravityScale;
        }
    }

    private void HandleAttack()
    {
        if (Input.GetKeyDown(attackKey) && canAttack)
        {
            canAttack = false;
            animator.SetTrigger("Attack");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerAttack, 0.1f);
            }

            Invoke(nameof(ResetAttack), attackCooldown);
        }
    }

    private void HandlePotion()
    {
        if (Input.GetKeyDown(potionKey))
        {
            // NOTA: Vinculá este llamado con tu script encargado de la lógica de ítems/vida extra.
            Debug.Log("Poción activada vía código con la tecla: " + potionKey);
            
            // Ejemplo de conexión directa si usás otra clase en el Player:
            // GetComponent<PlayerInventory>()?.ConsumePotion();
        }
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    public void EnableAttackHitbox()
    {
        IsAttackDamageActive = true;
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(true);
            PlayerAttackHitbox hitboxScript = attackHitbox.GetComponent<PlayerAttackHitbox>();
            if (hitboxScript != null)
            {
                hitboxScript.HitEverythingInsideNow();
            }
        }
    }

    public void DisableAttackHitbox()
    {
        IsAttackDamageActive = false;
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
    }

    public void ApplyAttackDamage()
    {
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(attackPoint.position, 0.35f, enemyLayer);
        foreach (Collider2D enemy in enemiesHit)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(attackDamage);
            }
        }
    }

    public bool IsDead() => isDead;

    public void RestoreFullHealth()
    {
        if (isDead) return;
        currentHealth = maxHealth;

        if (playerHealthUI != null)
        {
            playerHealthUI.UpdateLifeBar(currentHealth, maxHealth);
        }

        animator.SetTrigger("Heal");
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            _isGroundedForJump = false;
            return;
        }

        if (capsule != null && IsGroundedByContacts())
        {
            isGrounded = true;
            _isGroundedForJump = IsGroundedByCastDown();
            CheckAterrizaje();

            if (isGrounded && rb.velocity.y <= 0.01f)
                _jumpCount = 0;

            return;
        }

        _isGroundedForJump = IsGroundedByCastDown();
        isGrounded = _isGroundedForJump;
        CheckAterrizaje();

        if (isGrounded && rb.velocity.y <= 0.01f)
            _jumpCount = 0;
    }

    private void CheckAterrizaje()
    {
        if (!wasGroundedLastFrame && isGrounded && rb.velocity.y <= 0.1f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerLand, 0.5f);
            }
        }
    }

    public void PlayFootstepSFX()
    {
        if (isGrounded && !isDead && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerWalkStep, 0.05f);
        }
    }

    private bool IgnoringSelfCollider(Collider2D c)
    {
        if (c == null || !c.enabled || c.isTrigger) return true;
        if (rb != null && c.attachedRigidbody == rb) return true;
        return false;
    }

    private bool IsStandingOnCollider(Collider2D c)
    {
        if (IgnoringSelfCollider(c)) return false;
        return true;
    }

    private bool IsGroundedByContacts()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = groundLayer,
            useTriggers = false
        };

        int count = capsule.GetContacts(filter, ContactScratch);
        if (count <= 0) return false;

        const float minGroundNormalY = 0.75f;
        const float maxGroundNormalX = 0.35f;
        float bottomY = capsule.bounds.min.y;
        const float bottomBand = 0.08f;

        for (int i = 0; i < count; i++)
        {
            ContactPoint2D c = ContactScratch[i];
            if (c.normal.y >= minGroundNormalY &&
                Mathf.Abs(c.normal.x) <= maxGroundNormalX &&
                c.point.y <= bottomY + bottomBand)
                return true;
        }

        return false;
    }

    private bool IsGroundedByCastDown()
    {
        Vector2 origin;
        float radius;
        float castDistance = Mathf.Max(0.12f, groundProbeRayDistance);

        if (capsule != null)
        {
            Bounds b = capsule.bounds;
            radius = Mathf.Min(b.extents.x * 0.9f, 0.22f);
            origin = new Vector2(b.center.x, b.min.y + radius + 0.01f);
        }
        else
        {
            origin = groundCheck.position;
            radius = groundCheckRadius;
        }

        const float minGroundNormalY = 0.6f;
        const float maxGroundNormalX = 0.6f;

        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, Vector2.down, castDistance, groundLayer);
        if (IsStandingOnCollider(hit.collider) &&
            hit.normal.y >= minGroundNormalY &&
            Mathf.Abs(hit.normal.x) <= maxGroundNormalX)
        {
            return true;
        }

        if (relaxedGroundFallback)
        {
            RaycastHit2D anyHit = Physics2D.CircleCast(origin, radius, Vector2.down, castDistance, Physics2D.DefaultRaycastLayers);
            if (IsStandingOnCollider(anyHit.collider) &&
                anyHit.normal.y >= minGroundNormalY &&
                Mathf.Abs(anyHit.normal.x) <= maxGroundNormalX)
            {
                Collider2D c = anyHit.collider;
                if (HasGroundLayerMask(c.gameObject.layer) || IsTilemapCollider(c) || c.CompareTag("Ground"))
                    return true;
            }
        }

        return false;
    }

    private bool HasGroundLayerMask(int layer) => (groundLayer.value & (1 << layer)) != 0;

    private bool IsTilemapCollider(Collider2D c)
    {
        if (c is TilemapCollider2D || c.gameObject.TryGetComponent<TilemapCollider2D>(out _))
            return true;
        Tilemap tm = c.GetComponentInParent<Tilemap>();
        return tm != null;
    }

    private void FlipCharacter()
    {
        if (horizontalInput > 0)
        {
            SetFacingDirection(false);
        }
        else if (horizontalInput < 0)
        {
            SetFacingDirection(true);
        }
    }

    private void SetFacingDirection(bool faceRight)
    {
        if (isFacingRight == faceRight) return;

        isFacingRight = faceRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;

        Vector3 attackPos = attackPoint.localPosition;
        attackPos.x = isFacingRight ? Mathf.Abs(attackPos.x) : -Mathf.Abs(attackPos.x);
        attackPoint.localPosition = attackPos;

        if (attackHitbox != null)
        {
            Vector3 hitboxPos = attackHitbox.transform.localPosition;
            hitboxPos.x = isFacingRight ? -Mathf.Abs(hitboxPos.x) : Mathf.Abs(hitboxPos.x);
            attackHitbox.transform.localPosition = hitboxPos;
        }

        UpdateAttackHitboxPosition();
    }

    public void SetRuneDodgeInvulnerable(bool active) => IsRuneDodgeInvulnerable = active;

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable || IsRuneDodgeInvulnerable) return;

        currentHealth -= damage;

        if (playerHealthUI != null)
        {
            playerHealthUI.UpdateLifeBar(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerHurt);
            }

            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    private void Die()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        animator.SetTrigger("Death");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeath);
        }

        if (gameOverManager != null)
        {
            StartCoroutine(ShowGameOverAfterDelay());
        }
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        gameOverManager.ShowGameOver();
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", rb.velocity.y < -0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, 0.35f);
        }
    }
}