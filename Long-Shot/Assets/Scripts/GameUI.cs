using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds and drives every screen. Layout is authored in a 1080x1920 reference space.
public class GameUI : MonoBehaviour
{
    public static readonly Color Yellow = new Color(1f, 0.83f, 0.25f);
    public static readonly Color Red = new Color(1f, 0.33f, 0.33f);
    public static readonly Color Green = new Color(0.25f, 0.85f, 0.45f);
    public static readonly Color Orange = new Color(1f, 0.6f, 0.15f);
    public static readonly Color Cyan = new Color(0.5f, 0.9f, 1f);
    public static readonly Color Purple = new Color(0.75f, 0.5f, 1f);
    public static readonly Color Blue = new Color(0.25f, 0.6f, 1f);
    public static readonly Color Muted = new Color(0.65f, 0.7f, 0.82f);
    static readonly Color Dark = new Color(0.07f, 0.09f, 0.15f, 0.97f);
    static readonly Color Panel = new Color(0.12f, 0.16f, 0.26f, 1f);
    static readonly Color PanelLight = new Color(0.19f, 0.25f, 0.38f, 1f);
    static readonly Color Dim = new Color(0.02f, 0.03f, 0.07f, 0.72f);

    GameManager gm;
    RectTransform safe, menu, hud, cont, over, pausePanel, shop, missions, daily, floatLayer;
    Rect lastSafe;
    readonly List<RaycastResult> hits = new List<RaycastResult>();

    // Menu
    RectTransform title;
    Text menuLevel, menuRank, menuCoins, menuBest, tapText;
    RectTransform menuXpFill;
    Image menuSound;
    GameObject missionBadge;

    // HUD
    Text score, mult, hudCoins, hint, windText;
    RectTransform scoreRt;
    Image[] pips;
    Image shieldIco, slowIco, doubleIco;
    Text slowTxt, doubleTxt;
    float hintUntil;
    int shownScore = -1;

    // Banner and toasts
    RectTransform banner;
    Text bannerText;
    Coroutine bannerCo;
    RectTransform toast;
    Text toastTitle, toastDesc, toastReward;
    readonly Queue<string[]> toastQueue = new Queue<string[]>();
    bool toastBusy;

    // Continue
    Text contReason, contCount, contCoins;
    RectTransform contFill;
    float contUntil;
    string contReasonStr;
    const float ContinueSeconds = 5f;

    // Game over
    Text overReason, overScore, overBest, overCoins, overXp, overLevel, overLevelUp;
    RectTransform overXpFill, overRibbon, overCard;
    RectTransform[] overMissionRows = new RectTransform[3];

    // Pause
    Image pauseSound, pauseVibrate;

    // Shop
    bool shopBalls = true;
    RectTransform shopGrid;
    Text shopCoins, shopMsg;
    Button tabBalls, tabPitches;
    readonly List<Image> rainbowPreviews = new List<Image>();
    float shopMsgUntil;

    // Missions / daily
    RectTransform missionsBody;
    Text dailyTitleDay, dailyReward;

    // Floating text pool
    readonly List<Text> floatPool = new List<Text>();

    // ---------- Build ----------

    public void Build(GameManager manager)
    {
        gm = manager;

        var cgo = new GameObject("GameUI", typeof(RectTransform));
        cgo.layer = 5;
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        cgo.AddComponent<GraphicRaycaster>();

        floatLayer = UIKit.Stretch(UIKit.Rect(cgo.transform, "Floats"));
        safe = UIKit.Stretch(UIKit.Rect(cgo.transform, "Safe"));
        ApplySafeArea();

        BuildHud();
        BuildMenu();
        BuildBannerAndToast();
        BuildContinue();
        BuildGameOver();
        BuildPause();
        BuildShop();
        BuildMissions();
        BuildDaily();
    }

    RectTransform NewScreen(string name, bool dim)
    {
        var rt = UIKit.Stretch(UIKit.Rect(safe, name));
        rt.gameObject.AddComponent<CanvasGroup>();
        if (dim)
        {
            // Blocker extends past the safe area so the whole screen is covered.
            var bg = UIKit.Img(rt, "Dim", Dim, null, true);
            UIKit.Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(-400f, -400f);
            bg.rectTransform.offsetMax = new Vector2(400f, 400f);
        }
        rt.gameObject.SetActive(false);
        return rt;
    }

    void Show(RectTransform screen, bool on)
    {
        if (screen.gameObject.activeSelf == on) return;
        screen.gameObject.SetActive(on);
        if (on) StartCoroutine(FadeIn(screen.GetComponent<CanvasGroup>()));
    }

