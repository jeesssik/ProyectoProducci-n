using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private readonly HashSet<IDamageable> enemiesHit = new HashSet<IDamageable>();

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
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        if (enemiesHit.Contains(damageable))
        {
            return;
        }

        enemiesHit.Add(damageable);

        Debug.Log("El ataque del jugador golpeó a: " + other.name);

        damageable.TakeDamage(damage);
    }
}