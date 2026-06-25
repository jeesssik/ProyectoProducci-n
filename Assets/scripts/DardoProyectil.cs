using UnityEngine;

public class DardoProyectil : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 4f; 

    private Rigidbody2D rb;
    private GameObject emisor; // Guardamos quién lo disparó para ignorarlo

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Destrucción por tiempo si no impacta nada
        Destroy(gameObject, lifeTime);
    }

    // 🚀 Función clave: El enemigo llamará aquí al instanciarlo para darle velocidad y dueño
    public void InicializarProyectil(float direccionX, GameObject creador)
    {
        rb = GetComponent<Rigidbody2D>();
        emisor = creador;

        if (rb != null)
        {
            // Aplicamos la velocidad usando la dirección exacta recibida del Chaman
            rb.velocity = new Vector2(direccionX * speed, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ❌ REGLA DE ORO: Si colisiona con el Chaman que lo disparó, se ignora por completo
        if (emisor != null && (other.gameObject == emisor || other.transform.IsChildOf(emisor.transform))) 
            return;

        // Ignorar también si choca con otros ataques del jugador o enemigos genéricos
        if (other.CompareTag("Enemy") || other.CompareTag("PlayerAttack") || other.CompareTag("Projectile")) 
            return;

        // 🎯 Si golpea al Player
        if (other.CompareTag("Player"))
        {
            // Aquí podés descomentar tu sistema de daño cuando lo vincules
            // PlayerController player = other.GetComponent<PlayerController>();
            // if (player != null) player.TakeDamage(damage);
            
            Debug.Log("🎯 ¡El dardo impactó al Player!");
            Destroy(gameObject);
            return;
        }

        // 🧱 Si golpea el suelo, plataformas o paredes sólidas
        if (!other.isTrigger)
        {
            Debug.Log($"🧱 El dardo impactó contra estructura: {other.name}");
            Destroy(gameObject);
        }
    }
}