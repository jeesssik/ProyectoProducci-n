using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Image lifeBarImage;
    [SerializeField] private Sprite[] lifeSprites;

    public void UpdateLifeBar(int currentHealth, int maxHealth)
    {
        if (lifeBarImage == null) return;

        if (lifeSprites == null || lifeSprites.Length == 0) return;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percentage = (float)currentHealth / maxHealth;

        int spriteIndex = Mathf.RoundToInt(
            percentage * (lifeSprites.Length - 1)
        );

        spriteIndex = Mathf.Clamp(
            spriteIndex,
            0,
            lifeSprites.Length - 1
        );

        lifeBarImage.sprite = lifeSprites[spriteIndex];
    }
}