// Korijen UI-ja: platno (Canvas) prilagođeno telefonu (polegnuto), sigurna zona (notch), slojevi
// (natpisi iznad 3D svijeta, HUD, ekrani, poruke) i HUD trke: traka do cilja (mini-mapa) i poredak.
using System.Collections;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class UIRoot : MonoBehaviour
    {
        public static UIRoot I { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform Safe { get; private set; }
        public RectTransform Labels { get; private set; }
        public RectTransform Hud { get; private set; }
        public RectTransform Screen { get; private set; }
        public RectTransform Overlay { get; private set; }
        Rect lastSafe;

        public static UIRoot Create(Transform parent)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<UIRoot>();
            I = ui;
            ui.Canvas = go.AddComponent<Canvas>();
            ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ui.Canvas.sortingOrder = 10;
            // Platno je uvijek bar 1920x1080 jedinica (Expand): na širokim telefonima (20:9) dobija širinu,
            // na tabletima (4:3) visinu, pa raspored napravljen za 1920x1080 nikad nema manje mjesta.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();

            ui.Safe = UIKit.Rect(go.transform, "Sigurna zona").Stretch();
            ui.Labels = UIKit.Rect(ui.Safe, "Natpisi").Stretch();
            ui.Hud = UIKit.Rect(ui.Safe, "HUD").Stretch();
            ui.Screen = UIKit.Rect(ui.Safe, "Ekran").Stretch();
            ui.Overlay = UIKit.Rect(ui.Safe, "Poruke").Stretch();
            EnsureEventSystem(parent);
            ui.ApplySafeArea();
            return ui;
        }

        // EventSystem: novi Input System ako je uključen, inače stari ulaz.
        static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.transform.SetParent(parent, false);
            es.AddComponent<EventSystem>();
            var newInput = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (newInput != null) { es.AddComponent(newInput); return; }
#endif
            es.AddComponent<StandaloneInputModule>();
            _ = newInput;
        }

        public static readonly Vector2 Reference = new Vector2(1920, 1080);

        // Veličina platna (u UI jedinicama) za ekran od px piksela — isto što radi CanvasScaler (Expand).
        public static Vector2 CanvasSizeFor(Vector2 px)
        {
            float s = Mathf.Min(px.x / Reference.x, px.y / Reference.y);
            return px / s;
        }

        // Za testove: platno se prebaci u 3D prostor sa veličinom drugog ekrana (telefon, tablet),
        // pa se raspored provjeri i snimi bez obzira na prozor editora.
        bool emulating;
        public static readonly Vector3 EmulationOrigin = new Vector3(0, -5000, 0);

        public void Emulate(Vector2 px, Rect safePx)
        {
            emulating = true;
            Canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = CanvasSizeFor(px);
            rt.position = EmulationOrigin;
            rt.rotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            Safe.anchorMin = new Vector2(safePx.xMin / px.x, safePx.yMin / px.y);
            Safe.anchorMax = new Vector2(safePx.xMax / px.x, safePx.yMax / px.y);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
        }

        public void StopEmulating()
        {
            emulating = false;
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ApplySafeArea();
        }

        void ApplySafeArea()
        {
            var sa = UnityEngine.Screen.safeArea;
            lastSafe = sa;
            float w = UnityEngine.Screen.width, h = UnityEngine.Screen.height;
            if (w <= 0 || h <= 0) return;
            Safe.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            Safe.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (!emulating && UnityEngine.Screen.safeArea != lastSafe) ApplySafeArea();
        }

        public void ClearScreen() => UIKit.Clear(Screen);

        // Tamna pozadina preko svijeta (za menije).
        public Image Dim(Transform parent, float alpha = 0.45f)
        {
            var d = UIKit.Box(parent, new Color(0.04f, 0.08f, 0.14f, alpha), UIKit.Round, "Zatamnjenje", 1f);
            d.sprite = null;
            d.type = Image.Type.Simple;
            d.rectTransform.Stretch(-200, -200, -200, -200);
            return d;
        }

        // Kratka poruka na vrhu ekrana.
        public void Toast(string text, Color color, float seconds = 2.6f)
        {
            StartCoroutine(ToastCo(text, color, seconds));
        }

        IEnumerator ToastCo(string text, Color color, float seconds)
        {
            var box = UIKit.Box(Overlay, Theme.Paper, UIKit.Pill, "Poruka", 1f);
            var rt = box.rectTransform;
            rt.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(980, 92));
            var dot = UIKit.Box(box.transform, color, UIKit.Circle, "Boja");
            dot.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(34, 34));
            var t = UIKit.Label(box.transform, text, 32, Theme.Ink, TextAnchor.MiddleLeft);
            t.rectTransform.Stretch(80, 6, 30, 6);
            var cg = box.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            yield return Tw.Run(0.25f, k => { cg.alpha = k; rt.anchoredPosition = new Vector2(0, 40 - 70 * Tw.EaseOutBack(k)); }, true);
            yield return Tw.Wait(seconds, true);
            yield return Tw.Run(0.3f, k => cg.alpha = 1 - k, true);
            Destroy(box.gameObject);
        }
    }

    // Natpisi gradova, stanica i cilja: prate 3D tačke na ekranu (oštri na svakoj rezoluciji).
    public sealed class WorldLabels : MonoBehaviour
    {
        sealed class Item { public RectTransform Rt; public Vector3 Pos; public CanvasGroup Cg; }
        readonly List<Item> items = new List<Item>();
        Camera cam;

        public static WorldLabels Create(RectTransform layer, Camera cam)
        {
            var w = layer.gameObject.GetComponent<WorldLabels>();
            if (w == null) w = layer.gameObject.AddComponent<WorldLabels>();
            w.cam = cam;
            return w;
        }

        public void Clear()
        {
            foreach (var i in items) if (i.Rt != null) Destroy(i.Rt.gameObject);
            items.Clear();
        }

        public void Add(string text, Vector3 pos, string kind)
        {
            Color bg = kind == "stanica" ? Theme.Gold : kind == "cilj" ? Theme.Red : kind == "znamenitost" ? Col.Hex("#5a3fb0") : Theme.Paper;
            Color fg = kind == "grad" ? Theme.Ink : Color.white;
            int size = kind == "grad" ? 26 : kind == "cilj" ? 30 : 22;
            var box = UIKit.Box(transform, bg.WithA(0.94f), UIKit.Pill, "Natpis · " + text, 1f);
            box.raycastTarget = false;
            var t = UIKit.Label(box.transform, text, size, fg, TextAnchor.MiddleCenter, kind == "cilj");
            var fit = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            fit.padding = new RectOffset(18, 18, 4, 6);
            fit.childControlWidth = true; fit.childControlHeight = true;
            var csf = box.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = box.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0);
            items.Add(new Item { Rt = rt, Pos = pos, Cg = box.gameObject.AddComponent<CanvasGroup>() });
        }

        void LateUpdate()
        {
            if (cam == null) return;
            foreach (var i in items)
            {
                var sp = cam.WorldToViewportPoint(i.Pos);
                bool visible = sp.z > 0.3f && sp.x > -0.1f && sp.x < 1.1f && sp.y > -0.1f && sp.y < 1.1f;
                // daleki natpisi blijede, da ekran ne bude pretrpan
                float a = visible ? Mathf.Clamp01(1.3f - sp.z / 28f) : 0;
                i.Cg.alpha = a;
                if (!visible) continue;
                var parentRt = (RectTransform)i.Rt.parent;
                var screen = cam.WorldToScreenPoint(i.Pos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screen, null, out var local);
                i.Rt.anchoredPosition = local;
                float s = Mathf.Clamp(14f / sp.z, 0.55f, 1.15f);
                i.Rt.localScale = Vector3.one * s;
            }
        }
    }

    // HUD: traka napretka (start → cilj) sa svim igračima i stanicama, runda i poredak.
    public sealed class Hud : MonoBehaviour
    {
        RectTransform bar, board;
        Text roundText;
        readonly Dictionary<Racer, RectTransform> dots = new Dictionary<Racer, RectTransform>();
        Race race;

        public static Hud Create(RectTransform parent, Race race)
        {
            UIKit.Clear(parent);
            var go = UIKit.Rect(parent, "HUD trke").Stretch();
            var h = go.gameObject.AddComponent<Hud>();
            h.race = race;
            h.Build();
            return h;
        }

        Text itemText;
        Image itemBox;
        RectTransform side;
        bool sideHidden, hasItem;

        // Dok je otvorena kartica (pitanje, mini igra, cilj) poredak i predmet se sklanjaju da je ne prekriju;
        // isto se vidi na tabli runde i u zaglavlju pitanja.
        public static void ShowSide(bool on)
        {
            var root = UIRoot.I != null ? UIRoot.I.Hud : null;
            if (root == null) return;
            foreach (var h in root.GetComponentsInChildren<Hud>(true))
            {
                h.sideHidden = !on;
                if (h.side != null) h.side.gameObject.SetActive(on);
                if (h.itemBox != null) h.itemBox.gameObject.SetActive(on && h.hasItem);
            }
        }

        public static void ShowAll(bool on)
        {
            var root = UIRoot.I != null ? UIRoot.I.Hud : null;
            if (root == null) return;
            foreach (var h in root.GetComponentsInChildren<Hud>(true)) h.gameObject.SetActive(on);
        }

        void Build()
        {
            // gore: traka do cilja
            var top = UIKit.Card(transform, "Traka do cilja");
            top.rectTransform.parent.GetComponent<RectTransform>().Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(1000, 86));
            bar = UIKit.Rect(top.transform, "Put").Stretch(70, 30, 70, 30);
            var line = UIKit.Box(bar, Theme.Line, UIKit.Pill, "Linija", 1f);
            line.rectTransform.Stretch(0, 8, 0, 8);
            // kutije na traci (gdje se dobijaju napadi)
            for (int i = 1; i < race.Last; i++)
            {
                if (race.KindAt(i) != TileKinds.Box) continue;
                var st = UIKit.Box(bar, Col.Hex("#8e5bff"), UIKit.RoundSmall, "Kutija", 1f);
                var k = i / (float)race.Last;
                st.rectTransform.Anchor(new Vector2(k, 0.5f), new Vector2(k, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            }
            bar.offsetMin = new Vector2(96, bar.offsetMin.y);
            bar.offsetMax = new Vector2(-84, bar.offsetMax.y);
            var start = UIKit.Label(top.transform, "START", 22, Theme.Go, TextAnchor.MiddleCenter, true);
            start.horizontalOverflow = HorizontalWrapMode.Overflow;
            start.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(78, 40));
            var fin = UIKit.Label(top.transform, "CILJ", 22, Theme.Red, TextAnchor.MiddleCenter, true);
            fin.horizontalOverflow = HorizontalWrapMode.Overflow;
            fin.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(66, 40));
            foreach (var p in race.Players)
            {
                var d = UIKit.Box(bar, Col.Hex(p.Color), UIKit.Circle, "Igrač " + p.Name);
                var outline = d.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3, -3);
                d.rectTransform.sizeDelta = p.Human ? new Vector2(40, 40) : new Vector2(30, 30);
                if (p.Human) d.transform.SetAsLastSibling();
                dots[p] = d.rectTransform;
            }

            // desno: poredak
            var card = UIKit.Card(transform, "Poredak", new Color(1, 1, 1, 0.92f));
            side = card.rectTransform.parent.GetComponent<RectTransform>();
            side.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(380, 310));
            roundText = UIKit.Label(card.transform, "Runda 1", 28, Theme.InkSoft, TextAnchor.MiddleLeft, true);
            roundText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(-40, 44));
            board = UIKit.VStack(card.transform, 6, "Lista").GetComponent<RectTransform>();
            board.Stretch(20, 60, 20, 14);

            // ispod poretka: moj predmet iz kutije
            itemBox = UIKit.Box(transform, new Color(1, 1, 1, 0.92f), UIKit.Round, "Predmet", 0.8f);
            itemBox.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -342), new Vector2(380, 72));
            itemText = UIKit.Label(itemBox.transform, "", 26, Theme.Ink, TextAnchor.MiddleCenter, true);
            itemText.rectTransform.Stretch(14, 4, 14, 4);
            itemText.resizeTextForBestFit = true; itemText.resizeTextMinSize = 18; itemText.resizeTextMaxSize = 26; itemText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public void Refresh(int round)
        {
            roundText.text = Lang.L($"Runda {round}") + (race.IsDoubleRound ? " · ×2" : "");
            var me = race.Human;
            hasItem = me.Item != null || me.DoubleNext;
            itemBox.gameObject.SetActive(hasItem && !sideHidden);
            itemText.text = me.Item != null ? Lang.L("Predmet: " + Items.Label(me.Item)) : Lang.L("Dupla polja u sljedećem pitanju");
            itemBox.color = me.Item == Items.Shield ? Col.Hex("#dcebff") : me.Item != null ? Col.Hex("#ffe1e4") : Col.Hex("#fff3d6");
            UIKit.Clear(board);
            int i = 1;
            foreach (var p in race.Standings())
            {
                var row = UIKit.HStack(board, 10, "Red");
                row.Size(-1, 50);
                var dot = UIKit.Box(row.transform, Col.Hex(p.Color), UIKit.Circle, "Boja");
                dot.Size(22, 22);
                var name = UIKit.Label(row.transform, $"{i}. {p.Name}", 28, p.Human ? Theme.Ink : Theme.InkSoft, TextAnchor.MiddleLeft, p.Human);
                name.Size(-1, 50, 1);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 18; name.resizeTextMaxSize = 28; name.verticalOverflow = VerticalWrapMode.Truncate;
                var left = UIKit.Label(row.transform, p.Pos >= race.Last ? "cilj" : (race.Last - p.Pos).ToString(), 26, Theme.InkSoft, TextAnchor.MiddleRight, true);
                left.Size(64, 50);
                i++;
            }
        }

        void Update()
        {
            if (race == null) return;
            foreach (var kv in dots)
            {
                var k = Mathf.Clamp01(kv.Key.Pos / (float)race.Last);
                var target = new Vector2(k, 0.5f);
                var rt = kv.Value;
                var cur = rt.anchorMin;
                var next = Vector2.Lerp(cur, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 4));
                rt.anchorMin = rt.anchorMax = next;
                rt.anchoredPosition = Vector2.zero;
            }
        }
    }
}