    Text CoinPill(Transform parent, Vector2 anchor, Vector2 pos)
    {
        var bg = UIKit.Img(parent, "CoinPill", new Color(0f, 0f, 0f, 0.45f));
        UIKit.Place(bg.rectTransform, anchor, pos, new Vector2(290f, 96f));
        UIKit.Icon(bg.transform, "coin", Color.white, new Vector2(0f, 0.5f), new Vector2(56f, 0f), 64f);
        return UIKit.Txt(bg.transform, "0", 56, Yellow, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), TextAnchor.MiddleRight);
    }

    void BuildMenu()
    {
        menu = NewScreen("Menu", false);

        // Level badge and XP.
        var badge = UIKit.Img(menu, "LevelBadge", Yellow);
        UIKit.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -95f), new Vector2(130f, 130f));
        UIKit.Txt(badge.transform, "LV", 30, Dark, new Vector2(0.5f, 1f), new Vector2(0f, -26f)).GetComponent<Outline>().enabled = false;
        menuLevel = UIKit.Txt(badge.transform, "1", 66, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f));
        menuRank = UIKit.Txt(menu, "", 40, Color.white, new Vector2(0f, 1f), new Vector2(185f, -62f), TextAnchor.MiddleLeft);
        UIKit.Bar(menu, new Vector2(0f, 1f), new Vector2(370f, -122f), new Vector2(370f, 30f), new Color(0f, 0f, 0f, 0.5f), Cyan, out menuXpFill);

        menuCoins = CoinPill(menu, new Vector2(1f, 1f), new Vector2(-175f, -95f));
        var soundBtn = UIKit.Btn(menu, "", PanelLight, new Vector2(1f, 1f), new Vector2(-400f, -95f), new Vector2(110f, 110f), ToggleSound, "sound");
        menuSound = soundBtn.transform.Find("Face/sound").GetComponent<Image>();

        // Title.
        title = UIKit.Rect(menu, "Title");
        UIKit.Place(title, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(900f, 500f));
        UIKit.Txt(title, "LONG", 240, Color.white, new Vector2(0.5f, 0.5f), new Vector2(-30f, 100f));
        UIKit.Txt(title, "SHOT", 240, Yellow, new Vector2(0.5f, 0.5f), new Vector2(40f, -110f));
        menuBest = UIKit.Txt(menu, "", 60, Color.white, new Vector2(0.5f, 0.6f), Vector2.zero);

        UIKit.Btn(menu, "SHOP", Blue, new Vector2(0.5f, 0.45f), new Vector2(-200f, 0f), new Vector2(370f, 150f), () => OpenShop(true), "bag", 60);
        var mb = UIKit.Btn(menu, "GOALS", Purple, new Vector2(0.5f, 0.45f), new Vector2(200f, 0f), new Vector2(370f, 150f), OpenMissions, "flag", 60);
        var badgeImg = UIKit.Img(mb.transform, "Badge", Red);
        UIKit.Place(badgeImg.rectTransform, new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(56f, 56f));
        UIKit.Txt(badgeImg.transform, "!", 46, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
        missionBadge = badgeImg.gameObject;

        tapText = UIKit.Txt(menu, "TAP TO KICK OFF", 72, Color.white, new Vector2(0.5f, 0.27f), Vector2.zero);
    }

    void BuildHud()
    {
        hud = NewScreen("HUD", false);

        score = UIKit.Txt(hud, "0", 180, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -140f));
        scoreRt = score.rectTransform;
        mult = UIKit.Txt(hud, "", 56, Orange, new Vector2(0.5f, 1f), new Vector2(0f, -265f));

        pips = new Image[GameManager.FireStreak];
        for (int i = 0; i < pips.Length; i++)
        {
            pips[i] = UIKit.Img(hud, "Pip", Color.white);
            float x = (i - (pips.Length - 1) / 2f) * 46f;
            UIKit.Place(pips[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(x, -325f), new Vector2(32f, 32f));
        }

        UIKit.Icon(hud, "coin", Color.white, new Vector2(0f, 1f), new Vector2(70f, -80f), 64f);
        hudCoins = UIKit.Txt(hud, "0", 60, Yellow, new Vector2(0f, 1f), new Vector2(115f, -80f), TextAnchor.MiddleLeft);

        shieldIco = UIKit.Img(hud, "Shield", Color.white, PixelArt.Icon("shield"));
        slowIco = UIKit.Img(hud, "Slow", Color.white, PixelArt.Icon("slow"));
        doubleIco = UIKit.Img(hud, "Double", Color.white, PixelArt.Icon("double"));
        foreach (var img in new[] { shieldIco, slowIco, doubleIco })
            UIKit.Place(img.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -180f), new Vector2(70f, 70f));
        slowTxt = UIKit.Txt(slowIco.transform, "", 44, Color.white, new Vector2(1f, 0.5f), new Vector2(14f, 0f), TextAnchor.MiddleLeft);
        doubleTxt = UIKit.Txt(doubleIco.transform, "", 44, Color.white, new Vector2(1f, 0.5f), new Vector2(14f, 0f), TextAnchor.MiddleLeft);

        UIKit.Btn(hud, "", PanelLight, new Vector2(1f, 1f), new Vector2(-80f, -80f), new Vector2(110f, 110f), () => gm.SetPaused(true), "pause");

        hint = UIKit.Txt(hud, "", 54, Color.white, new Vector2(0.5f, 0.25f), Vector2.zero);
        windText = UIKit.Txt(hud, "", 46, Cyan, new Vector2(0.5f, 1f), new Vector2(0f, -390f));
    }

    void BuildBannerAndToast()
    {
        banner = UIKit.Rect(safe, "Banner");
        UIKit.Place(banner, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1500f, 190f));
        var bbg = banner.gameObject.AddComponent<Image>();
        bbg.color = new Color(0f, 0f, 0f, 0.55f);
        bbg.raycastTarget = false;
        bannerText = UIKit.Txt(banner, "", 104, Yellow, new Vector2(0.5f, 0.5f), Vector2.zero);
        banner.gameObject.SetActive(false);

        toast = UIKit.Rect(safe, "Toast");
        UIKit.Place(toast, new Vector2(0.5f, 1f), new Vector2(0f, 300f), new Vector2(960f, 150f));
        var tbg = toast.gameObject.AddComponent<Image>();
        tbg.color = new Color(0.16f, 0.12f, 0.3f, 0.96f);
        tbg.raycastTarget = false;
        UIKit.Icon(toast, "flag", Yellow, new Vector2(0f, 0.5f), new Vector2(75f, 0f), 80f);
        toastTitle = UIKit.Txt(toast, "", 44, Yellow, new Vector2(0f, 0.5f), new Vector2(140f, 30f), TextAnchor.MiddleLeft);
        toastDesc = UIKit.Txt(toast, "", 34, Color.white, new Vector2(0f, 0.5f), new Vector2(140f, -28f), TextAnchor.MiddleLeft);
        UIKit.Icon(toast, "coin", Color.white, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), 56f);
        toastReward = UIKit.Txt(toast, "", 52, Yellow, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), TextAnchor.MiddleRight);
        toast.gameObject.SetActive(false);
    }

    void BuildContinue()
    {
        cont = NewScreen("Continue", true);
        contReason = UIKit.Txt(cont, "", 140, Red, new Vector2(0.5f, 0.74f), Vector2.zero);
        UIKit.Txt(cont, "CONTINUE?", 84, Color.white, new Vector2(0.5f, 0.63f), Vector2.zero);
        contCount = UIKit.Txt(cont, "5", 200, Yellow, new Vector2(0.5f, 0.53f), Vector2.zero);
        UIKit.Bar(cont, new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(620f, 30f), new Color(0f, 0f, 0f, 0.6f), Yellow, out contFill);
        var b = UIKit.Btn(cont, "", Green, new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(560f, 170f), () => gm.AcceptContinue());
        var face = b.transform.Find("Face");
        UIKit.Icon(face, "coin", Color.white, new Vector2(0.5f, 0.5f), new Vector2(-110f, 0f), 90f);
        contCoins = UIKit.Txt(face, "", 80, Color.white, new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), TextAnchor.MiddleLeft);
        UIKit.Txt(cont, "SHIELD INCLUDED", 40, Cyan, new Vector2(0.5f, 0.31f), Vector2.zero);
        var skip = UIKit.Btn(cont, "NO THANKS", new Color(0.3f, 0.33f, 0.42f), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(420f, 110f),
            () => gm.DeclineContinue(contReasonStr), null, 48);
        skip.name = "Skip";
    }

    void BuildGameOver()
    {
        over = NewScreen("GameOver", true);
        var card = UIKit.Img(over, "Card", Panel);
        overCard = card.rectTransform;
        UIKit.Place(overCard, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1560f));
        var c = overCard;
        var mid = new Vector2(0.5f, 0.5f);

        overReason = UIKit.Txt(c, "", 100, Red, mid, new Vector2(0f, 680f));
        UIKit.Txt(c, "SCORE", 46, Muted, mid, new Vector2(0f, 565f));
        overScore = UIKit.Txt(c, "0", 200, Color.white, mid, new Vector2(0f, 445f));

        overRibbon = UIKit.Img(c, "Ribbon", Yellow).rectTransform;
        UIKit.Place(overRibbon, mid, new Vector2(0f, 300f), new Vector2(580f, 100f));
        overRibbon.localRotation = Quaternion.Euler(0, 0, -4f);
        var rt = UIKit.Txt(overRibbon, "NEW BEST!", 70, Color.white, mid, Vector2.zero);
        rt.color = new Color(0.35f, 0.15f, 0f);
        rt.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.5f);
        overBest = UIKit.Txt(c, "", 56, Muted, mid, new Vector2(0f, 300f));

        UIKit.Icon(c, "coin", Color.white, mid, new Vector2(-250f, 190f), 72f);
        overCoins = UIKit.Txt(c, "", 66, Yellow, mid, new Vector2(-195f, 190f), TextAnchor.MiddleLeft);
        overXp = UIKit.Txt(c, "", 66, Cyan, mid, new Vector2(110f, 190f), TextAnchor.MiddleLeft);

        overLevel = UIKit.Txt(c, "", 50, Color.white, mid, new Vector2(-420f, 90f), TextAnchor.MiddleLeft);
        UIKit.Bar(c, mid, new Vector2(70f, 90f), new Vector2(620f, 36f), new Color(0f, 0f, 0f, 0.5f), Cyan, out overXpFill);
        overLevelUp = UIKit.Txt(c, "", 46, Green, mid, new Vector2(0f, 25f));

        UIKit.Txt(c, "MISSIONS", 44, Muted, mid, new Vector2(0f, -50f));
        for (int i = 0; i < 3; i++)
        {
            var row = UIKit.Img(c, "Mission", PanelLight).rectTransform;
            UIKit.Place(row, mid, new Vector2(0f, -140f - i * 105f), new Vector2(880f, 92f));
            overMissionRows[i] = row;
        }

        UIKit.Btn(c, "RETRY", Green, mid, new Vector2(0f, -560f), new Vector2(500f, 170f), () => gm.Retry(), "play", 80);
        UIKit.Btn(c, "", Blue, mid, new Vector2(-375f, -560f), new Vector2(150f, 150f), () => gm.GoHome(), "home");
        UIKit.Btn(c, "", Purple, mid, new Vector2(375f, -560f), new Vector2(150f, 150f), () => OpenShop(true), "bag");
    }

    void BuildPause()
    {
        pausePanel = NewScreen("Pause", true);
        UIKit.Txt(pausePanel, "PAUSED", 150, Color.white, new Vector2(0.5f, 0.68f), Vector2.zero);
        UIKit.Btn(pausePanel, "RESUME", Green, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(560f, 170f), () => gm.SetPaused(false), "play", 76);
        var s = UIKit.Btn(pausePanel, "", PanelLight, new Vector2(0.5f, 0.4f), new Vector2(-90f, 0f), new Vector2(140f, 140f), ToggleSound, "sound");
        pauseSound = s.transform.Find("Face/sound").GetComponent<Image>();
        var v = UIKit.Btn(pausePanel, "", PanelLight, new Vector2(0.5f, 0.4f), new Vector2(90f, 0f), new Vector2(140f, 140f), ToggleVibrate, "vibrate");
        pauseVibrate = v.transform.Find("Face/vibrate").GetComponent<Image>();
        UIKit.Btn(pausePanel, "END MATCH", Red, new Vector2(0.5f, 0.27f), Vector2.zero, new Vector2(480f, 130f), () => gm.QuitRun(), null, 56);
    }

    void BuildShop()
    {
        shop = NewScreen("Shop", true);
        shop.Find("Dim").GetComponent<Image>().color = Dark;
        UIKit.Txt(shop, "SHOP", 110, Yellow, new Vector2(0.5f, 1f), new Vector2(0f, -120f));
        shopCoins = CoinPill(shop, new Vector2(0f, 1f), new Vector2(180f, -120f));
        UIKit.Btn(shop, "", Red, new Vector2(1f, 1f), new Vector2(-90f, -120f), new Vector2(120f, 120f), () => Show(shop, false), "close");

        tabBalls = UIKit.Btn(shop, "BALLS", Blue, new Vector2(0.5f, 1f), new Vector2(-240f, -280f), new Vector2(440f, 120f), () => { shopBalls = true; RefreshShop(); }, null, 56);
        tabPitches = UIKit.Btn(shop, "PITCHES", Blue, new Vector2(0.5f, 1f), new Vector2(240f, -280f), new Vector2(440f, 120f), () => { shopBalls = false; RefreshShop(); }, null, 56);

        shopGrid = UIKit.Rect(shop, "Grid");
        UIKit.Place(shopGrid, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(1000f, 1300f), new Vector2(0.5f, 1f));
        shopMsg = UIKit.Txt(shop, "", 54, Red, new Vector2(0.5f, 0f), new Vector2(0f, 90f));
    }

    void BuildMissions()
    {
        missions = NewScreen("Missions", true);
        missions.Find("Dim").GetComponent<Image>().color = Dark;
        UIKit.Txt(missions, "GOALS", 110, Yellow, new Vector2(0.5f, 1f), new Vector2(0f, -120f));
        UIKit.Btn(missions, "", Red, new Vector2(1f, 1f), new Vector2(-90f, -120f), new Vector2(120f, 120f), () => Show(missions, false), "close");
        missionsBody = UIKit.Stretch(UIKit.Rect(missions, "Body"));
    }

    void BuildDaily()
    {
        daily = NewScreen("Daily", true);
        var card = UIKit.Img(daily, "Card", Panel).rectTransform;
        UIKit.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 800f));
        var mid = new Vector2(0.5f, 0.5f);
        UIKit.Txt(card, "DAILY BONUS", 86, Yellow, mid, new Vector2(0f, 300f));
        dailyTitleDay = UIKit.Txt(card, "", 54, Muted, mid, new Vector2(0f, 195f));
        UIKit.Icon(card, "gift", Color.white, mid, new Vector2(0f, 50f), 180f);
        UIKit.Icon(card, "coin", Color.white, mid, new Vector2(-80f, -110f), 80f);
        dailyReward = UIKit.Txt(card, "", 84, Yellow, mid, new Vector2(-25f, -110f), TextAnchor.MiddleLeft);
        UIKit.Btn(card, "CLAIM", Green, mid, new Vector2(0f, -280f), new Vector2(460f, 150f), ClaimDaily, null, 76);
    }

    // ---------- Screens ----------

    public void ShowMenu()
    {
        Show(hud, false);
        Show(cont, false);
        Show(over, false);
        Show(pausePanel, false);
        Show(menu, true);
        Hint(null);
        RefreshMenu();
        // New players go straight to the game; the bonus waits until after their first match.
        if (Progression.DailyAvailable() > 0 && Save.D.games > 0) OpenDaily();
    }

    public void ShowHUD()
    {
        Show(menu, false);
        Show(cont, false);
        Show(over, false);
        Show(shop, false);
        Show(missions, false);
        Show(daily, false);
        Show(hud, true);
        shownScore = -1;
    }

    public void ShowPause(bool on)
    {
        Show(pausePanel, on);
        if (on) RefreshToggles();
    }

    public void ShowContinue(string reason, int cost)
    {
        contReasonStr = reason;
        contReason.text = reason;
        contCoins.text = cost.ToString();
        contUntil = Time.unscaledTime + ContinueSeconds;
        Show(cont, true);
    }

    public void ShowGameOver(RunResult r)
    {
        Show(cont, false);
        Show(hud, false);
        Hint(null);
        Show(over, true);
        StartCoroutine(Pop(overCard, 0.85f, 0.25f));

        overReason.text = r.reason;
        overScore.text = "0";
        StartCoroutine(CountUp(overScore, r.score, 0.6f));
        overRibbon.gameObject.SetActive(r.newBest && r.score > 0);
        overBest.gameObject.SetActive(!(r.newBest && r.score > 0));
        overBest.text = "BEST  " + r.best;
        if (r.newBest && r.score > 0) StartCoroutine(Pop(overRibbon, 1.6f, 0.35f));
        overCoins.text = "+" + r.coins;
        overXp.text = "+" + r.xp + " XP";

        int lvl = Progression.Level;
        overLevel.text = "LV " + lvl;
        float target = Progression.LevelProgress(Save.D.xp);
        float from = r.levelsGained > 0 ? 0f : Progression.LevelProgress(Mathf.Max(0, Save.D.xp - r.xp));
        StartCoroutine(AnimateFill(overXpFill, from, target, 0.8f));
        if (r.levelsGained > 0)
        {
            overLevelUp.text = "LEVEL UP!  " + Progression.Rank(lvl) + "  +" + Progression.LevelReward(lvl) + " COINS";
            Sfx.I.Play(Sfx.I.Fanfare, 0.7f);
        }
        else overLevelUp.text = "";

        for (int i = 0; i < 3; i++) FillMissionRow(overMissionRows[i], i < Save.D.missions.Count ? Save.D.missions[i] : null, 34);
    }

    void FillMissionRow(RectTransform row, MissionState m, int size)
    {
        UIKit.Clear(row);
        if (m == null) return;
        row.GetComponent<Image>().color = m.done ? new Color(0.15f, 0.42f, 0.25f) : PanelLight;
        UIKit.Txt(row, Progression.Describe(m), size, Color.white, new Vector2(0f, 0.5f), new Vector2(24f, 0f), TextAnchor.MiddleLeft);
        if (m.done) UIKit.Icon(row, "check", Green, new Vector2(1f, 0.5f), new Vector2(-50f, 0f), 56f);
        else UIKit.Txt(row, m.progress + "/" + m.target, size + 4, Muted, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), TextAnchor.MiddleRight);
    }

    void RefreshMenu()
    {
        int lvl = Progression.Level;
        menuLevel.text = lvl.ToString();
        menuRank.text = Progression.Rank(lvl);
        UIKit.SetFill(menuXpFill, Progression.LevelProgress(Save.D.xp));
        menuCoins.text = Save.D.coins.ToString();
        menuBest.text = Save.D.best > 0 ? "BEST  " + Save.D.best : "";
        missionBadge.SetActive(Progression.DailyAvailable() > 0 || Progression.AnyMissionDone());
        RefreshToggles();
    }

    void RefreshToggles()
    {
        var on = Color.white;
        var off = new Color(1f, 1f, 1f, 0.3f);
        menuSound.sprite = PixelArt.Icon(Save.D.sound ? "sound" : "mute");
        pauseSound.sprite = menuSound.sprite;
        pauseVibrate.color = Save.D.vibrate ? on : off;
    }

    void ToggleSound()
    {
        Save.D.sound = !Save.D.sound;
        Sfx.Apply();
        Save.Write();
        RefreshToggles();
    }

    void ToggleVibrate()
    {
        Save.D.vibrate = !Save.D.vibrate;
        Save.Write();
        RefreshToggles();
    }

    // ---------- Shop ----------

    void OpenShop(bool balls)
    {
        shopBalls = balls;
        RefreshShop();
        Show(shop, true);
    }

    void RefreshShop()
    {
        shopCoins.text = Save.D.coins.ToString();
        if (menu.gameObject.activeSelf) RefreshMenu();
        tabBalls.transform.Find("Face").GetComponent<Image>().color = shopBalls ? Blue : PanelLight;
        tabPitches.transform.Find("Face").GetComponent<Image>().color = shopBalls ? PanelLight : Blue;
        UIKit.Clear(shopGrid);
        rainbowPreviews.Clear();

        int count = shopBalls ? Catalog.Balls.Length : Catalog.Pitches.Length;
        for (int i = 0; i < count; i++)
        {
            string id, name;
            int price, level;
            bool equipped;
            if (shopBalls)
            {
                var b = Catalog.Balls[i];
                id = b.id; name = b.name; price = b.price; level = b.level; equipped = Save.D.ball == id;
            }
            else
            {
                var p = Catalog.Pitches[i];
                id = p.id; name = p.name; price = p.price; level = p.level; equipped = Save.D.pitch == id;
            }
            bool owned = Catalog.Owns(shopBalls, id, price, level);
            int col = i % 3, rowIdx = i / 3;
            var pos = new Vector2((col - 1) * 335f, -200f - rowIdx * 430f);
            int index = i;
            bool ballTab = shopBalls;
            var card = UIKit.Btn(shopGrid, "", equipped ? Yellow : Panel, new Vector2(0.5f, 1f), pos, new Vector2(310f, 400f),
                () => OnShopItem(ballTab, index));
            var face = card.transform.Find("Face");

            if (shopBalls) BallPreview(face, Catalog.Balls[i]);
            else PitchPreview(face, Catalog.Pitches[i]);

            UIKit.Txt(face, name, 38, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f));
            if (shopBalls && Catalog.Balls[i].perk != "")
                UIKit.Txt(face, Catalog.Balls[i].perk, 26, Cyan, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f));
            if (equipped)
                UIKit.Txt(face, "EQUIPPED", 36, new Color(0.3f, 0.15f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f)).GetComponent<Outline>().enabled = false;
            else if (owned)
                UIKit.Txt(face, "EQUIP", 38, Green, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f));
            else if (price == 0)
            {
                UIKit.Icon(face, "lock", Muted, new Vector2(0.5f, 0.5f), new Vector2(-70f, -150f), 40f);
                UIKit.Txt(face, "LV " + level, 40, Muted, new Vector2(0.5f, 0.5f), new Vector2(-35f, -150f), TextAnchor.MiddleLeft);
            }
            else
            {
                UIKit.Icon(face, "coin", Color.white, new Vector2(0.5f, 0.5f), new Vector2(-60f, -150f), 44f);
                UIKit.Txt(face, price.ToString(), 42, Save.D.coins >= price ? Yellow : Red, new Vector2(0.5f, 0.5f), new Vector2(-28f, -150f), TextAnchor.MiddleLeft);
            }
        }
    }

    void BallPreview(Transform face, BallSkin skin)
    {
        var sr = gm.Ball.GetComponent<SpriteRenderer>();
        var glow = UIKit.Img(face, "Glow", new Color(skin.trail.r, skin.trail.g, skin.trail.b, 0.35f), PixelArt.Glow);
        UIKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(230f, 230f));
        var img = UIKit.Img(face, "Ball", skin.tint, sr.sprite);
        UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(200f, 200f));
        if (skin.fx == BallFx.Rainbow) rainbowPreviews.Add(img);
    }

    void PitchPreview(Transform face, PitchTheme t)
    {
        var bg = UIKit.Img(face, "Swatch", t.a);
        UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(240f, 200f));
        for (int s = 0; s < 4; s++)
        {
            if (s % 2 == 0) continue;
            var stripe = UIKit.Img(bg.transform, "Stripe", t.b);
            UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 25f + s * 50f), new Vector2(240f, 50f));
        }
        var line = UIKit.Img(bg.transform, "Line", new Color(t.line.r, t.line.g, t.line.b, 1f));
        UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 6f));
        var ring = UIKit.Img(bg.transform, "Circle", new Color(t.line.r, t.line.g, t.line.b, 1f), PixelArt.Ring);
        UIKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
        var edge = bg.gameObject.AddComponent<Outline>();
        edge.effectColor = t.outside;
        edge.effectDistance = new Vector2(6f, -6f);
    }

    void OnShopItem(bool ball, int index)
    {
        string id;
        int price, level;
        if (ball) { var b = Catalog.Balls[index]; id = b.id; price = b.price; level = b.level; }
        else { var p = Catalog.Pitches[index]; id = p.id; price = p.price; level = p.level; }

        if (!Catalog.Owns(ball, id, price, level))
        {
            if (price == 0) { ShopMessage("REACH LEVEL " + level + " TO UNLOCK", Muted); return; }
            if (Save.D.coins < price) { ShopMessage("NOT ENOUGH COINS", Red); Sfx.I.Play(Sfx.I.Denied, 0.7f); return; }
            Save.D.coins -= price;
            Catalog.Grant(ball, id);
            Sfx.I.Play(Sfx.I.Fanfare, 0.7f);
            ShopMessage("UNLOCKED!", Green);
        }
        if (ball) Save.D.ball = id;
        else
        {
            Save.D.pitch = id;
            gm.ApplyPitch();
        }
        Save.Write();
        RefreshShop();
    }

    void ShopMessage(string msg, Color c)
    {
        shopMsg.text = msg;
        shopMsg.color = c;
        shopMsgUntil = Time.unscaledTime + 1.6f;
        StartCoroutine(Pop(shopMsg.rectTransform, 1.3f, 0.2f));
    }

    // ---------- Missions & daily ----------

    void OpenMissions()
    {
        RefreshMissions();
        Show(missions, true);
    }

    void RefreshMissions()
    {
        UIKit.Clear(missionsBody);
        var top = new Vector2(0.5f, 1f);
        for (int i = 0; i < Save.D.missions.Count && i < 3; i++)
        {
            var m = Save.D.missions[i];
            var card = UIKit.Img(missionsBody, "Mission", m.done ? new Color(0.15f, 0.42f, 0.25f) : Panel).rectTransform;
            UIKit.Place(card, top, new Vector2(0f, -310f - i * 190f), new Vector2(960f, 170f));
            var mid = new Vector2(0.5f, 0.5f);
            UIKit.Txt(card, Progression.Describe(m), 40, Color.white, mid, new Vector2(-450f, 32f), TextAnchor.MiddleLeft);
            RectTransform fill;
            UIKit.Bar(card, mid, new Vector2(-110f, -36f), new Vector2(680f, 32f), new Color(0f, 0f, 0f, 0.5f), m.done ? Green : Cyan, out fill);
            UIKit.SetFill(fill, m.progress / (float)Mathf.Max(1, m.target));
            UIKit.Txt(card, m.done ? "DONE!" : m.progress + "/" + m.target, 38, m.done ? Green : Muted, mid, new Vector2(450f, -36f), TextAnchor.MiddleRight);
            UIKit.Icon(card, "coin", Color.white, mid, new Vector2(330f, 32f), 50f);
            UIKit.Txt(card, "+" + m.reward, 44, Yellow, mid, new Vector2(362f, 32f), TextAnchor.MiddleLeft);
        }

        UIKit.Txt(missionsBody, "DAILY BONUS", 60, Yellow, top, new Vector2(0f, -880f));
        int current = Progression.DailyCurrentDay();
        int available = Progression.DailyAvailable();
        for (int d = 1; d <= 7; d++)
        {
            bool claimed = d < current || (d == current && available == 0);
            bool today = d == current && available > 0;
            var box = UIKit.Img(missionsBody, "Day", today ? Yellow : claimed ? new Color(0.15f, 0.42f, 0.25f) : Panel).rectTransform;
            UIKit.Place(box, top, new Vector2((d - 4) * 136f, -1030f), new Vector2(124f, 160f));
            UIKit.Txt(box, "DAY " + d, 28, today ? Dark : Muted, new Vector2(0.5f, 1f), new Vector2(0f, -26f));
            if (claimed) UIKit.Icon(box, "check", Green, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), 60f);
            else UIKit.Txt(box, Progression.DailyRewards[d - 1].ToString(), 40, today ? Color.white : Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f));
        }
        if (available > 0)
            UIKit.Btn(missionsBody, "CLAIM", Green, top, new Vector2(0f, -1200f), new Vector2(460f, 130f), ClaimDaily, null, 66);
        else
            UIKit.Txt(missionsBody, "COME BACK TOMORROW!", 44, Muted, top, new Vector2(0f, -1200f));

        UIKit.Txt(missionsBody, "CAREER", 56, Yellow, top, new Vector2(0f, -1340f));
        var d0 = Save.D;
        UIKit.Txt(missionsBody,
            "MATCHES  " + d0.games + "     GOALS  " + d0.goals + "\nCLOSE CALLS  " + d0.closeCalls + "     BEST  " + d0.best,
            40, Color.white, top, new Vector2(0f, -1450f));
    }

    void OpenDaily()
    {
        int day = Progression.DailyAvailable();
        if (day == 0) return;
        dailyTitleDay.text = "DAY " + day + " OF 7";
        dailyReward.text = "+" + Progression.DailyRewards[day - 1];
        Show(daily, true);
    }

    void ClaimDaily()
    {
        int reward = Progression.ClaimDaily();
        if (reward > 0)
        {
            Sfx.I.Play(Sfx.I.BigCoin, 0.8f);
            Fx.I.Burst(new Vector3(0f, 1f, 0f), 40, new[] { Yellow, Color.white, Orange }, 3f, 7f, 1.2f, 0.1f, 5f, 90f, 140f);
        }
        Show(daily, false);
        RefreshMenu();
        if (missions.gameObject.activeSelf) RefreshMissions();
    }

    // ---------- In-game feedback ----------

    public void Banner(string text, Color c)
    {
        if (bannerCo != null) StopCoroutine(bannerCo);
        bannerCo = StartCoroutine(BannerRoutine(text, c));
    }

    IEnumerator BannerRoutine(string text, Color c)
    {
        bannerText.text = text;
        bannerText.color = c;
        banner.gameObject.SetActive(true);
        yield return Slide(banner, new Vector2(1400f, 0f), Vector2.zero, 0.16f);
        yield return Wait(0.85f);
        yield return Slide(banner, Vector2.zero, new Vector2(-1400f, 0f), 0.16f);
        banner.gameObject.SetActive(false);
    }

    public void Toast(string titleText, string desc, int reward)
    {
        toastQueue.Enqueue(new[] { titleText, desc, reward.ToString() });
        if (!toastBusy) StartCoroutine(ToastRoutine());
    }

    IEnumerator ToastRoutine()
    {
        toastBusy = true;
        while (toastQueue.Count > 0)
        {
            var t = toastQueue.Dequeue();
            toastTitle.text = t[0];
            toastDesc.text = t[1];
            toastReward.text = "+" + t[2];
            toast.gameObject.SetActive(true);
            yield return Slide(toast, new Vector2(0f, 120f), new Vector2(0f, -470f), 0.2f);
            yield return Wait(2.2f);
            yield return Slide(toast, new Vector2(0f, -470f), new Vector2(0f, 120f), 0.2f);
            toast.gameObject.SetActive(false);
        }
        toastBusy = false;
    }

    public void Hint(string text)
    {
        hint.text = text ?? "";
        hintUntil = text == null ? 0f : Time.unscaledTime + 4f;
    }

    public void PopScore()
    {
        StartCoroutine(Pop(scoreRt, 1.35f, 0.2f));
    }

    public void Float(Vector3 world, string text, Color c, int size)
    {
        Text t = null;
        foreach (var f in floatPool) if (!f.gameObject.activeSelf) { t = f; break; }
        if (t == null)
        {
            t = UIKit.Txt(floatLayer, "", size, c);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 100f));
            floatPool.Add(t);
        }
        t.text = text;
        t.fontSize = size;
        t.color = c;
        Vector2 local;
        var screenPos = gm.Cam.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screenPos, null, out local);
        t.gameObject.SetActive(true);
        StartCoroutine(FloatRoutine(t, local));
    }

    IEnumerator FloatRoutine(Text t, Vector2 start)
    {
        var rt = t.rectTransform;
        Color c = t.color;
        float d = 0.9f;
        for (float e = 0f; e < d; e += Time.unscaledDeltaTime)
        {
            float k = e / d;
            rt.anchoredPosition = start + new Vector2(0f, 160f * (1f - (1f - k) * (1f - k)));
            float s = k < 0.15f ? Mathf.Lerp(0.4f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) * 4f);
            rt.localScale = Vector3.one * s;
            t.color = new Color(c.r, c.g, c.b, k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f);
            yield return null;
        }
        t.gameObject.SetActive(false);
    }

    public bool IsOverUI(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var ped = new PointerEventData(es) { position = screenPos };
        hits.Clear();
        es.RaycastAll(ped, hits);
        return hits.Count > 0;
    }

    // ---------- Per-frame ----------

    void Update()
    {
        ApplySafeArea();
        float t = Time.unscaledTime;

        if (menu.gameObject.activeSelf)
        {
            title.localRotation = Quaternion.Euler(0f, 0f, -4f + Mathf.Sin(t * 1.5f) * 1.5f);
            title.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.2f) * 0.02f);
            tapText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 5f) * 0.06f);
            menuCoins.text = Save.D.coins.ToString();
        }

        if (hud.gameObject.activeSelf) UpdateHud(t);

        if (cont.gameObject.activeSelf)
        {
            float left = contUntil - t;
            contCount.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
            UIKit.SetFill(contFill, left / ContinueSeconds);
            if (left <= 0f) gm.DeclineContinue(contReasonStr);
        }

        if (shop.gameObject.activeSelf)
        {
            foreach (var img in rainbowPreviews) if (img != null) img.color = Color.HSVToRGB((t * 0.4f) % 1f, 0.55f, 1f);
            if (t > shopMsgUntil) shopMsg.text = "";
        }
    }

    void UpdateHud(float t)
    {
        if (gm.Score != shownScore)
        {
            shownScore = gm.Score;
            score.text = shownScore.ToString();
        }
        hudCoins.text = gm.RunCoins.ToString();

        int m = gm.Multiplier;
        mult.text = gm.OnFire ? "ON FIRE  x" + m : m > 1 ? "x" + m : "";
        mult.color = gm.OnFire ? Color.Lerp(Orange, Yellow, Mathf.PingPong(t * 4f, 1f)) : Purple;

        bool showPips = gm.Streak > 0 || gm.OnFire;
        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].gameObject.SetActive(showPips);
            bool lit = gm.OnFire || i < gm.Streak;
            pips[i].color = lit ? (gm.OnFire ? Color.Lerp(Orange, Yellow, Mathf.PingPong(t * 6f + i * 0.2f, 1f)) : Orange) : new Color(1f, 1f, 1f, 0.25f);
        }

        // Stack active power-ups down the left side.
        float y = -180f;
        y = PlacePower(shieldIco, gm.Shield, y);
        y = PlacePower(slowIco, gm.SlowShots > 0, y);
        PlacePower(doubleIco, gm.DoubleGoals > 0, y);

        if (gm.Wind == 0f) windText.text = "";
        else
        {
            string arrows = new string(gm.Wind > 0f ? '>' : '<', Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(gm.Wind)), 1, 3));
            windText.text = gm.Wind > 0f ? "WIND " + arrows : arrows + " WIND";
        }
        slowTxt.text = gm.SlowShots.ToString();
        doubleTxt.text = gm.DoubleGoals.ToString();

        if (hintUntil > 0f && t > hintUntil) Hint(null);
        var hc = hint.color;
        hc.a = 0.75f + Mathf.Sin(t * 5f) * 0.25f;
        hint.color = hc;
    }

    float PlacePower(Image img, bool on, float y)
    {
        img.gameObject.SetActive(on);
        if (!on) return y;
        img.rectTransform.anchoredPosition = new Vector2(70f, y);
        return y - 90f;
    }

    void ApplySafeArea()
    {
        var sa = Screen.safeArea;
        if (sa == lastSafe || Screen.width <= 0 || Screen.height <= 0) return;
        lastSafe = sa;
        safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safe.offsetMin = safe.offsetMax = Vector2.zero;
    }

    // ---------- Tweens (unscaled so they run through hit-stop and pause) ----------

    static IEnumerator Wait(float seconds)
    {
        for (float e = 0f; e < seconds; e += Time.unscaledDeltaTime) yield return null;
    }

    static IEnumerator FadeIn(CanvasGroup g)
    {
        if (g == null) yield break;
        for (float e = 0f; e < 0.18f; e += Time.unscaledDeltaTime)
        {
            g.alpha = e / 0.18f;
            yield return null;
        }
        g.alpha = 1f;
    }

    static IEnumerator Pop(RectTransform rt, float from, float dur)
    {
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            float k = e / dur;
            rt.localScale = Vector3.one * Mathf.Lerp(from, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    static IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, float dur)
    {
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            float k = e / dur;
            rt.anchoredPosition = Vector2.Lerp(from, to, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    static IEnumerator CountUp(Text t, int target, float dur)
    {
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            t.text = Mathf.RoundToInt(target * (e / dur)).ToString();
            yield return null;
        }
        t.text = target.ToString();
    }

    static IEnumerator AnimateFill(RectTransform fill, float from, float to, float dur)
    {
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            UIKit.SetFill(fill, Mathf.Lerp(from, to, e / dur));
            yield return null;
        }
        UIKit.SetFill(fill, to);
    }
}
