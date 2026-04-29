using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Combat")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.4f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    private Rigidbody2D rb;
    private Animator animator;

    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool canAttack = true;
    private bool isDead = false;

    private int currentHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        ReadInput();
        CheckGround();
        HandleJump();
        HandleAttack();
        FlipCharacter();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Move();
    }

    // -------------------------
    // INPUT
    // -------------------------

    private void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    // -------------------------
    // MOVEMENT
    // -------------------------

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

    private void FlipCharacter()
    {
        if (horizontalInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // -------------------------
    // COMBAT
    // -------------------------

    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            Attack();
        }
    }

    private void Attack()
    {
        canAttack = false;

        animator.SetTrigger("Attack");

        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius//,
          //  enemyLayer
        );

       /* foreach (Collider2D enemy in enemiesHit)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }*/

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    // -------------------------
    // GROUND CHECK
    // -------------------------

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // -------------------------
    // HEALTH
    // -------------------------

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    private void Die()
    {
        isDead = true;
        rb.velocity = Vector2.zero;

        animator.SetBool("IsDead", true);
        animator.SetTrigger("Death");

        // Si usás respawn después, lo podés llamar acá
    }

    // -------------------------
    // ANIMATIONS
    // -------------------------

    private void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDead", isDead);
    }

    // -------------------------
    // DEBUG
    // -------------------------

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
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}