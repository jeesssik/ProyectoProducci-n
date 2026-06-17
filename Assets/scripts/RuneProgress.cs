using System;
using System.Collections.Generic;

public static class RuneProgress
{
    private static readonly HashSet<RuneType> Unlocked = new HashSet<RuneType>();

    public static event Action<RuneType> OnRuneUnlocked;

    public static bool IsUnlocked(RuneType rune) => Unlocked.Contains(rune);

    public static void Unlock(RuneType rune)
    {
        if (!Unlocked.Add(rune))
            return;

        OnRuneUnlocked?.Invoke(rune);
    }

    public static void ResetForNewGame()
    {
        Unlocked.Clear();
    }
}
