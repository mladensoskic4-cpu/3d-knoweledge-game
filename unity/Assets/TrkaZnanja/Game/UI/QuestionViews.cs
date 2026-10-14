// Izgled i ponašanje svakog tipa pitanja na ekranu (višestruki izbor, mutna zastava, melodija, procjena broja,
// poredaj, spoji parove, tapni na mapi). Bodovanje je u Core/QuizTypes, ovdje je samo interakcija.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public abstract class QuestionView
    {
        protected Question Q;
        protected RectTransform Body;
        protected Rng Rng;
        protected MonoBehaviour Host;
        Action<QuizResult> submit;
        protected bool Locked;

        public void Mount(Question q, RectTransform body, Rng rng, MonoBehaviour host, Action<QuizResult> onSubmit)
        {
            Q = q; Body = body; Rng = rng; Host = host; submit = onSubmit;
            Build();
        }

        protected void Submit(QuizResult r) { if (!Locked) submit(r); }
        protected abstract void Build();
        public virtual void Lock() => Locked = true;
        public virtual QuizResult Timeout() => new QuizResult { Accuracy = 0 };
        public virtual void Reveal(QuizResult r) { }
        public virtual bool CanFifty => false;
        public virtual void Fifty() { }
        public virtual void OnKey(int number) { }
        public virtual void OnConfirmKey() { }
        public virtual void Tick(float elapsed, float limit) { }
        public virtual void Dispose() { }

        public static QuestionView For(string tip)
        {
            switch (tip)
            {
                case "izbor": return new ChoiceView();
                case "mutna": return new ChoiceView { Blur = true };
                case "melodija": return new ChoiceView { Melody = true };
                case "procjena": return new EstimateView();
                case "poredaj": return new OrderView();
                case "spoji": return new MatchView();
                case "mapa": return new MapTapView();
                default: return null;
            }
        }

        protected static GameButton Confirm(Transform parent, Action click)
        {
            var b = UIKit.Button(parent, "Potvrdi", Theme.Go, click, 34);
            b.Size(260, 92);
            return b;
        }
    }

    // ---------- višestruki izbor (i mutna slika, i melodija) ----------
    public sealed class ChoiceView : QuestionView
    {
        public bool Blur, Melody;
        readonly List<GameButton> buttons = new List<GameButton>();
        List<string> opts;
        RawImage flag;
        Material blurMat;
        Text hint;
        RectTransform eq;
        static readonly Color[] Colors = { Col.Hex("#2b7de9"), Col.Hex("#9b4bd6"), Col.Hex("#f08a1c"), Col.Hex("#0f9a8a") };

        protected override void Build()
        {
            opts = Rng.Shuffle(new List<string>(Q.Opcije));
            var col = UIKit.VStack(Body, 18, "Izbor", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;

            if (Blur)
            {
                var frame = UIKit.Box(col.transform, Theme.PaperSoft, UIKit.Round, "Okvir", 0.8f);
                frame.Size(-1, 260, 1, 1);
                flag = UIKit.Raw(frame.transform, GameData.Flag(Q.Zastava), "Zastava");
                flag.rectTransform.Center(new Vector2(390, 240));
                var arf = flag.gameObject.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = flag.texture != null ? flag.texture.width / (float)flag.texture.height : 1.5f;
                flag.rectTransform.Stretch(16, 16, 16, 16);
                blurMat = new Material(Mats.UIBlur);
                blurMat.SetFloat("_Blur", 7);   // zastave su 480 px, pa jedan nivo više
                blurMat.SetFloat("_Desat", 0.7f);
                flag.material = blurMat;
            }
            if (Melody)
            {
                var stage = UIKit.Box(col.transform, Col.Hex("#1f2b3a"), UIKit.Round, "Melodija", 0.8f);
                stage.Size(-1, 170, 1);
                eq = UIKit.HStack(stage.transform, 12, "Ekvilajzer", TextAnchor.MiddleCenter).GetComponent<RectTransform>();
                eq.Stretch(40, 30, 380, 30);
                for (int i = 0; i < 9; i++)
                {
                    var b = UIKit.Box(eq, Color.Lerp(Theme.Gold, Theme.Red, i / 8f), UIKit.Pill, "Stub", 1f);
                    b.Size(26, 40);
                }
                hint = UIKit.Label(stage.transform, "Melodija svira dok ne odgovoriš.", 26, Color.white, TextAnchor.MiddleRight);
                hint.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 40), new Vector2(340, 60));
                var replay = UIKit.Button(stage.transform, "Pusti ponovo", Theme.Purple, () => { if (!Locked) Play(); }, 28);
                ((RectTransform)replay.transform).Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, -30), new Vector2(300, 78));
                Play();
            }

            var grid = UIKit.Grid(col.transform, new Vector2(520, 116), new Vector2(22, 20), 2, "Odgovori");
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.Size(-1, 252);
            for (int i = 0; i < opts.Count; i++)
            {
                var o = opts[i];
                var b = UIKit.Button(grid.transform, $"{i + 1}   {o}", Colors[i % Colors.Length], () => Pick(o), 34);
                buttons.Add(b);
            }
        }

        void Play()
        {
            AudioKit.I?.PlayMelody(Q.Note, Q.Tempo);
        }

        void Pick(string o)
        {
            if (Locked) return;
            Submit(new QuizResult { Accuracy = QuizTypes.ScoreChoice(Q, o), Chosen = o });
        }

        public override void OnKey(int n) { if (n >= 1 && n <= buttons.Count && buttons[n - 1].Interactable) buttons[n - 1].Click(); }

        public override void Tick(float elapsed, float limit)
        {
            if (blurMat != null && !Locked)
            {
                var k = Mathf.Clamp01(elapsed / (limit * 0.85f));
                blurMat.SetFloat("_Blur", (1 - k) * 7);
                blurMat.SetFloat("_Desat", (1 - k) * 0.7f);
                flag.rectTransform.localScale = Vector3.one * (1.15f - 0.15f * k);
            }
            if (eq != null)
                for (int i = 0; i < eq.childCount; i++)
                {
                    var le = eq.GetChild(i).GetComponent<LayoutElement>();
                    var on = AudioKit.I != null && AudioKit.I.MelodyPlaying;
                    le.preferredHeight = le.minHeight = on ? 30 + Mathf.Abs(Mathf.Sin(Time.time * (3 + i * 0.7f) + i)) * 80 : 16;
                }
        }

        public override void Lock()
        {
            base.Lock();
            foreach (var b in buttons) b.Interactable = false;
            if (Melody) Host.StartCoroutine(StopLater());
        }

        IEnumerator StopLater()
        {
            yield return Tw.Wait(1.2f, true);
            AudioKit.I?.StopMelody();
        }

        public override void Dispose() { if (Melody) AudioKit.I?.StopMelody(); }

        public override bool CanFifty => true;
        public override void Fifty()
        {
            var wrong = Enumerable.Range(0, opts.Count).Where(i => opts[i] != Q.TacnoText && buttons[i].Interactable).ToList();
            Rng.Shuffle(wrong);
            foreach (var i in wrong.Take(2)) { buttons[i].Interactable = false; buttons[i].Label.color = new Color(1, 1, 1, 0.4f); }
        }

        public override void Reveal(QuizResult r)
        {
            for (int i = 0; i < opts.Count; i++)
            {
                if (opts[i] == Q.TacnoText) buttons[i].Face.color = Theme.Right;
                else if (opts[i] == r.Chosen) buttons[i].Face.color = Theme.Wrong;
            }
            if (blurMat != null) { blurMat.SetFloat("_Blur", 0); blurMat.SetFloat("_Desat", 0); flag.rectTransform.localScale = Vector3.one; }
            if (hint != null && !string.IsNullOrEmpty(Q.Djelo)) hint.text = Lang.L(Q.Djelo);
        }
    }

    // ---------- procjena broja ----------
    public sealed class EstimateView : QuestionView
    {
        double value;
        bool moved;
        Text readout;
        Slider slider;
        InputField input;
        GameButton ok;
        RectTransform marks;

        protected override void Build()
        {
            value = QuizTypes.SnapEstimate(Q, (Q.Min + Q.Max) / 2);
            var col = UIKit.VStack(Body, 22, "Procjena", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch(40, 10, 40, 0);

            var row = UIKit.HStack(col.transform, 14, "Broj", TextAnchor.MiddleCenter);
            row.Size(-1, 130);
            readout = UIKit.Label(row.transform, QuizTypes.FormatEstimate(Q, value), 96, Theme.Ink, TextAnchor.MiddleRight, true);
            readout.Size(560, 130);
            var unit = UIKit.Label(row.transform, QuizTypes.EstimateUnit(Q), 40, Theme.InkSoft, TextAnchor.MiddleLeft);
            unit.Size(320, 130);

            // klizač
            var sl = UIKit.Rect(col.transform, "Klizač");
            sl.Size(-1, 90);
            var bg = UIKit.Box(sl, Theme.Line, UIKit.Pill, "Pozadina", 1f);
            bg.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 26));
            var fillArea = UIKit.Rect(sl, "Popunjeno").Stretch(0, 32, 0, 32);
            var fill = UIKit.Box(fillArea, Theme.Primary, UIKit.Pill, "Boja", 1f);
            fill.rectTransform.Stretch();
            var handleArea = UIKit.Rect(sl, "Ručica").Stretch(30, 0, 30, 0);
            var handle = UIKit.Box(handleArea, Color.white, UIKit.Circle, "Krug");
            handle.rectTransform.sizeDelta = new Vector2(76, 76);
            var o = handle.gameObject.AddComponent<Outline>();
            o.effectColor = Theme.Primary; o.effectDistance = new Vector2(4, -4);
            slider = sl.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = (float)Q.Min; slider.maxValue = (float)Q.Max;
            slider.value = (float)value;
            slider.onValueChanged.AddListener(v => Set(v, false));
            marks = UIKit.Rect(sl, "Oznake").Stretch(30, 0, 30, 0);

            var scale = UIKit.HStack(col.transform, 0, "Skala");
            scale.Size(-1, 40);
            scale.childForceExpandWidth = true;
            UIKit.Label(scale.transform, QuizTypes.FormatEstimate(Q, Q.Min), 26, Theme.InkSoft, TextAnchor.MiddleLeft);
            UIKit.Label(scale.transform, QuizTypes.FormatEstimate(Q, Q.Max), 26, Theme.InkSoft, TextAnchor.MiddleRight);

            var bar = UIKit.HStack(col.transform, 16, "Kontrole", TextAnchor.MiddleCenter);
            bar.Size(-1, 100);
            UIKit.Button(bar.transform, "−", Theme.Blue, () => Set(value - Q.Korak, true), 52).Size(110, 92);
            UIKit.Button(bar.transform, "+", Theme.Blue, () => Set(value + Q.Korak, true), 52).Size(110, 92);
            UIKit.Label(bar.transform, "ili upiši:", 28, Theme.InkSoft, TextAnchor.MiddleRight).Size(160, 92);
            input = UIKit.Input(bar.transform, Fmt.Num(value, Fmt.DecimalsOf(Q.Korak), false), "", 12, s =>
            {
                if (double.TryParse(s.Replace(",", ".").Replace(" ", ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) Set(v, true);
            });
            input.contentType = InputField.ContentType.DecimalNumber;
            input.Size(230, 92);
            ok = Confirm(bar.transform, () => { if (!Locked) Submit(Evaluate()); });
        }

        void Set(double v, bool updateSlider)
        {
            if (Locked) return;
            value = QuizTypes.SnapEstimate(Q, v);
            moved = true;
            readout.text = QuizTypes.FormatEstimate(Q, value);
            if (updateSlider) slider.SetValueWithoutNotify((float)value);
            input.SetTextWithoutNotify(Fmt.Num(value, Fmt.DecimalsOf(Q.Korak), false));
        }

        QuizResult Evaluate() => new QuizResult { Accuracy = QuizTypes.ScoreEstimate(Q, value), Value = value };
        public override QuizResult Timeout() => moved ? Evaluate() : new QuizResult { Accuracy = 0 };
        public override void OnConfirmKey() => ok.Click();

        public override void Lock()
        {
            base.Lock();
            slider.interactable = false; input.interactable = false; ok.Interactable = false;
        }

        public override void Reveal(QuizResult r)
        {
            Mark(Q.TacnoBroj, Theme.Right, QuizTypes.FormatEstimate(Q, Q.TacnoBroj));
            if (r.Value.HasValue) Mark(r.Value.Value, Theme.Wrong, null);
            ok.gameObject.SetActive(false);
        }

        void Mark(double v, Color c, string label)
        {
            float k = (float)((v - Q.Min) / (Q.Max - Q.Min));
            var m = UIKit.Box(marks, c, UIKit.Pill, "Oznaka", 1f);
            m.rectTransform.Anchor(new Vector2(k, 0.5f), new Vector2(k, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 90));
            if (label != null)
            {
                var chip = UIKit.Chip(marks, label, c, 26);
                chip.rectTransform.Anchor(new Vector2(k, 1), new Vector2(k, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(200, 48));
            }
        }
    }

    // ---------- poredaj ----------
    public sealed class OrderView : QuestionView
    {
        string[] placed;
        readonly Dictionary<string, GameButton> chips = new Dictionary<string, GameButton>();
        readonly List<(GameButton btn, Text badge)> slots = new List<(GameButton, Text)>();
        string top, bottom;
        GameButton ok, clear;
        RectTransform pool;

        protected override void Build()
        {
            var correct = Q.Redoslijed;
            int n = correct.Count;
            var start = Rng.Shuffle(new List<string>(correct));
            for (int k = 0; k < 5 && start.SequenceEqual(correct); k++) Rng.Shuffle(start);
            var ends = QuizTypes.EndLabels(Q.Pitanje, Lang.Mode == Lang.En);
            top = ends[0]; bottom = ends[1];
            placed = new string[n];

            var row = UIKit.HStack(Body, 30, "Poredaj", TextAnchor.UpperCenter);
            ((RectTransform)row.transform).Stretch(10, 0, 10, 0);
            row.childForceExpandHeight = true;

            var left = UIKit.VStack(row.transform, 12, "Mjesta");
            left.Size(-1, -1, 1.3f);
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var b = UIKit.Button(left.transform, "", Theme.Blue, () => Unplace(idx), 30);
                b.Size(-1, 84);
                var badge = UIKit.Label(b.Face.transform, "", 22, Color.white, TextAnchor.MiddleRight, true);
                badge.rectTransform.Stretch(16, 4, 20, 4);
                slots.Add((b, badge));
            }

            var right = UIKit.VStack(row.transform, 14, "Stavke", TextAnchor.UpperCenter);
            right.Size(-1, -1, 1);
            UIKit.Label(right.transform, "Tapni stavke redom: prva ide na mjesto 1.", 26, Theme.InkSoft, TextAnchor.MiddleCenter).Size(-1, 70);
            pool = UIKit.VStack(right.transform, 12, "Bazen").GetComponent<RectTransform>();
            foreach (var v in start)
            {
                var item = v;
                var c = UIKit.Button(pool, item, Theme.Primary, () => Place(item), 30);
                c.Size(-1, 84);
                chips[item] = c;
            }
            var bar = UIKit.HStack(right.transform, 14, "Dugmad", TextAnchor.MiddleCenter);
            bar.Size(-1, 100);
            clear = UIKit.Button(bar.transform, "Poništi", Theme.InkSoft, Reset, 30);
            clear.Size(220, 92);
            ok = Confirm(bar.transform, () => { if (!Locked && !placed.Contains(null)) Submit(Evaluate(placed.ToList())); });
            Render();
        }

        string TagOf(int i) => i == 0 ? "(" + top + ")" : i == placed.Length - 1 ? "(" + bottom + ")" : "";

        void Place(string v)
        {
            if (Locked) return;
            int i = Array.IndexOf(placed, null);
            if (i < 0) return;
            placed[i] = v;
            chips[v].gameObject.SetActive(false);
            var left = chips.Where(c => c.Value.gameObject.activeSelf).ToList();
            if (left.Count == 1) { placed[Array.IndexOf(placed, null)] = left[0].Key; left[0].Value.gameObject.SetActive(false); }
            Render();
        }

        void Unplace(int i)
        {
            if (Locked || placed[i] == null) return;
            chips[placed[i]].gameObject.SetActive(true);
            placed[i] = null;
            Render();
        }

        void Reset()
        {
            if (Locked) return;
            for (int i = 0; i < placed.Length; i++) placed[i] = null;
            foreach (var c in chips.Values) c.gameObject.SetActive(true);
            Render();
        }

        void Render()
        {
            int next = Array.IndexOf(placed, null);
            for (int i = 0; i < slots.Count; i++)
            {
                var (b, badge) = slots[i];
                var v = placed[i];
                b.SetText(v != null ? $"{i + 1}.  {v}" : $"{i + 1}.  {(TagOf(i) != "" ? TagOf(i) : "…")}");
                b.SetSelected(v != null || i == next);
                badge.text = v != null ? Lang.L(TagOf(i)) : "";
            }
            ok.Interactable = next < 0;
        }

        QuizResult Evaluate(List<string> order) => new QuizResult { Accuracy = QuizTypes.ScoreOrder(order, Q.Redoslijed), Order = order };

        List<string> Current()
        {
            var list = placed.Where(x => x != null).ToList();
            list.AddRange(chips.Where(c => c.Value.gameObject.activeSelf).Select(c => c.Key));
            return list;
        }

        public override QuizResult Timeout() => placed.Any(x => x != null) ? Evaluate(Current()) : new QuizResult { Accuracy = 0 };
        public override void OnConfirmKey() => ok.Click();

        public override void Lock()
        {
            base.Lock();
            ok.Interactable = false; clear.Interactable = false;
            foreach (var c in chips.Values) c.Interactable = false;
        }

        public override void Reveal(QuizResult r)
        {
            var order = Current();
            for (int i = 0; i < placed.Length && i < order.Count; i++) placed[i] = order[i];
            foreach (var c in chips.Values) c.gameObject.SetActive(false);
            for (int i = 0; i < slots.Count; i++)
            {
                var (b, badge) = slots[i];
                int ci = Q.Redoslijed.IndexOf(placed[i]);
                bool right = ci == i;
                b.SetText($"{i + 1}.  {placed[i]}");
                b.SetSelected(true);
                b.Face.color = right ? Theme.Right : Theme.Wrong;
                var lab = Q.Oznake != null && ci >= 0 && ci < Q.Oznake.Count ? " · " + Q.Oznake[ci] : "";
                badge.text = Lang.L(right ? "tačno" + lab : $"treba {ci + 1}." + lab);
            }
            ok.gameObject.SetActive(false); clear.gameObject.SetActive(false);
        }
    }

    // ---------- spoji parove ----------
    public sealed class MatchView : QuestionView
    {
        List<string> left, right;
        readonly List<GameButton> lb = new List<GameButton>(), rb = new List<GameButton>();
        readonly Dictionary<int, int> links = new Dictionary<int, int>();
        int activeL = -1, activeR = -1;
        RectTransform lines;
        GameButton ok;
        bool revealed;
        static readonly Color[] LineColors = { Col.Hex("#1f86c2"), Col.Hex("#9b4bd6"), Col.Hex("#d9822b"), Col.Hex("#2f9e5b"), Col.Hex("#c8102e"), Col.Hex("#3d5a80") };

        protected override void Build()
        {
            left = Rng.Shuffle(Q.Parovi.Select(p => p.Key).ToList());
            right = Rng.Shuffle(Q.Parovi.Select(p => p.Value).ToList());
            var col = UIKit.VStack(Body, 12, "Spoji", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch(10, 0, 10, 0);
            var area = UIKit.Rect(col.transform, "Kolone");
            area.Size(-1, -1, 1, 1);
            lines = UIKit.Rect(area, "Linije").Stretch();
            var row = UIKit.HStack(area, 220, "Red", TextAnchor.UpperCenter);
            ((RectTransform)row.transform).Stretch();
            row.childForceExpandWidth = true;
            var lc = UIKit.VStack(row.transform, 14, "Lijevo");
            var rc = UIKit.VStack(row.transform, 14, "Desno");
            for (int i = 0; i < left.Count; i++)
            {
                int k = i;
                var b = UIKit.Button(lc.transform, left[i], Theme.Blue, () => PickL(k), 28);
                b.Size(-1, 80); lb.Add(b);
                var c = UIKit.Button(rc.transform, right[i], Theme.Purple, () => PickR(k), 28);
                c.Size(-1, 80); rb.Add(c);
            }
            var bar = UIKit.HStack(col.transform, 16, "Dugmad", TextAnchor.MiddleRight);
            bar.Size(-1, 100);
            UIKit.Label(bar.transform, "Tapni pojam lijevo, pa njegov par desno.", 26, Theme.InkSoft, TextAnchor.MiddleLeft).Size(-1, 92, 1);
            ok = Confirm(bar.transform, () => { if (!Locked && links.Count > 0) Submit(Evaluate()); });
            ok.Interactable = false;
            Render();
        }

        void PickL(int i) { if (Locked) return; activeL = i; TryLink(); Render(); }
        void PickR(int i) { if (Locked) return; activeR = i; TryLink(); Render(); }

        void TryLink()
        {
            if (activeL < 0 || activeR < 0) return;
            foreach (var k in links.Where(x => x.Value == activeR).Select(x => x.Key).ToList()) links.Remove(k);
            links[activeL] = activeR;
            activeL = activeR = -1;
            ok.Interactable = links.Count > 0;
        }

        void Render()
        {
            for (int i = 0; i < lb.Count; i++)
            {
                bool linked = links.ContainsKey(i);
                lb[i].SetSelected(linked || i == activeL);
                if (linked && !revealed) lb[i].Face.color = LineColors[i % LineColors.Length];
            }
            for (int j = 0; j < rb.Count; j++)
            {
                var owner = links.FirstOrDefault(x => x.Value == j);
                bool linked = links.ContainsValue(j);
                rb[j].SetSelected(linked || j == activeR);
                if (linked && !revealed) rb[j].Face.color = LineColors[owner.Key % LineColors.Length];
            }
            Host.StartCoroutine(DrawLinesNextFrame());
        }

        IEnumerator DrawLinesNextFrame()
        {
            yield return null;   // sačekaj raspored (layout)
            if (lines == null) yield break;
            UIKit.Clear(lines);
            foreach (var kv in links)
            {
                Color c = revealed ? (Q.Parovi.Any(p => p.Key == left[kv.Key] && p.Value == right[kv.Value]) ? Theme.Right : Theme.Wrong) : LineColors[kv.Key % LineColors.Length];
                Line(lb[kv.Key], rb[kv.Value], c, false);
            }
            if (revealed)
                for (int i = 0; i < left.Count; i++)
                {
                    var want = Q.Parovi.First(p => p.Key == left[i]).Value;
                    int j = right.IndexOf(want);
                    if (!links.TryGetValue(i, out var got) || got != j) Line(lb[i], rb[j], Theme.Right, true);
                }
        }

        void Line(GameButton a, GameButton b, Color c, bool dashed)
        {
            var ra = (RectTransform)a.transform; var rbt = (RectTransform)b.transform;
            Vector3 pa = ra.TransformPoint(new Vector3(ra.rect.xMax, ra.rect.center.y, 0));
            Vector3 pb = rbt.TransformPoint(new Vector3(rbt.rect.xMin, rbt.rect.center.y, 0));
            Vector2 la = lines.InverseTransformPoint(pa), lbp = lines.InverseTransformPoint(pb);
            var d = lbp - la;
            int segs = dashed ? Mathf.Max(3, (int)(d.magnitude / 30)) : 1;
            for (int s = 0; s < segs; s++)
            {
                if (dashed && s % 2 == 1) continue;
                var p0 = la + d * (s / (float)segs);
                var p1 = la + d * ((s + 1) / (float)segs);
                var img = UIKit.Box(lines, c, UIKit.Pill, "Linija", 1f);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0, 0.5f);
                rt.localPosition = p0;
                rt.sizeDelta = new Vector2((p1 - p0).magnitude, 9);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        QuizResult Evaluate()
        {
            var map = links.ToDictionary(kv => left[kv.Key], kv => right[kv.Value]);
            var (acc, good) = QuizTypes.ScoreMatch(Q, map);
            return new QuizResult { Accuracy = acc, Good = good, Total = Q.Parovi.Count };
        }

        public override QuizResult Timeout() => links.Count > 0 ? Evaluate() : new QuizResult { Accuracy = 0, Total = Q.Parovi.Count };
        public override void OnConfirmKey() => ok.Click();

        public override void Lock()
        {
            base.Lock();
            ok.Interactable = false;
            foreach (var b in lb.Concat(rb)) b.Interactable = false;
        }

        public override void Reveal(QuizResult r)
        {
            revealed = true;
            for (int i = 0; i < left.Count; i++)
            {
                var want = Q.Parovi.First(p => p.Key == left[i]).Value;
                bool good = links.TryGetValue(i, out var j) && right[j] == want;
                lb[i].SetSelected(true);
                lb[i].Face.color = good ? Theme.Right : Theme.Wrong;
            }
            for (int j = 0; j < right.Count; j++) { rb[j].SetSelected(true); rb[j].Face.color = Theme.InkSoft; }
            ok.gameObject.SetActive(false);
            Host.StartCoroutine(DrawLinesNextFrame());
        }
    }

    // ---------- tapni na mapi ----------
    public sealed class MapTapView : QuestionView
    {
        GeoMap cfg;
        Geo.Projector proj;
        RawImage map;
        RectTransform pins;
        Vec2d? pick;
        GameButton ok;
        Text hint;
        int texW, texH;

        protected override void Build()
        {
            cfg = GeoMaps.Get(Q.Mapa);
            var tex = MapPainter.Get(cfg);
            texW = tex.width; texH = tex.height;
            proj = new Geo.Projector(cfg, texW, texH, 0);

            var col = UIKit.VStack(Body, 10, "Mapa", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            var holder = UIKit.Rect(col.transform, "Okvir mape");
            holder.Size(-1, -1, 1, 1);
            map = UIKit.Raw(holder, tex, "Mapa");
            var arf = map.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = texW / (float)texH;
            var tap = map.gameObject.AddComponent<TapArea>();
            tap.OnTap = OnTap;
            pins = UIKit.Rect(map.rectTransform, "Oznake").Stretch();
            foreach (var lab in cfg.Labels)
            {
                var t = UIKit.Label(pins, lab.Text.ToUpperInvariant(), 20, Col.Hex("#3b5361"), TextAnchor.MiddleCenter, true);
                Place(t.rectTransform, lab.Lon, lab.Lat, new Vector2(260, 30));
            }

            var bar = UIKit.HStack(col.transform, 16, "Dugmad", TextAnchor.MiddleRight);
            bar.Size(-1, 100);
            hint = UIKit.Label(bar.transform, "Tapni na mapu, pa potvrdi.", 28, Theme.InkSoft, TextAnchor.MiddleLeft);
            hint.Size(-1, 92, 1);
            ok = Confirm(bar.transform, () => { if (!Locked && pick.HasValue) Submit(Evaluate()); });
            ok.Interactable = false;
        }

        void Place(RectTransform rt, double lon, double lat, Vector2 size)
        {
            var p = proj.ToXY(lon, lat);
            rt.anchorMin = rt.anchorMax = new Vector2((float)(p.X / texW), 1 - (float)(p.Y / texH));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        void OnTap(Vector2 uv)
        {
            if (Locked) return;
            pick = proj.ToLonLat(uv.x * texW, (1 - uv.y) * texH);
            ok.Interactable = true;
            hint.text = Lang.L("Možeš tapnuti ponovo da pomjeriš oznaku.");
            DrawPins(null);
        }

        RectTransform Pin(double lon, double lat, Color c, string label)
        {
            var rt = UIKit.Rect(pins, "Igla");
            Place(rt, lon, lat, new Vector2(46, 46));
            rt.pivot = new Vector2(0.5f, 0);
            var head = UIKit.Box(rt, c, UIKit.Circle, "Glava");
            head.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(40, 40));
            var o = head.gameObject.AddComponent<Outline>(); o.effectColor = Color.white; o.effectDistance = new Vector2(3, -3);
            var stick = UIKit.Box(rt, c, UIKit.Pill, "Štap", 1f);
            stick.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(8, 20));
            if (label != null)
            {
                var chip = UIKit.Chip(rt, label, c, 24);
                chip.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(300, 46));
            }
            return rt;
        }

        void DrawPins(QuizResult r)
        {
            for (int i = pins.childCount - 1; i >= 0; i--)
                if (pins.GetChild(i).name == "Igla" || pins.GetChild(i).name == "Linija") UnityEngine.Object.Destroy(pins.GetChild(i).gameObject);
            if (r != null && pick.HasValue)
            {
                // isprekidana linija od tvog tapa do tačnog mjesta
                var a = proj.ToXY(pick.Value.X, pick.Value.Y); var b = proj.ToXY(Q.Tacka.Lon, Q.Tacka.Lat);
                for (int s = 0; s < 14; s += 2)
                {
                    double t0 = s / 14.0, t1 = (s + 1) / 14.0;
                    var seg = UIKit.Box(pins, Theme.Ink, UIKit.Pill, "Linija", 1f);
                    var rt = seg.rectTransform;
                    double x0 = a.X + (b.X - a.X) * t0, y0 = a.Y + (b.Y - a.Y) * t0, x1 = a.X + (b.X - a.X) * t1, y1 = a.Y + (b.Y - a.Y) * t1;
                    rt.anchorMin = rt.anchorMax = new Vector2((float)(x0 / texW), 1 - (float)(y0 / texH));
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    var size = map.rectTransform.rect.size;
                    var d = new Vector2((float)((x1 - x0) / texW * size.x), (float)(-(y1 - y0) / texH * size.y));
                    rt.sizeDelta = new Vector2(d.magnitude, 6);
                    rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
            }
            if (pick.HasValue) Pin(pick.Value.X, pick.Value.Y, Theme.Red, null);
            if (r != null) Pin(Q.Tacka.Lon, Q.Tacka.Lat, Theme.Right, Q.Tacka.Naziv);
        }

        QuizResult Evaluate()
        {
            var km = Geo.Haversine(pick.Value.X, pick.Value.Y, Q.Tacka.Lon, Q.Tacka.Lat);
            return new QuizResult { Accuracy = QuizTypes.ScoreMap(Q, km), Km = km };
        }

        public override QuizResult Timeout() => pick.HasValue ? Evaluate() : new QuizResult { Accuracy = 0 };
        public override void OnConfirmKey() => ok.Click();
        public override void Lock() { base.Lock(); ok.Interactable = false; }

        public override void Reveal(QuizResult r)
        {
            hint.text = pick.HasValue ? Lang.L($"Promašaj: {Fmt.Num(Math.Round(r.Km ?? 0))} km") : Lang.L("Nisi označio lokaciju.");
            ok.gameObject.SetActive(false);
            DrawPins(r);
        }
    }

    // Prima tap na sliku i javlja tačku u koordinatama 0..1 (za mape).
    public sealed class TapArea : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> OnTap;
        public void OnPointerClick(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
            var r = rt.rect;
            OnTap?.Invoke(new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height));
        }
    }
}
