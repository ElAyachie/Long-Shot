using UnityEngine;

// A defender patrolling one horizontal lane. Driven by GameManager.Tick for a deterministic update order.
public class Defender : MonoBehaviour
{
    enum State { Hidden, Patrol, Knocked, Respawn, Celebrate }

    public float LaneY;
    public Collider2D Col { get; private set; }
    public bool Active { get { return state == State.Patrol; } }
    public bool Visible { get { return state != State.Hidden; } }
    public AudioClip HitClip { get; private set; }

    State state = State.Hidden;
    SpriteRenderer sr;
    Animator anim;
    Transform shadow;
    Vector3 baseScale;
    float dir = 1f, speed = 2f, pause, feintTimer, timer;
    Vector3 knockVel;
    float knockSpin;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        Col = GetComponent<Collider2D>();
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.simulated = true; }
        var src = GetComponent<AudioSource>();
        if (src != null) { HitClip = src.clip; src.playOnAwake = false; }
        sr.sortingOrder = 10;
        baseScale = transform.localScale;
        if (anim != null) anim.enabled = false;

        shadow = transform.Find("Shadow");
        if (shadow == null)
        {
            var go = new GameObject("Shadow");
            shadow = go.transform;
            shadow.SetParent(transform, false);
            shadow.localPosition = new Vector3(0f, -1.45f, 0f);
            shadow.localScale = new Vector3(1.7f, 0.55f, 1f);
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite = PixelArt.Circle;
            s.color = new Color(0f, 0f, 0f, 0.22f);
            s.sortingOrder = -50;
        }
        Hide();
    }

    public void Hide()
    {
        state = State.Hidden;
        sr.enabled = false;
        if (Col != null) Col.enabled = false;
        shadow.gameObject.SetActive(false);
        if (anim != null) anim.enabled = false;
        transform.position = new Vector3(-6f, LaneY, transform.position.z);
    }

    // Runs in from one side of the pitch.
    public void Enter(bool fromLeft, Difficulty d)
    {
        state = State.Patrol;
        sr.enabled = true;
        sr.color = Color.white;
        if (Col != null) Col.enabled = true;
        shadow.gameObject.SetActive(true);
        transform.rotation = Quaternion.identity;
        transform.localScale = baseScale;
        float edge = GameManager.PatrolHalf + 1.0f;
        transform.position = new Vector3(fromLeft ? -edge : edge, LaneY, transform.position.z);
        dir = fromLeft ? 1f : -1f;
        speed = Random.Range(d.minSpeed, d.maxSpeed);
        pause = 0f;
        feintTimer = Random.Range(1.2f, 3f);
        if (anim != null) anim.enabled = true;
    }

    // Shield impact: the defender is sent flying and comes back a moment later.
    public void Knock(Vector3 from)
    {
        state = State.Knocked;
        timer = 0.7f;
        float side = transform.position.x >= from.x ? 1f : -1f;
        knockVel = new Vector3(side * 4f, 3f, 0f);
        knockSpin = -side * 900f;
        if (Col != null) Col.enabled = false;
        if (anim != null) anim.enabled = false;
        shadow.gameObject.SetActive(false);
    }

    public void Celebrate()
    {
        if (state != State.Patrol) return;
        state = State.Celebrate;
        timer = 0f;
        if (anim != null) anim.enabled = false;
    }

    public void Resume()
    {
        if (state != State.Celebrate) return;
        state = State.Patrol;
        transform.position = new Vector3(transform.position.x, LaneY, transform.position.z);
        if (anim != null) anim.enabled = true;
    }

    public void Tick(float dt, Difficulty d, float speedMul, bool slowTint)
    {
        switch (state)
        {
            case State.Patrol: Patrol(dt, d, speedMul); break;
            case State.Knocked:
                timer -= dt;
                knockVel.y -= 12f * dt;
                transform.position += knockVel * dt;
                transform.Rotate(0, 0, knockSpin * dt);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(timer / 0.7f));
                if (timer <= 0f)
                {
                    state = State.Respawn;
                    sr.enabled = false;
                    timer = 1.0f;
                }
                break;
            case State.Respawn:
                timer -= dt;
                if (timer <= 0f) Enter(Random.value < 0.5f, d);
                break;
            case State.Celebrate:
                timer += dt;
                float hop = Mathf.Abs(Mathf.Sin(timer * 10f)) * 0.15f;
                transform.position = new Vector3(transform.position.x, LaneY + hop, transform.position.z);
                break;
        }
        if (state == State.Patrol || state == State.Celebrate)
            sr.color = slowTint ? new Color(0.65f, 0.85f, 1f) : Color.white;
    }

    void Patrol(float dt, Difficulty d, float speedMul)
    {
        if (pause > 0f)
        {
            pause -= dt;
            if (anim != null) anim.speed = 0f;
            return;
        }

        if (d.feintChance > 0f)
        {
            feintTimer -= dt;
            if (feintTimer <= 0f)
            {
                feintTimer = Random.Range(1.2f, 3f);
                if (Random.value < d.feintChance)
                {
                    pause = Random.Range(0.2f, 0.45f);
                    if (Random.value < 0.5f) dir = -dir;
                    speed = Random.Range(d.maxSpeed * 0.9f, d.maxSpeed * 1.3f);
                }
            }
        }

        var p = transform.position;
        p.x += dir * speed * speedMul * dt;
        float range = GameManager.PatrolHalf;
        if ((dir > 0f && p.x >= range) || (dir < 0f && p.x <= -range))
        {
            p.x = Mathf.Clamp(p.x, -range, range);
            dir = -dir;
            speed = Random.Range(d.minSpeed, d.maxSpeed);
        }
        p.y = LaneY;
        transform.position = p;
        sr.flipX = dir < 0f;
        if (anim != null) anim.speed = speed * speedMul / 2.6f;
    }
}
