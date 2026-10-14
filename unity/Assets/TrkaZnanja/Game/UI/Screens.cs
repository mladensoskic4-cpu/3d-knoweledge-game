// Ekrani: meni (svijet, oblasti, uzrast, dužina, botovi), podešavanja, garaža (3D izlog), stanica sa mini igrom,
// bonus runda i ekran na cilju (postolje, nagrade, novi rang).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class MenuChoice
    {
        public bool Start, Garage, Settings, Leaderboard, Multiplayer;
        public string MapChanged;
    }

    public static class MenuScreen
    {
        public static void Build(RectTransform parent, Profile p, IDictionary<string, int> areaCounts, Action<MenuChoice> onChoice)
        {
            UIKit.Clear(parent);
            var S = p.Podesavanja;
            var root = UIKit.Rect(parent, "Meni").Stretch();

            // ---- lijevo: naslov, profil, velika dugmad ----
            var left = UIKit.VStack(root, 22, "Lijevo", TextAnchor.UpperLeft, new RectOffset(56, 20, 50, 40));
            ((RectTransform)left.transform).Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(780, 0));
            var title = UIKit.Label(left.transform, "Trka znanja", 104, Color.white, TextAnchor.MiddleLeft, true);
            title.Outlined(Col.Hex("#1b4f8a"), 5);
            title.Size(-1, 130);
            var sub = UIKit.Label(left.transform, "Odgovaraj brzo i tačno da pređeš više polja. Pokupi kutije sa napadima, a svake 4 runde svi igraju mini igru.", 30, Color.white, TextAnchor.UpperLeft);
            sub.Outlined(new Color(0, 0, 0, 0.45f), 2);
            sub.Size(-1, 90);
            ProfileCard(left.transform, p);
            var playRow = UIKit.HStack(left.transform, 16, "Igraj");
            playRow.Size(-1, 150);
            playRow.childForceExpandWidth = true;
            UIKit.Button(playRow.transform, "Igraj", Theme.Go, () => onChoice(new MenuChoice { Start = true }), 64).Size(-1, 150, 1.3f);
            UIKit.Button(playRow.transform, "Sa drugima", Theme.Primary, () => onChoice(new MenuChoice { Multiplayer = true }), 40).Size(-1, 150, 1);
            var row = UIKit.HStack(left.transform, 16, "Dugmad");
            row.Size(-1, 100);
            row.childForceExpandWidth = true;
            UIKit.Button(row.transform, "Garaža", Theme.Purple, () => onChoice(new MenuChoice { Garage = true }), 32).Size(-1, 96, 1);
            UIKit.Button(row.transform, "Rang-lista", Theme.Gold, () => onChoice(new MenuChoice { Leaderboard = true }), 32).Size(-1, 96, 1);
            UIKit.Button(row.transform, "Podešavanja", Theme.Blue, () => onChoice(new MenuChoice { Settings = true }), 32).Size(-1, 96, 1);
            if (!Net.Online)
            {
                var off = UIKit.Label(left.transform, "Bez interneta: igraš protiv botova, rezultati se šalju kasnije.", 24, Color.white);
                off.Outlined(new Color(0, 0, 0, 0.45f), 2);
                off.Size(-1, 60);
            }

            // ---- desno: podešavanja trke ----
            var face = UIKit.Card(root, "Trka", new Color(1, 1, 1, 0.95f));
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(1000, -80));
            var list = UIKit.Scroll(face.transform, "Podešavanja trke", 22, new RectOffset(36, 36, 30, 40));
            ((RectTransform)list.parent).Stretch(0, 0, 0, 0);

            Section(list, "Svijet");
            var maps = UIKit.Grid(list, new Vector2(445, 120), new Vector2(18, 16), 2, "Mape");
            maps.Size(-1, 256);
            var mapButtons = new List<GameButton>();
            foreach (var m in Catalog.Maps)
            {
                var map = m;
                bool open = p.MapUnlocked(m);
                var rank = Catalog.Ranks.First(r => r.Id == m.Rank);
                var b = UIKit.Button(maps.transform, open ? m.Name : $"{m.Name}\n<size=22>Otključava se na rangu {rank.Label}</size>", open ? Theme.Blue : Theme.InkSoft, null, 34);
                if (!open) b.Label.text = Lang.L(m.Name) + "\n<size=22>" + Lang.L($"Otključava se na rangu {rank.Label}") + "</size>";
                b.Tag = m.Id;
                b.Interactable = open;
                b.SetSelected(S.Mapa == m.Id && open);
                if (!open) b.Interactable = false;
                b.OnClick += () =>
                {
                    S.Mapa = map.Id;
                    foreach (var x in mapButtons) if (x.Interactable) x.SetSelected(x.Tag == map.Id);
                    onChoice(new MenuChoice { MapChanged = map.Id });
                };
                mapButtons.Add(b);
            }

            Section(list, "Oblasti");
            var avail = Catalog.Areas.Where(a => areaCounts.ContainsKey(a.Id)).ToList();
            var picked = new HashSet<string>((S.Oblasti ?? new List<string>()).Where(a => areaCounts.ContainsKey(a)));
            bool mix = S.Miks || picked.Count == 0;
            var hint = UIKit.Label(list, "", 26, Theme.InkSoft);
            var chips = UIKit.Grid(list, new Vector2(290, 92), new Vector2(14, 14), 3, "Oblasti");
            chips.Size(-1, (Mathf.CeilToInt((avail.Count + 1) / 3f)) * 106);
            var areaButtons = new Dictionary<string, GameButton>();
            GameButton mixBtn = null;
            void Refresh()
            {
                S.Miks = mix;
                S.Oblasti = mix ? null : picked.ToList();
                mixBtn.SetSelected(mix);
                foreach (var kv in areaButtons) kv.Value.SetSelected(!mix && picked.Contains(kv.Key));
                hint.text = Lang.L(mix ? $"Pitanja iz svih oblasti ({avail.Count})." : $"Izabrano {picked.Count} od 3.");
            }
            mixBtn = UIKit.Button(chips.transform, "Miks", Theme.Primary, () => { mix = true; picked.Clear(); Refresh(); }, 28);
            foreach (var a in avail)
            {
                var id = a.Id;
                var b = UIKit.Button(chips.transform, a.Label, Col.Hex(a.Color), () =>
                {
                    if (mix) { mix = false; picked.Clear(); }
                    if (picked.Contains(id)) picked.Remove(id); else if (picked.Count < 3) picked.Add(id);
                    if (picked.Count == 0) mix = true;
                    Refresh();
                }, 24);
                areaButtons[id] = b;
            }
            hint.Size(-1, 40);
            Refresh();

            Section(list, "Uzrast");
            var ageNote = UIKit.Label(list, Catalog.AgeOf(S.Uzrast).Note, 24, Theme.InkSoft);
            UIKit.Segmented(list, Catalog.Ages.Select(a => (a.Id, a.Label)).ToList(), S.Uzrast, id => { S.Uzrast = id; ageNote.text = Lang.L(Catalog.AgeOf(id).Note); });
            ageNote.transform.SetAsLastSibling();
            ageNote.Size(-1, 36);

            Section(list, "Dužina trke");
            UIKit.Segmented(list, Catalog.Lengths.Select(l => (l.Id, l.Label)).ToList(), S.Duzina, id => { S.Duzina = id; onChoice(new MenuChoice { MapChanged = S.Mapa }); });

            Section(list, "Botovi");
            UIKit.Segmented(list, Bots.Levels.Values.Select(l => (l.Id, l.Label)).ToList(), S.Nivo, id => S.Nivo = id);

            var credits = UIKit.Label(list, "Zastave: flag-icons (MIT) · Mape: Natural Earth · Font: Nunito (OFL) · Pitanja: tim Trke znanja", 20, Theme.InkSoft);
            credits.Size(-1, 60);
        }

        static void Section(Transform parent, string title)
        {
            var t = UIKit.Label(parent, title, 34, Theme.Ink, TextAnchor.LowerLeft, true);
            t.Size(-1, 56);
        }

        public static void ProfileCard(Transform parent, Profile p)
        {
            var face = UIKit.Card(parent, "Profil", new Color(1, 1, 1, 0.94f));
            ((RectTransform)face.rectTransform.parent).Size(-1, 150);
            var rank = p.Rank;
            var prog = Catalog.RankProgress(p.Bodovi);
            var row = UIKit.HStack(face.transform, 20, "Red", TextAnchor.MiddleLeft, new RectOffset(26, 26, 18, 18));
            ((RectTransform)row.transform).Stretch();
            var badge = UIKit.Box(row.transform, Col.Hex(rank.Color), UIKit.Circle, "Rang");
            badge.Size(100, 100);
            UIKit.Label(badge.transform, rank.Label.Substring(0, 1), 54, Color.white, TextAnchor.MiddleCenter, true).rectTransform.Stretch();
            var mid = UIKit.VStack(row.transform, 8, "Napredak");
            mid.Size(-1, 110, 1);
            var name = string.IsNullOrEmpty(p.Ime) ? Lang.L("Igrač") : p.Ime;
            UIKit.Label(mid.transform, $"{name} · {Lang.L(rank.Label)}", 34, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 44);
            UIKit.Bar(mid.transform, Theme.Line, Col.Hex(rank.Color), out var fill, 16).Size(-1, 16);
            UIKit.SetFill(fill, (float)prog.pct);
            UIKit.Label(mid.transform, prog.next != null ? $"{p.Bodovi} bodova · još {prog.need} do ranga {prog.next.Label}" : $"{p.Bodovi} bodova · najviši rang", 24, Theme.InkSoft).Size(-1, 34);
            var coins = UIKit.Box(row.transform, Theme.Gold, UIKit.Pill, "Novčići", 1f);
            coins.Size(170, 70);
            UIKit.Label(coins.transform, p.Novcici.ToString(), 34, Color.white, TextAnchor.MiddleCenter, true).Outlined(Col.Hex("#a26a00"), 2).rectTransform.Stretch();
        }
    }

    public static class SettingsScreen
    {
        public static void Build(RectTransform parent, Profile p, Action<string> changed, Action close)
        {
            UIKit.Clear(parent);
            UIRoot.I.Dim(parent, 0.5f);
            var face = UIKit.Card(parent, "Podešavanja");
            ((RectTransform)face.rectTransform.parent).Center(new Vector2(1180, 940));
            var list = UIKit.Scroll(face.transform, "Lista", 20, new RectOffset(44, 44, 36, 40));
            ((RectTransform)list.parent).Stretch(0, 0, 0, 120);
            var S = p.Podesavanja;

            UIKit.Label(list, "Podešavanja", 56, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 80);
            Row(list, "Jezik");
            var langs = new List<(string, string)> { ("", "Automatski") };
            langs.AddRange(Lang.All.Select(l => (l.id, l.label)));
            UIKit.Segmented(list, langs, S.Jezik ?? "", id => { S.Jezik = id; changed("jezik"); }, 26);
            Row(list, "Kvalitet grafike");
            UIKit.Segmented(list, new List<(string, string)> { ("auto", "Automatski"), ("nisko", "Nisko"), ("srednje", "Srednje"), ("visoko", "Visoko") }, S.Kvalitet, id => { S.Kvalitet = id; changed("kvalitet"); }, 26);
            Row(list, "Muzika");
            UIKit.Segmented(list, new List<(string, string)> { ("da", "Uključeno"), ("ne", "Isključeno") }, S.Muzika ? "da" : "ne", id => { S.Muzika = id == "da"; changed("zvuk"); }, 26);
            Row(list, "Zvučni efekti");
            UIKit.Segmented(list, new List<(string, string)> { ("da", "Uključeno"), ("ne", "Isključeno") }, S.Efekti ? "da" : "ne", id => { S.Efekti = id == "da"; changed("zvuk"); }, 26);
            Row(list, "Vibracija");
            UIKit.Segmented(list, new List<(string, string)> { ("da", "Uključeno"), ("ne", "Isključeno") }, S.Vibracija ? "da" : "ne", id => { S.Vibracija = id == "da"; changed("vibracija"); }, 26);
            Row(list, "Tvoje ime");
            UIKit.Input(list, p.Ime, "Ti", 12, v => { p.Ime = v; changed("profil"); });
            Row(list, "Grad (za rang-listu)");
            UIKit.Input(list, p.Grad, "npr. Podgorica", 40, v => { p.Grad = v; changed("profil"); });
            Row(list, "Škola (opciono)");
            UIKit.Input(list, p.Skola, "", 60, v => { p.Skola = v; changed("profil"); });
            Row(list, "Zasluge");
            UIKit.Label(list, "Zastave: flag-icons (MIT licenca). Mape: Natural Earth (javno vlasništvo). Font: Nunito (SIL Open Font License). Zvuk i muzika: sintetizovani u igri; CC0 zvukovi navedeni u PROCITAJ.md kad se dodaju. 3D modeli: napravljeni u kodu igre.", 24, Theme.InkSoft).FitHeight();

            var closeBtn = UIKit.Button(face.transform, "Zatvori", Theme.Primary, close, 36);
            ((RectTransform)closeBtn.transform).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(360, 96));
        }

        static void Row(Transform parent, string text) => UIKit.Label(parent, text, 30, Theme.Ink, TextAnchor.LowerLeft, true).Size(-1, 50);
    }

    // Garaža: 3D izlog koji se okreće; strelice (ili prevlačenje) mijenjaju predmet, kupi ili opremi.
    public sealed class GarageScreen : MonoBehaviour
    {
        Profile p;
        Action changed, close;
        string slot = "zivotinja";
        int index;
        Look preview;
        Transform stage, model;
        Text title, price, coins;
        GameButton action;
        Vector2? dragStart;
        static readonly (string id, string label)[] Slots = { ("zivotinja", "Životinja"), ("vozilo", "Vozilo"), ("boja", "Boja vozila"), ("kapa", "Kapa"), ("naocare", "Naočare") };

        public static GarageScreen Open(RectTransform parent, Transform world3d, Profile p, Action changed, Action close)
        {
            UIKit.Clear(parent);
            var root = UIKit.Rect(parent, "Garaža").Stretch();
            var g = root.gameObject.AddComponent<GarageScreen>();
            g.p = p; g.changed = changed; g.close = close;
            g.preview = p.Izgled.Clone();
            g.BuildStage(world3d);
            g.BuildUI();
            g.Select("zivotinja");
            return g;
        }

        void BuildStage(Transform world3d)
        {
            var go = new GameObject("Izlog");
            go.transform.SetParent(world3d, false);
            go.transform.position = new Vector3(0, -200, 0);    // daleko ispod svijeta, sa svojim svjetlom
            stage = go.transform;
            var k = new MeshKit();
            k.Cylinder(new Vector3(0, -0.12f, 0), 1.0f, 0.95f, 0.12f, 32, MeshKit.C("#f6bd2a"), true, MeshKit.C("#ffe08a"));
            k.Cylinder(new Vector3(0, -0.6f, 0), 1.2f, 1.1f, 0.5f, 32, MeshKit.C("#2b7de9"));
            k.Disc(new Vector3(0, -0.6f, 0), 8, 40, MeshKit.C("#cfe8ff"), true);
            k.ToObject("Postolje", stage);
            var lightGo = new GameObject("Svjetlo izloga");
            lightGo.transform.SetParent(stage, false);
            lightGo.transform.localPosition = new Vector3(1.5f, 2.5f, 2.5f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point; l.range = 8; l.intensity = 2.2f; l.color = Col.Hex("#fff3dc");
            App.I.Camera.Showroom(stage.position + new Vector3(0, 0.55f, 2.1f), stage.position + new Vector3(0, 0.25f, 0));
            App.I.Camera.Cam.fieldOfView = 34;
        }

        void BuildUI()
        {
            var top = UIKit.HStack(transform, 12, "Kategorije", TextAnchor.MiddleCenter);
            ((RectTransform)top.transform).Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1500, 96));
            foreach (var (id, label) in Slots)
            {
                var sid = id;
                var b = UIKit.Button(top.transform, label, Theme.Blue, () => Select(sid), 28);
                b.Tag = id;
                b.Size(270, 90);
            }
            var prev = UIKit.Button(transform, "<", Theme.Primary, () => Step(-1), 64);
            ((RectTransform)prev.transform).Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(130, 150));
            var next = UIKit.Button(transform, ">", Theme.Primary, () => Step(1), 64);
            ((RectTransform)next.transform).Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(130, 150));

            var face = UIKit.Card(transform, "Info", new Color(1, 1, 1, 0.95f));
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1300, 170));
            var row = UIKit.HStack(face.transform, 24, "Red", TextAnchor.MiddleLeft, new RectOffset(36, 30, 20, 20));
            ((RectTransform)row.transform).Stretch();
            var texts = UIKit.VStack(row.transform, 4, "Tekst");
            texts.Size(-1, 130, 1);
            title = UIKit.Label(texts.transform, "", 44, Theme.Ink, TextAnchor.MiddleLeft, true);
            title.Size(-1, 64);
            price = UIKit.Label(texts.transform, "", 28, Theme.InkSoft);
            price.Size(-1, 50);
            coins = UIKit.Label(row.transform, "", 30, Theme.Ink, TextAnchor.MiddleRight, true);
            coins.Size(220, 130);
            action = UIKit.Button(row.transform, "Kupi", Theme.Go, Act, 34);
            action.Size(260, 110);
            UIKit.Button(row.transform, "Nazad", Theme.InkSoft, Close, 30).Size(200, 110);
        }

        List<ShopItem> Items => Catalog.Shop.Where(i => i.Slot == slot).ToList();

        void Select(string s)
        {
            slot = s;
            var items = Items;
            index = Math.Max(0, items.FindIndex(i => p.IsEquipped(i)));
            foreach (var b in GetComponentsInChildren<GameButton>().Where(b => Slots.Any(x => x.id == b.Tag))) b.SetSelected(b.Tag == s);
            Show();
        }

        void Step(int d)
        {
            var n = Items.Count;
            index = (index + d + n) % n;
            Show();
        }

        void Show()
        {
            var item = Items[index];
            preview = Profile.Apply(p.Izgled, item);
            if (model != null) Destroy(model.gameObject);
            var m = Avatars.Build(preview, stage, "Model");
            m.transform.localScale = Vector3.one * 2.4f;
            model = m.transform;
            title.text = Lang.L(item.Label);
            bool owns = p.Owns(item.Id), rankOk = Catalog.RankAllows(item.Rank, p.Bodovi);
            if (p.IsEquipped(item)) { price.text = Lang.L("Opremljeno"); action.SetText("Opremljeno"); action.Interactable = false; }
            else if (owns) { price.text = Lang.L("Imaš ovo."); action.SetText("Opremi"); action.Interactable = true; action.SetColor(Theme.Blue); }
            else if (!rankOk) { price.text = Lang.L($"Otključava se na rangu {Catalog.Ranks.First(r => r.Id == item.Rank).Label}"); action.SetText("Zaključano"); action.Interactable = false; }
            else { price.text = Lang.L($"Cijena: {item.Price} novčića"); action.SetText("Kupi"); action.SetColor(Theme.Go); action.Interactable = p.Novcici >= item.Price; }
            coins.text = Lang.L($"Novčići: {p.Novcici}");
        }

        void Act()
        {
            var item = Items[index];
            if (!p.Owns(item.Id))
            {
                if (!p.Buy(item)) { UIRoot.I.Toast("Nemaš dovoljno novčića.", Theme.Red); return; }
                AudioKit.I?.Play("novcic");
            }
            p.Equip(item);
            changed();
            Show();
        }

        void Update()
        {
            if (model != null) model.localRotation = Quaternion.Euler(0, Time.unscaledTime * 35, 0);
            // prevlačenje lijevo/desno mijenja predmet
            try
            {
                if (Input.GetMouseButtonDown(0)) dragStart = Input.mousePosition;
                if (Input.GetMouseButtonUp(0) && dragStart.HasValue)
                {
                    var dx = Input.mousePosition.x - dragStart.Value.x;
                    if (Mathf.Abs(dx) > Screen.width * 0.12f) Step(dx < 0 ? 1 : -1);
                    dragStart = null;
                }
            }
            catch (InvalidOperationException) { }
        }

        void Close()
        {
            if (stage != null) Destroy(stage.gameObject);
            App.I.Camera.Cam.fieldOfView = 46;
            close();
        }
    }

    // Stanica ili bonus runda: najava, mini igra, rezultati svih igrača.
    public static class MinigameScreen
    {
        public static IEnumerator Run(RectTransform parent, Minigame mg, int seconds, string chip, Color chipColor, string rule,
            Rng rng, List<StationOutcome> resultsOut, Func<double, string, List<StationOutcome>> computeResults,
            Func<double, string, IEnumerator> exchange = null)
        {
            UIKit.Clear(parent);
            mg.Prepare();
            var face = UIKit.Card(parent, "Mini igra");
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(-110, -40), new Vector2(1240, -150));
            var col = UIKit.VStack(face.transform, 18, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(40, 40, 30, 30));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var head = UIKit.HStack(col.transform, 12, "Zaglavlje");
            head.Size(-1, 52);
            UIKit.Chip(head.transform, chip, chipColor, 24);
            UIKit.Chip(head.transform, $"{seconds} s", Theme.InkSoft, 24);
            UIKit.Label(col.transform, mg.Name, 56, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 80);
            UIKit.Label(col.transform, mg.Intro(seconds), 30, Theme.InkSoft).FitHeight();
            UIKit.Label(col.transform, rule, 26, Theme.Ink).FitHeight();
            bool go = false;
            var start = UIKit.Button(col.transform, "Počni", Theme.Go, () => go = true, 40);
            start.Size(320, 100);
            while (!go && !App.AutoPlay) yield return null;

            UIKit.Clear(col.transform);
            var head2 = UIKit.HStack(col.transform, 12, "Zaglavlje");
            head2.Size(-1, 52);
            UIKit.Chip(head2.transform, mg.Name, chipColor, 24);
            var host = UIKit.Rect(col.transform, "Igra");
            host.Size(-1, 600, 1, 1);
            yield return mg.Play(host, rng, seconds);
            if (exchange != null)
            {
                // multiplayer: pošalji svoj rezultat i sačekaj ostale ljude
                UIKit.Clear(host);
                var wait = UIKit.Label(host, $"Tvoj rezultat: {mg.ResultLabel}\nČekam ostale igrače…", 40, Theme.Ink, TextAnchor.MiddleCenter, true);
                wait.rectTransform.Stretch();
                wait.text = Lang.L($"Tvoj rezultat: {mg.ResultLabel}") + "\n" + Lang.L("Čekam ostale igrače…");
                yield return exchange(mg.Score, mg.ResultLabel);
            }
            var results = computeResults(mg.Score, mg.ResultLabel);
            resultsOut.AddRange(results);

            UIKit.Clear(host);
            var list = UIKit.VStack(host, 10, "Rezultati");
            ((RectTransform)list.transform).Stretch();
            int place = 1;
            foreach (var o in results)
            {
                var cell = UIKit.Box(list.transform, o.Racer.Human ? Col.Hex("#fff3d6") : Theme.PaperSoft, UIKit.RoundSmall, "Red", 0.8f);
                cell.Size(-1, 72);
                var hr = UIKit.HStack(cell.transform, 14, "Red", TextAnchor.MiddleLeft, new RectOffset(20, 20, 0, 0));
                ((RectTransform)hr.transform).Stretch();
                UIKit.Box(hr.transform, Col.Hex(o.Racer.Color), UIKit.Circle, "Boja").Size(26, 26);
                UIKit.Label(hr.transform, $"{place}. {(o.Racer.Human ? Lang.L("Ti") : o.Racer.Name)}", 30, Theme.Ink, TextAnchor.MiddleLeft, true).Size(300, 72);
                UIKit.Label(hr.transform, $"{o.Label} · {o.Coins} novčića", 28, Theme.InkSoft).Size(-1, 72, 1);
                UIKit.Label(hr.transform, o.Jump > 0 ? "+" + o.Jump : "0", 34, o.Jump > 0 ? Theme.Right : Theme.InkSoft, TextAnchor.MiddleRight, true).Size(80, 72);
                place++;
            }
            go = false;
            var cont = UIKit.Button(col.transform, "Nastavi", Theme.Primary, () => go = true, 38);
            cont.Size(320, 96);
            float w = 0;
            while (!go) { w += Time.unscaledDeltaTime; if (App.AutoPlay && w > 0.3f) go = true; yield return null; }
            UIKit.Clear(parent);
        }
    }

    public static class ResultsScreen
    {
        public static void Build(RectTransform parent, Race race, List<Racer> ranked, Profile.Award award, RaceMap map, Profile p, Action again, Action menu)
        {
            UIKit.Clear(parent);
            var me = race.Human;
            int place = ranked.IndexOf(me);
            var winner = ranked[0];
            var face = UIKit.Card(parent, "Cilj", new Color(1, 1, 1, 0.95f));
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(880, -80));
            var col = UIKit.VStack(face.transform, 16, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(40, 40, 34, 30));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            UIKit.Label(col.transform, $"Cilj: {map.Finish} · {race.Round} rundi", 26, Theme.InkSoft).Size(-1, 40);
            var t = UIKit.Label(col.transform, winner.Human ? "Pobjeda!" : $"{winner.Name} je prvi stigao", 64, winner.Human ? Theme.Go : Theme.Ink, TextAnchor.MiddleLeft, true);
            t.Size(-1, 90);
            UIKit.Label(col.transform, winner.Human ? "Stigao si prvi do cilja." : $"Ti si {place + 1}. na cilju. Probaj ponovo!", 30, Theme.InkSoft).Size(-1, 50);
            var rewards = UIKit.HStack(col.transform, 18, "Nagrade");
            rewards.Size(-1, 110);
            Reward(rewards.transform, $"+{award.Pts}", "bodova za rang", Theme.Blue);
            Reward(rewards.transform, $"+{award.Coins}", "novčića", Theme.Gold);
            if (award.RankUp != null) Banner(col.transform, $"Novi rang: {award.RankUp.Label}!", Col.Hex(award.RankUp.Color));
            foreach (var m in award.NewMaps) Banner(col.transform, $"Otključana mapa: {m.Name}", Theme.Go);
            int i = 1;
            foreach (var r in ranked)
            {
                var row = UIKit.HStack(col.transform, 12, "Igrač");
                row.Size(-1, 52);
                UIKit.Label(row.transform, $"{i}.", 30, Theme.Ink, TextAnchor.MiddleLeft, true).Size(50, 52);
                UIKit.Box(row.transform, Col.Hex(r.Color), UIKit.Circle, "Boja").Size(24, 24);
                UIKit.Label(row.transform, r.Human ? Lang.L("Ti") : r.Name, 28, Theme.Ink, TextAnchor.MiddleLeft, r.Human).Size(220, 52);
                UIKit.Label(row.transform, $"{r.Correct}/{r.Answered} dobrih · {r.Coins} novčića", 24, Theme.InkSoft).Size(-1, 52, 1);
                UIKit.Label(row.transform, r.Pos >= race.Last ? "cilj" : $"{race.Last - r.Pos} do cilja", 24, Theme.InkSoft, TextAnchor.MiddleRight).Size(150, 52);
                i++;
            }
            var spacer = UIKit.Rect(col.transform, "Razmak");
            spacer.Size(-1, 10, 1, 1);
            var bar = UIKit.HStack(col.transform, 16, "Dugmad", TextAnchor.MiddleRight);
            bar.Size(-1, 110);
            UIKit.Button(bar.transform, "Meni", Theme.InkSoft, menu, 34).Size(240, 100);
            UIKit.Button(bar.transform, "Nova trka", Theme.Go, again, 38).Size(340, 100);

            // lijevo dolje: profil
            var left = UIKit.VStack(parent, 10, "Profil", TextAnchor.LowerLeft, new RectOffset(40, 0, 0, 40));
            ((RectTransform)left.transform).Anchor(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(820, 220));
            MenuScreen.ProfileCard(left.transform, p);
        }

        static void Reward(Transform parent, string big, string small, Color c)
        {
            var b = UIKit.Box(parent, c, UIKit.Round, "Nagrada", 0.8f);
            b.Size(300, 104);
            var v = UIKit.VStack(b.transform, 0, "Tekst", TextAnchor.MiddleCenter);
            ((RectTransform)v.transform).Stretch(0, 8, 0, 8);
            UIKit.Label(v.transform, big, 48, Color.white, TextAnchor.MiddleCenter, true).Size(-1, 56);
            UIKit.Label(v.transform, small, 24, Color.white, TextAnchor.MiddleCenter).Size(-1, 30);
        }

        static void Banner(Transform parent, string text, Color c)
        {
            var b = UIKit.Box(parent, c, UIKit.Pill, "Traka", 1f);
            b.Size(-1, 70);
            UIKit.Label(b.transform, text, 32, Color.white, TextAnchor.MiddleCenter, true).rectTransform.Stretch();
        }
    }
}
