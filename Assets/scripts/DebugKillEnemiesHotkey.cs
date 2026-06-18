// =============================================================================
// DEBUG / TEST ONLY — NO DEJAR EN BUILDS FINALES
//
// Qué hace: tecla 9 mata a todos los enemigos del nivel de un golpe.
//
// Cómo sacarlo (elegí una):
//   1) Borrá este archivo: Assets/scripts/DebugKillEnemiesHotkey.cs
//   2) O comentá / borrá todo lo que está entre las marcas DEBUG_KILL_ENEMIES
//   3) O cambiá DEBUG_KILL_ENEMIES_ENABLED a false abajo
// =============================================================================

#define DEBUG_KILL_ENEMIES_ENABLED

#if DEBUG_KILL_ENEMIES_ENABLED

using UnityEngine;

/// <summary>Solo para testear niveles más rápido. Borrar con el archivo entero.</summary>
public class DebugKillEnemiesHotkey : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // DEBUG_KILL_ENEMIES: este GameObject se crea solo al cargar una escena.
        GameObject host = new GameObject("DEBUG_KillEnemiesHotkey");
        host.hideFlags = HideFlags.DontSave;
        DontDestroyOnLoad(host);
        host.AddComponent<DebugKillEnemiesHotkey>();
    }

    private void Update()
    {
        // DEBUG_KILL_ENEMIES: tecla 9 (fila superior del teclado).
        if (Input.GetKeyDown(KeyCode.Alpha9))
            KillAllEnemiesInScene();
    }

    private static void KillAllEnemiesInScene()
    {
        const int lethalDamage = 99999;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int killed = 0;
        int skippedBosses = 0;
        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (enemy.TriggersWinOnDeath)
            {
                skippedBosses++;
                continue;
            }

            enemy.TakeDamage(lethalDamage);
            killed++;
        }

        FlowerEnemy[] flowers = FindObjectsByType<FlowerEnemy>(FindObjectsSortMode.None);
        foreach (FlowerEnemy flower in flowers)
        {
            if (flower != null)
                flower.TakeDamage(lethalDamage);
        }

        Debug.Log($"[DEBUG_KILL_ENEMIES] Tecla 9: {killed} enemigos + {flowers.Length} flores dañados. Boss final omitidos: {skippedBosses}.");
    }
}

#endif
