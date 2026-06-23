using UnityEngine;

public class DardoProyectil : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 4f; // Para que no viaje infinitamente si erra

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Destruir automáticamente tras unos segundos para optimizar memoria
        Destroy(gameObject, lifeTime);

        // 🚀 Movimiento: Viaja hacia adelante relativo a su propia rotación
        if (rb != null)
        {
            // Como el enemigo cambia el localScale en X (-4.5 o 4.5), el FirePoint hereda esa dirección.
            // Con transform.right * -1 o transform.right controlamos el avance. 
            // Evaluamos el signo de la escala para saber hacia dónde mirar
            float direction = Mathf.Sign(transform.lossyScale.x);
            
            // Si tu sprite por defecto apunta a la izquierda, usamos direction. Si apunta a la derecha, -direction.
            // Probemos con el estándar:
            rb.velocity = new Vector2(-direction * speed, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Evitamos golpear al propio enemigo o a otros proyectiles
        if (other.CompareTag("Enemy") || other.CompareTag("PlayerAttack")) return;

        // Si golpea al Player
        if (other.CompareTag("Player"))
        {
            // Aquí llamarías al sistema de daño de tu Player, por ejemplo:
            // PlayerController player = other.GetComponent<PlayerController>();
            // if (player != null) player.TakeDamage(damage);
            
            Debug.Log("El dardo golpeó al jugador.");
            Destroy(gameObject);
            return;
        }

        // Si golpea el suelo/paredes (Cualquier objeto sólido que no sea trigger)
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}