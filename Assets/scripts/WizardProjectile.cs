using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class WizardProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float speed = 8f;

    private Rigidbody2D rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        direction.Normalize();
        rb.velocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Ground"))
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !player.IsDead())
        {
            player.TakeDamage(damage);
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
