using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lightweight pooled sprite particles, afterimages, camera shake and time effects.
public class Fx : MonoBehaviour
{
    public static Fx I;

    class Particle
    {
        public SpriteRenderer sr;
        public Vector3 vel;
        public float life, maxLife, gravity, drag, spin, size0, size1, stretch = 1f;
        public Color c0, c1;
    }

    readonly List<Particle> live = new List<Particle>();
    readonly Stack<Particle> pool = new Stack<Particle>();

    Camera cam;
    Vector3 camBase;
    float shakeAmp, shakeTime, shakeDur;
    float timeFxUntil;

    public static readonly Color[] Confetti =
    {
        new Color(1f, 0.85f, 0.25f), new Color(0.3f, 0.75f, 1f), new Color(1f, 0.35f, 0.4f),
        new Color(0.4f, 0.95f, 0.5f), new Color(0.85f, 0.5f, 1f), Color.white
    };

    public void Init(Camera c)
    {
        I = this;
        cam = c;
        camBase = c.transform.position;
    }

    public void SetCameraBase(Vector3 p)
    {
        camBase = p;
        if (cam != null) cam.transform.position = p;
    }

    Particle Get()
    {
        Particle p;
        if (pool.Count > 0) p = pool.Pop();
        else
        {
            var go = new GameObject("fx");
            go.transform.SetParent(transform, false);
            p = new Particle { sr = go.AddComponent<SpriteRenderer>() };
        }
        p.sr.gameObject.SetActive(true);
        live.Add(p);
        return p;
    }

    public void Burst(Vector3 pos, int count, Color[] colors, float speedMin, float speedMax, float life,
        float size, float gravity = 0f, float angle = 90f, float spread = 360f, Sprite sprite = null, int order = 30)
    {
        for (int i = 0; i < count; i++)
        {
            var p = Get();
            float a = (angle + Random.Range(-spread / 2f, spread / 2f)) * Mathf.Deg2Rad;
            float sp = Random.Range(speedMin, speedMax);
            p.vel = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * sp;
            p.maxLife = p.life = life * Random.Range(0.7f, 1.2f);
            p.gravity = gravity;
            p.drag = 2.5f;
            p.spin = Random.Range(-540f, 540f);
            p.size0 = size * Random.Range(0.7f, 1.3f);
            p.size1 = 0f;
            p.c0 = colors[Random.Range(0, colors.Length)];
            p.c1 = new Color(p.c0.r, p.c0.g, p.c0.b, 0f);
            p.stretch = 1f;
            p.sr.sprite = sprite != null ? sprite : PixelArt.Square;
            p.sr.sortingOrder = order;
            p.sr.transform.position = new Vector3(pos.x, pos.y, -4f);
            p.sr.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }
    }

    // A fading copy of a sprite, used for trails.
    public void Ghost(Sprite sprite, Vector3 pos, float rot, float scale, Color c, float life, int order = 18)
    {
        var p = Get();
        p.vel = Vector3.zero;
        p.maxLife = p.life = life;
        p.gravity = 0f;
        p.drag = 0f;
        p.spin = 0f;
        p.size0 = scale;
        p.size1 = scale * 0.4f;
        p.c0 = c;
        p.c1 = new Color(c.r, c.g, c.b, 0f);
        p.stretch = 1f;
        p.sr.sprite = sprite;
        p.sr.sortingOrder = order;
        p.sr.transform.position = new Vector3(pos.x, pos.y, -3.5f);
        p.sr.transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    public void Ring(Vector3 pos, Color c, float from, float to, float life)
    {
        var p = Get();
        p.vel = Vector3.zero;
        p.maxLife = p.life = life;
        p.gravity = 0f;
        p.drag = 0f;
        p.spin = 0f;
        p.size0 = from;
        p.size1 = to;
        p.c0 = c;
        p.c1 = new Color(c.r, c.g, c.b, 0f);
        p.stretch = 1f;
        p.sr.sprite = PixelArt.Ring;
        p.sr.sortingOrder = 31;
        p.sr.transform.position = new Vector3(pos.x, pos.y, -4f);
        p.sr.transform.rotation = Quaternion.identity;
    }

    // A long thin streak drifting across the screen, used to show wind.
    public void Streak(Vector3 pos, Vector3 vel, float life, float length, Color c)
    {
        var p = Get();
        p.vel = vel;
        p.maxLife = p.life = life;
        p.gravity = 0f;
        p.drag = 0f;
        p.spin = 0f;
        p.size0 = p.size1 = 0.03f;
        p.stretch = length / 0.03f;
        p.c0 = c;
        p.c1 = new Color(c.r, c.g, c.b, 0f);
        p.sr.sprite = PixelArt.Square;
        p.sr.sortingOrder = 3;
        p.sr.transform.position = new Vector3(pos.x, pos.y, -3.2f);
        p.sr.transform.rotation = Quaternion.identity;
    }

    public void Shake(float amp, float dur)
    {
        if (amp >= shakeAmp * (shakeTime / Mathf.Max(shakeDur, 0.001f)))
        {
            shakeAmp = amp;
            shakeTime = shakeDur = dur;
        }
    }

    // Briefly changes time scale (hit-stop / slow motion), measured in real seconds.
    public void TimeFx(float scale, float realSeconds)
    {
        StopAllCoroutines();
        StartCoroutine(TimeRoutine(scale, realSeconds));
    }

    IEnumerator TimeRoutine(float scale, float realSeconds)
    {
        timeFxUntil = Time.unscaledTime + realSeconds;
        if (!GameManager.Paused) Time.timeScale = scale;
        while (Time.unscaledTime < timeFxUntil) yield return null;
        if (!GameManager.Paused) Time.timeScale = 1f;
    }

    public void ClearParticles()
    {
        for (int i = live.Count - 1; i >= 0; i--) Recycle(i);
    }

    void Recycle(int i)
    {
        var p = live[i];
        live.RemoveAt(i);
        p.sr.gameObject.SetActive(false);
        pool.Push(p);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.life -= dt;
            if (p.life <= 0f) { Recycle(i); continue; }
            p.vel.y -= p.gravity * dt;
            p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
            var t = p.sr.transform;
            t.position += p.vel * dt;
            if (p.spin != 0f) t.Rotate(0, 0, p.spin * dt);
            float k = 1f - p.life / p.maxLife;
            float s = Mathf.Lerp(p.size0, p.size1, k);
            t.localScale = new Vector3(s * p.stretch, s, 1f);
            p.sr.color = p.stretch > 1f ? new Color(p.c0.r, p.c0.g, p.c0.b, p.c0.a * Mathf.Sin(k * Mathf.PI)) : Color.Lerp(p.c0, p.c1, k * k);
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 off = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float a = shakeAmp * Mathf.Clamp01(shakeTime / shakeDur);
            off = new Vector3(Random.Range(-a, a), Random.Range(-a, a), 0f);
        }
        cam.transform.position = camBase + off;
    }
}
