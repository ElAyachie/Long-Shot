using System.Collections.Generic;
using UnityEngine;

// The player's ball. Kicked at an angle with variable power, slows down as it travels and bounces off
// the side boards. GameManager decides what each contact means.
public class Ball : MonoBehaviour
{
    public enum State { Hidden, Spawning, Ready, Flying, Scored, Knocked, Missed }

    public const float MinSpeed = 9f, MaxSpeed = 21f, TapSpeed = 16f;
    public const float Decel = 4f;          // speed lost per second
    public const float StopSpeed = 1.5f;    // below this the shot has died
    public const float Restitution = 0.85f; // speed kept across the ball when it hits a board
    public const float CurveAccel = 10f;    // sideways pull at full swipe
    public const float MaxBumperSpeed = 24f;
    public const float Radius = 0.24f;
    const float CloseDist = 0.22f;
    const float StepSize = 0.07f;

    public static float WallX { get { return GameManager.FieldHalf - Radius; } }

    public State St { get; private set; }
    public float ReadyTime { get; private set; }
    public AudioClip KickClip { get; private set; }
    public bool CanKick { get { return St == State.Ready; } }
    public int Bounces { get; private set; }
    public float Speed { get { return vel.magnitude; } }
    public Vector3 SpawnPos { get { return spawn; } }
    public float Curve { get; private set; }      // current bend, -1..1 (positive bends right)
    public float CurvePeak { get; private set; }  // strongest bend applied this shot
    public int BumperHits { get; private set; }
    bool hitPost;

    SpriteRenderer sr, shieldRing, quickRing;
    Collider2D col;
    Transform shadow;
    Vector3 spawn, baseScale, vel;
    float spin, timer, ghostTimer, hue, squash;
    Vector3 scoreTarget;

    readonly HashSet<Defender> near = new HashSet<Defender>();
    readonly HashSet<Defender> awarded = new HashSet<Defender>();
    readonly List<Pickup> pickupScratch = new List<Pickup>();

    public void Init(Vector3 spawnPos)
    {
        spawn = spawnPos;
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = 20;
        col = GetComponent<Collider2D>();
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.simulated = true; }
        var src = GetComponent<AudioSource>();
        if (src != null) { KickClip = src.clip; src.playOnAwake = false; }
        baseScale = transform.localScale;

        shadow = MakeChildless("BallShadow", PixelArt.Circle, new Color(0f, 0f, 0f, 0.25f), -50).transform;
        shadow.localScale = new Vector3(0.42f, 0.16f, 1f);

