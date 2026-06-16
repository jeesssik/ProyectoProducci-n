using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("Ajustes de Flotado")]
    [SerializeField] private float amplitude = 0.15f; // Qué tan arriba/abajo va
    [SerializeField] private float frequency = 2f;    // Qué tan rápido flota

    private Vector3 startPosition;

    private void Start()
    {
        // Guardamos la posición inicial donde la pusiste en la escena
        startPosition = transform.position;
    }

    private void Update()
    {
        // Calculamos la nueva posición usando una onda Seno
        Vector3 tempPos = startPosition;
        tempPos.y += Mathf.Sin(Time.time * frequency) * amplitude;

        transform.position = tempPos;
    }
}
