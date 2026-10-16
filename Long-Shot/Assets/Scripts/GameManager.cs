using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RunResult
{
    public string reason;
    public int score, goals, closeCalls, coins, xp, levelsGained, best;
    public bool newBest;
}

// Boots itself into the main scene, takes over the scene objects and runs the whole game loop.
public class GameManager : MonoBehaviour
{
    public enum State { Menu, Playing, Dying, Continue, GameOver }

    public static GameManager I;
    public static bool Paused;

    public const float FieldHalf = 2.45f;
    public const float PatrolHalf = 2.05f;
    public const float QuickWindow = 1.5f;
    const float AimDeadZone = 0.3f;   // world units of pull before it counts as aiming
    const float MaxPull = 2.2f;       // world units of pull for full power
    const float MaxAimAngle = 70f;    // degrees either side of straight up
    const float CurveSwipe = 1.2f;    // world units of sideways swipe for full curve
    public const int FireStreak = 5;
    public const int ContinueCost = 40;

    static readonly float[] LaneY = { -1.15f, 0.15f, 1.45f, 2.75f };
    static readonly float[] GapY = { -1.95f, -0.55f, 0.75f, 2.05f };
    static readonly float[] BumperRows = { -0.55f, 0.75f, 2.05f };
    static readonly string[] TrickNames = { "", "", "TRICK SHOT!", "SUPER TRICK!", "INSANE!" };
    static readonly int[] TrickBonus = { 0, 0, 2, 4, 8 };
    static readonly int[] LaneOrder = { 2, 0, 3, 1 };
    static readonly int[] PartnerOrder = { 1, 3, 0, 2 };
    const float KeeperY = 3.42f;

    public State St { get; private set; }
    public Camera Cam { get; private set; }
    public Goal Goal { get; private set; }
    public Goalkeeper Keeper { get; private set; }
    public Ball Ball { get; private set; }
    public readonly List<Defender> Defenders = new List<Defender>();
    public readonly List<Pickup> Pickups = new List<Pickup>();
    public readonly List<Bumper> Bumpers = new List<Bumper>();
    public float Wind { get; private set; }  // sideways push in units/s^2, + blows right
    public Difficulty Diff { get; private set; }

    readonly Defender[] lanes = new Defender[4];
    readonly Defender[] partners = new Defender[4];
    GameUI ui;
    Pitch pitch;
    Boards boards;
    AimGuide guide;
    bool aiming, aimDragged, aimValid;
    Vector2 aimStart, aimDir;
    float aimSpeed;
    bool curving, curveDone;
    Vector2 curveStart;
    readonly HashSet<string> shotTricks = new HashSet<string>();
    float windStreakTimer;
    Vector3 spawnPos;
    int lastW, lastH;

    // Run state.
    public int Score { get; private set; }
    public int Goals { get; private set; }
    public int RunCoins { get; private set; }
    public int CloseCalls { get; private set; }
    public int Streak { get; private set; }
    public bool OnFire { get; private set; }
    public bool Shield { get; private set; }
    public int SlowShots { get; private set; }
    public int DoubleGoals { get; private set; }
    public bool CanContinue { get { return !continued && Save.D.coins >= ContinueCost && Score >= 3; } }
    bool continued, quickShot, bestAnnounced;
    int startBest;

