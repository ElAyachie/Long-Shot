using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MissionState
{
    public int type;
    public int target;
    public int progress;
    public int reward;
    public bool done;
}

// Everything that persists between sessions. Stored as one JSON blob in PlayerPrefs.
[Serializable]
public class SaveData
{
    public int coins;
    public int best;
    public int lastScore;
    public int xp;
    public int games, goals, closeCalls, pickups, fires;
    public string ball = "classic";
    public string pitch = "classic";
    public List<string> owned = new List<string>();
    public bool sound = true;
    public bool vibrate = true;
    public string dailyDate = "";
    public int dailyStreak;
    public List<MissionState> missions = new List<MissionState>();
    public int missionsDone;
    public int tips;
}

public static class Save
{
    const string Key = "LongShot.Save.v1";

    public static SaveData D;

    public static void Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { D = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { D = null; }
        }
        if (D == null)
        {
            D = new SaveData();
            // Carry over scores from the original version of the game.
            D.best = PlayerPrefs.GetInt("HighScore", 0);
            D.lastScore = PlayerPrefs.GetInt("LastScore", 0);
        }
        if (D.owned == null) D.owned = new List<string>();
        if (D.missions == null) D.missions = new List<MissionState>();
    }

    public static void Write()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
        PlayerPrefs.SetInt("HighScore", D.best);
        PlayerPrefs.Save();
    }

    public static bool TipSeen(int bit) { return (D.tips & (1 << bit)) != 0; }
    public static void MarkTip(int bit) { D.tips |= 1 << bit; }
}
