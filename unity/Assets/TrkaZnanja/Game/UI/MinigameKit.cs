// Zajednički dijelovi mini igara: traka vremena, runde sa 4 ponuđena odgovora (tasteri 1–4),
// okvir za fotografiju sa autorom, tasteri i mapa svijeta koja se uvećava i pomjera (točkić, dva prsta, prevlačenje).
using System;
using System.Collections;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class ChoiceRound
    {
        public Action<RectTransform> Stage;   // crta sliku/zvuk/obris u prostor lijevo od odgovora
        public List<string> Options;
        public string Answer;
        public Action OnShow, OnDone;
    }

    public sealed class MgTimer
    {
        public Image Fill;
        public Text Status;

        public static MgTimer Create(Transform parent)
        {
            var row = UIKit.HStack(parent, 14, "Tajmer");
            row.Size(-1, 40);
            var bar = UIKit.Bar(row.transform, Theme.Line, Theme.Go, out var fill, 20);
            bar.Size(-1, 20, 1);
            var status = UIKit.Label(row.transform, "", 28, Theme.Ink, TextAnchor.MiddleRight, true);
            status.Size(360, 40);
            return new MgTimer { Fill = fill, Status = status };
        }

        public void Set(float leftFrac, string status)
        {
            UIKit.SetFill(Fill, leftFrac);
            Fill.color = leftFrac > 0.25f ? Theme.Go : Theme.Red;
            Status.text = Lang.L(status);
        }
    }

    public static class Keys
    {
        // 1–4 (ili 0 kad ništa nije pritisnuto). Ako je uključen samo novi Input System, tastatura se ignoriše.
        public static int Number()
        {
            try
            {
                for (int n = 1; n <= 9; n++)
                    if (Input.GetKeyDown(KeyCode.Alpha0 + n) || Input.GetKeyDown(KeyCode.Keypad0 + n)) return n;
            }
            catch (InvalidOperationException) { }
            return 0;
        }

        public static int TouchCount
        {
            get { try { return Input.touchCount; } catch (InvalidOperationException) { return 0; } }
        }
    }

    public abstract partial class Minigame
    {
        static readonly Color[] OptionColors = { Col.Hex("#2b7de9"), Col.Hex("#9b4bd6"), Col.Hex("#f08a1c"), Col.Hex("#0f9a8a") };

        protected static IEnumerator Wait(float s)
        {
            float t = 0;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // Runde sa 4 ponuđena odgovora (port kit.playChoiceRounds): tačno +1, greška −0,5, rezultat = bodovi / cilj.
        protected IEnumerator ChoiceRounds(RectTransform host, Rng rng, int seconds, int goal, string unit, Func<ChoiceRound> next)
        {
            var col = UIKit.VStack(host, 14, "Runde", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var timer = MgTimer.Create(col.transform);
            var body = UIKit.HStack(col.transform, 22, "Igra", TextAnchor.MiddleCenter);
            body.Size(-1, -1, 1, 1);
            body.childForceExpandHeight = true;
            body.childControlHeight = true;
            var stage = UIKit.Rect(body.transform, "Scena");
            stage.Size(-1, -1, 1, 1);
            var opts = UIKit.VStack(body.transform, 16, "Odgovori", TextAnchor.MiddleCenter);
            opts.Size(470, -1, 0, 1);
            opts.childForceExpandHeight = true;
            opts.childControlHeight = true;

            int hits = 0, misses = 0;
            float t = 0, nextAt = -1;
            bool busy = false;
            ChoiceRound cur = null;
            var buttons = new List<GameButton>();

            void Show()
            {
                cur?.OnDone?.Invoke();
                cur = next();
                UIKit.Clear(stage);
                cur.Stage?.Invoke(stage);
                UIKit.Clear(opts.transform);
                buttons.Clear();
                for (int i = 0; i < cur.Options.Count; i++)
                {
                    int k = i;
                    buttons.Add(UIKit.Button(opts.transform, $"{i + 1}   {cur.Options[i]}", OptionColors[i % OptionColors.Length], () => Pick(k), 32));
                }
                cur.OnShow?.Invoke();
            }

            void Pick(int i)
            {
                if (busy || t >= seconds || cur == null || i >= cur.Options.Count) return;
                bool ok = cur.Options[i] == cur.Answer;
                busy = true;
                for (int j = 0; j < buttons.Count; j++)
                {
                    if (cur.Options[j] == cur.Answer) buttons[j].SetColor(Theme.Right);
                    else if (j == i) buttons[j].SetColor(Theme.Wrong);
                    else buttons[j].SetColor(Col.Hex("#b8c4ce"));
                }
                if (ok) { hits++; AudioKit.I?.Correct(); }
                else { misses++; AudioKit.I?.Wrong(); Haptics.Tap(40); }
                nextAt = t + (ok ? 0.35f : 0.9f);
            }

            yield return null;    // raspored
            Show();
            float botWait = 0;
            while (t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                timer.Set(1 - t / seconds, $"{hits} {unit} · {Mathf.CeilToInt(Mathf.Max(0, seconds - t))} s");
                if (busy && t >= nextAt) { busy = false; if (t < seconds) Show(); }
                int key = Keys.Number();
                if (!busy && key >= 1 && key <= buttons.Count) buttons[key - 1].Click();
                if (Auto && !busy)
                {
                    botWait += dt;
                    if (botWait > 0.6f) { botWait = 0; Pick(rng.Chance(0.8) ? cur.Options.IndexOf(cur.Answer) : rng.Range(0, cur.Options.Count)); }
                }
                yield return null;
            }
            cur?.OnDone?.Invoke();
            foreach (var b in buttons) b.Interactable = false;
            Score = Mathx.Clamp((hits - misses * 0.5) / (goal * seconds / (double)Duration), 0, 1);
            ResultLabel = $"{hits} {unit}";
        }

        // Fotografija u okviru (bez sječenja) i autor sa licencom preko donjeg dijela.
        protected static RawImage PhotoFrame(RectTransform parent, PhotoItem p, Texture2D tex)
        {
            var frame = UIKit.Box(parent, Col.Hex("#1f2b3a"), UIKit.Round, "Okvir slike", 0.8f);
            frame.rectTransform.Stretch();
            frame.gameObject.AddComponent<RectMask2D>();
            var img = UIKit.Raw(frame.transform, tex, "Fotografija");
            img.rectTransform.Stretch(10, 10, 10, 10);
            var arf = img.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = tex != null ? tex.width / (float)tex.height : 1.5f;
            img.raycastTarget = false;
            var cr = UIKit.Box(frame.transform, new Color(0, 0, 0, 0.5f), UIKit.Pill, "Autor", 1f);
            cr.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(520, 34));
            cr.raycastTarget = false;
            var t = UIKit.Label(cr.transform, p.Credit, 18, new Color(1, 1, 1, 0.92f), TextAnchor.MiddleCenter);
            t.rectTransform.Stretch(14, 0, 14, 0);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 18;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return img;
        }

        protected static Text Prompt(Transform parent, float height = 76)
        {
            var box = UIKit.Box(parent, Theme.Ink, UIKit.Pill, "Zadatak", 1f);
            box.Size(-1, height);
            var t = UIKit.Label(box.transform, "", 36, Color.white, TextAnchor.MiddleCenter, true);
            t.rectTransform.Stretch(24, 0, 24, 0);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = 36;
            return t;
        }
    }

    // Mapa svijeta za mini igre: uvećanje 1–12×, beskonačno pomjeranje lijevo-desno (mapa se ponavlja),
    // sloj za isticanje (država, rijeka, linija) i igle koje ostaju iste veličine.
    public sealed class WorldMapView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public Action<double, double> OnTap;
        RawImage baseImg, overlayImg;
        RectTransform rt, pinLayer;
        Canvas32 overlay;
        Texture2D overlayTex;
        float k = 1;
        double ox, oy;      // gornji lijevi ugao pogleda (u, v); u može izaći iz 0..1 (mapa se ponavlja)
        Vector2 downPos;
        bool dragged, pinching, pressed;
        float pinchDist;
        double startLon = 15;
        bool laidOut;
        readonly List<(RectTransform rt, double u, double v)> pins = new List<(RectTransform, double, double)>();
        const int OverlayW = 1536;
        const float MaxZoom = 12;

        public static WorldMapView Create(Transform parent, Action<double, double> onTap, double startLon = 15)
        {
            var frame = UIKit.Box(parent, WorldPainterColors.Sea, UIKit.Round, "Mapa svijeta", 0.8f);
            frame.gameObject.AddComponent<RectMask2D>();
            var view = UIKit.Rect(frame.transform, "Pogled").Stretch(6, 6, 6, 6);
            var v = view.gameObject.AddComponent<WorldMapView>();
            v.rt = view;
            v.OnTap = onTap;
            v.startLon = startLon;
            v.baseImg = view.gameObject.AddComponent<RawImage>();
            v.baseImg.texture = MinigameData.WorldMap;
            v.overlay = new Canvas32(OverlayW, (int)Math.Round(OverlayW * WorldFrame.Aspect));
            v.overlayTex = new Texture2D(v.overlay.W, v.overlay.H, TextureFormat.RGBA32, false) { name = "Mapa sloj", wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            MinigameData.Upload(v.overlayTex, v.overlay);
            v.overlayImg = UIKit.Raw(view, v.overlayTex, "Sloj");
            v.overlayImg.rectTransform.Stretch();
            v.overlayImg.raycastTarget = false;
            v.pinLayer = UIKit.Rect(view, "Igle").Stretch();

            var tools = UIKit.VStack(frame.transform, 10, "Alati", TextAnchor.UpperRight);
            ((RectTransform)tools.transform).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(76, 252));
            UIKit.Button(tools.transform, "+", Theme.Blue, () => v.ZoomCenter(1.6f), 40).Size(76, 76);
            UIKit.Button(tools.transform, "-", Theme.Blue, () => v.ZoomCenter(1 / 1.6f), 40).Size(76, 76);
            UIKit.Button(tools.transform, "1×", Theme.InkSoft, () => v.ResetView(), 28).Size(76, 76);
            return v;
        }

        public RectTransform Frame => (RectTransform)rt.parent;

        Vector2 ViewSize => rt.rect.size;
        double TexAspect => 1 / WorldFrame.Aspect;     // širina / visina mape
        double H0 { get { var s = ViewSize; double a = s.x / Math.Max(1, s.y); return Math.Min(1, TexAspect / a); } }
        double W0 { get { var s = ViewSize; double a = s.x / Math.Max(1, s.y); return H0 * a / TexAspect; } }
        double Wv => W0 / k;
        double Hv => H0 / k;

        public void ResetView()
        {
            k = 1;
            ox = WorldFrame.U(startLon) - Wv / 2;
            oy = (1 - Hv) / 2;
            Clamp();
        }

        void Clamp()
        {
            k = Mathf.Clamp(k, 1, MaxZoom);
            oy = Mathx.Clamp(oy, 0, Math.Max(0, 1 - Hv));
            if (Hv >= 1) oy = (1 - Hv) / 2;
        }

        public void ZoomCenter(float f) => ZoomAt(f, new Vector2(0.5f, 0.5f));

        // f = faktor, at = tačka u pogledu (0..1, y odozgo)
        void ZoomAt(float f, Vector2 at)
        {
            double u = ox + at.x * Wv, v = oy + at.y * Hv;
            k = Mathf.Clamp(k * f, 1, MaxZoom);
            ox = u - at.x * Wv;
            oy = v - at.y * Hv;
            Clamp();
        }

        public void Focus(IList<Vec2d> lonLat)
        {
            if (lonLat.Count == 0) return;
            double u0 = WorldFrame.U(lonLat[0].X);
            double ulo = u0, uhi = u0, vlo = 1, vhi = 0;
            foreach (var p in lonLat)
            {
                double u = WorldFrame.U(p.X);
                while (u - u0 > 0.5) u -= 1;
                while (u - u0 < -0.5) u += 1;
                ulo = Math.Min(ulo, u); uhi = Math.Max(uhi, u);
                double v = WorldFrame.V(p.Y);
                vlo = Math.Min(vlo, v); vhi = Math.Max(vhi, v);
            }
            double du = Math.Max(0.02, (uhi - ulo) * 1.6), dv = Math.Max(0.04, (vhi - vlo) * 1.6);
            k = (float)Math.Min(6, Math.Max(1, Math.Min(W0 / du, H0 / dv)));
            ox = (ulo + uhi) / 2 - Wv / 2;
            oy = (vlo + vhi) / 2 - Hv / 2;
            Clamp();
        }

        Vector2 Local01(Vector2 screen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, cam, out var l);
            var r = rt.rect;
            return new Vector2((l.x - r.xMin) / r.width, 1 - (l.y - r.yMin) / r.height);
        }

        public void OnPointerDown(PointerEventData e) { pressed = true; dragged = false; downPos = e.position; }

        public void OnDrag(PointerEventData e)
        {
            if (pinching) return;
            if ((e.position - downPos).magnitude > 12) dragged = true;
            if (!dragged) return;
            var a = Local01(e.position - e.delta, e.pressEventCamera);
            var b = Local01(e.position, e.pressEventCamera);
            ox -= (b.x - a.x) * Wv;
            oy -= (b.y - a.y) * Hv;
            Clamp();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!pressed) return;
            pressed = false;
            if (dragged || pinching || Keys.TouchCount > 1) return;
            var p = Local01(e.position, e.pressEventCamera);
            if (p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
            var ll = LonLatAt(p);
            OnTap?.Invoke(ll.X, ll.Y);
        }

        public void OnScroll(PointerEventData e)
        {
            if (Mathf.Abs(e.scrollDelta.y) < 0.01f) return;
            ZoomAt(e.scrollDelta.y > 0 ? 1.18f : 1 / 1.18f, Local01(e.position, e.enterEventCamera));
        }

        public Vec2d LonLatAt(Vector2 p01)
        {
            double u = ox + p01.x * Wv, v = oy + p01.y * Hv;
            u -= Math.Floor(u);
            return WorldFrame.ToLonLat(u, Mathx.Clamp(v, 0, 1));
        }

        void Update()
        {
            if (Keys.TouchCount >= 2)
            {
                var t0 = Input.GetTouch(0).position; var t1 = Input.GetTouch(1).position;
                float d = Vector2.Distance(t0, t1);
                if (!pinching) { pinching = true; pinchDist = Mathf.Max(1, d); dragged = true; }
                else if (d > 1) { ZoomAt(d / pinchDist, Local01((t0 + t1) / 2, null)); pinchDist = d; }
            }
            else if (pinching && Keys.TouchCount == 0) pinching = false;
        }

        void LateUpdate()
        {
            if (ViewSize.x < 10) return;
            if (!laidOut) { laidOut = true; ResetView(); }
            var uv = new Rect((float)ox, (float)(1 - oy - Hv), (float)Wv, (float)Hv);
            baseImg.uvRect = uv;
            overlayImg.uvRect = uv;
            foreach (var (prt, u, v) in pins)
            {
                double du = u - ox;
                du -= Math.Floor(du);
                double lx = du / Wv, ly = (v - oy) / Hv;
                bool vis = lx >= -0.02 && lx <= 1.02 && ly >= -0.02 && ly <= 1.02;
                prt.gameObject.SetActive(vis);
                if (vis) { prt.anchorMin = prt.anchorMax = new Vector2((float)lx, (float)(1 - ly)); prt.anchoredPosition = Vector2.zero; }
            }
        }

        // ---- sloj i igle ----
        public void ClearMarks()
        {
            Array.Clear(overlay.Data, 0, overlay.Data.Length);
            MinigameData.Upload(overlayTex, overlay);
            foreach (var p in pins) if (p.rt != null) Destroy(p.rt.gameObject);
            pins.Clear();
        }

        public void HighlightCountry(WorldCountry c, Rgba color) { WorldPainter.Highlight(overlay, c, color); }

        public void DrawLine(IList<Vec2d> lonLat, float width, Rgba color)
        {
            foreach (var q in WorldPainter.Project(lonLat, overlay.W, overlay.H)) overlay.Stroke(q, width, color, false);
        }

        public void ApplyMarks() => MinigameData.Upload(overlayTex, overlay);

        public void Pin(double lon, double lat, Color color, float size = 30)
        {
            var p = UIKit.Box(pinLayer, color, UIKit.Circle, "Igla");
            p.rectTransform.sizeDelta = new Vector2(size, size);
            p.raycastTarget = false;
            var o = p.gameObject.AddComponent<Outline>(); o.effectColor = Color.white; o.effectDistance = new Vector2(3, -3);
            pins.Add((p.rectTransform, WorldFrame.U(lon), WorldFrame.V(lat)));
        }
    }

    public static class WorldPainterColors
    {
        public static readonly Color Sea = Col.Hex("#bfe3f2");
    }
}
