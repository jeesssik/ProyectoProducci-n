using UnityEngine;

[DisallowMultipleComponent]
public class KillOnFall : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Tooltip("Referencia al borde inferior (Borders/Bottom). Si el jugador cae por debajo, muere.")]
    [SerializeField] private Transform bottomBorder;

    [Header("Tuning")]
    [Tooltip("Margen extra (en unidades de mundo) por debajo del Bottom para evitar muertes por rozar el borde.")]
    [SerializeField] private float extraMargin = 1.0f;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (player == null || player.IsDead()) return;
        if (bottomBorder == null) return;

        if (transform.position.y < bottomBorder.position.y - extraMargin)
        {
            // Reusar la lógica de muerte del player (animación + GameOverManager).
            player.TakeDamage(999999);
        }
    }
}

