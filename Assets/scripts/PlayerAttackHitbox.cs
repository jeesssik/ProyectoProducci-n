using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private readonly HashSet<EnemyController> enemiesHit = new HashSet<EnemyController>();

   private void OnEnable()
{
    enemiesHit.Clear();
   // Debug.Log("Hitbox de ataque ACTIVADA - puede hacer daño");
}

private void OnDisable()
{
   // Debug.Log("Hitbox de ataque DESACTIVADA - no puede hacer daño");
}

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("La hitbox tocó a: " + other.name);

        EnemyController enemy = other.GetComponent<EnemyController>();

        if (enemy == null)
        {
           // Debug.Log("El objeto tocado no tiene EnemyController");
            return;
        }

        if (enemiesHit.Contains(enemy))
        {
            Debug.Log("Este enemigo ya fue golpeado en este ataque: " + other.name);
            return;
        }

        enemiesHit.Add(enemy);

        Debug.Log("ENEMIGO GOLPEADO CON ATAQUE: " + other.name);

        enemy.TakeDamage(damage);
    }

   
}