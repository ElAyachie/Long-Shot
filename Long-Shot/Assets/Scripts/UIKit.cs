using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Small helpers for building uGUI from code.
public static class UIKit
{
    static Font font;

    public static Font Font
    {
        get
        {
            if (font != null) return font;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
            if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
            return font;
        }
    }

    public static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        return Place(rt, anchor, pos, size, new Vector2(0.5f, 0.5f));
    }

    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Img(Transform parent, string name, Color c, Sprite s = null, bool raycast = false)
    {
        var rt = Rect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        img.sprite = s;
        img.raycastTarget = raycast;
        if (s != null) img.preserveAspect = true;
        return img;
    }

    public static Image Icon(Transform parent, string icon, Color c, Vector2 anchor, Vector2 pos, float size)
    {
        var img = Img(parent, icon, c, PixelArt.Icon(icon));
        Place(img.rectTransform, anchor, pos, new Vector2(size, size));
        return img;
    }

    public static Text Txt(Transform parent, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
    {
        var rt = Rect(parent, "Text");
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = c;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.lineSpacing = 0.9f;
        if (outline)
        {
            var o = rt.gameObject.AddComponent<Outline>();
            float d = Mathf.Clamp(size * 0.05f, 2f, 7f);
            o.effectColor = new Color(0.05f, 0.06f, 0.1f, 0.95f);
            o.effectDistance = new Vector2(d, -d);
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0f, -d * 1.6f);
        }
        return t;
    }

    public static Text Txt(Transform parent, string text, int size, Color c, Vector2 anchor, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var t = Txt(parent, text, size, c, align);
        Vector2 pivot = align == TextAnchor.MiddleLeft ? new Vector2(0f, 0.5f) : align == TextAnchor.MiddleRight ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);
        Place(t.rectTransform, anchor, pos, new Vector2(10f, size * 1.2f), pivot);
        return t;
    }

    // A chunky button with a darker bottom edge that presses down when touched.
    public static Button Btn(Transform parent, string label, Color face, Vector2 anchor, Vector2 pos, Vector2 size,
        Action onClick, string icon = null, int fontSize = 64)
    {
        var rt = Place(Rect(parent, "Button " + label), anchor, pos, size);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = Color.Lerp(face, Color.black, 0.4f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;

        var faceRt = Rect(rt, "Face");
        Stretch(faceRt);
        faceRt.offsetMin = new Vector2(0f, 12f);
        var faceImg = faceRt.gameObject.AddComponent<Image>();
        faceImg.color = face;
        faceImg.raycastTarget = false;

        var hi = Img(faceRt, "Highlight", new Color(1f, 1f, 1f, 0.18f));
        hi.rectTransform.anchorMin = new Vector2(0f, 1f);
        hi.rectTransform.anchorMax = new Vector2(1f, 1f);
        hi.rectTransform.pivot = new Vector2(0.5f, 1f);
        hi.rectTransform.sizeDelta = new Vector2(0f, 8f);
        hi.rectTransform.anchoredPosition = Vector2.zero;

        bool hasLabel = !string.IsNullOrEmpty(label);
        if (icon != null)
        {
            float isz = Mathf.Min(size.y - 50f, 90f);
            float ix = hasLabel ? -size.x / 2f + 30f + isz / 2f : 0f;
            Icon(faceRt, icon, Color.white, new Vector2(0.5f, 0.5f), new Vector2(ix, 0f), isz);
        }
        if (hasLabel)
        {
            var t = Txt(faceRt, label, fontSize, Color.white);
            Stretch(t.rectTransform);
            if (icon != null) t.rectTransform.offsetMin = new Vector2(Mathf.Min(size.y - 50f, 90f) + 20f, 0f);
        }

        var fx = rt.gameObject.AddComponent<ButtonFx>();
        fx.face = faceRt;
        btn.onClick.AddListener(() =>
        {
            if (Sfx.I != null) Sfx.I.Play(Sfx.I.Click, 0.6f);
            onClick();
        });
        return btn;
    }

    public static Image Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color back, Color fill, out RectTransform fillRt)
    {
        var bg = Img(parent, "Bar", back);
        Place(bg.rectTransform, anchor, pos, size);
        var f = Img(bg.transform, "Fill", fill);
        fillRt = f.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        return bg;
    }

    public static void SetFill(RectTransform fill, float k)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(k), 1f);
    }

    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }
}

// Pushes a button's face down while pressed.
public class ButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform face;

    public void OnPointerDown(PointerEventData e) { Set(true); }
    public void OnPointerUp(PointerEventData e) { Set(false); }
    public void OnPointerExit(PointerEventData e) { Set(false); }

    void Set(bool down)
    {
        if (face == null) return;
        face.offsetMin = new Vector2(0f, down ? 2f : 12f);
        face.offsetMax = new Vector2(0f, down ? -10f : 0f);
    }

    void OnDisable() { Set(false); }
}
