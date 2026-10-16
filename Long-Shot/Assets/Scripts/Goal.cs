using UnityEngine;

// The goal at the top of the pitch. Can slide side to side at higher difficulty.
public class Goal : MonoBehaviour
{
    const float WidthScale = 1.3f, HeightScale = 1.12f;

    BoxCollider2D box;
    SpriteRenderer sr;
    Vector3 basePos, baseScale;
    float phase, punch;

    public AudioClip GoalClip { get; private set; }

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.simulated = true; }
        var src = GetComponent<AudioSource>();
        if (src != null) { GoalClip = src.clip; src.playOnAwake = false; }
        basePos = transform.position;
        // Wider than the original art so aimed and banked shots have a fair target.
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(WidthScale, HeightScale, 1f));
        baseScale = transform.localScale;
        if (sr != null) sr.sortingOrder = 8;
    }

    Vector3 Scale { get { return transform.lossyScale; } }

    // Horizontal centre of the scoring area.
    public float CenterX { get { return transform.position.x + (box != null ? box.offset.x * baseScale.x : 0f); } }

    // Half the width a ball centre must be inside to count as a goal.
    public float HalfWidth { get { return box != null ? box.size.x * 0.5f * Mathf.Abs(baseScale.x) : 0.72f; } }

    // Half the width of the whole frame including posts.
    public float FrameHalfWidth { get { return sr != null ? sr.sprite.bounds.extents.x * Mathf.Abs(baseScale.x) : 0.93f; } }

    // The y position at which the ball counts as having reached the goal.
    public float LineY
    {
        get
        {
            if (box == null) return basePos.y - 0.2f;
            return basePos.y + (box.offset.y - box.size.y * 0.5f) * baseScale.y;
        }
    }

    // Bottom of the goal frame sprite, where the painted goal line goes.
    public float FrameBottom { get { return sr != null ? basePos.y - sr.sprite.bounds.extents.y * baseScale.y : basePos.y - 0.38f; } }

    public void ResetGoal()
    {
        phase = 0f;
        transform.position = basePos;
        transform.localScale = baseScale;
    }

    public void Punch() { punch = 1f; }

    public void Tick(float dt, Difficulty d)
    {
        var p = transform.position;
        if (d.goalMoves)
        {
            phase += dt * d.goalFreq;
            float amp = Mathf.Min(d.goalAmp, GameManager.FieldHalf - FrameHalfWidth - 0.05f);
            p.x = basePos.x + Mathf.Sin(phase) * amp;
        }
        else
        {
            p.x = Mathf.MoveTowards(p.x, basePos.x, dt * 2f);
        }
        transform.position = p;

        if (punch > 0f)
        {
            punch = Mathf.Max(0f, punch - dt * 4f);
            float k = Mathf.Sin(punch * Mathf.PI) * 0.12f;
            transform.localScale = new Vector3(baseScale.x * (1f + k), baseScale.y * (1f + k * 2f), baseScale.z);
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.6f), punch);
        }
    }
}
