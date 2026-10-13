// UI kit (uGUI, sve u kodu): veliki šareni 3D dugmići, kartice sa sjenkom, natpisi (Nunito), segmenti, čipovi,
// klizač, polje za unos, traka napretka. Svaki tekst prolazi kroz Lang.L (latinica / ćirilica / engleski).
using System;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public static class Theme
    {
        public static readonly Color Ink = Col.Hex("#14232e");
        public static readonly Color InkSoft = Col.Hex("#4a5d6b");
        public static readonly Color Paper = Col.Hex("#ffffff");
        public static readonly Color PaperSoft = Col.Hex("#f2f6fa");
        public static readonly Color Line = Col.Hex("#d9e3ec");
        public static readonly Color Primary = Col.Hex("#ff9f1c");     // narandžasto-zlatna
        public static readonly Color Go = Col.Hex("#2fbf6b");          // zeleno
        public static readonly Color Blue = Col.Hex("#2b7de9");
        public static readonly Color Red = Col.Hex("#e2283e");
        public static readonly Color Purple = Col.Hex("#9b4bd6");
        public static readonly Color Gold = Col.Hex("#f6bd2a");
        public static readonly Color Right = Col.Hex("#1fae5b");
        public static readonly Color Wrong = Col.Hex("#e2283e");
        public static readonly Color Dim = new Color(0.05f, 0.09f, 0.14f, 0.55f);
    }

    public static class UIKit
    {
        static Sprite round, roundSmall, pill, shadow, circle;

        // ---- sprajtovi (zaobljeni pravougaonici, sjenka, krug) ----
        static Sprite RoundedSprite(int size, int radius, float soft = 1.2f)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Zaobljeno " + radius, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01((radius - d) / soft + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static Sprite Round => round ??= RoundedSprite(96, 30);
        public static Sprite RoundSmall => roundSmall ??= RoundedSprite(48, 14);
        public static Sprite Pill => pill ??= RoundedSprite(128, 63);
        public static Sprite Circle => circle ??= Sprite.Create(Tex.Radial(64, 0.9f), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        public static Sprite Shadow => shadow ??= RoundedSprite(128, 56, 22f);

        // ---- osnovni elementi ----
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;   // UI
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Center(this RectTransform rt, Vector2 size, Vector2 pos = default) =>
            rt.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        public static LayoutElement Size(this Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static Image Box(Transform parent, Color color, Sprite sprite = null, string name = "Kutija", float ppu = 1f)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Round;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
            img.color = color;
            return img;
        }

        // Kartica: bijela, zaobljena, sa mekom sjenkom.
        public static Image Card(Transform parent, string name = "Kartica", Color? color = null)
        {
            var holder = Rect(parent, name);
            var sh = Box(holder, new Color(0.05f, 0.1f, 0.2f, 0.28f), Shadow, "Sjenka", 0.5f);
            sh.rectTransform.Stretch(-26, -18, -26, -40);
            sh.raycastTarget = false;
            var face = Box(holder, color ?? Theme.Paper, Round, "Lice", 0.7f);
            face.rectTransform.Stretch();
            return face;
        }

        public static Text Label(Transform parent, string text, int size = 34, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, bool black = false, string name = "Tekst")
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = black ? GameData.FontBlack : GameData.Font;
            t.fontSize = size;
            t.color = color ?? Theme.Ink;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.text = Lang.L(text ?? "");
            t.lineSpacing = 0.95f;
            return t;
        }

        public static Text Outlined(this Text t, Color color, float px = 3)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(px, -px);
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = color.WithA(0.6f);
            s.effectDistance = new Vector2(0, -px * 2);
            return t;
        }

        public static HorizontalLayoutGroup HStack(Transform parent, float spacing = 16, string name = "Red", TextAnchor align = TextAnchor.MiddleLeft, RectOffset pad = null)
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = false; g.childForceExpandHeight = false;
            if (pad != null) g.padding = pad;
            return g;
        }

        public static VerticalLayoutGroup VStack(Transform parent, float spacing = 16, string name = "Kolona", TextAnchor align = TextAnchor.UpperLeft, RectOffset pad = null)
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            if (pad != null) g.padding = pad;
            return g;
        }

        public static GridLayoutGroup Grid(Transform parent, Vector2 cell, Vector2 spacing, int columns = 0, string name = "Mreža")
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell; g.spacing = spacing;
            g.childAlignment = TextAnchor.UpperLeft;
            if (columns > 0) { g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = columns; }
            return g;
        }

        public static ContentSizeFitter FitHeight(this Component c)
        {
            if (!c.TryGetComponent<ContentSizeFitter>(out var f)) f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return f;
        }

        // Pomjerljiva lista (vertikalno); vraća sadržaj u koji se dodaju elementi.
        public static RectTransform Scroll(Transform parent, string name = "Lista", float spacing = 18, RectOffset pad = null)
        {
            var view = Rect(parent, name);
            var img = view.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.001f);
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var content = VStack(view, spacing, "Sadržaj", TextAnchor.UpperLeft, pad ?? new RectOffset(8, 8, 8, 24));
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero; crt.sizeDelta = Vector2.zero;
            content.FitHeight();
            sr.content = crt;
            sr.viewport = view;
            return crt;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        // ---- dugme ----
        public static GameButton Button(Transform parent, string text, Color color, Action onClick, int fontSize = 36, string name = null)
        {
            var rt = Rect(parent, name ?? ("Dugme · " + text));
            var b = rt.gameObject.AddComponent<GameButton>();
            b.Build(text, color, fontSize);
            if (onClick != null) b.OnClick += onClick;
            return b;
        }

        public static Image Icon(Transform parent, Sprite sprite, Color color, float size)
        {
            var rt = Rect(parent, "Ikona");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.preserveAspect = true; img.raycastTarget = false;
            img.Size(size, size);
            return img;
        }

        // Segmentirani izbor (npr. Lako / Srednje / Teško).
        public static List<GameButton> Segmented(Transform parent, IList<(string id, string label)> options, string current, Action<string> onPick, int fontSize = 28, float height = 78)
        {
            var row = HStack(parent, 10, "Segmenti");
            row.childForceExpandWidth = true;
            row.Size(-1, height);
            var list = new List<GameButton>();
            foreach (var (id, label) in options)
            {
                var opt = id;
                var b = Button(row.transform, label, Theme.Blue, null, fontSize);
                b.Size(-1, height, 1);
                b.OnClick += () =>
                {
                    foreach (var x in list) x.SetSelected(x.Tag == opt);
                    onPick(opt);
                };
                b.Tag = id;
                b.SetSelected(id == current);
                list.Add(b);
            }
            return list;
        }

        public static InputField Input(Transform parent, string value, string placeholder, int maxLength, Action<string> onChange)
        {
            var bg = Box(parent, Theme.PaperSoft, RoundSmall, "Unos", 0.8f);
            bg.Size(-1, 76);
            var field = bg.gameObject.AddComponent<InputField>();
            var txt = Label(bg.transform, "", 32, Theme.Ink);
            txt.supportRichText = false;
            txt.rectTransform.Stretch(22, 6, 22, 6);
            var ph = Label(bg.transform, placeholder, 32, Theme.InkSoft.WithA(0.6f));
            ph.rectTransform.Stretch(22, 6, 22, 6);
            field.textComponent = txt;
            field.placeholder = ph;
            field.characterLimit = maxLength;
            field.text = value ?? "";
            field.onEndEdit.AddListener(v => onChange?.Invoke(v.Trim()));
            return field;
        }

        public static Image Bar(Transform parent, Color back, Color fill, out Image fillImg, float height = 18)
        {
            var bg = Box(parent, back, Pill, "Traka", 1f);
            bg.Size(-1, height);
            fillImg = Box(bg.transform, fill, Pill, "Puno", 1f);
            fillImg.rectTransform.Stretch();
            fillImg.type = Image.Type.Sliced;
            return bg;
        }

        public static void SetFill(Image fill, float k)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(k), 1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Image Chip(Transform parent, string text, Color color, int fontSize = 24)
        {
            var bg = Box(parent, color, Pill, "Čip · " + text, 1f);
            var t = Label(bg.transform, text, fontSize, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.Stretch(18, 2, 18, 2);
            var fit = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(20, 20, 6, 6);
            fit.childControlWidth = true; fit.childControlHeight = true;
            return bg;
        }

        public static RawImage Raw(Transform parent, Texture tex, string name = "Slika")
        {
            var rt = Rect(parent, name);
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            return r;
        }
    }

    // Veliko 3D dugme: tamnija „podloga” ispod lica, lice se spusti kad se pritisne, klik zvuk i blaga vibracija.
    public sealed class GameButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
    {
        public event Action OnClick;
        public string Tag;
        public Text Label { get; private set; }
        public Image Face { get; private set; }
        Image under;
        RectTransform faceRt;
        Color color;
        bool interactable = true, selected = false, selectable;
        const float Depth = 8;

        public void Build(string text, Color c, int fontSize)
        {
            color = c;
            var rt = (RectTransform)transform;
            under = UIKit.Box(rt, Darker(c), UIKit.Round, "Podloga", 0.9f);
            under.rectTransform.Stretch(0, Depth, 0, 0);
            Face = UIKit.Box(rt, c, UIKit.Round, "Lice", 0.9f);
            faceRt = Face.rectTransform;
            faceRt.Stretch(0, 0, 0, Depth);
            // odsjaj na vrhu (sjajna plastika)
            var hl = UIKit.Box(faceRt, new Color(1, 1, 1, 0.22f), UIKit.Round, "Odsjaj", 1.2f);
            hl.raycastTarget = false;
            hl.rectTransform.anchorMin = new Vector2(0, 0.52f); hl.rectTransform.anchorMax = Vector2.one;
            hl.rectTransform.offsetMin = new Vector2(8, 0); hl.rectTransform.offsetMax = new Vector2(-8, -6);
            Label = UIKit.Label(faceRt, text, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            Label.rectTransform.Stretch(14, 4, 14, 4);
            Label.Outlined(Darker(c, 0.55f), 2);
            Label.resizeTextForBestFit = true;
            Label.resizeTextMinSize = Mathf.Max(14, fontSize / 2);
            Label.resizeTextMaxSize = fontSize;
        }

        static Color Darker(Color c, float k = 0.72f) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        public void SetText(string text) => Label.text = Lang.L(text);

        public void SetColor(Color c)
        {
            color = c;
            Face.color = interactable ? c : Color.Lerp(c, Color.gray, 0.65f);
            under.color = Darker(Face.color);
            var o = Label.GetComponent<Outline>();
            if (o != null) o.effectColor = Darker(Face.color, 0.55f);
        }

        public bool Interactable
        {
            get => interactable;
            set { interactable = value; SetColor(color); }
        }

        // Za segmente i čipove: izabrano = puna boja, neizabrano = bijelo sa obojenim tekstom.
        public void SetSelected(bool on)
        {
            selectable = true;
            selected = on;
            if (on)
            {
                Face.color = color; under.color = Darker(color);
                Label.color = Color.white;
                foreach (var e in Label.GetComponents<Shadow>()) e.enabled = true;   // Outline je takođe Shadow
            }
            else
            {
                Face.color = Theme.Paper; under.color = Theme.Line;
                Label.color = Darker(color, 0.85f);
                foreach (var e in Label.GetComponents<Shadow>()) e.enabled = false;
            }
        }
        public bool Selected => selected;

        public void OnPointerDown(PointerEventData e)
        {
            if (!interactable) return;
            faceRt.offsetMin = new Vector2(0, Depth * 0.15f);
            faceRt.offsetMax = new Vector2(0, -Depth * 0.85f);
        }

        void Release()
        {
            if (faceRt == null) return;
            faceRt.offsetMin = new Vector2(0, Depth);
            faceRt.offsetMax = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        public void OnPointerClick(PointerEventData e) => Click();

        public void Click()
        {
            if (!interactable || !isActiveAndEnabled) return;
            AudioKit.I?.Click();
            Haptics.Tap(15);
            OnClick?.Invoke();
        }

        void OnDisable() => Release();
        public bool IsSelectable => selectable;
    }
}
