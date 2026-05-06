using UnityEngine;

public class EnemyDamageTrigger : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;

    private bool canDamage = true;
    private Animator enemyAnimator;

    private void Awake()
    {
        enemyAnimator = GetComponentInParent<Animator>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canDamage) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null || player.IsDead())
            {
                return;
            }

            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger("Attack");
            }

            player.TakeDamage(damage);
            player.ApplyKnockback(transform.position);

            canDamage = false;
            Invoke(nameof(ResetDamage), damageCooldown);
        }
    }
    private void ResetDamage()
    {
        canDamage = true;
    }
}