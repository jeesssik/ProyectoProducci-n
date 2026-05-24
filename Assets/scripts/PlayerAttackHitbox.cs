using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private PlayerController playerController;

    private readonly HashSet<EnemyController> enemiesHit = new HashSet<EnemyController>();

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }
    }

    private void OnEnable()
    {
        enemiesHit.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (playerController == null) return;

        // Solo hace daño si el PlayerController dice que estamos en ventana real de ataque
        if (!playerController.IsAttackDamageActive) return;

        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null) return;

        if (enemiesHit.Contains(enemy)) return;

        enemiesHit.Add(enemy);

        Debug.Log("ENEMIGO GOLPEADO CON ATAQUE: " + enemy.name);

        enemy.TakeDamage(damage);
    }
}