using UnityEngine;

// A collectible drifting across the gap between two lanes. Shoot through it to grab it.
public class Pickup : MonoBehaviour
{
    public enum Kind { Coin, BigCoin, Shield, Slow, Double }

    public Kind kind;
    public const float Radius = 0.2f;

    SpriteRenderer sr, glow;
    float dir, speed, t;

    public static Pickup Spawn(Kind kind, float y, bool fromLeft, float speed)
    {
        var go = new GameObject("Pickup_" + kind);
        var p = go.AddComponent<Pickup>();
        p.kind = kind;
        p.dir = fromLeft ? 1f : -1f;
        p.speed = speed;

        var g = new GameObject("Glow");
        g.transform.SetParent(go.transform, false);
        g.transform.localScale = Vector3.one * 0.75f;
        p.glow = g.AddComponent<SpriteRenderer>();
        p.glow.sprite = PixelArt.Glow;
        p.glow.color = p.GlowColor;
        p.glow.sortingOrder = 4;

        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = PixelArt.World(p.SpriteName);
        p.sr.sortingOrder = 5;

        float edge = GameManager.FieldHalf + 0.6f;
        go.transform.position = new Vector3(fromLeft ? -edge : edge, y, -3.7f);
        return p;
    }

    string SpriteName
    {
        get
        {
            switch (kind)
            {
                case Kind.BigCoin: return "gem";
                case Kind.Shield: return "shield";
                case Kind.Slow: return "slow";
                case Kind.Double: return "double";
                default: return "coin";
            }
        }
    }

    public Color GlowColor
    {
        get
        {
            switch (kind)
            {
                case Kind.BigCoin: return new Color(0.5f, 0.9f, 1f, 0.7f);
                case Kind.Shield: return new Color(0.3f, 0.6f, 1f, 0.7f);
                case Kind.Slow: return new Color(0.6f, 0.95f, 1f, 0.6f);
                case Kind.Double: return new Color(0.75f, 0.45f, 1f, 0.7f);
                default: return new Color(1f, 0.85f, 0.3f, 0.6f);
            }
        }
    }

    public bool IsPowerup { get { return kind == Kind.Shield || kind == Kind.Slow || kind == Kind.Double; } }

    void Update()
    {
        t += Time.deltaTime;
        var p = transform.position;
        p.x += dir * speed * Time.deltaTime;
        transform.position = p;
        float s = 1f + Mathf.Sin(t * 6f) * 0.08f;
        transform.localScale = new Vector3(s, s, 1f);
        glow.transform.localScale = Vector3.one * (0.7f + Mathf.Sin(t * 4f) * 0.1f);
        if (Mathf.Abs(p.x) > GameManager.FieldHalf + 1f) Remove();
    }

    public void Remove()
    {
        if (GameManager.I != null) GameManager.I.Pickups.Remove(this);
        Destroy(gameObject);
    }
}
