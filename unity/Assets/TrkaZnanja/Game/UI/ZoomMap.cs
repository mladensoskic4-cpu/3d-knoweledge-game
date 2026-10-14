// Mapa koja se uvećava i pomjera (kao mapa svijeta u mini igrama): točkić, dva prsta ili +/− za zum,
// prevlačenje za pomjeranje, tap za odgovor. Oznake (nazivi, igle, linije) su u koordinatama slike (0..1, y nadolje)
// i prate pogled, pa ostaju iste veličine i čitljive i kad je mapa uvećana.
using System;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class ZoomMap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public Action<Vector2> OnTap;          // tačka na slici (0..1, y nadolje)
        public float MaxZoom = 6;
        RawImage img;
        RectTransform rt, marks;
        float texAspect = 1, k = 1;
        double ox, oy;                         // gornji lijevi ugao pogleda u koordinatama slike
        Vector2 downPos;
        bool dragged, pinching, pressed, laidOut;
        float pinchDist;

        sealed class Mark { public RectTransform Rt; public double U, V; }
        sealed class Seg { public RectTransform Rt; public double U0, V0, U1, V1; public float Width; }
        readonly List<Mark> pins = new List<Mark>();
        readonly List<Seg> segs = new List<Seg>();

        public RectTransform Frame => (RectTransform)rt.parent;
        public RectTransform Marks => marks;

        public static ZoomMap Create(Transform parent, Texture tex, Color sea, Action<Vector2> onTap)
        {
            var frame = UIKit.Box(parent, sea, UIKit.Round, "Mapa", 0.8f);
            frame.gameObject.AddComponent<RectMask2D>();
            var view = UIKit.Rect(frame.transform, "Pogled").Stretch(5, 5, 5, 5);
            view.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f);   // prima dodire
            var z = view.gameObject.AddComponent<ZoomMap>();
            z.rt = view;
            z.OnTap = onTap;
            // slika zauzima samo dio pogleda koji je zaista mapa (ostatak je more okvira, bez razvlačenja ivica)
            z.img = UIKit.Raw(view, tex, "Slika");
            z.img.raycastTarget = false;
            z.texAspect = tex != null ? tex.width / (float)tex.height : 1;
            z.marks = UIKit.Rect(view, "Oznake").Stretch();

            var tools = UIKit.VStack(frame.transform, 10, "Alati", TextAnchor.UpperRight);
            ((RectTransform)tools.transform).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(80, 270));
            tools.childForceExpandHeight = false;
            UIKit.Button(tools.transform, "", Theme.Blue, () => z.ZoomAt(1.6f, new Vector2(0.5f, 0.5f)), 30).SetIcon("plus", 40).Size(80, 80);
            UIKit.Button(tools.transform, "", Theme.Blue, () => z.ZoomAt(1 / 1.6f, new Vector2(0.5f, 0.5f)), 30).SetIcon("minus", 40).Size(80, 80);
            UIKit.Button(tools.transform, "1×", Theme.InkSoft, () => z.ResetView(), 28).Size(80, 80);
            return z;
        }

        Vector2 ViewSize => rt.rect.size;
        // veličina pogleda u koordinatama slike kad k = 1 (cijela mapa stane)
        double W0 { get { var s = ViewSize; double a = s.x / Math.Max(1, s.y); return a >= texAspect ? a / texAspect : 1; } }
        double H0 { get { var s = ViewSize; double a = s.x / Math.Max(1, s.y); return a >= texAspect ? 1 : texAspect / a; } }
        double Wv => W0 / k;
        double Hv => H0 / k;

        public void ResetView()
        {
            k = 1;
            ox = (1 - Wv) / 2; oy = (1 - Hv) / 2;
            Clamp();
        }

        void Clamp()
        {
            k = Mathf.Clamp(k, 1, MaxZoom);
            ox = Wv >= 1 ? (1 - Wv) / 2 : Mathx.Clamp(ox, 0, 1 - Wv);
            oy = Hv >= 1 ? (1 - Hv) / 2 : Mathx.Clamp(oy, 0, 1 - Hv);
        }

        public void ZoomAt(float f, Vector2 at)
        {
            double u = ox + at.x * Wv, v = oy + at.y * Hv;
            k = Mathf.Clamp(k * f, 1, MaxZoom);
            ox = u - at.x * Wv; oy = v - at.y * Hv;
            Clamp();
        }

        // pogled koji obuhvata sve tačke (sa marginom)
        public void Focus(IList<Vector2> uv)
        {
            if (uv.Count == 0) return;
            double u0 = 1, u1 = 0, v0 = 1, v1 = 0;
            foreach (var p in uv) { u0 = Math.Min(u0, p.x); u1 = Math.Max(u1, p.x); v0 = Math.Min(v0, p.y); v1 = Math.Max(v1, p.y); }
            double du = Math.Max(0.08, (u1 - u0) * 1.7), dv = Math.Max(0.08, (v1 - v0) * 1.7);
            k = (float)Math.Max(1, Math.Min(MaxZoom, Math.Min(W0 / du, H0 / dv)));
            ox = (u0 + u1) / 2 - Wv / 2; oy = (v0 + v1) / 2 - Hv / 2;
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
            ox -= (b.x - a.x) * Wv; oy -= (b.y - a.y) * Hv;
            Clamp();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!pressed) return;
            pressed = false;
            if (dragged || pinching || Keys.TouchCount > 1) return;
            var p = Local01(e.position, e.pressEventCamera);
            if (p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
            var uv = new Vector2((float)(ox + p.x * Wv), (float)(oy + p.y * Hv));
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
            OnTap?.Invoke(uv);
        }

        public void OnScroll(PointerEventData e)
        {
            if (Mathf.Abs(e.scrollDelta.y) < 0.01f) return;
            ZoomAt(e.scrollDelta.y > 0 ? 1.18f : 1 / 1.18f, Local01(e.position, e.enterEventCamera));
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

        Vector2 ToView(double u, double v) => new Vector2((float)((u - ox) / Wv), (float)(1 - (v - oy) / Hv));

        void LateUpdate()
        {
            if (ViewSize.x < 10) return;
            if (!laidOut) { laidOut = true; ResetView(); }
            Clamp();   // i kad se pogled promijeni (npr. kartica se smanji), mapa ostaje u sredini
            double u0 = Math.Max(0, ox), u1 = Math.Min(1, ox + Wv), v0 = Math.Max(0, oy), v1 = Math.Min(1, oy + Hv);
            var irt = img.rectTransform;
            irt.anchorMin = new Vector2((float)((u0 - ox) / Wv), (float)(1 - (v1 - oy) / Hv));
            irt.anchorMax = new Vector2((float)((u1 - ox) / Wv), (float)(1 - (v0 - oy) / Hv));
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            img.uvRect = new Rect((float)u0, (float)(1 - v1), (float)(u1 - u0), (float)(v1 - v0));
            var size = ViewSize;
            foreach (var m in pins)
            {
                if (m.Rt == null) continue;
                var p = ToView(m.U, m.V);
                bool vis = p.x >= -0.05f && p.x <= 1.05f && p.y >= -0.05f && p.y <= 1.05f;
                m.Rt.gameObject.SetActive(vis);
                if (vis) { m.Rt.anchorMin = m.Rt.anchorMax = p; m.Rt.anchoredPosition = Vector2.zero; }
            }
            foreach (var sg in segs)
            {
                if (sg.Rt == null) continue;
                var a = ToView(sg.U0, sg.V0); var b = ToView(sg.U1, sg.V1);
                sg.Rt.anchorMin = sg.Rt.anchorMax = a;
                sg.Rt.anchoredPosition = Vector2.zero;
                var d = new Vector2((b.x - a.x) * size.x, (b.y - a.y) * size.y);
                sg.Rt.sizeDelta = new Vector2(d.magnitude, sg.Width);
                sg.Rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        // ---- oznake ----
        public RectTransform AddMark(RectTransform mark, double u, double v)
        {
            mark.SetParent(marks, false);
            pins.Add(new Mark { Rt = mark, U = u, V = v });
            return mark;
        }

        public void AddLine(double u0, double v0, double u1, double v1, Color c, float width, bool dashed)
        {
            int n = dashed ? 14 : 1;
            for (int s = 0; s < n; s += dashed ? 2 : 1)
            {
                double t0 = s / (double)n, t1 = (s + 1) / (double)n;
                var img = UIKit.Box(marks, c, UIKit.Pill, "Linija", 1f);
                img.raycastTarget = false;
                img.rectTransform.pivot = new Vector2(0, 0.5f);
                segs.Add(new Seg { Rt = img.rectTransform, U0 = u0 + (u1 - u0) * t0, V0 = v0 + (v1 - v0) * t0, U1 = u0 + (u1 - u0) * t1, V1 = v0 + (v1 - v0) * t1, Width = width });
            }
        }

        public void ClearMarks(Func<RectTransform, bool> which)
        {
            foreach (var m in pins.FindAll(x => x.Rt != null && which(x.Rt))) Destroy(m.Rt.gameObject);
            pins.RemoveAll(x => x.Rt == null || which(x.Rt));
            foreach (var sg in segs) if (sg.Rt != null) Destroy(sg.Rt.gameObject);
            segs.Clear();
        }
    }
}