    public int Multiplier { get { return (OnFire ? 2 : 1) * (DoubleGoals > 0 ? 2 : 1); } }
    public float SpeedMul { get { return SlowShots > 0 ? 0.55f : 1f; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        if (GameObject.FindWithTag("goal") == null) return; // only the game scene
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Input.multiTouchEnabled = false;
        Save.Load();
        Progression.EnsureMissions();

        Cam = Camera.main;
        Cam.orthographic = true;

        // The old UI is replaced by GameUI.
        var oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas != null) Destroy(oldCanvas);
        if (EventSystem.current == null && FindEventSystem() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var fx = new GameObject("Fx").AddComponent<Fx>();
        fx.Init(Cam);
        FitCamera();

        Goal = GameObject.FindWithTag("goal").GetComponent<Goal>();

        // Defenders: sorted bottom to top and pinned to fixed lanes.
        var found = new List<Defender>();
        foreach (var go in GameObject.FindGameObjectsWithTag("defender"))
        {
            var d = go.GetComponent<Defender>();
            if (d != null) found.Add(d);
        }
        found.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        AudioClip hit1 = null, hit2 = null;
        for (int i = 0; i < 4 && i < found.Count; i++)
        {
            lanes[i] = found[i];
            lanes[i].LaneY = LaneY[i];
            lanes[i].Hide();
            Defenders.Add(lanes[i]);
            if (hit1 == null) hit1 = found[i].HitClip;
            else if (hit2 == null && found[i].HitClip != hit1) hit2 = found[i].HitClip;
        }
        for (int i = 0; i < 4; i++)
        {
            if (lanes[i] == null) continue;
            var p = Instantiate(lanes[i].gameObject).GetComponent<Defender>();
            p.name = "partner " + i;
            p.LaneY = LaneY[i];
            p.Hide();
            partners[i] = p;
            Defenders.Add(p);
        }

        // Keeper reaches just past the posts at the ends of its patrol, leaving a gap to shoot through.
        Keeper = Goalkeeper.Create(KeeperY, Goal.HalfWidth - 0.08f);

        // Ball comes from the spawn point's prefab.
        var spawnGo = GameObject.Find("ball spawn");
        var spawner = spawnGo != null ? spawnGo.GetComponent<Ball_Spawn>() : null;
        spawnPos = new Vector3(0f, spawnGo != null ? spawnGo.transform.position.y : -2.55f, -3.5f);
        Ball = Instantiate(spawner.ball, spawnPos, Quaternion.identity).GetComponent<Ball>();
        Ball.Init(spawnPos);

        var sfx = gameObject.AddComponent<Sfx>();
        sfx.Init(Ball.KickClip, Goal.GoalClip, hit1, hit2);
        Sfx.Apply();

        boards = Boards.Create();
        guide = AimGuide.Create();
        pitch = Pitch.Create();
        ApplyPitch();

        ui = gameObject.AddComponent<GameUI>();
        ui.Build(this);

        EnterMenu();
    }

    static EventSystem FindEventSystem()
    {
        var go = GameObject.Find("EventSystem");
        return go != null ? go.GetComponent<EventSystem>() : null;
    }

    public void ApplyPitch()
    {
        pitch.Apply(Catalog.CurrentPitch, Cam, Goal.FrameBottom, spawnPos.y);
    }

    void FitCamera()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
        Cam.orthographicSize = Mathf.Max(4.3f, 2.72f / aspect);
        Fx.I.SetCameraBase(new Vector3(0f, 0.85f, -10f));
    }

    // ---------- Flow ----------

    public void EnterMenu()
    {
        St = State.Menu;
        SetPaused(false);
        ClearPickups();
        ClearBumpers();
        Goal.ResetGoal();
        Keeper.SetActive(false, false);
        foreach (var d in Defenders) d.Hide();
        Diff = Difficulty.Menu();
        ApplyDifficulty(false);
        ResetRunState();
        Ball.ResetToSpawn();
        CancelAim();
        boards.SetLitEnabled(false);
        ui.ShowMenu();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    void ResetRunState()
    {
        Score = Goals = RunCoins = CloseCalls = Streak = SlowShots = DoubleGoals = 0;
        OnFire = Shield = continued = bestAnnounced = false;
        Wind = 0f;
    }

    public void StartRun()
    {
        Progression.EnsureMissions();
        ResetRunState();
        startBest = Save.D.best;
        ClearPickups();
        ClearBumpers();
        Goal.ResetGoal();
        Keeper.SetActive(false, false);
        foreach (var d in Defenders) d.Hide();
        St = State.Playing;
        SetPaused(false);
        Diff = Difficulty.For(0);
        ApplyDifficulty(false);
        Ball.ResetToSpawn();
        ui.ShowHUD();
        Sfx.I.Play(Sfx.I.Whistle, 0.8f);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        CancelAim();
        boards.SetLitEnabled(true);
        if (!Save.TipSeen(0)) ui.Hint("PULL BACK TO AIM, LET GO TO SHOOT\nTAP FOR A STRAIGHT SHOT");
        MaybeSpawnPickup();
    }

    void ApplyDifficulty(bool announce)
    {
        var prev = Diff;
        var d = St == State.Menu ? Difficulty.Menu() : Difficulty.For(Goals);
        Diff = d;

        for (int i = 0; i < 4; i++)
        {
            var lane = lanes[LaneOrder[i]];
            if (lane == null) continue;
            if (i < d.defenders && !lane.Visible) lane.Enter(Random.value < 0.5f, d);
            if (i >= d.defenders && lane.Visible) lane.Hide();

            var partner = partners[PartnerOrder[i]];
            if (partner == null) continue;
            if (i < d.partners && !partner.Visible) partner.Enter(Random.value < 0.5f, d);
            if (i >= d.partners && partner.Visible) partner.Hide();
        }
        Keeper.SetActive(d.keeper, true);
        if (d.bumpers != Bumpers.Count) RollBumpers(d.bumpers);

        if (!announce) return;
        if (d.bumpers > 0 && prev.bumpers == 0) ui.Banner("BUMPERS!", GameUI.Red);
        else if (d.keeper && !prev.keeper) ui.Banner("KEEPER IN GOAL!", GameUI.Yellow);
        else if (d.goalMoves && !prev.goalMoves) ui.Banner("MOVING GOAL!", GameUI.Yellow);
        else if (d.feintChance > 0f && prev.feintChance <= 0f) ui.Banner("TRICKY DEFENDERS!", GameUI.Yellow);
        else if (d.partners > prev.partners) ui.Banner("DOUBLE TEAMED!", GameUI.Red);
        else if (d.defenders > prev.defenders) ui.Banner(d.defenders == 4 ? "FULL BACK LINE!" : "NEW DEFENDER!", GameUI.Yellow);
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) FitCamera();
        if (Paused) return;

        float dt = Time.deltaTime;
        bool slow = SlowShots > 0 && St != State.Menu;
        foreach (var d in Defenders) d.Tick(dt, Diff, SpeedMul, slow);
        Goal.Tick(dt, Diff);
        Keeper.Tick(dt, Goal.CenterX, Diff, SpeedMul);
        Ball.Tick(dt);
        HandleInput();
        WindStreaks(dt);
    }

