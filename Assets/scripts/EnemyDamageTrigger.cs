using UnityEngine;

public class EnemyDamageTrigger : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;
    [SerializeField] private int damageFromPlayerAttack = 2;
    [SerializeField] private string playerAttackTag = "PlayerAttack";

    private bool canDamage = true;
    private Animator enemyAnimator;
    private EnemyController _enemy;
    private IDamageable _damageable;

    private void Awake()
    {
        enemyAnimator = GetComponentInParent<Animator>();
        _enemy = GetComponentInParent<EnemyController>();
        _damageable = GetComponentInParent<IDamageable>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerAttackTag))
            return;

        if (_enemy != null)
            _enemy.TakeDamage(damageFromPlayerAttack);
        else if (_damageable != null)
            _damageable.TakeDamage(damageFromPlayerAttack);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canDamage) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null || player.IsDead())
                return;

            if (enemyAnimator != null)
                enemyAnimator.SetTrigger("Attack");

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
