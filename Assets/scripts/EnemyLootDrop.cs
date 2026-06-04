using UnityEngine;

[DisallowMultipleComponent]
public class EnemyLootDrop : MonoBehaviour
{
    [SerializeField] private GameObject lootPrefab;
    [Tooltip("Suele subir un poco en Y para que la runa no nazca dentro del piso.")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.35f, 0f);

    public void DropLoot()
    {
        if (lootPrefab == null)
        {
            Debug.LogWarning($"{name}: no hay prefab de loot asignado en EnemyLootDrop.");
            return;
        }

        Vector3 spawnPosition = transform.position + spawnOffset;
        GameObject loot = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);

        RuneDropLaunch launch = loot.GetComponent<RuneDropLaunch>();
        if (launch != null)
            launch.BeginDrop(spawnPosition);
    }
}