    void WindStreaks(float dt)
    {
        if (Wind == 0f || St == State.Menu) return;
        windStreakTimer -= dt;
        if (windStreakTimer > 0f) return;
        windStreakTimer = Mathf.Lerp(0.12f, 0.04f, Mathf.Abs(Wind) / 3f);
        float dir = Mathf.Sign(Wind);
        float y = Random.Range(-3.5f, 5f);
        var start = new Vector3(-dir * (FieldHalf + 0.5f), y, 0f);
        var v = new Vector3(dir * Random.Range(4f, 6f) * (0.6f + Mathf.Abs(Wind) * 0.3f), 0f, 0f);
        Fx.I.Streak(start, v, 1.6f, Random.Range(0.3f, 0.7f), new Color(1f, 1f, 1f, 0.35f));
    }

    // ---------- Aiming ----------

    void HandleInput()
    {
        bool down, held, up;
        Vector2 pos;
        ReadPointer(out down, out held, out up, out pos);

        if (St == State.Menu)
        {
            if (down && !ui.IsOverUI(pos)) StartRun();
            return;
        }
        if (St != State.Playing)
        {
            CancelAim();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && Ball.CanKick && !aiming)
        {
            Kick(Vector2.up, Ball.TapSpeed);
            return;
        }

        if (Ball.St == Ball.State.Flying)
        {
            HandleCurve(down, held, up, pos);
            return;
        }
        curving = false;

        if (down && Ball.CanKick && !ui.IsOverUI(pos))
        {
            aiming = true;
            aimDragged = false;
            aimStart = pos;
            aimDir = Vector2.up;
            aimSpeed = Ball.TapSpeed;
        }
        if (!aiming) return;
        if (!Ball.CanKick) { CancelAim(); return; }
        if (held || down) UpdateAim(pos);
        if (up)
        {
            bool cancelled = aimDragged && !aimValid;
            CancelAim();
            if (!cancelled) Kick(aimDir, aimSpeed);
        }
    }

