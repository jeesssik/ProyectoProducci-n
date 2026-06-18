using UnityEngine;

[CreateAssetMenu(fileName = "RuneIconLibrary", menuName = "Game/Rune Icon Library")]
public class RuneIconLibrary : ScriptableObject
{
    [SerializeField] private Sprite yellowIcon;
    [SerializeField] private Sprite greenIcon;
    [SerializeField] private Sprite celesteIcon;
    [SerializeField] private Sprite redIcon;

    private static RuneIconLibrary _cached;

    public static RuneIconLibrary Instance
    {
        get
        {
            if (_cached == null)
                _cached = Resources.Load<RuneIconLibrary>("RuneIconLibrary");

            return _cached;
        }
    }

    public Sprite GetIcon(RuneType rune)
    {
        switch (rune)
        {
            case RuneType.Yellow: return yellowIcon;
            case RuneType.Green: return greenIcon;
            case RuneType.Celeste: return celesteIcon;
            case RuneType.Red: return redIcon;
            default: return null;
        }
    }
}
