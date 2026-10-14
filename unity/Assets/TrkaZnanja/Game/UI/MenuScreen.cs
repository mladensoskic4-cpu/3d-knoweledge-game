// Glavni meni: u sredini tvoj lik u vozilu na postolju (3D, tap → garaža), lijevo velika dugmad
// (Igraj, Sa drugima, Prijatelji, Rang-lista), desno staklena kartica „Tvoja trka” (svijet, oblasti, dužina,
// botovi, uzrast), gore profil i novčići, a podešavanja na zupčaniku.
using System;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class MenuChoice
    {
        public bool Start, Garage, Settings, Leaderboard, Multiplayer;
        public string Friends;          // "soba" = napravi sobu odmah, "kod" = uđi kodom
        public bool LookChanged;        // promijenjen lik (dobrodošlica): osvježi izlog i sačuvaj
        public string MapChanged;
    }

    public static class MenuScreen
    {
        public const float LeftWidth = 520, RightWidth = 640;

        public static void Build(RectTransform parent, Profile p, IDictionary<string, int> areaCounts, Action<MenuChoice> onChoice)
        {
            UIKit.Clear(parent);
            var S = p.Podesavanja;
            var root = UIKit.Rect(parent, "Meni").Stretch();

            // tap na lik (sredina ekrana, iza svega ostalog) → garaža
            var heroTap = UIKit.Box(root, new Color(1, 1, 1, 0.001f), null, "Lik (tap = garaža)", 1f);
            heroTap.rectTransform.Stretch(LeftWidth + 60, 200, RightWidth + 60, 120);
            heroTap.gameObject.AddComponent<TapArea>().OnTap = _ => onChoice(new MenuChoice { Garage = true });

            // ---- gore: naslov ----
            var title = UIKit.Label(root, "Trka znanja", 96, Color.white, TextAnchor.MiddleCenter, true);
            title.Outlined(Col.Hex("#1b4f8a"), 5);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((LeftWidth - RightWidth) / 4, -22), new Vector2(760, 120));

            // ---- gore lijevo: profil ----
            var prof = ProfilePill(root, p);
            prof.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -26), new Vector2(LeftWidth, 128));

            // ---- gore desno: novčići i podešavanja ----
            var gear = UIKit.Button(root, "", Theme.InkSoft, () => onChoice(new MenuChoice { Settings = true }), 30);
            gear.SetIcon("podesavanja", 54);
            ((RectTransform)gear.transform).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -26), new Vector2(96, 96));
            var coins = CoinPill(root, p.Novcici);
            coins.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-146, -32), new Vector2(240, 84));

            // ---- lijevo: velika dugmad ----
            var left = UIKit.VStack(root, 20, "Dugmad", TextAnchor.UpperLeft);
            ((RectTransform)left.transform).Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -190), new Vector2(LeftWidth, 640));
            left.childForceExpandHeight = false;
            UIKit.Button(left.transform, "Igraj", Theme.Go, () => onChoice(new MenuChoice { Start = true }), 72).SetIcon("igraj", 76).Size(-1, 176);
            UIKit.Button(left.transform, "Sa drugima", Theme.Primary, () => onChoice(new MenuChoice { Multiplayer = true }), 40).SetIcon("ljudi", 58).Size(-1, 112);
            UIKit.Button(left.transform, "Prijatelji", Theme.Blue, () => Friends(parent, onChoice), 40).SetIcon("prijatelj", 58).Size(-1, 112);
            UIKit.Button(left.transform, "Rang-lista", Theme.Gold, () => onChoice(new MenuChoice { Leaderboard = true }), 40).SetIcon("pehar", 58).Size(-1, 112);
            if (!Net.Online)
            {
                var off = UIKit.Label(left.transform, "Bez interneta: igraš protiv botova, rezultati se šalju kasnije.", 24, Color.white);
                off.Outlined(new Color(0, 0, 0, 0.5f), 2);
                off.Size(-1, 64);
            }

            // ---- dolje u sredini: garaža ----
            var garage = UIKit.Button(root, "Garaža · uredi lik i vozilo", Theme.Purple, () => onChoice(new MenuChoice { Garage = true }), 32);
            garage.SetIcon("auto", 46);
            ((RectTransform)garage.transform).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((LeftWidth - RightWidth) / 2, 34), new Vector2(520, 96));

            // ---- desno: postavke trke (na prvom pokretanju umjesto nje dobrodošlica) ----
            var race = RaceCard(root, p, areaCounts, onChoice);
            if (string.IsNullOrEmpty(p.Ime) && !App.AutoPlay)
            {
                race.gameObject.SetActive(false);
                Welcome(root, p, onChoice, () => race.gameObject.SetActive(true));
            }
        }

        // ---------- dobrodošlica: ime, životinja i vozilo (prvo pokretanje) ----------
        static void Welcome(RectTransform root, Profile p, Action<MenuChoice> onChoice, Action done)
        {
            var face = UIKit.GlassCard(root, "Dobrodošlica", new Color(1, 1, 1, 0.94f));
            var holder = (RectTransform)face.rectTransform.parent;
            holder.Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-32, -45), new Vector2(RightWidth, -250));
            var col = UIKit.VStack(face.transform, 14, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(32, 32, 26, 26));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);
            UIKit.Label(col.transform, "Dobro došao!", 48, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 64);
            UIKit.Label(col.transform, "Napravi svoj lik. Sve ostalo (boje, šare, nalijepnice) uređuješ u garaži.", 26, Theme.InkSoft).FitHeight();
            Section(col.transform, "Kako se zoveš?", "prijatelj");
            string typed = "";
            UIKit.Input(col.transform, "", "Tvoje ime", 12, v => typed = v);

            Section(col.transform, "Životinja", "sapa");
            var arow = UIKit.HStack(col.transform, 12, "Životinje", TextAnchor.MiddleLeft);
            arow.Size(-1, 110);
            arow.childForceExpandWidth = true;
            var animals = new List<GameButton>();
            foreach (var a in Catalog.Animals.Where(x => x.Price == 0 && string.IsNullOrEmpty(x.Rank)))
            {
                var id = a.Id;
                var b = UIKit.Button(arow.transform, a.Name, Theme.Blue, null, 26);
                b.Tag = id;
                b.SetIcon("sapa", 46, true);
                b.Size(-1, 106, 1);
                b.SetSelected(p.Izgled.Zivotinja == id);
                b.OnClick += () =>
                {
                    p.Izgled.Zivotinja = id;
                    foreach (var x in animals) x.SetSelected(x.Tag == id);
                    onChoice(new MenuChoice { LookChanged = true });
                };
                animals.Add(b);
            }

            Section(col.transform, "Vozilo", "auto");
            var vrow = UIKit.HStack(col.transform, 12, "Vozila", TextAnchor.MiddleLeft);
            vrow.Size(-1, 110);
            vrow.childForceExpandWidth = true;
            var vehicles = new List<GameButton>();
            foreach (var v in Catalog.Vehicles.Where(x => x.Price == 0 && string.IsNullOrEmpty(x.Rank)))
            {
                var id = v.Id;
                var b = UIKit.Button(vrow.transform, v.Name, Theme.Primary, null, 26);
                b.Tag = id;
                b.SetIcon("auto", 46, true);
                b.Size(-1, 106, 1);
                b.SetSelected(p.Izgled.Vozilo == id);
                b.OnClick += () =>
                {
                    p.Izgled.Vozilo = id;
                    foreach (var x in vehicles) x.SetSelected(x.Tag == id);
                    onChoice(new MenuChoice { LookChanged = true });
                };
                vehicles.Add(b);
            }

            var go = UIKit.Button(col.transform, "Spremno!", Theme.Go, null, 40);
            go.SetIcon("kvacica", 44);
            go.Size(-1, 100);
            go.OnClick += () =>
            {
                var name = (typed ?? "").Trim();
                if (name.Length < 2) { UIRoot.I.Toast("Upiši ime (bar 2 slova).", Theme.Red); return; }
                p.Ime = name;
                onChoice(new MenuChoice { LookChanged = true, MapChanged = null, Settings = false });
                UnityEngine.Object.Destroy(holder.gameObject);
                done();
                var prof = root.Find("Profil");
                if (prof != null) UnityEngine.Object.Destroy(prof.gameObject);
                ProfilePill(root, p).Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -26), new Vector2(LeftWidth, 128));
            };
        }

        // ---------- profil (ime, rang, napredak) ----------
        public static RectTransform ProfilePill(Transform parent, Profile p)
        {
            var face = UIKit.GlassCard(parent, "Profil");
            var holder = (RectTransform)face.rectTransform.parent;
            var rank = p.Rank;
            var prog = Catalog.RankProgress(p.Bodovi);
            var row = UIKit.HStack(face.transform, 18, "Red", TextAnchor.MiddleLeft, new RectOffset(18, 24, 14, 14));
            ((RectTransform)row.transform).Stretch();
            var badge = UIKit.Box(row.transform, Col.Hex(rank.Color), UIKit.Circle, "Rang");
            badge.Size(96, 96);
            UIKit.Label(badge.transform, rank.Label.Substring(0, 1), 52, Color.white, TextAnchor.MiddleCenter, true).rectTransform.Stretch();
            var mid = UIKit.VStack(row.transform, 6, "Napredak");
            mid.Size(-1, 100, 1);
            mid.childAlignment = TextAnchor.MiddleLeft;
            var name = string.IsNullOrEmpty(p.Ime) ? Lang.L("Igrač") : p.Ime;
            var nameT = UIKit.Label(mid.transform, $"{name} · {Lang.L(rank.Label)}", 34, Theme.Ink, TextAnchor.MiddleLeft, true);
            nameT.Size(-1, 44);
            nameT.resizeTextForBestFit = true; nameT.resizeTextMinSize = 22; nameT.resizeTextMaxSize = 34; nameT.verticalOverflow = VerticalWrapMode.Truncate;
            UIKit.Bar(mid.transform, new Color(0, 0, 0, 0.12f), Col.Hex(rank.Color), out var fill, 14).Size(-1, 14);
            UIKit.SetFill(fill, (float)prog.pct);
            var pt = UIKit.Label(mid.transform, prog.next != null ? $"{p.Bodovi} bodova · još {prog.need} do ranga {prog.next.Label}" : $"{p.Bodovi} bodova · najviši rang", 24, Theme.InkSoft);
            pt.Size(-1, 32);
            pt.resizeTextForBestFit = true; pt.resizeTextMinSize = 18; pt.resizeTextMaxSize = 24; pt.verticalOverflow = VerticalWrapMode.Truncate;
            return holder;
        }

        public static RectTransform CoinPill(Transform parent, int coins)
        {
            var face = UIKit.GlassCard(parent, "Novčići");
            var holder = (RectTransform)face.rectTransform.parent;
            var row = UIKit.HStack(face.transform, 10, "Red", TextAnchor.MiddleCenter, new RectOffset(14, 20, 8, 8));
            ((RectTransform)row.transform).Stretch();
            UIKit.IconImage(row.transform, "novcic", Theme.Gold, 54);
            var t = UIKit.Label(row.transform, coins.ToString(), 38, Theme.Ink, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.Size(-1, 60, 1);
            return holder;
        }

        // ---------- desna kartica: svijet, oblasti, dužina, botovi, uzrast ----------
        static RectTransform RaceCard(RectTransform root, Profile p, IDictionary<string, int> areaCounts, Action<MenuChoice> onChoice)
        {
            var S = p.Podesavanja;
            var face = UIKit.GlassCard(root, "Tvoja trka");
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-32, -45), new Vector2(RightWidth, -250));
            var col = UIKit.VStack(face.transform, 12, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(30, 30, 24, 26));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);

            UIKit.Label(col.transform, "Tvoja trka", 42, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 60);

            // svijet (mape)
            Section(col.transform, "Svijet", "globus");
            var maps = UIKit.Grid(col.transform, new Vector2((RightWidth - 60 - 14) / 2, 92), new Vector2(14, 12), 2, "Mape");
            maps.Size(-1, 196);
            var mapButtons = new List<GameButton>();
            foreach (var m in Catalog.Maps)
            {
                var map = m;
                bool open = p.MapUnlocked(m);
                var rank = Catalog.Ranks.First(r => r.Id == m.Rank);
                var b = UIKit.Button(maps.transform, m.Name, open ? Theme.Blue : Theme.InkSoft, null, 32);
                if (!open)
                {
                    b.Label.text = Lang.L(m.Name) + "\n<size=20>" + Lang.L($"rang {rank.Label}") + "</size>";
                    b.SetIcon("katanac", 34);
                }
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

            // oblasti (otvara izbor preko cijelog ekrana)
            Section(col.transform, "Oblasti", "lupa");
            var areasBtn = UIKit.Button(col.transform, AreasSummary(S, areaCounts), Theme.Primary, null, 30);
            areasBtn.Size(-1, 80);
            areasBtn.SetSelected(true);
            areasBtn.OnClick += () => AreaPicker((RectTransform)root.parent, S, areaCounts, () => areasBtn.SetText(AreasSummary(S, areaCounts)));

            Section(col.transform, "Dužina trke", "sat");
            UIKit.Segmented(col.transform, Catalog.Lengths.Select(l => (l.Id, l.Label)).ToList(), S.Duzina, id => { S.Duzina = id; onChoice(new MenuChoice { MapChanged = S.Mapa }); }, 28, 70);

            Section(col.transform, "Botovi", "auto");
            UIKit.Segmented(col.transform, Bots.Levels.Values.Select(l => (l.Id, l.Label)).ToList(), S.Nivo, id => S.Nivo = id, 28, 70);

            Section(col.transform, "Uzrast", "ljudi");
            var ageNote = UIKit.Label(col.transform, Catalog.AgeOf(S.Uzrast).Note, 22, Theme.InkSoft);
            UIKit.Segmented(col.transform, Catalog.Ages.Select(a => (a.Id, a.Label)).ToList(), S.Uzrast, id => { S.Uzrast = id; ageNote.text = Lang.L(Catalog.AgeOf(id).Note); }, 28, 70);
            ageNote.transform.SetAsLastSibling();
            ageNote.Size(-1, 30);
            return (RectTransform)face.rectTransform.parent;
        }

        static void Section(Transform parent, string title, string icon)
        {
            var row = UIKit.HStack(parent, 10, "Naslov " + title, TextAnchor.LowerLeft);
            row.Size(-1, 40);
            UIKit.IconImage(row.transform, icon, Theme.InkSoft, 28);
            UIKit.Label(row.transform, title, 28, Theme.Ink, TextAnchor.LowerLeft, true).Size(-1, 40, 1);
        }

        static string AreasSummary(GameSettings S, IDictionary<string, int> counts)
        {
            var avail = Catalog.Areas.Where(a => counts.ContainsKey(a.Id)).ToList();
            if (S.Miks || S.Oblasti == null || S.Oblasti.Count == 0) return Lang.L($"Miks · sve oblasti ({avail.Count})");
            return string.Join(", ", S.Oblasti.Select(id => Lang.L(Catalog.AreaOf(id).Label)));
        }

        // ---------- izbor oblasti (preko cijelog ekrana) ----------
        static void AreaPicker(RectTransform screen, GameSettings S, IDictionary<string, int> counts, Action changed)
        {
            var layer = UIKit.Rect(screen, "Izbor oblasti").Stretch();
            UIRoot.I.Dim(layer, 0.45f);
            var face = UIKit.GlassCard(layer, "Oblasti", new Color(1, 1, 1, 0.9f));
            ((RectTransform)face.rectTransform.parent).Center(new Vector2(1180, 800));
            var col = UIKit.VStack(face.transform, 16, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(44, 44, 34, 34));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);
            UIKit.Label(col.transform, "Izaberi oblasti", 52, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 70);
            var hint = UIKit.Label(col.transform, "", 28, Theme.InkSoft);
            hint.Size(-1, 40);
            var avail = Catalog.Areas.Where(a => counts.ContainsKey(a.Id)).ToList();
            var picked = new HashSet<string>((S.Oblasti ?? new List<string>()).Where(counts.ContainsKey));
            bool mix = S.Miks || picked.Count == 0;
            var grid = UIKit.Grid(col.transform, new Vector2(350, 96), new Vector2(18, 16), 3, "Oblasti");
            grid.Size(-1, Mathf.CeilToInt((avail.Count + 1) / 3f) * 112);
            var buttons = new Dictionary<string, GameButton>();
            GameButton mixBtn = null;
            void Refresh()
            {
                S.Miks = mix;
                S.Oblasti = mix ? null : picked.ToList();
                mixBtn.SetSelected(mix);
                foreach (var kv in buttons) kv.Value.SetSelected(!mix && picked.Contains(kv.Key));
                hint.text = Lang.L(mix ? $"Pitanja iz svih oblasti ({avail.Count})." : $"Izabrano {picked.Count} od 3.");
            }
            mixBtn = UIKit.Button(grid.transform, "Miks", Theme.Primary, () => { mix = true; picked.Clear(); Refresh(); }, 30);
            foreach (var a in avail)
            {
                var id = a.Id;
                buttons[id] = UIKit.Button(grid.transform, a.Label, Col.Hex(a.Color), () =>
                {
                    if (mix) { mix = false; picked.Clear(); }
                    if (picked.Contains(id)) picked.Remove(id); else if (picked.Count < 3) picked.Add(id);
                    if (picked.Count == 0) mix = true;
                    Refresh();
                }, 28);
            }
            Refresh();
            var done = UIKit.Button(col.transform, "Gotovo", Theme.Go, () => { changed(); UnityEngine.Object.Destroy(layer.gameObject); }, 40);
            done.SetIcon("kvacica", 46);
            done.Size(-1, 100);
        }

        // ---------- prijatelji: soba sa kodom (lista prijatelja stiže uskoro) ----------
        static void Friends(RectTransform screen, Action<MenuChoice> onChoice)
        {
            var layer = UIKit.Rect(screen, "Prijatelji").Stretch();
            UIRoot.I.Dim(layer, 0.45f);
            var face = UIKit.GlassCard(layer, "Prijatelji", new Color(1, 1, 1, 0.9f));
            ((RectTransform)face.rectTransform.parent).Center(new Vector2(980, 640));
            var col = UIKit.VStack(face.transform, 20, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(48, 48, 38, 38));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);
            var head = UIKit.HStack(col.transform, 16, "Naslov", TextAnchor.MiddleLeft);
            head.Size(-1, 80);
            UIKit.IconImage(head.transform, "prijatelj", Theme.Blue, 64);
            UIKit.Label(head.transform, "Igraj sa prijateljima", 52, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 80, 1);
            UIKit.Label(col.transform, "Napravi sobu i pošalji prijateljima kod od 5 znakova (npr. preko WhatsApp-a). Kad svi uđu, ti pokrećeš trku. Prazna mjesta popunjavaju botovi.", 30, Theme.InkSoft).FitHeight();
            var row = UIKit.HStack(col.transform, 20, "Dugmad", TextAnchor.MiddleCenter);
            row.Size(-1, 130);
            row.childForceExpandWidth = true;
            UIKit.Button(row.transform, "Napravi sobu", Theme.Go, () => { UnityEngine.Object.Destroy(layer.gameObject); onChoice(new MenuChoice { Multiplayer = true, Friends = "soba" }); }, 40).SetIcon("plus", 48).Size(-1, 124, 1);
            UIKit.Button(row.transform, "Uđi kodom", Theme.Purple, () => { UnityEngine.Object.Destroy(layer.gameObject); onChoice(new MenuChoice { Multiplayer = true, Friends = "kod" }); }, 40).SetIcon("prijatelj", 48).Size(-1, 124, 1);
            UIKit.Label(col.transform, "Uskoro: lista prijatelja, poziv jednim klikom i ko je trenutno u igri.", 24, Theme.InkSoft).FitHeight();
            var close = UIKit.Button(face.transform, "", Theme.InkSoft, () => UnityEngine.Object.Destroy(layer.gameObject), 30);
            close.SetIcon("zatvori", 40);
            ((RectTransform)close.transform).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(76, 76));
        }
    }
}