    // Pull back from where you pressed: the shot goes the opposite way, further pull means more power.
    void UpdateAim(Vector2 screenPos)
    {
        float unitsPerPx = 2f * Cam.orthographicSize / Mathf.Max(1, Screen.height);
        Vector2 pull = (aimStart - screenPos) * unitsPerPx;
        float len = pull.magnitude;
        if (len < AimDeadZone)
        {
            aimValid = false;
            aimDir = Vector2.up;
            aimSpeed = Ball.TapSpeed;
            guide.Hide();
            return;
        }
        aimDragged = true;
        aimValid = true;

        float ang = Mathf.Atan2(pull.x, pull.y) * Mathf.Rad2Deg; // 0 = straight up
        ang = Mathf.Clamp(ang, -MaxAimAngle, MaxAimAngle);
        aimDir = new Vector2(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad));
        float power = Mathf.Clamp01((len - AimDeadZone) / MaxPull);
        aimSpeed = Mathf.Lerp(Ball.MinSpeed, Ball.MaxSpeed, power);
        guide.Show(Ball.transform.position, aimDir * aimSpeed, power, Goal.LineY, Bumpers);
    }

    // Aftertouch: one sideways swipe per shot bends the ball while it flies.
    void HandleCurve(bool down, bool held, bool up, Vector2 pos)
    {
        if (curveDone) return;
        if (down && !ui.IsOverUI(pos))
        {
            curving = true;
            curveStart = pos;
        }
        if (!curving) return;
        float unitsPerPx = 2f * Cam.orthographicSize / Mathf.Max(1, Screen.height);
        float dx = (pos.x - curveStart.x) * unitsPerPx;
        if ((held || down) && Mathf.Abs(dx) > 0.15f)
        {
            if (Ball.Curve == 0f) Sfx.I.Play(Sfx.I.Close, 0.4f, 0.7f);
            Ball.SetCurve(dx / CurveSwipe);
        }
        if (up)
        {
            curving = false;
            if (Ball.Curve != 0f) curveDone = true;
        }
    }

    void CancelAim()
    {
        aiming = false;
        aimValid = false;
        guide.Hide();
    }

    static void ReadPointer(out bool down, out bool held, out bool up, out Vector2 pos)
    {
        down = held = up = false;
        pos = Vector2.zero;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            pos = t.position;
            down = t.phase == TouchPhase.Began;
            held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            return;
        }
        pos = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        up = Input.GetMouseButtonUp(0);
    }

    void Kick(Vector2 dir, float speed)
    {
        quickShot = Time.time - Ball.ReadyTime <= QuickWindow;
        if (!quickShot)
        {
            if (OnFire) ui.Float(Ball.transform.position + Vector3.up * 0.5f, "FIRE OUT", GameUI.Muted, 46);
            Streak = 0;
            OnFire = false;
        }
        Ball.Kick(dir, speed);
        shotTricks.Clear();
        curving = false;
        curveDone = false;
        if (Save.TipSeen(0) && !Save.TipSeen(6) && Goals >= 2)
        {
            Save.MarkTip(6);
            ui.Hint("SWIPE SIDEWAYS WHILE THE BALL\nIS IN THE AIR TO CURVE IT!");
        }
        float power = Mathf.InverseLerp(Ball.MinSpeed, Ball.MaxSpeed, speed);
        Sfx.I.Play(Sfx.I.Kick, Mathf.Lerp(0.6f, 1f, power), Mathf.Lerp(1.1f, 0.92f, power) * Random.Range(0.97f, 1.03f));
        if (power > 0.85f) Fx.I.Shake(0.04f, 0.1f);
        if (!Save.TipSeen(0)) { Save.MarkTip(0); ui.Hint(null); }
    }

    // ---------- Ball events ----------

    public void OnGoal(float dx)
    {
        bool perfect = Diff.goalMoves && dx < 0.16f;
        if (quickShot) Streak++;
        else Streak = 0;

        int mult = Multiplier;
        int banks = Mathf.Min(Ball.Bounces, 3);
        bool banana = Ball.CurvePeak >= 0.35f;
        if (banks > 0) shotTricks.Add("BANK");
        if (perfect) shotTricks.Add("TOP BINS");
        if (banana) shotTricks.Add("BANANA");
        int tricks = Mathf.Min(shotTricks.Count, 4);
        int pts = mult + (perfect ? mult : 0) + banks * mult + (banana ? mult : 0) + TrickBonus[tricks] * mult;
        Score += pts;
        Goals++;
        if (DoubleGoals > 0) DoubleGoals--;
        if (SlowShots > 0) SlowShots--;

        var bp = Ball.transform.position;
        Ball.Scored(Goal.CenterX, Goal.LineY);
        Goal.Punch();
        Fx.I.Burst(new Vector3(Goal.CenterX, Goal.LineY + 0.3f, 0f), 26, Fx.Confetti, 2f, 5f, 0.9f, 0.09f, 6f, 90f, 150f);
        Fx.I.Burst(new Vector3(bp.x, Goal.LineY + 0.35f, 0f), 12, new[] { new Color(1f, 1f, 1f, 0.9f), new Color(0.85f, 0.85f, 0.9f, 0.8f) },
            1f, 2.5f, 0.4f, 0.06f, 0f, 90f, 100f);
        Fx.I.Shake(0.06f, 0.15f);
        Sfx.I.Play(Sfx.I.Goal, 0.9f, 1f + 0.04f * Mathf.Min(Streak, 10));
        ui.Float(bp + Vector3.down * 0.3f, "+" + pts, mult > 1 ? GameUI.Orange : Color.white, 80);
        ui.PopScore();
        if (perfect) ui.Float(bp + Vector3.down * 0.9f, "TOP BINS!", GameUI.Yellow, 64);
        if (banks > 0)
        {
            string[] names = { "BANK SHOT!", "DOUBLE BANK!", "TRIPLE BANK!" };
            ui.Float(bp + Vector3.down * (perfect ? 1.5f : 0.9f), names[banks - 1], GameUI.Cyan, 64);
            Missions(Progression.Track(MissionType.BankShots, 1));
        }
        if (banana) ui.Float(bp + Vector3.down * 2.1f, "BANANA!", GameUI.Purple, 60);
        if (tricks >= 2)
        {
            ui.Banner(TrickNames[tricks] + "  +" + TrickBonus[tricks] * mult, tricks >= 4 ? GameUI.Red : tricks == 3 ? GameUI.Orange : GameUI.Cyan);
            ui.Float(new Vector3(0f, 1.2f, 0f), string.Join(" + ", new List<string>(shotTricks).ToArray()), Color.white, 44);
            Fx.I.Burst(new Vector3(Goal.CenterX, Goal.LineY, 0f), 20 * tricks, Fx.Confetti, 3f, 7f, 1.2f, 0.1f, 5f, 90f, 160f);
            Sfx.I.Play(Sfx.I.Fanfare, 0.7f, 1f + 0.08f * tricks);
            Missions(Progression.Track(MissionType.TrickShots, 1));
        }

        if (!OnFire && Streak >= FireStreak)
        {
            OnFire = true;
            Save.D.fires++;
            ui.Banner("ON FIRE!  x2", GameUI.Orange);
            Sfx.I.Play(Sfx.I.Fire, 0.9f);
            Missions(Progression.Track(MissionType.Fire, 1));
        }
        else if (Streak == 2 && !Save.TipSeen(1))
        {
            Save.MarkTip(1);
            ui.Hint("QUICK SHOTS BUILD A STREAK!\n" + FireStreak + " IN A ROW = ON FIRE");
        }

        if (!bestAnnounced && startBest > 0 && Score > startBest)
        {
            bestAnnounced = true;
            ui.Banner("NEW BEST!", GameUI.Green);
            Sfx.I.Play(Sfx.I.Fanfare, 0.7f);
        }

        Missions(Progression.Track(MissionType.Goals, 1));
        Missions(Progression.Track(MissionType.ScoreInRun, Score));
        ApplyDifficulty(true);
        if (Diff.bumpers > 0 && Goals % 5 == 0) RollBumpers(Diff.bumpers);
        if (Diff.windMax > 0f && (Goals == Difficulty.WindAt || Goals % 4 == 0)) ShiftWind();
        StartCoroutine(NextBall());
    }

    IEnumerator NextBall()
    {
        yield return new WaitForSeconds(0.3f);
        if (St != State.Playing) yield break;
        Ball.ResetToSpawn();
        MaybeSpawnPickup();
    }

    public void OnCloseCall(Defender d, Vector3 pos)
    {
        CloseCalls++;
        Score += 1;
        shotTricks.Add("CLOSE");
        ui.PopScore();
        ui.Float(pos + new Vector3(0f, -0.2f, 0f), "CLOSE! +1", GameUI.Cyan, 54);
        Fx.I.Burst(pos, 10, new[] { Color.white, new Color(0.6f, 0.95f, 1f) }, 2f, 4f, 0.3f, 0.07f);
        Fx.I.TimeFx(0.35f, 0.12f);
        Sfx.I.Play(Sfx.I.Close, 0.8f, Random.Range(0.95f, 1.1f));
        if (!Save.TipSeen(2)) { Save.MarkTip(2); ui.Hint("GRAZE DEFENDERS FOR BONUS POINTS!"); }
        Missions(Progression.Track(MissionType.CloseCalls, 1));
        Missions(Progression.Track(MissionType.CloseInRun, CloseCalls));
        Missions(Progression.Track(MissionType.ScoreInRun, Score));
    }

    // Returns true if the run ended (ball stopped).
    public bool OnBallHitDefender(Defender d)
    {
        var pos = Ball.transform.position;
        if (Shield)
        {
            BreakShield(pos);
            d.Knock(pos);
            return false;
        }
        Sfx.I.Play(d.HitClip != null ? d.HitClip : Sfx.I.Hit1, 1f);
        d.Celebrate();
        EndRun("TACKLED!", d.transform.position);
        return true;
    }

    public bool OnBallSaved()
    {
        var pos = Ball.transform.position;
        if (Shield)
        {
            BreakShield(pos);
            Keeper.Knock();
            return false;
        }
        Sfx.I.Play(Sfx.I.Hit2, 1f, 1.1f);
        Keeper.Celebrate();
        EndRun("SAVED!", Keeper.transform.position);
        return true;
    }

    public void OnMiss()
    {
        Ball.Miss();
        EndRun("WIDE!", Ball.transform.position + Vector3.up);
    }

    // Ran out of pace, or rebounded all the way back past the kicker.
    public void OnShotDied(bool fellBack)
    {
        Ball.Miss();
        EndRun(fellBack ? "MISSED!" : "TOO SHORT!", Ball.transform.position);
    }

    public void OnPost(Vector3 pos)
    {
        Sfx.I.Play(Sfx.I.Hit2, 0.9f, 1.5f);
        Fx.I.Burst(pos, 10, new[] { Color.white, new Color(0.8f, 0.8f, 0.85f) }, 2f, 4f, 0.3f, 0.07f);
        Fx.I.Shake(0.1f, 0.18f);
        ui.Float(pos + Vector3.down * 0.4f, "POST!", GameUI.Red, 64);
    }

    public void OnWallBounce(Vector3 pos, int side)
    {
        float k = Mathf.InverseLerp(Ball.MinSpeed, Ball.MaxSpeed, Ball.Speed);
        Sfx.I.Play(Sfx.I.Kick, Mathf.Lerp(0.35f, 0.7f, k), 1.5f);
        var wallPos = new Vector3(side * FieldHalf, pos.y, pos.z);
        Fx.I.Burst(wallPos, 6, new[] { Color.white, new Color(0.85f, 0.9f, 1f) }, 1f, 3f, 0.25f, 0.07f, 0f, side > 0 ? 180f : 0f, 120f);
        Fx.I.Shake(0.03f, 0.08f);
        if (boards.Hit(side, pos.y))
        {
            RunCoins += 3;
            Sfx.I.Play(Sfx.I.BigCoin, 0.7f);
            Fx.I.Burst(wallPos, 16, new[] { GameUI.Yellow, Color.white }, 2f, 4.5f, 0.5f, 0.09f);
            ui.Float(wallPos - new Vector3(side * 0.5f, 0f, 0f), "+3", GameUI.Yellow, 60);
            Missions(Progression.Track(MissionType.Coins, 3));
            if (!Save.TipSeen(5)) { Save.MarkTip(5); ui.Hint("HIT THE GOLD BOARD FOR COINS!"); }
        }
    }

    void BreakShield(Vector3 pos)
    {
        Shield = false;
        Fx.I.Burst(pos, 20, new[] { new Color(0.5f, 0.8f, 1f), Color.white }, 2f, 6f, 0.5f, 0.1f);
        Fx.I.Ring(pos, new Color(0.5f, 0.8f, 1f), 0.6f, 2.2f, 0.35f);
        Fx.I.Shake(0.12f, 0.2f);
        Fx.I.TimeFx(0.05f, 0.08f);
        Sfx.I.Play(Sfx.I.Hit1, 0.8f, 1.2f);
        Sfx.I.Play(Sfx.I.Shield, 0.8f);
        ui.Float(pos, "SMASHED!", GameUI.Cyan, 64);
    }

    public void OnBumper(Bumper b, Vector3 pos)
    {
        b.Hit();
        shotTricks.Add("BUMPER");
        Score += 1;
        ui.PopScore();
        ui.Float(pos + Vector3.up * 0.3f, "BOING! +1", GameUI.Red, 54);
        Fx.I.Burst(pos, 12, new[] { new Color(1f, 0.4f, 0.4f), GameUI.Yellow, Color.white }, 2f, 5f, 0.35f, 0.08f);
        Fx.I.Ring(b.transform.position, new Color(1f, 0.6f, 0.4f), 0.4f, 1.2f, 0.25f);
        Fx.I.Shake(0.07f, 0.12f);
        Sfx.I.Play(Sfx.I.Bumper, 0.8f, Random.Range(0.95f, 1.1f));
        Missions(Progression.Track(MissionType.ScoreInRun, Score));
    }

    // Places bumpers in the gaps between lanes, one per row so they never stack.
    void RollBumpers(int count)
    {
        ClearBumpers();
        var rows = new List<float>(BumperRows);
        for (int i = 0; i < count && rows.Count > 0; i++)
        {
            int r = Random.Range(0, rows.Count);
            float x = Random.Range(0.5f, 1.6f) * (Random.value < 0.5f ? -1f : 1f);
            if (Random.value < 0.25f) x = Random.Range(-0.3f, 0.3f);
            Bumpers.Add(Bumper.Create(new Vector2(x, rows[r])));
            rows.RemoveAt(r);
        }
    }

    void ClearBumpers()
    {
        foreach (var b in Bumpers) if (b != null) b.Despawn();
        Bumpers.Clear();
    }

    void ShiftWind()
    {
        float old = Wind;
        Wind = Random.value < 0.35f ? 0f : Random.Range(0.6f, Diff.windMax) * (Random.value < 0.5f ? -1f : 1f);
        if (Wind == 0f && old == 0f) return;
        string msg = Wind == 0f ? "WIND DROPS" : Wind > 0f ? "WIND  >>>" : "<<<  WIND";
        ui.Float(new Vector3(0f, 0.2f, 0f), msg, GameUI.Cyan, 66);
        if (Wind != 0f && !Save.TipSeen(7)) { Save.MarkTip(7); ui.Hint("WIND PUSHES THE BALL SIDEWAYS -\nAIM INTO IT!"); }
    }

    public void Collect(Pickup p)
    {
        var pos = p.transform.position;
        Fx.I.Burst(pos, 12, new[] { p.GlowColor, Color.white }, 1.5f, 3.5f, 0.4f, 0.08f);
        switch (p.kind)
        {
            case Pickup.Kind.Coin:
                int c1 = 1 + Catalog.CurrentBall.coinBonus;
                RunCoins += c1;
                Sfx.I.Play(Sfx.I.Coin, 0.7f);
                ui.Float(pos, "+" + c1, GameUI.Yellow, 50);
                Missions(Progression.Track(MissionType.Coins, c1));
                break;
            case Pickup.Kind.BigCoin:
                int c5 = 5 + Catalog.CurrentBall.coinBonus;
                RunCoins += c5;
                Sfx.I.Play(Sfx.I.BigCoin, 0.7f);
                ui.Float(pos, "+" + c5, GameUI.Cyan, 64);
                Missions(Progression.Track(MissionType.Coins, c5));
                break;
            case Pickup.Kind.Shield:
                Shield = true;
                PowerupGot(pos, "SHIELD!");
                break;
            case Pickup.Kind.Slow:
                SlowShots = 3;
                PowerupGot(pos, "SLOW-MO!");
                break;
            case Pickup.Kind.Double:
                DoubleGoals = 3;
                PowerupGot(pos, "DOUBLE POINTS!");
                break;
        }
        p.Remove();
    }

    void PowerupGot(Vector3 pos, string label)
    {
        Save.D.pickups++;
        Sfx.I.Play(Sfx.I.Power, 0.7f);
        ui.Float(pos, label, GameUI.Purple, 58);
        Missions(Progression.Track(MissionType.Powerups, 1));
    }

    void MaybeSpawnPickup()
    {
        if (Pickups.Count >= 2 || St != State.Playing) return;
        float r = Random.value;
        Pickup.Kind kind;
        if (Goals >= 2 && r < 0.14f)
        {
            var options = new List<Pickup.Kind> { Pickup.Kind.Slow, Pickup.Kind.Double };
            if (!Shield) options.Add(Pickup.Kind.Shield);
            kind = options[Random.Range(0, options.Count)];
        }
        else if (r < 0.62f) kind = Random.value < 0.08f ? Pickup.Kind.BigCoin : Pickup.Kind.Coin;
        else return;

        var free = new List<float>(GapY);
        foreach (var p in Pickups) free.RemoveAll(y => Mathf.Abs(y - p.transform.position.y) < 0.1f);
        if (free.Count == 0) return;
        float row = free[Random.Range(0, free.Count)];
        float speed = Random.Range(0.9f, 1.5f) + Mathf.Min(Goals * 0.01f, 0.6f);
        Pickups.Add(Pickup.Spawn(kind, row, Random.value < 0.5f, speed));

        // Waits until the player has taken a shot, so it never covers the aiming tip.
        if (!Save.TipSeen(3) && Save.TipSeen(0)) { Save.MarkTip(3); ui.Hint("SHOOT THROUGH ITEMS TO GRAB THEM!"); }
    }

    void ClearPickups()
    {
        foreach (var p in Pickups) if (p != null) Destroy(p.gameObject);
        Pickups.Clear();
    }

    void Missions(List<MissionState> completed)
    {
        foreach (var m in completed)
        {
            ui.Toast("MISSION COMPLETE!", Progression.Describe(m), m.reward);
            Sfx.I.Play(Sfx.I.Fanfare, 0.6f);
        }
    }

    // ---------- Ending ----------

    void EndRun(string reason, Vector3 at)
    {
        St = State.Dying;
        if (Ball.St == Ball.State.Flying) Ball.Knock(at);
        Fx.I.TimeFx(0f, 0.1f);
        Fx.I.Shake(0.25f, 0.4f);
        Fx.I.Burst(Ball.transform.position, 14, new[] { Color.white, new Color(1f, 0.4f, 0.4f) }, 2f, 5f, 0.4f, 0.09f);
        Vibrate();
        ui.Banner(reason, GameUI.Red);
        StartCoroutine(AfterDeath(reason));
    }

    IEnumerator AfterDeath(string reason)
    {
        yield return new WaitForSecondsRealtime(1.1f);
        if (CanContinue)
        {
            St = State.Continue;
            ui.ShowContinue(reason, ContinueCost);
        }
        else Finish(reason);
    }

    public void AcceptContinue()
    {
        if (St != State.Continue || Save.D.coins < ContinueCost) return;
        Save.D.coins -= ContinueCost;
        Save.Write();
        continued = true;
        Shield = true;
        Streak = 0;
        OnFire = false;
        foreach (var d in Defenders) d.Resume();
        Keeper.Resume();
        Ball.ResetToSpawn();
        St = State.Playing;
        ui.ShowHUD();
        ui.Banner("SECOND CHANCE!", GameUI.Green);
        Sfx.I.Play(Sfx.I.Power, 0.8f);
    }

    public void DeclineContinue(string reason)
    {
        if (St == State.Continue) Finish(reason);
    }

    void Finish(string reason)
    {
        St = State.GameOver;
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        var r = new RunResult { reason = reason, score = Score, goals = Goals, closeCalls = CloseCalls };
        r.coins = RunCoins + Score / 2;
        r.xp = Score + CloseCalls;
        r.newBest = Score > Save.D.best;
        if (r.newBest) Save.D.best = Score;
        r.best = Save.D.best;

        Save.D.lastScore = Score;
        Save.D.coins += r.coins;
        Save.D.games++;
        Save.D.goals += Goals;
        Save.D.closeCalls += CloseCalls;
        Missions(Progression.Track(MissionType.Games, 1));
        r.levelsGained = Progression.AddXp(r.xp);
        Save.Write();

        ClearPickups();
        ui.ShowGameOver(r);
        if (r.newBest && r.score > 0)
        {
            Fx.I.Burst(new Vector3(0f, 2f, 0f), 60, Fx.Confetti, 3f, 8f, 1.5f, 0.1f, 5f, 90f, 120f);
            Sfx.I.Play(Sfx.I.Fanfare, 0.8f);
        }
    }

    public void Retry()
    {
        Progression.EnsureMissions();
        Save.Write();
        StartRun();
    }

    public void GoHome()
    {
        Progression.EnsureMissions();
        Save.Write();
        EnterMenu();
    }

    // ---------- Pause ----------

    public void SetPaused(bool on)
    {
        Paused = on && St == State.Playing;
        Time.timeScale = Paused ? 0f : 1f;
        if (ui != null) ui.ShowPause(Paused);
    }

    public void QuitRun()
    {
        if (St != State.Playing) return;
        SetPaused(false);
        St = State.Dying;
        Ball.Hide();
        Finish("FULL TIME");
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && St == State.Playing) SetPaused(true);
        if (pause && Save.D != null) Save.Write();
    }

    void OnApplicationQuit()
    {
        if (Save.D != null) Save.Write();
    }

    void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Save.D.vibrate) Handheld.Vibrate();
#endif
    }
}
