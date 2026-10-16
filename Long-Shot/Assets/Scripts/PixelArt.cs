using System.Collections.Generic;
using UnityEngine;

// Builds crisp pixel-art sprites at runtime from text grids, so new art needs no extra asset files.
public static class PixelArt
{
    // Same pixel size as the original defender art (32 art pixels over 0.96 world units).
    public const float WorldPPU = 33.333f;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static readonly Dictionary<char, Color32> palette = new Dictionary<char, Color32>
    {
        { 'K', new Color32(20, 20, 28, 255) },
        { 'W', new Color32(255, 255, 255, 255) },
        { 'w', new Color32(200, 205, 215, 255) },
        { 'Y', new Color32(255, 214, 64, 255) },
        { 'y', new Color32(214, 150, 30, 255) },
        { 'O', new Color32(255, 140, 40, 255) },
        { 'R', new Color32(230, 60, 60, 255) },
        { 'r', new Color32(150, 30, 40, 255) },
        { 'B', new Color32(70, 160, 255, 255) },
        { 'b', new Color32(30, 90, 190, 255) },
        { 'C', new Color32(150, 235, 255, 255) },
        { 'G', new Color32(110, 230, 90, 255) },
        { 'g', new Color32(50, 150, 50, 255) },
        { 'P', new Color32(190, 110, 255, 255) },
        { 'p', new Color32(110, 50, 170, 255) },
        { 'F', new Color32(234, 192, 134, 255) },
        { 'H', new Color32(70, 45, 25, 255) },
        { 'S', new Color32(45, 45, 55, 255) },
    };

