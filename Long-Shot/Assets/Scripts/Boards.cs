using System.Collections.Generic;
using UnityEngine;

// Advertising boards along both touchlines. The ball bounces off them; one lit segment pays a bonus.
public class Boards : MonoBehaviour
{
    const float SegLen = 1.1f, Width = 0.14f, MinY = -7f, MaxY = 9f;
    const float LitMinY = -1.6f, LitMaxY = 3.2f;

    static readonly Color[] colors = { new Color(0.12f, 0.2f, 0.45f), new Color(0.85f, 0.85f, 0.9f), new Color(0.75f, 0.15f, 0.2f) };
    static readonly Color Lit = new Color(1f, 0.82f, 0.2f);

    class Seg { public SpriteRenderer sr; public Color baseColor; public float flash; public float y0, y1; }

    readonly List<Seg>[] sides = { new List<Seg>(), new List<Seg>() };
    Seg lit;
    SpriteRenderer litGlow;
    bool litEnabled;
    float relightTimer;

    public static Boards Create()
    {
        var b = new GameObject("Boards").AddComponent<Boards>();
        b.Build();
        return b;
    }

    void Build()
    {
        for (int s = 0; s < 2; s++)
        {
            float x = (s == 0 ? -1f : 1f) * (GameManager.FieldHalf + Width / 2f);
            int i = 0;
            for (float y = MinY; y < MaxY; y += SegLen, i++)
            {
                var go = new GameObject("Board");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(x, y + SegLen / 2f, -3.6f);
                go.transform.localScale = new Vector3(Width, SegLen - 0.04f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.Square;
                sr.sortingOrder = 6;
                var c = colors[(i + s) % colors.Length];
                sr.color = c;
                sides[s].Add(new Seg { sr = sr, baseColor = c, y0 = y, y1 = y + SegLen });
            }
        }

        var g = new GameObject("LitGlow");
        g.transform.SetParent(transform, false);
        litGlow = g.AddComponent<SpriteRenderer>();
        litGlow.sprite = PixelArt.Glow;
        litGlow.sortingOrder = 5;
        litGlow.enabled = false;
    }

    // Turns the bonus segment on (during a match) or off (menus).
    public void SetLitEnabled(bool on)
    {
        litEnabled = on;
        if (on) RollLit();
        else SetLit(null);
    }

    void RollLit()
    {
        var options = new List<Seg>();
        foreach (var side in sides)
            foreach (var s in side)
                if (s.y0 >= LitMinY && s.y1 <= LitMaxY && s != lit) options.Add(s);
        SetLit(options.Count > 0 ? options[Random.Range(0, options.Count)] : null);
    }

    void SetLit(Seg s)
    {
        if (lit != null) lit.sr.color = lit.baseColor;
        lit = s;
        litGlow.enabled = s != null;
        if (s != null)
        {
            var p = s.sr.transform.position;
            litGlow.transform.position = new Vector3(p.x, p.y, -3.55f);
        }
    }

    // Called when the ball hits a board. Returns true if it struck the lit segment.
    public bool Hit(int side, float y)
    {
        var list = sides[side > 0 ? 1 : 0];
        Seg hit = null;
        foreach (var s in list) if (y >= s.y0 && y < s.y1) { hit = s; break; }
        if (hit == null) return false;
        hit.flash = 1f;
        if (hit != lit) return false;
        SetLit(null);
        relightTimer = 1.2f;
        return true;
    }

    void Update()
    {
        float t = Time.time;
        if (litEnabled && lit == null)
        {
            relightTimer -= Time.deltaTime;
            if (relightTimer <= 0f) RollLit();
        }

        foreach (var side in sides)
            foreach (var s in side)
            {
                Color c = s == lit ? Color.Lerp(Lit, Color.white, Mathf.PingPong(t * 3f, 0.5f)) : s.baseColor;
                if (s.flash > 0f)
                {
                    s.flash = Mathf.Max(0f, s.flash - Time.deltaTime * 4f);
                    c = Color.Lerp(c, Color.white, s.flash);
                }
                s.sr.color = c;
            }

        if (litGlow.enabled)
        {
            float k = 0.6f + Mathf.Sin(t * 5f) * 0.15f;
            litGlow.transform.localScale = new Vector3(0.9f, SegLen * 1.5f, 1f) * (0.9f + k * 0.2f);
            litGlow.color = new Color(1f, 0.8f, 0.2f, k * 0.7f);
        }
    }
}
