using UnityEngine;

public enum BallFx { None, Rainbow, Sparkle, Flame }

public class BallSkin
{
    public string id, name;
    public int price, level;
    public Color tint, trail;
    public BallFx fx;

    // Physics perks.
    public string perk = "";
    public float decelMul = 1f;        // lower keeps pace for longer
    public float restitution = 0.85f;  // speed kept when bouncing off a board
    public float curveMul = 1f;        // how hard swipes bend the ball
    public float windMul = 1f;         // how much wind pushes it
    public float closeMul = 1f;        // how generous close calls are
    public int coinBonus;              // extra coins per pickup

    public BallSkin Perk(string label, float decel = 1f, float rest = 0.85f, float curve = 1f, float wind = 1f, float close = 1f, int coins = 0)
    {
        perk = label; decelMul = decel; restitution = rest; curveMul = curve; windMul = wind; closeMul = close; coinBonus = coins;
        return this;
    }
}

public class PitchTheme
{
    public string id, name;
    public int price, level;
    public Color a, b, line, outside;
}

// Unlockable cosmetics. Items with a level requirement and no price unlock for free at that level.
public static class Catalog
{
    public static readonly BallSkin[] Balls =
    {
        Ball("classic", "CLASSIC", 0, 0, new Color(1f, 1f, 1f), new Color(1f, 1f, 1f, 0.5f)),
        Ball("ice", "ICE", 120, 0, new Color(0.65f, 0.92f, 1f), new Color(0.4f, 0.85f, 1f, 0.6f)).Perk("KEEPS ITS PACE", decel: 0.7f),
        Ball("toxic", "TOXIC", 150, 0, new Color(0.7f, 1f, 0.35f), new Color(0.5f, 1f, 0.2f, 0.6f)).Perk("BOUNCY WALLS", rest: 1f),
        Ball("lava", "LAVA", 200, 0, new Color(1f, 0.55f, 0.3f), new Color(1f, 0.35f, 0.1f, 0.7f), BallFx.Flame).Perk("WIND PROOF", decel: 0.9f, wind: 0.3f),
        Ball("neon", "NEON", 250, 0, new Color(1f, 0.45f, 0.95f), new Color(1f, 0.3f, 0.9f, 0.7f)).Perk("BIG CURVE", curve: 1.6f),
        Ball("gold", "GOLD", 350, 0, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.8f, 0.2f, 0.6f), BallFx.Sparkle).Perk("+1 PER COIN", coins: 1),
        Ball("shadow", "SHADOW", 450, 0, new Color(0.45f, 0.42f, 0.6f), new Color(0.35f, 0.2f, 0.6f, 0.7f)).Perk("EASY CLOSE CALLS", close: 1.45f),
        Ball("rainbow", "RAINBOW", 0, 10, Color.white, Color.white, BallFx.Rainbow).Perk("ALL-ROUNDER", decel: 0.85f, curve: 1.25f, wind: 0.7f, close: 1.15f),
        Ball("legend", "LEGEND", 0, 20, new Color(1f, 0.95f, 0.7f), new Color(1f, 0.9f, 0.4f, 0.8f), BallFx.Sparkle).Perk("ALL PERKS", decel: 0.75f, rest: 1f, curve: 1.4f, wind: 0.5f, close: 1.3f, coins: 1),
    };

    public static readonly PitchTheme[] Pitches =
    {
        Pitch("classic", "CLASSIC", 0, 0, C(84, 160, 62), C(76, 148, 55), new Color(1f, 1f, 1f, 0.85f), C(62, 125, 46)),
        Pitch("night", "NIGHT MATCH", 200, 0, C(34, 86, 52), C(29, 77, 46), new Color(0.85f, 0.95f, 1f, 0.75f), C(16, 40, 30)),
        Pitch("retro", "RETRO", 300, 0, C(155, 188, 15), C(139, 172, 15), new Color(0.19f, 0.38f, 0.19f, 1f), C(110, 140, 20)),
        Pitch("futsal", "FUTSAL", 350, 0, C(222, 160, 95), C(210, 150, 86), new Color(0.15f, 0.35f, 0.85f, 0.9f), C(170, 70, 60)),
        Pitch("beach", "BEACH", 450, 0, C(244, 220, 160), C(236, 210, 150), new Color(0.2f, 0.5f, 0.9f, 0.8f), C(70, 175, 225)),
        Pitch("snow", "SNOW", 500, 0, C(236, 243, 255), C(222, 232, 250), new Color(0.85f, 0.2f, 0.25f, 0.85f), C(180, 200, 230)),
        Pitch("cyber", "CYBER", 0, 15, C(24, 16, 48), C(30, 20, 58), new Color(0.2f, 1f, 0.9f, 0.9f), C(12, 8, 26)),
    };

    static Color C(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f); }

    static BallSkin Ball(string id, string name, int price, int level, Color tint, Color trail, BallFx fx = BallFx.None)
    {
        return new BallSkin { id = id, name = name, price = price, level = level, tint = tint, trail = trail, fx = fx };
    }

    static PitchTheme Pitch(string id, string name, int price, int level, Color a, Color b, Color line, Color outside)
    {
        return new PitchTheme { id = id, name = name, price = price, level = level, a = a, b = b, line = line, outside = outside };
    }

    public static BallSkin CurrentBall
    {
        get { foreach (var b in Balls) if (b.id == Save.D.ball) return b; return Balls[0]; }
    }

    public static PitchTheme CurrentPitch
    {
        get { foreach (var p in Pitches) if (p.id == Save.D.pitch) return p; return Pitches[0]; }
    }

    static string Key(bool ball, string id) { return (ball ? "ball_" : "pitch_") + id; }

    public static bool Owns(bool ball, string id, int price, int level)
    {
        if (price == 0 && level == 0) return true;
        if (price == 0 && level > 0) return Progression.Level >= level;
        return Save.D.owned.Contains(Key(ball, id));
    }

    public static void Grant(bool ball, string id)
    {
        string k = Key(ball, id);
        if (!Save.D.owned.Contains(k)) Save.D.owned.Add(k);
    }
}
