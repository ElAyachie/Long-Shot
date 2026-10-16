using System;
using System.Collections.Generic;
using UnityEngine;

// New types go at the end: saves store these as numbers.
public enum MissionType { ScoreInRun, Goals, CloseCalls, CloseInRun, Coins, Games, Fire, Powerups, BankShots, TrickShots }

// Player level, missions and the daily bonus. All state lives in Save.D.
public static class Progression
{
    public const int MissionSlots = 3;
    public static readonly int[] DailyRewards = { 20, 30, 40, 60, 80, 100, 250 };

    static readonly string[] ranks = { "SUNDAY LEAGUE", "ACADEMY", "RESERVES", "STARTER", "PRO", "CAPTAIN", "STAR", "LEGEND", "G.O.A.T." };
    static readonly int[] rankLevels = { 1, 3, 6, 10, 15, 20, 30, 40, 50 };

    // ---------- Levels ----------

    public static int XpToNext(int level) { return 25 + 15 * (level - 1); }

    public static int Level { get { int l, x; LevelFromXp(Save.D.xp, out l, out x); return l; } }

    public static void LevelFromXp(int xp, out int level, out int intoLevel)
    {
        level = 1;
        while (xp >= XpToNext(level)) { xp -= XpToNext(level); level++; }
        intoLevel = xp;
    }

    public static float LevelProgress(int xp)
    {
        int l, into;
        LevelFromXp(xp, out l, out into);
        return into / (float)XpToNext(l);
    }

    public static string Rank(int level)
    {
        string r = ranks[0];
        for (int i = 0; i < rankLevels.Length; i++) if (level >= rankLevels[i]) r = ranks[i];
        return r;
    }

    public static int LevelReward(int level) { return 20 + 10 * level; }

    // Adds xp and pays out level-up rewards. Returns how many levels were gained.
    public static int AddXp(int amount)
    {
        int before = Level;
        Save.D.xp += Mathf.Max(0, amount);
        int after = Level;
        for (int l = before + 1; l <= after; l++) Save.D.coins += LevelReward(l);
        return after - before;
    }

    // ---------- Missions ----------

    public static void EnsureMissions()
    {
        var list = Save.D.missions;
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].done) list.RemoveAt(i);
        while (list.Count < MissionSlots) list.Add(NewMission());
    }

    public static bool AnyMissionDone()
    {
        foreach (var m in Save.D.missions) if (m.done) return true;
        return false;
    }

    static MissionState NewMission()
    {
        var used = new List<int>();
        foreach (var m in Save.D.missions) used.Add(m.type);
        var options = new List<int>();
        int typeCount = Enum.GetValues(typeof(MissionType)).Length;
        for (int t = 0; t < typeCount; t++) if (!used.Contains(t)) options.Add(t);
        int type = options[UnityEngine.Random.Range(0, options.Count)];
        int tier = Save.D.missionsDone / 3;
        var ms = new MissionState { type = type, target = Target((MissionType)type, tier) };
        ms.reward = Mathf.Min(25 + 10 * tier, 150);
        return ms;
    }

    static int Target(MissionType t, int tier)
    {
        switch (t)
        {
            case MissionType.ScoreInRun: return Mathf.Min(8 + 5 * tier, 120);
            case MissionType.Goals: return 15 + 10 * tier;
            case MissionType.CloseCalls: return 4 + 3 * tier;
            case MissionType.CloseInRun: return Mathf.Min(2 + tier, 12);
            case MissionType.Coins: return 5 + 4 * tier;
            case MissionType.Games: return Mathf.Min(3 + tier, 10);
            case MissionType.Fire: return Mathf.Min(1 + tier / 2, 6);
            case MissionType.BankShots: return 3 + 2 * tier;
            case MissionType.TrickShots: return Mathf.Min(2 + tier, 15);
            default: return Mathf.Min(2 + tier, 12);
        }
    }

    public static string Describe(MissionState m)
    {
        switch ((MissionType)m.type)
        {
            case MissionType.ScoreInRun: return "Score " + m.target + " points in one match";
            case MissionType.Goals: return "Score " + m.target + " goals";
            case MissionType.CloseCalls: return "Get " + m.target + " close calls";
            case MissionType.CloseInRun: return "Get " + m.target + " close calls in one match";
            case MissionType.Coins: return "Grab " + m.target + " coins on the pitch";
            case MissionType.Games: return "Play " + m.target + " matches";
            case MissionType.Fire: return "Get ON FIRE " + m.target + (m.target == 1 ? " time" : " times");
            case MissionType.BankShots: return "Score " + m.target + " bank shots";
            case MissionType.TrickShots: return "Land " + m.target + " trick shots";
            default: return "Grab " + m.target + " power-ups";
        }
    }

    // Records progress. "Run" missions take the best single-run value instead of accumulating.
    // Rewards are paid immediately; returns the missions that were completed by this call.
    public static List<MissionState> Track(MissionType type, int amount)
    {
        var completed = new List<MissionState>();
        bool runBased = type == MissionType.ScoreInRun || type == MissionType.CloseInRun;
        foreach (var m in Save.D.missions)
        {
            if (m.done || m.type != (int)type) continue;
            m.progress = runBased ? Mathf.Max(m.progress, amount) : m.progress + amount;
            if (m.progress >= m.target)
            {
                m.progress = m.target;
                m.done = true;
                Save.D.coins += m.reward;
                Save.D.missionsDone++;
                completed.Add(m);
            }
        }
        return completed;
    }

    // ---------- Daily bonus ----------

    static string Today { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }

    // Which day of the 7-day cycle can be claimed right now (1-7), or 0 if already claimed today.
    public static int DailyAvailable()
    {
        if (Save.D.dailyDate == Today) return 0;
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        return Save.D.dailyDate == yesterday ? Save.D.dailyStreak % 7 + 1 : 1;
    }

    // Day shown as "current" in the calendar, whether or not it has been claimed yet.
    public static int DailyCurrentDay()
    {
        int d = DailyAvailable();
        return d > 0 ? d : Mathf.Max(1, Save.D.dailyStreak);
    }

    public static int ClaimDaily()
    {
        int day = DailyAvailable();
        if (day == 0) return 0;
        int reward = DailyRewards[day - 1];
        Save.D.dailyStreak = day;
        Save.D.dailyDate = Today;
        Save.D.coins += reward;
        Save.Write();
        return reward;
    }
}
