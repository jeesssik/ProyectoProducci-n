using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private readonly HashSet<IDamageable> enemiesHit = new HashSet<IDamageable>();
    private PlayerRuneAbilities _runeAbilities;

    private void Awake()
    {
        _runeAbilities = GetComponentInParent<PlayerRuneAbilities>();
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

        int hitDamage = damage;
        if (_runeAbilities != null && _runeAbilities.TryConsumeReprisalAttack(out int reprisalDamage))
            hitDamage = reprisalDamage;

        Debug.Log("El ataque del jugador golpeó a: " + other.name);

        damageable.TakeDamage(hitDamage);
    }

    public void HitEverythingInsideNow()
{
    Collider2D myCollider = GetComponent<Collider2D>();

    if (myCollider == null)
    {
        Debug.LogWarning("PlayerAttackHitbox no tiene Collider2D.");
        return;
    }

    ContactFilter2D filter = new ContactFilter2D();
    filter.NoFilter();

    Collider2D[] results = new Collider2D[20];

    int count = Physics2D.OverlapCollider(myCollider, filter, results);

    for (int i = 0; i < count; i++)
    {
        TryHit(results[i]);
    }
}
}