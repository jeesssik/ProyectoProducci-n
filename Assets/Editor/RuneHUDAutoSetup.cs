#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RuneHUDAutoSetup
{
    private const string PrefabPath = "Assets/Prefabs/UI/RuneHUD.prefab";

    private static readonly string[] LevelScenes =
    {
        "Assets/Scenes/Level-1.unity",
        "Assets/Scenes/Level-2.unity",
        "Assets/Scenes/Level-3.unity"
    };

    static RuneHUDAutoSetup()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/UI/Setup Rune HUD In All Levels")]
    public static void SetupManually()
    {
        TrySetup();
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool changed = false;

        if (!File.Exists(PrefabPath))
        {
            RuneHUDBuilder.CreatePrefab();
            AssetDatabase.Refresh();
            changed = true;
        }

        if (!AllLevelScenesHaveHud())
        {
            AddHudToMissingScenes();
            changed = true;
        }

        if (changed)
            Debug.Log("Rune HUD listo: prefab creado y agregado a los niveles que faltaban.");
    }

    private static bool AllLevelScenesHaveHud()
    {
        foreach (string scenePath in LevelScenes)
        {
            if (!File.Exists(scenePath))
                continue;

            if (!SceneContainsHud(scenePath))
                return false;
        }

        return true;
    }

    private static bool SceneContainsHud(string scenePath)
    {
        string yaml = File.ReadAllText(scenePath);
        return yaml.Contains("m_Name: RuneHUD") || yaml.Contains("guid: cd83f8dc3bc9c2f43992c0f993099178");
    }

    private static void AddHudToMissingScenes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            return;

        string activeScenePath = SceneManager.GetActiveScene().path;

        foreach (string scenePath in LevelScenes)
        {
            if (!File.Exists(scenePath) || SceneContainsHud(scenePath))
                continue;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
    }
}
#endif