        shieldRing = MakeChildless("ShieldRing", PixelArt.Ring, new Color(0.4f, 0.75f, 1f, 0.9f), 21);
        quickRing = MakeChildless("QuickRing", PixelArt.Ring, new Color(1f, 0.6f, 0.15f, 0.8f), 19);
        Hide();
    }

    SpriteRenderer MakeChildless(string name, Sprite s, Color c, int order)
    {
        var go = new GameObject(name);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = s;
        r.color = c;
        r.sortingOrder = order;
        return r;
    }

    public void Hide()
    {
        St = State.Hidden;
        sr.enabled = false;
        shadow.gameObject.SetActive(false);
        shieldRing.enabled = false;
        quickRing.enabled = false;
    }

    public void ResetToSpawn()
    {
        St = State.Spawning;
        timer = 0f;
        sr.enabled = true;
        sr.color = SkinColor();
        transform.position = new Vector3(spawn.x, spawn.y, -3.5f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.zero;
        shadow.gameObject.SetActive(true);
    }

    public void Kick(Vector2 dir, float speed)
    {
        St = State.Flying;
        near.Clear();
        awarded.Clear();
        Bounces = 0;
        BumperHits = 0;
        Curve = CurvePeak = 0f;
        hitPost = false;
        vel = (Vector3)(dir.normalized * speed);
        squash = 1f;
        spin = -500f - speed * 30f;
        float back = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
        Fx.I.Burst(transform.position - (Vector3)dir * 0.2f, 8, new[] { new Color(1f, 1f, 1f, 0.8f), new Color(0.8f, 0.9f, 0.7f, 0.7f) },
            1f, 2.5f, 0.35f, 0.08f, 0f, back, 160f);
    }

    // Simulates a shot ignoring defenders, for the aim guide. Stops a little after the given number of bounces,
    // at the goal line, or where the ball would die.
    public static void Predict(Vector2 p, Vector2 v, float lineY, int maxBounces, float afterBounce,
        float spacing, List<Vector2> points, int maxPoints, List<Bumper> bumpers)
    {
        points.Clear();
        var skin = Catalog.CurrentBall;
        float decel = Decel * skin.decelMul;
        const float dt = 0.005f;
        int bounces = 0;
        float sinceDot = 0f, sinceBounce = 0f;
        for (int i = 0; i < 4000 && points.Count < maxPoints; i++)
        {
            float speed = v.magnitude - decel * dt;
            if (speed < StopSpeed) break;
            v = v.normalized * speed;
            Vector2 step = v * dt;
            p += step;
            if ((p.x > WallX && v.x > 0f) || (p.x < -WallX && v.x < 0f))
            {
                p.x = Mathf.Sign(p.x) * WallX * 2f - p.x;
                v.x = -v.x * skin.restitution;
                bounces++;
            }
            if (bumpers != null)
                foreach (var b in bumpers)
                    if (b != null && b.Solid && Bounce(ref p, ref v, b.Pos)) bounces++;
            if (p.y >= lineY) { points.Add(p); break; }
            sinceDot += step.magnitude;
            if (bounces >= maxBounces) sinceBounce += step.magnitude;
            if (sinceDot >= spacing) { sinceDot = 0f; points.Add(p); }
            if (bounces >= maxBounces && sinceBounce >= afterBounce) break;
        }
    }

    // Reflects a ball off a bumper it overlaps while moving towards it. Returns true on contact.
    static bool Bounce(ref Vector2 p, ref Vector2 v, Vector2 c)
    {
        Vector2 d = p - c;
        float min = Radius + Bumper.Radius;
        if (d.sqrMagnitude >= min * min || Vector2.Dot(v, d) >= 0f) return false;
        Vector2 n = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
        v = Vector2.Reflect(v, n) * Bumper.Boost;
        if (v.magnitude > MaxBumperSpeed) v = v.normalized * MaxBumperSpeed;
        p = c + n * min;
        return true;
    }

    // Swipe aftertouch: bends the ball while it is in the air.
    public void SetCurve(float amount)
    {
        if (St != State.Flying) return;
        Curve = Mathf.Clamp(amount, -1f, 1f);
        CurvePeak = Mathf.Max(CurvePeak, Mathf.Abs(Curve));
    }

    // Deflected off a defender or keeper: bounce away and fade.
    public void Knock(Vector3 from)
    {
        St = State.Knocked;
        timer = 0f;
        float side = transform.position.x >= from.x ? 1f : -1f;
        if (Mathf.Abs(transform.position.x - from.x) < 0.05f) side = Random.value < 0.5f ? -1f : 1f;
        vel = new Vector3(side * Random.Range(2.5f, 4f), -Random.Range(3f, 5f), 0f);
        spin = side * 1200f;
    }

    // Shot is over without a contact: wide of the goal, died short, or rolled back past the player.
    public void Miss()
    {
        St = State.Missed;
        timer = 0f;
    }

    public void Scored(float goalX, float lineY)
    {
        St = State.Scored;
        timer = 0f;
        scoreTarget = new Vector3(transform.position.x, lineY + 0.32f, -3.5f);
    }

    public Color SkinColor()
    {
        var skin = Catalog.CurrentBall;
        if (skin.fx == BallFx.Rainbow) return Color.HSVToRGB(hue % 1f, 0.55f, 1f);
        return skin.tint;
    }

    Color TrailColor()
    {
        var skin = Catalog.CurrentBall;
        if (skin.fx == BallFx.Rainbow) return Color.HSVToRGB(hue % 1f, 0.8f, 1f) * new Color(1f, 1f, 1f, 0.7f);
        return skin.trail;
    }

    public void Tick(float dt)
    {
        var gm = GameManager.I;
        hue += dt * 0.6f;
        var skin = Catalog.CurrentBall;

        switch (St)
        {
            case State.Spawning:
                timer += dt;
                float k = Mathf.Clamp01(timer / 0.18f);
                float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;
                transform.localScale = baseScale * k * overshoot;
                if (k >= 1f)
                {
                    transform.localScale = baseScale;
                    St = State.Ready;
                    ReadyTime = Time.time;
                }
                break;

            case State.Ready:
                // Idle bounce.
                float b = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.04f;
                transform.position = new Vector3(spawn.x, spawn.y + b, -3.5f);
                break;

            case State.Flying:
                Fly(dt, gm);
                break;

            case State.Scored:
                timer += dt;
                transform.position = Vector3.MoveTowards(transform.position, scoreTarget, dt * 6f);
                transform.localScale = baseScale * Mathf.Lerp(1f, 0.6f, timer / 0.2f);
                transform.Rotate(0, 0, spin * 0.3f * dt);
                if (timer > 0.25f) Hide();
                break;

            case State.Knocked:
            case State.Missed:
                timer += dt;
                vel *= Mathf.Max(0f, 1f - (St == State.Knocked ? 1.5f : 0f) * dt);
                transform.position += vel * dt;
                transform.Rotate(0, 0, spin * dt);
                spin *= Mathf.Max(0f, 1f - dt);
                var c = sr.color;
                c.a = Mathf.Clamp01(1.4f - timer);
                sr.color = c;
                if (timer > 1.4f) Hide();
                break;
        }

        if (St == State.Hidden) return;

        if (St != State.Knocked && St != State.Missed) sr.color = SkinColor();

        if (squash > 0f)
        {
            squash = Mathf.Max(0f, squash - dt * 6f);
            if (St == State.Flying)
                transform.localScale = new Vector3(baseScale.x * (1f - 0.2f * squash), baseScale.y * (1f + 0.3f * squash), baseScale.z);
        }

        var pos = transform.position;
        shadow.position = new Vector3(pos.x + 0.05f, pos.y - 0.2f, -3.4f);
        shadow.gameObject.SetActive(St != State.Scored);

        shieldRing.enabled = gm.Shield && (St == State.Ready || St == State.Flying || St == State.Spawning);
        if (shieldRing.enabled)
        {
            shieldRing.transform.position = pos + Vector3.back * 0.01f;
            float s = 0.68f + Mathf.Sin(Time.time * 8f) * 0.04f;
            shieldRing.transform.localScale = new Vector3(s, s, 1f);
        }

        // Shrinking ring shows the window for a streak-building quick shot.
        float quickLeft = GameManager.QuickWindow - (Time.time - ReadyTime);
        quickRing.enabled = St == State.Ready && gm.Streak > 0 && quickLeft > 0f && gm.St == GameManager.State.Playing;
        if (quickRing.enabled)
        {
            float s = Mathf.Lerp(0.55f, 1.3f, quickLeft / GameManager.QuickWindow);
            quickRing.transform.position = pos;
            quickRing.transform.localScale = new Vector3(s, s, 1f);
        }

        // Trails and skin effects.
        bool moving = St == State.Flying || St == State.Knocked || St == State.Missed;
        ghostTimer -= dt;
        if (ghostTimer <= 0f && (moving || gm.OnFire))
        {
            ghostTimer = 0.02f;
            if (moving)
                Fx.I.Ghost(sr.sprite, pos, transform.eulerAngles.z, baseScale.x, TrailColor(), 0.18f);
            if (gm.OnFire)
                Fx.I.Burst(pos, 2, new[] { new Color(1f, 0.85f, 0.2f), new Color(1f, 0.45f, 0.1f), new Color(1f, 0.2f, 0.1f) },
                    0.3f, 1.2f, 0.35f, 0.12f, -2f, -90f, 60f);
            else if (moving && skin.fx == BallFx.Flame)
                Fx.I.Burst(pos, 1, new[] { new Color(1f, 0.6f, 0.2f), new Color(1f, 0.3f, 0.1f) }, 0.2f, 0.8f, 0.3f, 0.09f, -1f, -90f, 60f);
            else if (moving && skin.fx == BallFx.Sparkle && Random.value < 0.5f)
                Fx.I.Burst(pos, 1, new[] { new Color(1f, 0.95f, 0.6f), Color.white }, 0.2f, 0.9f, 0.4f, 0.07f, 0f);
        }
    }

    void Fly(float dt, GameManager gm)
    {
        transform.Rotate(0, 0, spin * dt);

        var skin = Catalog.CurrentBall;
        float speed = vel.magnitude - Decel * skin.decelMul * dt;
        if (speed < StopSpeed)
        {
            vel = vel.normalized * Mathf.Max(speed, 0f);
            gm.OnShotDied(false);
            return;
        }
        vel = vel.normalized * speed;

        // Curve pulls sideways relative to the direction of travel; spin slowly fades.
        if (Curve != 0f)
        {
            var right = new Vector3(vel.y, -vel.x, 0f).normalized;
            vel += right * Curve * CurveAccel * skin.curveMul * dt;
            vel = vel.normalized * speed;
            spin -= Curve * 1500f * dt;
            Curve *= Mathf.Max(0f, 1f - 0.5f * dt);
        }
        // Wind drifts the ball across the pitch.
        vel.x += gm.Wind * skin.windMul * dt;
        speed = vel.magnitude;

        float dist = speed * dt;
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / StepSize));

        for (int i = 0; i < steps; i++)
        {
            var pos = transform.position;
            pos += vel * (dt / steps);

            // Side boards.
            if ((pos.x > WallX && vel.x > 0f) || (pos.x < -WallX && vel.x < 0f))
            {
                int side = pos.x > 0f ? 1 : -1;
                pos.x = side * WallX * 2f - pos.x;
                vel.x = -vel.x * skin.restitution;
                spin = -spin * 0.8f;
                Curve = -Curve * 0.5f;
                Bounces++;
                transform.position = pos;
                gm.OnWallBounce(pos, side);
            }

            // Bumpers.
            foreach (var bm in gm.Bumpers)
            {
                if (bm == null || !bm.Solid) continue;
                Vector2 p2 = pos, v2 = vel;
                if (!Bounce(ref p2, ref v2, bm.Pos)) continue;
                pos = new Vector3(p2.x, p2.y, pos.z);
                vel = v2;
                spin = -spin;
                Curve = 0f;
                BumperHits++;
                transform.position = pos;
                gm.OnBumper(bm, pos);
            }

            transform.position = pos;
            Physics2D.SyncTransforms();

            // Pickups.
            pickupScratch.Clear();
            pickupScratch.AddRange(gm.Pickups);
            foreach (var p in pickupScratch)
            {
                if (p == null) continue;
                if (((Vector2)(p.transform.position - pos)).magnitude < Radius + Pickup.Radius)
                    gm.Collect(p);
            }

            // Defenders: contact ends the run, grazing them earns a close call.
            foreach (var d in gm.Defenders)
            {
                if (!d.Active || d.Col == null) continue;
                var cd = col.Distance(d.Col);
                if (!cd.isValid) continue;
                if (cd.isOverlapped || cd.distance <= 0f)
                {
                    near.Remove(d);
                    if (gm.OnBallHitDefender(d)) return;
                    continue;
                }
                if (cd.distance < CloseDist * skin.closeMul && !awarded.Contains(d)) near.Add(d);
            }
            foreach (var d in gm.Defenders)
            {
                if (near.Contains(d) && !awarded.Contains(d) && pos.y - Radius > d.LaneY + 0.4f)
                {
                    awarded.Add(d);
                    near.Remove(d);
                    gm.OnCloseCall(d, pos);
                }
            }

            // Keeper.
            if (gm.Keeper.Blocks(pos, Radius * 0.8f))
            {
                if (gm.OnBallSaved()) return;
            }

            // Goal line: in, off the post (bounces back into play), or wide.
            if (vel.y > 0f && pos.y >= gm.Goal.LineY)
            {
                float rel = pos.x - gm.Goal.CenterX;
                float dx = Mathf.Abs(rel);
                if (dx <= gm.Goal.HalfWidth - 0.04f) { gm.OnGoal(dx); return; }
                if (dx <= gm.Goal.FrameHalfWidth + 0.1f && !hitPost)
                {
                    hitPost = true;
                    vel = new Vector3(vel.x + Mathf.Sign(rel) * 1.5f, -Mathf.Abs(vel.y) * 0.55f, 0f);
                    spin = -spin;
                    pos.y = gm.Goal.LineY - 0.02f;
                    transform.position = pos;
                    gm.OnPost(pos);
                    continue;
                }
                gm.OnMiss();
                return;
            }

            // Came back down past the kicker.
            if (vel.y < 0f && pos.y < spawn.y - 0.6f)
            {
                gm.OnShotDied(true);
                return;
            }
        }
    }
}
