using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class EnemyController : MonoBehaviour
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

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attack2Chance = 0.3f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;

    private Rigidbody2D rb;
    private Animator animator;

    private Transform currentPoint;
    private int currentHealth;

    private bool isFacingRight = true;
    private bool isGrounded;
    private bool isDead = false;
    private bool canAttack = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        currentPoint = pointB;
    }

    private void Update()
    {
        if (isDead) return;

        CheckGround();
        HandleBehaviour();
        UpdateAnimator();
    }

    private void HandleBehaviour()
    {
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                StopMovement();
                TryAttack();
                return;
            }

            if (distance <= detectionRange)
            {
                ChasePlayer();
                return;
            }
        }

        Patrol();
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null)
        {
            StopMovement();
            return;
        }

        float distanceToPoint = Vector2.Distance(transform.position, currentPoint.position);

        if (distanceToPoint < 0.2f)
        {
            currentPoint = currentPoint == pointA ? pointB : pointA;
        }

        MoveTowards(currentPoint.position);
    }

    private void ChasePlayer()
    {
        MoveTowards(player.position);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        float directionX = Mathf.Sign(targetPosition.x - transform.position.x);

        rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);

        if (directionX > 0 && !isFacingRight)
            Flip();

        if (directionX < 0 && isFacingRight)
            Flip();
    }

    private void StopMovement()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    private void TryAttack()
    {
        if (!canAttack) return;

        canAttack = false;

        float randomValue = Random.value;

        if (randomValue <= attack2Chance)
        {
            animator.SetTrigger("Attack2");
        }
        else
        {
            animator.SetTrigger("Attack");
        }

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
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

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", rb.velocity.y < -0.1f);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}