    public static Sprite Make(string key, string[] rows, float ppu, Vector2 pivot)
    {
        Sprite s;
        if (cache.TryGetValue(key, out s)) return s;
        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = rows[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                Color32 col;
                px[y * w + x] = palette.TryGetValue(c, out col) ? col : new Color32(0, 0, 0, 0);
            }
        }
        s = FromPixels(px, w, h, ppu, pivot, FilterMode.Point);
        cache[key] = s;
        return s;
    }

    public static Sprite Icon(string name)
    {
        return Make("icon_" + name, IconRows(name), 10f, new Vector2(0.5f, 0.5f));
    }

    public static Sprite World(string name)
    {
        return Make("world_" + name, IconRows(name), WorldPPU, new Vector2(0.5f, 0.5f));
    }

    public static Sprite FromPixels(Color32[] px, int w, int h, float ppu, Vector2 pivot, FilterMode filter)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
    }

    // 1x1 world-unit white square.
    public static Sprite Square
    {
        get
        {
            Sprite s;
            if (cache.TryGetValue("square", out s)) return s;
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            return cache["square"] = FromPixels(px, 4, 4, 4f, new Vector2(0.5f, 0.5f), FilterMode.Point);
        }
    }

    // Chunky pixel circle, 1 world unit across.
    public static Sprite Circle { get { return Disc("circle", 16, 0f, FilterMode.Point); } }

    // Thin pixel ring, 1 world unit across.
    public static Sprite Ring { get { return Disc("ring", 32, 0.8f, FilterMode.Point); } }

    // Smooth radial glow, 1 world unit across.
    public static Sprite Glow
    {
        get
        {
            Sprite s;
            if (cache.TryGetValue("glow", out s)) return s;
            const int n = 32;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            return cache["glow"] = FromPixels(px, n, n, n, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        }
    }

    static Sprite Disc(string key, int n, float inner, FilterMode filter)
    {
        Sprite s;
        if (cache.TryGetValue(key, out s)) return s;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                bool on = d <= 1f && d >= inner;
                px[y * n + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        return cache[key] = FromPixels(px, n, n, n, new Vector2(0.5f, 0.5f), filter);
    }

    static string[] IconRows(string name)
    {
        switch (name)
        {
            case "coin": return new[]
            {
                "...KKKK...",
                ".KKYYYYKK.",
                ".KYWYYYyK.",
                "KYWYYYYYyK",
                "KYWYYYYYyK",
                "KYYYYYYYyK",
                "KYYYYYYyyK",
                ".KYYYYyyK.",
                ".KKyyyyKK.",
                "...KKKK...",
            };
            case "gem": return new[]
            {
                "....KKKK....",
                "...KWCCBK...",
                "..KWCCCCBK..",
                ".KWCCCCCCBK.",
                "KKKKKKKKKKKK",
                "KCWCCCCCCBbK",
                ".KCWCCCCBbK.",
                "..KCCCCBbK..",
                "...KCCBbK...",
                "....KCbK....",
                ".....KK.....",
            };
            case "shield": return new[]
            {
                "KKKKKKKKKKK",
                "KBBBBBBBBbK",
                "KBWBBBBBBbK",
                "KBBBBWBBBbK",
                "KBBBWWWBBbK",
                "KBBWWWWWBbK",
                "KBBBWWWBBbK",
                ".KBBBWBBbK.",
                ".KBBBBBBbK.",
                "..KBBBBbK..",
                "...KBBbK...",
                "....KKK....",
            };
            case "slow": return new[]
            {
                "...KKKKK...",
                ".KKCCCCCKK.",
                ".KCWWKWWCK.",
                "KCWWWKWWWCK",
                "KCWWWKWWWCK",
                "KCWWWKKKWCK",
                "KCWWWWWWWCK",
                "KCWWWWWWWCK",
                ".KCWWWWWCK.",
                ".KKCCCCCKK.",
                "...KKKKK...",
            };
            case "double": return new[]
            {
                "..KKKKKKKK..",
                ".KPPPPPPPPK.",
                "KPPPPPPPPPpK",
                "KPWPWPWWWPpK",
                "KPPWPPPPWPpK",
                "KPWPWPWWWPpK",
                "KPPPPPWPPPpK",
                "KPPPPPWWWPpK",
                "KPPPPPPPPPpK",
                ".KppppppppK.",
                "..KKKKKKKK..",
            };
            case "fire": return new[]
            {
                "....R.....",
                "....RR....",
                "...RRR..R.",
                "..RRORR.R.",
                "..RROORRR.",
                ".RROOOORR.",
                ".RROOYOORR",
                "RROOYYYORR",
                "RROYYYYYOR",
                "RROYYWYYOR",
                ".RROYYYORR",
                "..RRRRRRR.",
            };
            case "gift": return new[]
            {
                "..KK....KK..",
                ".KRRK..KRRK.",
                "..KRRKKRRK..",
                "KKKKKKKKKKKK",
                "KRRRRYYRRRrK",
                "KKKKKYYKKKKK",
                ".KRRRYYRRrK.",
                ".KRRRYYRRrK.",
                ".KRRRYYRRrK.",
                ".KRRRYYRRrK.",
                ".KrrrYyrrrK.",
                ".KKKKKKKKKK.",
            };
            case "star": return new[]
            {
                ".....W.....",
                "....WWW....",
                "....WWW....",
                "WWWWWWWWWWW",
                ".WWWWWWWWW.",
                "..WWWWWWW..",
                "...WWWWW...",
                "..WWWWWWW..",
                "..WWW.WWW..",
                ".WWW...WWW.",
                ".WW.....WW.",
            };
            case "play": return new[]
            {
                "WW........",
                "WWWW......",
                "WWWWWW....",
                "WWWWWWWW..",
                "WWWWWWWWWW",
                "WWWWWWWWWW",
                "WWWWWWWW..",
                "WWWWWW....",
                "WWWW......",
                "WW........",
            };
            case "pause": return new[]
            {
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
            };
            case "home": return new[]
            {
                ".....WW.....",
                "....WWWW....",
                "...WWWWWW...",
                "..WWWWWWWW..",
                ".WWWWWWWWWW.",
                "WWWWWWWWWWWW",
                "..WWWWWWWW..",
                "..WWW..WWW..",
                "..WWW..WWW..",
                "..WWW..WWW..",
                "..WWW..WWW..",
            };
            case "bag": return new[]
            {
                "....WWWW....",
                "...W....W...",
                "...W....W...",
                ".WWWWWWWWWW.",
                ".WWWWWWWWWW.",
                ".WWWWWWWWWW.",
                "WWWWWWWWWWWW",
                "WWWWWWWWWWWW",
                "WWWWWWWWWWWW",
                "WWWWWWWWWWWW",
                ".WWWWWWWWWW.",
            };
            case "flag": return new[]
            {
                "WW.........",
                "WWWWWWWW...",
                "WWWWWWWWWW.",
                "WWWWWWWWWWW",
                "WWWWWWWWWW.",
                "WWWWWWWW...",
                "WW.........",
                "WW.........",
                "WW.........",
                "WW.........",
                "WW.........",
                "WWWW.......",
            };
            case "sound": return new[]
            {
                "....WW......",
                "...WWW...W..",
                "WWWWWW....W.",
                "WWWWWW..W.W.",
                "WWWWWW..W.W.",
                "WWWWWW..W.W.",
                "WWWWWW....W.",
                "...WWW...W..",
                "....WW......",
            };
            case "mute": return new[]
            {
                "....WW......",
                "...WWW......",
                "WWWWWW.W...W",
                "WWWWWW..W.W.",
                "WWWWWW...W..",
                "WWWWWW..W.W.",
                "WWWWWW.W...W",
                "...WWW......",
                "....WW......",
            };
            case "vibrate": return new[]
            {
                "W..WWWW..W",
                "W.W....W.W",
                "..W....W..",
                "W.W....W.W",
                "W.W....W.W",
                "..W....W..",
                "W.W....W.W",
                "W..WWWW..W",
            };
            case "lock": return new[]
            {
                "..WWWWWW..",
                ".WW....WW.",
                ".WW....WW.",
                ".WW....WW.",
                "WWWWWWWWWW",
                "WWWWWWWWWW",
                "WWWW..WWWW",
                "WWWW..WWWW",
                "WWWWWWWWWW",
                "WWWWWWWWWW",
            };
            case "check": return new[]
            {
                "..........W",
                ".........WW",
                "........WW.",
                "W......WW..",
                "WW....WW...",
                ".WW..WW....",
                "..WWWW.....",
                "...WW......",
            };
            case "close": return new[]
            {
                "WW.....WW",
                "WWW...WWW",
                ".WWW.WWW.",
                "..WWWWW..",
                "...WWW...",
                "..WWWWW..",
                ".WWW.WWW.",
                "WWW...WWW",
                "WW.....WW",
            };
            case "bumper": return new[]
            {
                "....KKKKKK....",
                "..KKRRRRRRKK..",
                ".KRRRRRRRRRrK.",
                ".KRRWWWWWWRrK.",
                "KRRWWRRRRWWRrK",
                "KRRWRRYYRRWRrK",
                "KRRWRYYYYRWRrK",
                "KRRWRYYYYRWRrK",
                "KRRWRRYYRRWRrK",
                "KRRWWRRRRWWRrK",
                ".KRRWWWWWWRrK.",
                ".KrRRRRRRRrrK.",
                "..KKrrrrrrKK..",
                "....KKKKKK....",
            };
            case "wind": return new[]
            {
                "......WWW...",
                ".....W...W..",
                ".........W..",
                "WWWWWWWWW...",
                "............",
                "WWWWWWWWWWW.",
                "...........W",
                "WWWWWWWW..W.",
                "........W...",
                ".......W....",
            };
            case "keeper": return new[]
            {
                "........KKKKKK........",
                ".......KHHHHHHK.......",
                "......KHHHHHHHHK......",
                "......KFFFFFFFFK......",
                "......KFKFFFFKFK......",
                "......KFFFFFFFFK......",
                ".......KFFFFFFK.......",
                ".KKK..KKYYYYYYKK..KKK.",
                "KWWWKKYYYYYYYYYYKKWWWK",
                "KWWWYYYYYYYYYYYYYYWWWK",
                "KWWWKKYYYYYYYYYYKKWWWK",
                ".KKK..KYYYYYYYYK..KKK.",
                "......KYYYYYYYYK......",
                "......KyyyyyyyyK......",
                "......KSSSSSSSSK......",
                "......KSSSSSSSSK......",
                "......KSSSKKSSSK......",
                "......KFFK..KFFK......",
                "......KYYK..KYYK......",
                "......KYYK..KYYK......",
                ".....KKKKK..KKKKK.....",
                ".....KKKKK..KKKKK.....",
            };
        }
        return new[] { "W" };
    }
}
