// Ekrani: podešavanja, mini igra za sve (najava, igra, rezultati) i ekran na cilju (postolje, nagrade, novi rang).
// Meni je u MenuScreen.cs, a garaža u GarageScreen.cs.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public static class SettingsScreen
    {
        public static void Build(RectTransform parent, Profile p, Action<string> changed, Action close)
        {
            UIKit.Clear(parent);
            UIRoot.I.Dim(parent, 0.5f);
            var face = UIKit.GlassCard(parent, "Podešavanja", new Color(1, 1, 1, 0.9f));
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

    // Stanica ili bonus runda: najava, mini igra, rezultati svih igrača.
    public static class MinigameScreen
    {
        public static IEnumerator Run(RectTransform parent, Minigame mg, int seconds, string chip, Color chipColor, string rule,
            Rng rng, List<StationOutcome> resultsOut, Func<double, string, List<StationOutcome>> computeResults,
            Func<double, string, IEnumerator> exchange = null)
        {
            UIKit.Clear(parent);
            mg.Prepare();
            Hud.ShowSide(false);
            var face = UIKit.GlassCard(parent, "Mini igra", new Color(1, 1, 1, 0.72f));
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -42), new Vector2(QuestionScreen.CardWidth, -146));
            var col = UIKit.VStack(face.transform, 18, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(40, 40, 30, 30));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);
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
            Hud.ShowSide(true);
        }
    }

    public static class ResultsScreen
    {
        public static void Build(RectTransform parent, Race race, List<Racer> ranked, Profile.Award award, RaceMap map, Profile p, Action again, Action menu)
        {
            UIKit.Clear(parent);
            Hud.ShowAll(false);   // na cilju HUD trke više ne treba (prekrio bi karticu)
            var me = race.Human;
            int place = ranked.IndexOf(me);
            var winner = ranked[0];
            var face = UIKit.GlassCard(parent, "Cilj", new Color(1, 1, 1, 0.8f));
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(880, -80));
            var col = UIKit.VStack(face.transform, 16, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(40, 40, 34, 30));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);
            UIKit.Label(col.transform, $"Cilj: {map.Finish} · {race.Round} {Fmt.Plural(race.Round, "runda", "runde", "rundi")}", 26, Theme.InkSoft).Size(-1, 40);
            var t = UIKit.Label(col.transform, winner.Human ? "Pobjeda!" : $"{winner.Name} je prvi stigao", 64, winner.Human ? Theme.Go : Theme.Ink, TextAnchor.MiddleLeft, true);
            t.Size(-1, 90);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 34; t.resizeTextMaxSize = 64; t.verticalOverflow = VerticalWrapMode.Truncate;
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
            var prof = MenuScreen.ProfilePill(parent, p);
            prof.Anchor(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(MenuScreen.LeftWidth + 60, 128));
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
