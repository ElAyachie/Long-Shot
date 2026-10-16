using UnityEngine;

// Shuffles across the goal mouth once the player is good enough to need one.
public class Goalkeeper : MonoBehaviour
{
    public const float HalfW = 0.22f, HalfH = 0.3f;
    float Range = 0.62f;

    SpriteRenderer sr;
    Transform shadow;
    float rel, dir = 1f, pause, bob, timer, baseY;
    bool active, knocked, celebrating;
    Vector3 knockVel;

    public bool Active { get { return active && !knocked; } }

    // range: how far either side of the goal centre the keeper shuffles.
    public static Goalkeeper Create(float y, float range)
    {
        var go = new GameObject("Goalkeeper");
        var k = go.AddComponent<Goalkeeper>();
        k.baseY = y;
        k.Range = range;
        k.sr = go.AddComponent<SpriteRenderer>();
        k.sr.sprite = PixelArt.World("keeper");
        k.sr.sortingOrder = 12;

        var sh = new GameObject("Shadow");
        k.shadow = sh.transform;
        k.shadow.SetParent(go.transform, false);
        k.shadow.localPosition = new Vector3(0f, -0.32f, 0f);
        k.shadow.localScale = new Vector3(0.55f, 0.16f, 1f);
        var s = sh.AddComponent<SpriteRenderer>();
        s.sprite = PixelArt.Circle;
        s.color = new Color(0f, 0f, 0f, 0.22f);
        s.sortingOrder = -50;

        go.transform.position = new Vector3(0f, y, -3.78f);
        k.SetActive(false, false);
        return k;
    }

    // Visuals always follow the active flag, so the keeper is never visible while switched off.
    // Staying on mid-match leaves a knocked-out keeper to finish its knockback and return on its own.
    public void SetActive(bool on, bool animateIn)
    {
        bool turningOn = on && !active;
        active = on;
        if (!on || turningOn)
        {
            knocked = false;
            celebrating = false;
            transform.rotation = Quaternion.identity;
            sr.color = Color.white;
            sr.enabled = on;
            shadow.gameObject.SetActive(on);
        }
        if (turningOn && animateIn) rel = Random.value < 0.5f ? -Range : Range;
    }

    public bool Blocks(Vector2 p, float r)
    {
        if (!Active) return false;
        var k = transform.position;
        return Mathf.Abs(p.x - k.x) < HalfW + r && Mathf.Abs(p.y - k.y) < HalfH + r;
    }

    public void Knock()
    {
        knocked = true;
        timer = 1.6f;
        knockVel = new Vector3(Random.value < 0.5f ? -3f : 3f, 3.5f, 0f);
        shadow.gameObject.SetActive(false);
    }

    public void Celebrate() { celebrating = true; timer = 0f; }
    public void Resume() { celebrating = false; }

    public void Tick(float dt, float goalX, Difficulty d, float speedMul)
    {
        if (!active) return;

        if (knocked)
        {
            timer -= dt;
            if (timer > 1.0f)
            {
                knockVel.y -= 12f * dt;
                transform.position += knockVel * dt;
                transform.Rotate(0, 0, 800f * dt);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01((timer - 1.0f) / 0.6f));
            }
            else
            {
                sr.color = new Color(1f, 1f, 1f, 0f);
            }
            if (timer <= 0f)
            {
                knocked = false;
                transform.rotation = Quaternion.identity;
                sr.color = Color.white;
                shadow.gameObject.SetActive(true);
                rel = Random.value < 0.5f ? -Range : Range;
            }
            return;
        }

        if (celebrating)
        {
            timer += dt;
            float hop = Mathf.Abs(Mathf.Sin(timer * 9f)) * 0.18f;
            transform.position = new Vector3(goalX + rel, baseY + hop, transform.position.z);
            return;
        }

        if (pause > 0f) pause -= dt;
        else
        {
            rel += dir * d.keeperSpeed * speedMul * dt;
            if ((dir > 0f && rel >= Range) || (dir < 0f && rel <= -Range))
            {
                rel = Mathf.Clamp(rel, -Range, Range);
                dir = -dir;
                pause = Random.Range(0.25f, 0.6f);
            }
        }
        bob += dt * 6f;
        transform.position = new Vector3(goalX + rel, baseY + Mathf.Sin(bob) * 0.02f, transform.position.z);
        sr.flipX = dir < 0f;
    }
}
