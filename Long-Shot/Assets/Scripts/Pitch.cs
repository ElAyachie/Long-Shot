using UnityEngine;

// Paints a mowed pitch with markings into a pixel texture behind everything.
public class Pitch : MonoBehaviour
{
    const float MinX = -4.4f, MaxX = 4.4f, MinY = -5.8f, MaxY = 7.8f;
    const float UnitsPerPx = 0.04f;
    const float Half = 0.045f; // half line thickness

    SpriteRenderer sr;

    public static Pitch Create()
    {
        var go = new GameObject("Pitch");
        var p = go.AddComponent<Pitch>();
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sortingOrder = -100;
        go.transform.position = new Vector3((MinX + MaxX) / 2f, (MinY + MaxY) / 2f, 5f);
        return p;
    }

    public void Apply(PitchTheme theme, Camera cam, float goalLineY, float centerY)
    {
        int w = Mathf.RoundToInt((MaxX - MinX) / UnitsPerPx);
        int h = Mathf.RoundToInt((MaxY - MinY) / UnitsPerPx);
        var px = new Color32[w * h];
        var rng = new System.Random(1234);
        float touch = GameManager.FieldHalf;
        bool night = theme.id == "night" || theme.id == "cyber";

        for (int y = 0; y < h; y++)
        {
            float wy = MinY + (y + 0.5f) * UnitsPerPx;
            for (int x = 0; x < w; x++)
            {
                float wx = MinX + (x + 0.5f) * UnitsPerPx;
                bool inside = Mathf.Abs(wx) <= touch && wy <= goalLineY;
                int stripe = Mathf.FloorToInt((goalLineY - wy) / 0.9f);
                Color c = (stripe & 1) == 0 ? theme.a : theme.b;
                if (!inside) c = Color.Lerp(c, theme.outside, 0.65f);

                float n = (float)rng.NextDouble();
                c *= 0.97f + 0.06f * n;

                if (night)
                {
                    float v = new Vector2(wx / 3.2f, (wy - 0.8f) / 6f).magnitude;
                    c *= Mathf.Lerp(1.15f, 0.7f, Mathf.Clamp01(v));
                }

                if (IsLine(wx, wy, touch, goalLineY, centerY))
                    c = Color.Lerp(c, theme.line, theme.line.a);

                c.a = 1f;
                px[y * w + x] = c;
            }
        }

        if (sr.sprite != null)
        {
            Destroy(sr.sprite.texture);
            Destroy(sr.sprite);
        }
        sr.sprite = PixelArt.FromPixels(px, w, h, 1f / UnitsPerPx, new Vector2(0.5f, 0.5f), FilterMode.Point);
        cam.backgroundColor = theme.outside;
    }

    static bool Near(float v, float target) { return Mathf.Abs(v - target) < Half; }

    static bool IsLine(float x, float y, float touch, float gl, float cy)
    {
        float ax = Mathf.Abs(x);
        // Touchlines and goal line.
        if (Near(ax, touch) && y <= gl + Half) return true;
        if (Near(y, gl) && ax <= touch) return true;
        // Penalty box.
        float boxBottom = gl - 1.55f;
        if (Near(ax, 1.75f) && y >= boxBottom && y <= gl) return true;
        if (Near(y, boxBottom) && ax <= 1.75f) return true;
        // Six-yard box.
        if (Near(ax, 0.95f) && y >= gl - 0.5f && y <= gl) return true;
        if (Near(y, gl - 0.5f) && ax <= 0.95f) return true;
        // Penalty spot and arc.
        var spot = new Vector2(0f, gl - 1.05f);
        float ds = (new Vector2(x, y) - spot).magnitude;
        if (ds < 0.06f) return true;
        if (Near(ds, 0.85f) && y < boxBottom) return true;
        // Halfway line, centre circle and spot.
        if (Near(y, cy) && ax <= touch) return true;
        float dc = (new Vector2(x, y) - new Vector2(0f, cy)).magnitude;
        if (Near(dc, 1.0f)) return true;
        if (dc < 0.07f) return true;
        // Corner arcs.
        float dk = (new Vector2(ax, y) - new Vector2(touch, gl)).magnitude;
        if (Near(dk, 0.3f) && ax <= touch && y <= gl) return true;
        return false;
    }
}
