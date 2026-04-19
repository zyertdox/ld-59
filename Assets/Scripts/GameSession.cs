using UnityEngine;

public static class GameSession
{
    const string UnlockedKey = "progress.unlocked";

    public static string CurrentLevelId;
    public static bool FastPlayback;

    public static int UnlockedLevels
    {
        get => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));
        set
        {
            PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(1, value));
            PlayerPrefs.Save();
        }
    }
}
