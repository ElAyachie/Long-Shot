using UnityEngine;

// Plays the game's original clips plus a handful of synthesized retro blips.
public class Sfx : MonoBehaviour
{
    public static Sfx I;

    public AudioClip Kick, Goal, Hit1, Hit2;
    public AudioClip Coin, BigCoin, Power, Click, Close, Fanfare, Fire, Denied, Shield, Whistle, Bumper;

    AudioSource[] sources;
    AudioSource ambience;
    int next;
    const int Rate = 44100;

    public void Init(AudioClip kick, AudioClip goal, AudioClip hit1, AudioClip hit2)
    {
        I = this;
        Kick = kick; Goal = goal; Hit1 = hit1; Hit2 = hit2 != null ? hit2 : hit1;

        sources = new AudioSource[8];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }

        Coin = Notes("coin", new[] { 988f, 1319f }, 0.07f, 0.35f, true);
        BigCoin = Notes("bigcoin", new[] { 988f, 1319f, 1568f, 1976f }, 0.06f, 0.35f, true);
        Power = Notes("power", new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.05f, 0.3f, true);
        Click = Notes("click", new[] { 880f }, 0.03f, 0.25f, true);
        Fanfare = Notes("fanfare", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.09f, 0.3f, true);
        Fire = Sweep("fire", 200f, 900f, 0.35f, 0.3f, 0.6f);
        Close = Sweep("close", 1400f, 500f, 0.18f, 0.25f, 0.8f);
        Denied = Notes("denied", new[] { 196f, 147f }, 0.09f, 0.3f, true);
        Shield = Sweep("shield", 300f, 1500f, 0.2f, 0.3f, 0.2f);
        Whistle = Whistle2("whistle");
        Bumper = Notes("bumper", new[] { 660f, 990f, 1320f }, 0.035f, 0.35f, true);

        ambience = gameObject.AddComponent<AudioSource>();
        ambience.clip = Crowd();
        ambience.loop = true;
        ambience.volume = 0.18f;
        ambience.Play();
    }

    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sources == null) return;
        var s = sources[next];
        next = (next + 1) % sources.Length;
        s.Stop();
        s.clip = clip;
        s.volume = volume;
        s.pitch = pitch;
        s.Play();
    }

    public static void Apply()
    {
        AudioListener.volume = Save.D.sound ? 1f : 0f;
    }

    // ---------- Synthesis ----------

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Notes(string name, float[] freqs, float noteLen, float vol, bool square)
    {
        int per = (int)(noteLen * Rate);
        var data = new float[per * freqs.Length + Rate / 20];
        for (int n = 0; n < freqs.Length; n++)
        {
            double phase = 0;
            for (int i = 0; i < per; i++)
            {
                phase += freqs[n] / Rate;
                float env = 1f - i / (float)per * 0.6f;
                float w = square ? (phase % 1.0 < 0.5 ? 1f : -1f) : Mathf.Sin((float)(phase * Mathf.PI * 2));
                data[n * per + i] = w * env * vol;
            }
        }
        // Short tail fade on the last note.
        for (int i = per * freqs.Length; i < data.Length; i++) data[i] = 0f;
        return Make(name, data);
    }

    static AudioClip Sweep(string name, float f0, float f1, float len, float vol, float noise)
    {
        int count = (int)(len * Rate);
        var data = new float[count];
        double phase = 0;
        var rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            phase += Mathf.Lerp(f0, f1, t) / Rate;
            float tone = phase % 1.0 < 0.5 ? 1f : -1f;
            float n = (float)(rng.NextDouble() * 2 - 1);
            float env = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Lerp(tone, n, noise) * env * vol;
        }
        return Make(name, data);
    }

    static AudioClip Whistle2(string name)
    {
        int count = (int)(0.45f * Rate);
        var data = new float[count];
        double phase = 0;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float trill = Mathf.Sin(t * 0.45f * 2 * Mathf.PI * 28f) * 60f;
            phase += (2300f + trill) / Rate;
            float env = Mathf.Clamp01(t * 20f) * Mathf.Clamp01((1f - t) * 8f);
            data[i] = Mathf.Sin((float)(phase * Mathf.PI * 2)) * env * 0.22f;
        }
        return Make(name, data);
    }

    // Soft, slowly breathing brown noise that reads as a distant stadium crowd.
    static AudioClip Crowd()
    {
        int count = Rate * 4;
        var data = new float[count];
        var rng = new System.Random(7);
        float brown = 0f, lp = 0f;
        for (int i = 0; i < count; i++)
        {
            brown = Mathf.Clamp(brown + (float)(rng.NextDouble() * 2 - 1) * 0.04f, -1f, 1f);
            lp += (brown - lp) * 0.15f;
            float t = i / (float)count;
            float swell = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 2 * 2);
            data[i] = lp * swell * 0.8f;
        }
        // Crossfade the loop seam.
        int fade = Rate / 4;
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            data[i] = data[i] * k + data[count - fade + i] * (1f - k);
        }
        System.Array.Resize(ref data, count - fade);
        return Make("crowd", data);
    }
}
