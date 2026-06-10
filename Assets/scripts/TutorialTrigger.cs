using UnityEngine;
using System.Collections;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI del Tutorial")]
    [SerializeField] private GameObject tutorialPrompt;
    
    [Header("Ajustes de Tiempo")]
    [Tooltip("Cuánto tiempo (en segundos) se queda el cartel visible después de que el jugador se aleja.")]
    [SerializeField] private float extraTimeBeforeHide = 2.5f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        if (tutorialPrompt != null) tutorialPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Si había una cuenta regresiva para apagarse, la cancelamos porque el jugador volvió
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            
            if (tutorialPrompt != null) tutorialPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Iniciamos la cuenta regresiva para apagar el cartel
            hideCoroutine = StartCoroutine(HidePromptAfterDelay());
        }
    }

    private IEnumerator HidePromptAfterDelay()
    {
        // Espera los segundos que configuraste en el Inspector
        yield return new WaitForSeconds(extraTimeBeforeHide);
        
        if (tutorialPrompt != null) tutorialPrompt.SetActive(false);
        
      
        Destroy(gameObject);
    }
}