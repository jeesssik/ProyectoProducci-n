using UnityEngine;
using System.Collections;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI del Tutorial")]
    [SerializeField] private GameObject tutorialPrompt;
    
    [Header("Ajustes de Tiempo")]
    [Tooltip("Cuánto tiempo (en segundos) se queda el cartel visible después de que el jugador se aleja.")]
    [SerializeField] private float extraTimeBeforeHide = 2f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        if (tutorialPrompt != null) tutorialPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (tutorialPrompt != null) tutorialPrompt.SetActive(true);

            hideCoroutine = StartCoroutine(HidePromptAfterDelay());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // No hacemos nada: el cartel se oculta por tiempo aunque el jugador siga dentro.
        }
    }

    private IEnumerator HidePromptAfterDelay()
    {
        // Espera los segundos que configuraste en el Inspector
        yield return new WaitForSeconds(extraTimeBeforeHide);

        if (tutorialPrompt != null) tutorialPrompt.SetActive(false);

        hideCoroutine = null;

        Destroy(gameObject);
    }
}