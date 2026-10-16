using System.Collections.Generic;
using UnityEngine;

// Dotted trajectory preview shown while the player pulls back to aim.
public class AimGuide : MonoBehaviour
{
    const int MaxDots = 40;

    readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();
    readonly List<Vector2> points = new List<Vector2>();
    SpriteRenderer powerRing;

    public static AimGuide Create()
    {
        var g = new GameObject("AimGuide").AddComponent<AimGuide>();
        for (int i = 0; i < MaxDots; i++)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(g.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Circle;
            sr.sortingOrder = 25;
            sr.enabled = false;
            g.dots.Add(sr);
        }
        var ring = new GameObject("PowerRing");
        ring.transform.SetParent(g.transform, false);
        g.powerRing = ring.AddComponent<SpriteRenderer>();
        g.powerRing.sprite = PixelArt.Ring;
        g.powerRing.sortingOrder = 24;
        g.powerRing.enabled = false;
        return g;
    }

    public static Color PowerColor(float power)
    {
        return power < 0.5f
            ? Color.Lerp(new Color(0.5f, 1f, 0.5f), new Color(1f, 0.9f, 0.3f), power * 2f)
            : Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.35f, 0.3f), (power - 0.5f) * 2f);
    }

    public void Show(Vector2 from, Vector2 velocity, float power, float lineY, List<Bumper> bumpers)
    {
        Ball.Predict(from, velocity, lineY, 1, 2.0f, 0.3f, points, MaxDots, bumpers);
        Color c = PowerColor(power);
        for (int i = 0; i < dots.Count; i++)
        {
            var d = dots[i];
            d.enabled = i < points.Count;
            if (!d.enabled) continue;
            float k = i / (float)Mathf.Max(1, points.Count - 1);
            d.transform.position = new Vector3(points[i].x, points[i].y, -3.9f);
            float s = Mathf.Lerp(0.13f, 0.07f, k);
            d.transform.localScale = new Vector3(s, s, 1f);
            d.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.95f, 0.25f, k));
        }

        powerRing.enabled = true;
        float r = Mathf.Lerp(0.6f, 1.1f, power);
        powerRing.transform.position = new Vector3(from.x, from.y, -3.9f);
        powerRing.transform.localScale = new Vector3(r, r, 1f);
        powerRing.color = new Color(c.r, c.g, c.b, 0.8f);
    }

    public void Hide()
    {
        foreach (var d in dots) d.enabled = false;
        powerRing.enabled = false;
    }
}
