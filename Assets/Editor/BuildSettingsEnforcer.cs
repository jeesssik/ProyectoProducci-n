#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Asegura que Menu, Level-1 y Level-2 estén en Build Settings al abrir el proyecto.
/// </summary>
[InitializeOnLoad]
public static class BuildSettingsEnforcer
{
    private static readonly string[] RequiredScenes =
    {
        "Assets/Scenes/Menu.unity",
        "Assets/Scenes/Level-1.unity",
        "Assets/Scenes/Level-2.unity",
    };

    static BuildSettingsEnforcer()
    {
        EnsureScenes();
    }

    private static void EnsureScenes()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;

        foreach (string path in RequiredScenes)
        {
            if (scenes.Any(s => s.path == path))
                continue;

            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"BuildSettingsEnforcer: no se encontró {path}");
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
        }

        if (changed)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("BuildSettingsEnforcer: se añadieron escenas faltantes a Build Settings.");
        }
    }
}
#endif
