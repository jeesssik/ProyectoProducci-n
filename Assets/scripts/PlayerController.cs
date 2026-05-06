using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Combate")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackCooldown = 0.45f;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private float healCooldown = 1.5f;
    [SerializeField] private float invulnerabilityTime = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;
    private bool isGrounded;
    private bool canAttack = true;
    private bool canHeal = true;
    private bool isDead = false;
    private bool isInvulnerable = false;

    private int currentHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        ReadInput();
        CheckGround();
        HandleJump();
        HandleAttack();
        HandleHeal();
        FlipCharacter();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Move();
    }

    private void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    private void Move()
    {
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            canAttack = false;
            animator.SetTrigger("Attack");

            Invoke(nameof(ResetAttack), attackCooldown);
        }
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

        animator.SetTrigger("Heal");

        Invoke(nameof(ResetHeal), healCooldown);
    }

    private void ResetHeal()
    {
        canHeal = true;
    }

    private void CheckGround()
    {
        if (groundCheck == null) return;

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void FlipCharacter()
    {
        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        Debug.Log("Player recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.SetTrigger("Hurt");
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

        Debug.Log("Player murió");
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