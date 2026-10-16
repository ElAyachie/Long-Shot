using UnityEngine;

// Pinball-style pop bumper. The ball ricochets off it with a little extra pace.
public class Bumper : MonoBehaviour
{
    public const float Radius = 0.21f;
    public const float Boost = 1.08f;

    SpriteRenderer sr, glow;
    float appear, punch, vanish = -1f;

    public bool Solid { get { return vanish < 0f && appear >= 1f; } }
    public Vector2 Pos { get { return transform.position; } }

    public static Bumper Create(Vector2 pos)
    {
        var go = new GameObject("Bumper");
        var b = go.AddComponent<Bumper>();
        go.transform.position = new Vector3(pos.x, pos.y, -3.7f);
        go.transform.localScale = Vector3.zero;

        var g = new GameObject("Glow");
        g.transform.SetParent(go.transform, false);
        g.transform.localScale = Vector3.one * 0.9f;
        b.glow = g.AddComponent<SpriteRenderer>();
        b.glow.sprite = PixelArt.Glow;
        b.glow.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        b.glow.sortingOrder = 4;

        b.sr = go.AddComponent<SpriteRenderer>();
        b.sr.sprite = PixelArt.World("bumper");
        b.sr.sortingOrder = 7;
        return b;
    }

    public void Hit() { punch = 1f; }

    public void Despawn()
    {
        if (vanish < 0f) vanish = 1f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float s;
        if (vanish >= 0f)
        {
            vanish -= dt * 5f;
            s = Mathf.Max(0f, vanish);
            if (vanish <= 0f) { Destroy(gameObject); return; }
        }
        else
        {
            appear = Mathf.Min(1f, appear + dt * 4f);
            s = appear < 1f ? appear * (1f + Mathf.Sin(appear * Mathf.PI) * 0.3f) : 1f;
        }

        if (punch > 0f)
        {
            punch = Mathf.Max(0f, punch - dt * 6f);
            s *= 1f + punch * 0.35f;
        }
        transform.localScale = new Vector3(s, s, 1f);
        glow.color = new Color(1f, Mathf.Lerp(0.3f, 1f, punch), Mathf.Lerp(0.3f, 0.6f, punch), 0.45f + 0.15f * Mathf.Sin(Time.time * 4f) + punch * 0.4f);
    }
}
