using UnityEngine;

// How hard the pitch is after a given number of goals.
public struct Difficulty
{
    public int defenders;      // active lanes (1-4)
    public int partners;       // extra defenders doubling up lanes
    public float minSpeed, maxSpeed;
    public float feintChance;  // chance a defender stops and changes pace
    public bool keeper;
    public float keeperSpeed;
    public bool goalMoves;
    public float goalAmp, goalFreq;
    public int bumpers;        // pinball bumpers on the pitch
    public float windMax;      // strongest possible gust (0 = always calm)

    public const int KeeperAt = 15, GoalMovesAt = 22, FeintsAt = 28, PartnersAt = 35;
    public const int WindAt = 8, BumpersAt = 10;

    public static Difficulty For(int goals)
    {
        var d = new Difficulty();
        d.defenders = goals < 3 ? 1 : goals < 7 ? 2 : goals < 12 ? 3 : 4;
        d.partners = goals < PartnersAt ? 0 : goals < 45 ? 1 : goals < 60 ? 2 : 3;
        d.minSpeed = Mathf.Min(1.6f + 0.045f * goals, 3.6f);
        d.maxSpeed = Mathf.Min(2.6f + 0.07f * goals, 6.0f);
        d.feintChance = goals < FeintsAt ? 0f : Mathf.Min(0.3f + 0.01f * (goals - FeintsAt), 0.6f);
        d.keeper = goals >= KeeperAt;
        d.keeperSpeed = Mathf.Min(1.0f + 0.04f * (goals - KeeperAt), 2.6f);
        d.goalMoves = goals >= GoalMovesAt;
        d.goalAmp = Mathf.Min(0.9f + 0.02f * (goals - GoalMovesAt), 1.4f);
        d.goalFreq = Mathf.Min(1.8f + 0.03f * (goals - GoalMovesAt), 3.2f);
        d.bumpers = goals < BumpersAt ? 0 : goals < 20 ? 1 : goals < 30 ? 2 : 3;
        d.windMax = goals < WindAt ? 0f : Mathf.Min(1.2f + 0.05f * (goals - WindAt), 3f);
        return d;
    }

    // Gentle background activity for the title screen.
    public static Difficulty Menu()
    {
        var d = For(0);
        d.defenders = 2;
        d.minSpeed = 1.2f;
        d.maxSpeed = 1.8f;
        return d;
    }
}
