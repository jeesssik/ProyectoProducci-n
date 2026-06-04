
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

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Si el overlap no encuentra el suelo contra el LayerMask, prueba overlaps sueltos (Tilemaps en Default, tag Ground).")]
    [SerializeField] private bool relaxedGroundFallback = true;

    [Tooltip("Ray muy corto hacia abajo para captar huecos entre tiles y líneas finas.")]
    [SerializeField] private float groundProbeRayDistance = 0.08f;

    private static readonly Collider2D[] OverlapScratch = new Collider2D[24];

    [Header("Combate")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointOffsetX = 0.5f;

    private bool isFacingRight = true;
    [SerializeField] private float attackCooldown = 0.45f;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private float healCooldown = 1.5f;
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
    private bool isKnockbacked = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CapsuleCollider2D capsule;

    private float horizontalInput;
    private bool isGrounded;
    private bool wasGroundedLastFrame; // NUEVO: Para detectar el momento exacto del aterrizaje
    private bool canAttack = true;
    private bool canHeal = true;
    private bool isDead = false;
    private bool isInvulnerable = false;

    private int currentHealth;

    private float _baseGravityScale;
    private float _jumpHoldTimer;
    private bool _isHoldingJump;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isGroundedForJump;

    public bool IsAttackDamageActive { get; private set; }

    private void Awake()
    {
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
    // Quitamos CheckGround de acá para que no dependa de los frames de renderizado
    UpdateJumpTimers();
    HandleJump();
    HandleAttack();
    HandleHeal();
    FlipCharacter();
    UpdateAnimator();
}

private void FixedUpdate()
{
    if (isDead || isKnockbacked) return;

    // Primero guardamos cómo estaba el suelo ANTES de movernos en este tick de físicas
    wasGroundedLastFrame = isGrounded;

    Move();
    ApplyBetterJumpPhysics();

    // Calculamos el suelo inmediatamente después del movimiento físico
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

        hitboxPos.x = attackToRight
            ? Mathf.Abs(hitboxPos.x)
            : -Mathf.Abs(hitboxPos.x);

        attackHitbox.transform.localPosition = hitboxPos;

        BoxCollider2D box = attackHitbox.GetComponent<BoxCollider2D>();

        if (box != null)
        {
            Vector2 offset = box.offset;

            offset.x = attackToRight
                ? -Mathf.Abs(offset.x)
                : Mathf.Abs(offset.x);

            box.offset = offset;
        }
    }

    private void ReadInput()
    {
        horizontalInput = 0f;
        try
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        catch { }

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

        bool grounded = isGrounded;
        if (!grounded)
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

        bool canUseCoyote = _coyoteTimer > 0f;
        bool bufferedJump = _jumpBufferTimer > 0f;

        if (bufferedJump && (_isGroundedForJump || canUseCoyote))
        {
            float bonus = 0f;
            float speedAbs = Mathf.Abs(rb.velocity.x);
            if (runJumpBonusAtSpeed > 0.01f)
            {
                float t = Mathf.Clamp01(speedAbs / runJumpBonusAtSpeed);
                bonus = runJumpBonus * t;
            }

            float initialVy = Mathf.Min(jumpForce + bonus, maxInitialJumpVelocity);
            rb.velocity = new Vector2(rb.velocity.x, initialVy);
            
            animator.SetTrigger("Jump");

            // INVOCACIÓN AUDIO: SALTO
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerJump);
            }

            _jumpHoldTimer = jumpHoldTime;
            _isHoldingJump = true;

            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        if (GetJumpUp())
        {
            _isHoldingJump = false;
        }
    }

    private bool GetJumpDown()
    {
        bool down = false;
        try { down = Input.GetButtonDown("Jump"); } catch { }
        return down || Input.GetKeyDown(KeyCode.Space);
    }

    private bool GetJumpUp()
    {
        bool up = false;
        try { up = Input.GetButtonUp("Jump"); } catch { }
        return up || Input.GetKeyUp(KeyCode.Space);
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
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            canAttack = false;

            FaceMouseDirection();

            animator.SetTrigger("Attack");

            // INVOCACIÓN AUDIO: ESPADAZO AL AIRE
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerAttack, 0.1f);
            }

            Invoke(nameof(ResetAttack), attackCooldown);
        }
    }

    private void Start()
    {
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
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
        else
        {
            Debug.LogWarning("AttackHitbox no está asignado en el Inspector del Player");
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
        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            attackPoint.position,
            0.35f,
            enemyLayer
        );

        foreach (Collider2D enemy in enemiesHit)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();

            if (enemyController != null)
            {
                enemyController.TakeDamage(1);
                // NOTA: El sonido de impacto se reproducirá desde el script del enemigo al recibir daño.
            }
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void HandleHeal()
    {
        if (Input.GetKeyDown(KeyCode.E) && canHeal && currentHealth < maxHealth)
        {
            Heal();
        }
    }

    private void Heal()
    {
        canHeal = false;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (playerHealthUI != null)
        {
            playerHealthUI.UpdateLifeBar(currentHealth, maxHealth);
        }

        animator.SetTrigger("Heal");

        Invoke(nameof(ResetHeal), healCooldown);
    }

    private void ResetHeal()
    {
        canHeal = true;
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
            CheckAterrizaje(); // Llama a la validación de impacto de caída
            return;
        }

        _isGroundedForJump = IsGroundedByCastDown();
        isGrounded = _isGroundedForJump;

        CheckAterrizaje(); // Llama a la validación de impacto de caída
    }

    // NUEVO MÉTODO: Detecta el frame exacto de aterrizaje
    private void CheckAterrizaje()
{
    // Evaluamos el impacto: si el frame anterior NO estaba en el suelo, AHORA SÍ lo está,
    // y además el Rigidbody venía bajando o quieto (evita errores al subir)
    if (!wasGroundedLastFrame && isGrounded && rb.velocity.y <= 0.1f)
    {
        // INVOCACIÓN AUDIO: ATERRIZAJE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerLand, 0.5f);
        }
    }
}

    // NUEVO MÉTODO PÚBLICO: Para llamarlo mediante Eventos de Animación (Animation Events)
    // Agregá este evento en los frames de pisada de tus animaciones de caminata/carrera
    public void PlayFootstepSFX()
{
    if (isGrounded && !isDead && AudioManager.Instance != null)
    {
        // Le pasamos un 0.25f (25% del volumen original)
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

    private bool IsValidGroundHit(RaycastHit2D hit, float minNormalY)
    {
        if (!IsStandingOnCollider(hit.collider)) return false;
        return hit.normal.y >= minNormalY;
    }

    private static readonly ContactPoint2D[] ContactScratch = new ContactPoint2D[24];

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

    private bool HasGroundLayerMask(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    private bool IsTilemapCollider(Collider2D c)
    {
        if (c is TilemapCollider2D || c.gameObject.TryGetComponent<TilemapCollider2D>(out _))
            return true;
        Tilemap tm = c.GetComponentInParent<Tilemap>();
        return tm != null;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;

        Vector3 attackPos = attackPoint.localPosition;
        attackPos.x = isFacingRight ? Mathf.Abs(attackPos.x) : -Mathf.Abs(attackPos.x);
        attackPoint.localPosition = attackPos;
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

    private void FaceMouseDirection()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool shouldFaceRight = mouseWorldPosition.x < transform.position.x;
        SetFacingDirection(shouldFaceRight);
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

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;

        if (playerHealthUI != null)
        {
            playerHealthUI.UpdateLifeBar(currentHealth, maxHealth);
        }
        Debug.Log("Player recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt");

            // INVOCACIÓN AUDIO: RECIBIR DAÑO
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

        // INVOCACIÓN AUDIO: MUERTE
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeath);
        }

        Debug.Log("Player murió");

        if (gameOverManager != null)
        {
            StartCoroutine(ShowGameOverAfterDelay());
        }
        else
        {
            Debug.LogWarning("No está asignado el GameOverManager en el PlayerController.");
        }
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        gameOverManager.ShowGameOver();
    }

    private void ShowGameOver()
    {
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