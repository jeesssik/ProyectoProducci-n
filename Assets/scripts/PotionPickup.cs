using UnityEngine;

[DisallowMultipleComponent]
public class PotionPickup : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float pickupRadius = 1.2f;

    [Header("UI Indicador (Cartel Q)")]
    [Tooltip("Arrastrá acá el Canvas flotante o el texto de 'Presioná Q' que creaste dentro de la poción.")]
    [SerializeField] private GameObject interactionPrompt;

    private void Start()
    {
        // Al iniciar el nivel, el cartel arranca oculto por defecto
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // Buscamos si el jugador está dentro del radio configurado
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
        bool playerIsClose = false;
        PlayerController targetPlayer = null;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(playerTag)) continue;

            PlayerController player = hit.GetComponent<PlayerController>();
            if (player == null)
                player = hit.GetComponentInParent<PlayerController>();

            if (player == null || player.IsDead()) continue;

            // Si encontramos al jugador vivo y en rango, guardamos la referencia
            playerIsClose = true;
            targetPlayer = player;
            break; // Salimos del bucle porque ya encontramos al Player
        }

        // Controlamos la visibilidad del cartel de forma inteligente basándonos en si está cerca o no
        if (interactionPrompt != null)
        {
            if (playerIsClose && !interactionPrompt.activeSelf)
            {
                interactionPrompt.SetActive(true); // Se enciende al entrar en rango
            }
            else if (!playerIsClose && interactionPrompt.activeSelf)
            {
                interactionPrompt.SetActive(false); // Se apaga al alejarse
            }
        }

        // Si el jugador está cerca y presiona la tecla de interacción (Q), consume la poción
        if (playerIsClose && targetPlayer != null)
        {
            if (Input.GetKeyDown(pickupKey))
            {
                targetPlayer.RestoreFullHealth();
                
                // Opcional: Podés meter un sonido de curación acá si tenés AudioManager
                // if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.playerHeal);

                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}