// Ekran „Igraj sa drugima”: brza igra (nasumični igrači istog uzrasta), napravi sobu ili uđi kodom.
// U sobi se vidi ko je tu; domaćin pokreće trku, a prazna mjesta popunjavaju botovi.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public static class MultiplayerScreen
    {
        public static void Open(RectTransform parent, Profile p, MonoBehaviour runner, Func<MpSession, RaceSetup> makeSetup,
            Action<MpSession, RaceSetup> onStart, Action back)
        {
            UIKit.Clear(parent);
            var face = UIKit.Card(parent, "Sa drugima");
            ((RectTransform)face.rectTransform.parent).Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 820));
            var col = UIKit.VStack(face.transform, 20, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(48, 48, 40, 40));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;

            bool cancelled = false;
            MpSession session = null;

            void Title(string t, string sub)
            {
                UIKit.Clear(col.transform);
                UIKit.Label(col.transform, t, 60, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 84);
                if (sub != null) UIKit.Label(col.transform, sub, 28, Theme.InkSoft).FitHeight();
            }

            void Choice()
            {
                cancelled = true;
                session?.Leave();
                session = null;
                Title("Igraj sa drugima", "Brza igra te spaja sa nasumičnim igračima tvog uzrasta. Sa prijateljima: napravi sobu i pošalji im kod. Prazna mjesta popunjavaju botovi.");
                if (!RealtimeChannel.Supported)
                {
                    UIKit.Label(col.transform, "Igra sa drugima radi u aplikaciji za telefon i računar, ne u web verziji.", 30, Theme.Red).FitHeight();
                    UIKit.Button(col.transform, "Nazad", Theme.InkSoft, () => back(), 34).Size(260, 90);
                    return;
                }
                var row = UIKit.HStack(col.transform, 18, "Dugmad");
                row.Size(-1, 130);
                row.childForceExpandWidth = true;
                UIKit.Button(row.transform, "Brza igra", Theme.Go, Quick, 44).Size(-1, 130, 1);
                UIKit.Button(row.transform, "Napravi sobu", Theme.Blue, () => Room(RoomCode.New(new Rng()), true), 40).Size(-1, 130, 1);

                UIKit.Label(col.transform, "Imaš kod od prijatelja?", 32, Theme.Ink, TextAnchor.LowerLeft, true).Size(-1, 60);
                var codeRow = UIKit.HStack(col.transform, 16, "Kod");
                codeRow.Size(-1, 100);
                string typed = "";
                var input = UIKit.Input(codeRow.transform, "", "Kod sobe (5 znakova)", 8, v => typed = v);
                input.Size(-1, 96, 1);
                var err = UIKit.Label(col.transform, "", 26, Theme.Red);
                UIKit.Button(codeRow.transform, "Uđi", Theme.Purple, () =>
                {
                    var code = RoomCode.Normalize(typed);
                    if (code == null) { err.text = Lang.L("Kod ima 5 slova i brojeva, npr. K7RTX."); return; }
                    Room(code, false);
                }, 38).Size(220, 96);
                err.Size(-1, 40);
                UIKit.Button(col.transform, "Nazad", Theme.InkSoft, () => back(), 34).Size(260, 90);
            }

            void Quick()
            {
                cancelled = false;
                Title("Brza igra", null);
                var status = UIKit.Label(col.transform, "Povezujem se…", 34, Theme.Ink);
                status.Size(-1, 120);
                UIKit.Label(col.transform, "Čim se skupe 4 igrača, trka kreće. Ako čekaš duže od 15 sekundi, krećemo i sa manje (ostalo su botovi).", 26, Theme.InkSoft).FitHeight();
                UIKit.Button(col.transform, "Otkaži", Theme.Red, Choice, 34).Size(260, 90);
                runner.StartCoroutine(MpSession.QuickMatch(p, runner.transform, s => { if (status != null) status.text = Lang.L(s); },
                    s => { session = s; RoomView(true); }, () => cancelled));
            }

            void Room(string code, bool created)
            {
                cancelled = false;
                session = MpSession.Join(code, p, runner.transform);
                RoomView(false);
            }

            void RoomView(bool quick)
            {
                var s = session;
                Title(quick ? "Brza igra" : "Soba za prijatelje", quick ? "Našli smo igrače! Trka kreće čim svi uđu." : "Pošalji prijateljima ovaj kod da uđu u sobu.");
                if (!quick)
                {
                    var big = UIKit.Box(col.transform, Theme.PaperSoft, UIKit.Round, "Kod", 0.8f);
                    big.Size(-1, 120);
                    var t = UIKit.Label(big.transform, s.Code, 80, Theme.Blue, TextAnchor.MiddleCenter, true);
                    t.rectTransform.Stretch();
                    t.text = s.Code;   // kod se ne prevodi ni ne pretvara u ćirilicu
                }
                var list = UIKit.VStack(col.transform, 10, "Igrači");
                list.Size(-1, 300);
                list.childForceExpandHeight = false;
                var status = UIKit.Label(col.transform, "Povezujem se…", 28, Theme.InkSoft);
                status.Size(-1, 50);
                var bar = UIKit.HStack(col.transform, 16, "Dugmad");
                bar.Size(-1, 100);
                GameButton start = null;
                if (!quick) start = UIKit.Button(bar.transform, "Kreni", Theme.Go, () => { if (s.IsHost && s.Connected) s.StartRace(makeSetup(s)); }, 40);
                start?.Size(360, 96);
                UIKit.Button(bar.transform, "Izađi", Theme.Red, Choice, 34).Size(240, 96);

                void Refresh()
                {
                    if (list == null) return;
                    UIKit.Clear(list.transform);
                    var members = s.Members.Count > 0 ? s.Members : new List<MpMember> { new MpMember { Id = s.LocalId, Name = string.IsNullOrEmpty(p.Ime) ? "Ti" : p.Ime } };
                    for (int i = 0; i < Math.Min(RaceSetup.Seats, members.Count); i++)
                    {
                        var m = members[i];
                        var cell = UIKit.Box(list.transform, m.Id == s.LocalId ? Col.Hex("#fff3d6") : Theme.PaperSoft, UIKit.RoundSmall, "Igrač", 0.8f);
                        cell.Size(-1, 66);
                        var r = UIKit.HStack(cell.transform, 14, "Red", TextAnchor.MiddleLeft, new RectOffset(20, 20, 0, 0));
                        ((RectTransform)r.transform).Stretch();
                        r.childForceExpandWidth = false;
                        r.childControlWidth = true;
                        UIKit.Box(r.transform, Col.Hex(RaceSetup.SeatColors[i]), UIKit.Circle, "Boja").Size(26, 26, 0, 0);
                        UIKit.Label(r.transform, m.Name + (m.Id == s.LocalId ? " (" + Lang.L("ti") + ")" : ""), 30, Theme.Ink, TextAnchor.MiddleLeft, true).Size(-1, 66, 1);
                        if (m.Id == s.HostId) UIKit.Chip(r.transform, "domaćin", Theme.Gold, 22).Size(150, 44, 0, 0);
                    }
                    for (int i = members.Count; i < RaceSetup.Seats; i++)
                    {
                        var cell = UIKit.Box(list.transform, new Color(0, 0, 0, 0.04f), UIKit.RoundSmall, "Prazno", 0.8f);
                        cell.Size(-1, 56);
                        var l = UIKit.Label(cell.transform, "slobodno mjesto (bot)", 26, Theme.InkSoft, TextAnchor.MiddleLeft);
                        l.rectTransform.Stretch(66, 0, 20, 0);
                    }
                    if (start != null)
                    {
                        start.Interactable = s.IsHost && s.Connected;
                        start.gameObject.SetActive(s.IsHost);
                    }
                    if (s.Connected)
                        status.text = Lang.L(s.IsHost ? (quick ? "Čekam da svi uđu…" : "Ti si domaćin: pokreni trku kad su svi tu.") : "Čeka se da domaćin pokrene trku.");
                }
                s.OnMembers += Refresh;
                s.OnProblem += msg => { if (status != null) status.text = Lang.L(msg); };
                s.OnStart += setup => onStart(s, setup);
                Refresh();
                runner.StartCoroutine(Watch());

                IEnumerator Watch()
                {
                    float t = 0;
                    bool wasConnected = false;
                    while (session == s && s != null && s.Setup == null)
                    {
                        if (s.Connected && !wasConnected) { wasConnected = true; Refresh(); }
                        // brza igra: domaćin sam pokreće kad uđu svi iz grupe (ili poslije 10 s sa onima koji su tu)
                        if (quick && s.IsHost && s.Connected)
                        {
                            bool all = s.Group == null || s.Group.All(id => s.Present(id));
                            if (all || t > 10) { s.StartRace(makeSetup(s)); yield break; }
                        }
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }

            Choice();
        }
    }
}
