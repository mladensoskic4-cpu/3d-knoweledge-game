// Garaža: tvoj lik na postolju (isto kao u meniju, samo kamera bliže). Prevlačenje okreće vozilo, točkić ili
// dva prsta približavaju/udaljavaju. Lijevo kategorije (životinja, vozilo, boja, šara, boja šare, nalijepnica,
// kapa, naočare, trag), dolje traka sa predmetima, desno ime, cijena i dugme Kupi/Opremi.
using System;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class GarageScreen : MonoBehaviour
    {
        public static readonly (string id, string label, string icon)[] Slots =
        {
            ("zivotinja", "Životinja", "sapa"), ("vozilo", "Vozilo", "auto"), ("boja", "Boja", "boja"), ("sara", "Šara", "sara"),
            ("boja2", "Boja šare", "boja"), ("nalijepnica", "Nalijepnica", "nalijepnica"), ("kapa", "Kapa", "kapa"),
            ("naocare", "Naočare", "naocare"), ("trag", "Trag u vožnji", "iskra"),
        };

        Profile p;
        Showcase stage;
        WorldView world;
        Action changed, close;
        string slot = "zivotinja";
        ShopItem selected;
        Look preview;
        RectTransform strip, coinsHolder;
        Text title, status;
        GameButton action;
        readonly List<GameButton> slotButtons = new List<GameButton>();
        readonly Dictionary<string, GameButton> tiles = new Dictionary<string, GameButton>();
        public float Pitch = 12, Dist = 2.0f, Shift = -0.1f, Lift = 0.12f;
        public const float MinDist = 1.1f, MaxDist = 4.2f;
        float idle;

        public static GarageScreen Open(RectTransform parent, Showcase stage, WorldView world, Profile p, Action changed, Action close)
        {
            UIKit.Clear(parent);
            var root = UIKit.Rect(parent, "Garaža").Stretch();
            var g = root.gameObject.AddComponent<GarageScreen>();
            g.p = p; g.stage = stage; g.world = world; g.changed = changed; g.close = close;
            g.preview = p.Izgled.Clone();
            g.BuildUI();
            g.Select("zivotinja");
            if (App.I != null) App.I.Camera.FixedRate = 7;
            return g;
        }

        void BuildUI()
        {
            // površina za okretanje i zum (iza svega)
            var catcher = UIKit.Box(transform, new Color(1, 1, 1, 0.001f), null, "Okretanje", 1f);
            catcher.rectTransform.Stretch(-200, -200, -200, -200);
            catcher.gameObject.AddComponent<GarageInput>().Garage = this;

            var back = UIKit.Button(transform, "Nazad", Theme.InkSoft, Close, 34);
            back.SetIcon("nazad", 44);
            ((RectTransform)back.transform).Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -28), new Vector2(240, 92));
            var head = UIKit.Label(transform, "Garaža", 76, Color.white, TextAnchor.MiddleLeft, true);
            head.Outlined(Col.Hex("#1b4f8a"), 5);
            head.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(300, -24), new Vector2(500, 100));
            RefreshCoins();

            // lijevo: kategorije
            var catFace = UIKit.GlassCard(transform, "Kategorije");
            ((RectTransform)catFace.rectTransform.parent).Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(32, -55), new Vector2(330, -190));
            var cats = UIKit.VStack(catFace.transform, 10, "Lista", TextAnchor.UpperLeft, new RectOffset(16, 16, 16, 16));
            ((RectTransform)cats.transform).Stretch();
            cats.childForceExpandHeight = false;
            FitInside.Add((RectTransform)cats.transform);
            foreach (var (id, label, icon) in Slots)
            {
                var sid = id;
                var b = UIKit.Button(cats.transform, label, Theme.Blue, () => Select(sid), 28);
                b.Tag = id;
                b.SetIcon(icon, 40);
                b.Label.alignment = TextAnchor.MiddleLeft;
                b.Size(-1, 84);
                slotButtons.Add(b);
            }

            // dolje: traka sa predmetima (vodoravno pomjeranje)
            var stripFace = UIKit.GlassCard(transform, "Predmeti");
            ((RectTransform)stripFace.rectTransform.parent).Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(197, 32), new Vector2(-458, 236));
            var view = UIKit.Rect(stripFace.transform, "Pogled").Stretch(16, 16, 16, 16);
            view.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.001f);
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.vertical = false; sr.horizontal = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            var content = UIKit.HStack(view, 14, "Sadržaj", TextAnchor.MiddleLeft, new RectOffset(4, 4, 0, 0));
            strip = (RectTransform)content.transform;
            strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(0, 1); strip.pivot = new Vector2(0, 0.5f);
            strip.anchoredPosition = Vector2.zero; strip.sizeDelta = Vector2.zero;
            content.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = strip;
            sr.viewport = view;

            // desno iznad trake: ime, cijena, dugme
            var info = UIKit.GlassCard(transform, "Predmet");
            ((RectTransform)info.rectTransform.parent).Anchor(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 300), new Vector2(560, 230));
            var col = UIKit.VStack(info.transform, 8, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(28, 28, 20, 20));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            title = UIKit.Label(col.transform, "", 44, Theme.Ink, TextAnchor.MiddleLeft, true);
            title.Size(-1, 58);
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 26; title.resizeTextMaxSize = 44; title.verticalOverflow = VerticalWrapMode.Truncate;
            status = UIKit.Label(col.transform, "", 28, Theme.InkSoft);
            status.Size(-1, 40);
            action = UIKit.Button(col.transform, "Kupi", Theme.Go, Act, 38);
            action.Size(-1, 92);

            var hint = UIKit.Label(transform, "Prevuci da okreneš · točkić ili dva prsta za zum", 26, Color.white, TextAnchor.MiddleCenter, true);
            hint.Outlined(new Color(0, 0, 0, 0.45f), 2);
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            hint.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(60, -112), new Vector2(760, 44));
        }

        void RefreshCoins()
        {
            if (coinsHolder != null) Destroy(coinsHolder.gameObject);
            coinsHolder = MenuScreen.CoinPill(transform, p.Novcici);
            coinsHolder.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -32), new Vector2(240, 84));
        }

        List<ShopItem> Items => Catalog.Shop.Where(i => i.Slot == slot).ToList();

        void Select(string s)
        {
            slot = s;
            if (s == "trag") { stage.Yaw = 75; idle = 0; }       // bočno, da se vidi trag iza vozila
            foreach (var b in slotButtons) b.SetSelected(b.Tag == s);
            BuildStrip();
            var eq = Items.FirstOrDefault(i => p.IsEquipped(i)) ?? Items[0];
            Pick(eq, false);
        }

        void BuildStrip()
        {
            UIKit.Clear(strip);
            tiles.Clear();
            foreach (var it in Items)
            {
                var item = it;
                bool owns = p.Owns(item.Id), rankOk = Catalog.RankAllows(item.Rank, p.Bodovi);
                string label = Lang.L(item.Label) + (owns ? "" : "\n<size=20>" + (rankOk ? Lang.L($"{item.Price} novčića") : Lang.L("zaključano")) + "</size>");
                var b = UIKit.Button(strip, "", Theme.Blue, () => Pick(item, true), 22);
                b.Tag = item.Id;
                b.Label.text = label;
                b.Size(176, 186);
                b.Label.rectTransform.Stretch(8, 104, 8, 10);
                b.Label.alignment = TextAnchor.MiddleCenter;
                // sličica: boja, ikonica ili oboji ikonicu bojom predmeta
                var pic = TileImage(b, item);
                pic.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(84, 84));
                if (p.IsEquipped(item)) Badge(b, "kvacica", Theme.Go);
                else if (!rankOk) Badge(b, "katanac", Theme.InkSoft);
                tiles[item.Id] = b;
            }
        }

        Image TileImage(GameButton b, ShopItem item)
        {
            var face = b.Face.transform;
            switch (item.Slot)
            {
                case "boja":
                case "boja2":
                    {
                        var c = UIKit.Box(face, Col.Hex(item.Value), UIKit.Circle, "Boja");
                        c.raycastTarget = false;
                        var o = c.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.25f); o.effectDistance = new Vector2(2, -2);
                        return c;
                    }
                default:
                    {
                        var (icon, color) = IconFor(item);
                        var img = UIKit.Box(face, color, UIKit.IconSprite(icon), "Slika", 1f);
                        img.type = Image.Type.Simple; img.preserveAspect = true; img.raycastTarget = false;
                        return img;
                    }
            }
        }

        static (string icon, Color color) IconFor(ShopItem item)
        {
            var none = (item.Value == "nema" || item.Value == "ne") ? ("zatvori", Theme.InkSoft) : default;
            if (none.Item1 != null) return none;
            switch (item.Slot)
            {
                case "zivotinja": return ("sapa", Col.Hex(Catalog.AnimalOf(item.Value).Body).Mul(0.85f).WithA(1));
                case "vozilo": return ("auto", Col.Hex(Catalog.VehicleOf(item.Value).DefaultPaint).Mul(0.9f).WithA(1));
                case "kapa": return ("kapa", item.Value == "kruna" ? Theme.Gold : Col.Hex("#8a3b2b"));
                case "naocare": return ("naocare", Theme.Ink);
                case "sara":
                    switch (item.Value)
                    {
                        case "pruge": return ("pruge", Theme.Blue);
                        case "tacke": return ("tacke", Theme.Purple);
                        case "plamen": return ("plamen", Col.Hex("#f08a1c"));
                        case "sah": return ("sah", Theme.Ink);
                        case "munje": return ("munja", Theme.Gold);
                        default: return ("zvijezda", Theme.Gold);
                    }
                case "nalijepnica":
                    switch (item.Value)
                    {
                        case "zvijezda": return ("zvijezda", Theme.Gold);
                        case "srce": return ("srce", Col.Hex("#ff3b6b"));
                        case "munja": return ("munja", Theme.Gold);
                        case "broj": return ("sedam", Theme.Red);
                        case "cvijet": return ("cvijet", Col.Hex("#ff6fae"));
                        case "smajli": return ("smajli", Theme.Gold);
                        case "cg": return ("zastava", Col.Hex("#c8102e"));
                        default: return ("kruna", Theme.Gold);
                    }
                case "trag":
                    switch (item.Value)
                    {
                        case "iskre": return ("iskra", Theme.Gold);
                        case "srca": return ("srce", Col.Hex("#ff3b6b"));
                        case "duga": return ("duga", Theme.Purple);
                        default: return ("zvijezda", Theme.Gold);
                    }
            }
            return ("zvijezda", Theme.Gold);
        }

        static void Badge(GameButton b, string icon, Color c)
        {
            var bg = UIKit.Box(b.Face.transform, c, UIKit.Circle, "Oznaka");
            bg.raycastTarget = false;
            bg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-18, -18), new Vector2(44, 44));
            var i = UIKit.IconImage(bg.transform, icon, Color.white, 28);
            i.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
        }

        void Pick(ShopItem item, bool fromTap)
        {
            selected = item;
            foreach (var kv in tiles) kv.Value.SetSelected(kv.Key == item.Id);
            preview = Profile.Apply(p.Izgled, item);
            stage.SetLook(preview);
            if (fromTap) { idle = 0; AudioKit.I?.Click(); }
            title.text = Lang.L(item.Label);
            bool owns = p.Owns(item.Id), rankOk = Catalog.RankAllows(item.Rank, p.Bodovi);
            if (p.IsEquipped(item)) { status.text = Lang.L("Opremljeno"); action.SetText("Opremljeno"); action.Interactable = false; action.SetColor(Theme.InkSoft); }
            else if (owns) { status.text = Lang.L("Imaš ovo."); action.SetText("Opremi"); action.SetColor(Theme.Blue); action.Interactable = true; }
            else if (!rankOk) { status.text = Lang.L($"Otključava se na rangu {Catalog.Ranks.First(r => r.Id == item.Rank).Label}"); action.SetText("Zaključano"); action.SetColor(Theme.InkSoft); action.Interactable = false; }
            else
            {
                status.text = Lang.L($"Cijena: {item.Price} novčića");
                action.SetText(p.Novcici >= item.Price ? $"Kupi · {item.Price}" : "Nemaš dovoljno");
                action.SetColor(Theme.Go);
                action.Interactable = p.Novcici >= item.Price;
            }
        }

        void Act()
        {
            var item = selected;
            if (item == null) return;
            if (!p.Owns(item.Id))
            {
                if (!p.Buy(item)) { UIRoot.I.Toast("Nemaš dovoljno novčića.", Theme.Red); return; }
                AudioKit.I?.Play("novcic");
                Effects.Sparkle(stage.Pivot.position + Vector3.up * 0.4f, stage.transform, Theme.Gold);
                RefreshCoins();
            }
            p.Equip(item);
            changed();
            BuildStrip();
            Pick(item, false);
        }

        void Close()
        {
            stage.SetLook(p.Izgled);
            stage.AutoSpin = false;
            if (App.I != null) App.I.Camera.FixedRate = 2;
            close();
        }

        // ---- okretanje i zum ----
        public void Rotate(float dx, float dy)
        {
            stage.Yaw -= dx * 0.35f;
            Pitch = Mathf.Clamp(Pitch + dy * 0.12f, 2, 40);
            idle = 0;
        }

        public void Zoom(float factor)
        {
            Dist = Mathf.Clamp(Dist / factor, MinDist, MaxDist);
            idle = 0;
        }

        void Update()
        {
            idle += Time.unscaledDeltaTime;
            stage.AutoSpin = idle > 4;
            // trag se vidi samo u vožnji: dok biraš trag, točkovi se vrte i trag se pušta
            if (stage.Rig != null) stage.Rig.Speed = slot == "trag" ? 2.2f : 0;
            if (App.I == null) return;
            stage.CameraAt(0, Pitch, Dist, Shift, world, out var pos, out var look, Lift);
            App.I.Camera.Fixed(pos, look);
        }
    }

    // Prevlačenje okreće vozilo, točkić miša i dva prsta približavaju.
    public sealed class GarageInput : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
    {
        public GarageScreen Garage;
        float pinch;
        bool pinching;

        public void OnPointerDown(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (pinching || Keys.TouchCount > 1) return;
            float k = 1920f / Mathf.Max(1, Screen.width);   // isti osjećaj na svakoj rezoluciji
            Garage.Rotate(e.delta.x * k, e.delta.y * k);
        }

        public void OnScroll(PointerEventData e)
        {
            if (Mathf.Abs(e.scrollDelta.y) < 0.01f) return;
            Garage.Zoom(e.scrollDelta.y > 0 ? 1.12f : 1 / 1.12f);
        }

        void Update()
        {
            if (Keys.TouchCount >= 2)
            {
                var d = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (!pinching) { pinching = true; pinch = Mathf.Max(1, d); }
                else if (d > 1) { Garage.Zoom(d / pinch); pinch = d; }
            }
            else if (pinching && Keys.TouchCount == 0) pinching = false;
        }
    }
}
