// Kartica sa pitanjem preko 3D staze: oblast, tip, težina, tajmer, power-upovi, tijelo pitanja (QuestionViews),
// presuda, zanimljivost, tabla runde (svi igrači) i dugme „Kreni”.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class QuestionScreen : MonoBehaviour
    {
        Question q;
        QuestionView view;
        RectTransform card, body, foot, puBar;
        Image timerFill;
        Text timerNum;
        float limit, elapsed, frozen;
        bool finished, go;
        readonly HashSet<string> used = new HashSet<string>();
        public QuizResult Result { get; private set; }
        public Func<string, bool> OnUsePowerup;   // vraća true ako je power-up potrošen
        GameButton goBtn;
        public string TutorialHint;

        int roundNo;
        RectTransform timerRow;

        // Širina kartice: što šira na širokim ekranima, ali da tabla runde (620) uvijek stane pored nje.
        public static float CardWidth
        {
            get
            {
                float w = UIRoot.I != null ? UIRoot.I.Safe.rect.width : 1920;
                if (w <= 0) w = 1920;
                return Mathf.Clamp(w - 620 - 72, 1080, 1500);
            }
        }

        // Stanje igrača za ovo pitanje: predmet iz kutije, zbunjenost (napad), dupla runda i dupla polja.
        public sealed class Extras
        {
            public string Item;
            public bool Confused, DoubleRound, DoubleNext;
        }

        public static QuestionScreen Open(RectTransform parent, Question question, int round, Rng rng, IList<(Powerup pu, int count)> powerups, Extras extras = null)
        {
            var root = UIKit.Rect(parent, "Pitanje").Stretch();
            var s = root.gameObject.AddComponent<QuestionScreen>();
            s.q = question;
            s.roundNo = round;
            s.limit = (float)QuizTypes.TimeLimitOf(question);
            s.extras = extras ?? new Extras();
            if (s.extras.Confused) s.limit = Mathf.Max(5, Mathf.Round(s.limit * 0.6f));
            s.Build(round, rng, powerups);
            return s;
        }

        Extras extras;

        void Build(int round, Rng rng, IList<(Powerup pu, int count)> powerups)
        {
            // kartica je u sredini; kad se pojavi tabla runde, ide ulijevo, a tabla desno (širina se računa da obje stanu)
            Hud.ShowSide(false);
            var face = UIKit.Card(transform, "Kartica");
            card = (RectTransform)face.rectTransform.parent;
            card.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(CardWidth, -150));
            var col = UIKit.VStack(face.transform, 14, "Sadržaj", TextAnchor.UpperLeft, new RectOffset(40, 40, 28, 28));
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            FitInside.Add((RectTransform)col.transform);

            // zaglavlje
            var head = UIKit.HStack(col.transform, 12, "Zaglavlje");
            head.Size(-1, 52);
            var area = Catalog.AreaOf(q.Oblast);
            UIKit.Chip(head.transform, area.Label, Col.Hex(area.Color), 24);
            UIKit.Chip(head.transform, QuizTypes.Get(q.Tip).Label, Theme.InkSoft, 24);
            for (int i = 0; i < 3; i++)
            {
                var d = UIKit.Box(head.transform, i < q.Tezina ? Theme.Gold : Theme.Line, UIKit.Circle, "Težina");
                d.Size(22, 22);
            }
            if (extras.DoubleRound) UIKit.Chip(head.transform, "×2 runda", Theme.Gold, 24);
            else if (extras.DoubleNext) UIKit.Chip(head.transform, "Dupla polja", Theme.Gold, 24);
            if (extras.Confused) UIKit.Chip(head.transform, "Zbunjen: manje vremena", Theme.Red, 24);

            // tajmer
            var trow = UIKit.HStack(col.transform, 14, "Tajmer");
            trow.Size(-1, 40);
            timerRow = (RectTransform)trow.transform;
            var bar = UIKit.Bar(trow.transform, Theme.Line, Theme.Go, out timerFill, 20);
            bar.Size(-1, 20, 1);
            timerNum = UIKit.Label(trow.transform, ((int)limit).ToString(), 32, Theme.Ink, TextAnchor.MiddleRight, true);
            timerNum.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerNum.Size(70, 40);

            // power-upovi i predmet iz kutije
            bool attack = extras.Item != null && Items.IsAttack(extras.Item);
            if (powerups.Count > 0 || extras.Item != null)
            {
                // dugmad dijele širinu reda (tekst se po potrebi smanji), da nikad ne izađu iz kartice
                var pb = UIKit.HStack(col.transform, 12, "Power-upovi");
                pb.Size(-1, 70);
                pb.childForceExpandWidth = true;
                puBar = (RectTransform)pb.transform;
                if (attack)
                {
                    var item = extras.Item;
                    var ab = UIKit.Button(pb.transform, "Napad: " + Items.Label(item), Theme.Red, null, 26);
                    ab.Size(-1, 66, 1.4f);
                    ab.OnClick += () =>
                    {
                        if (finished || used.Contains(Items.UsePrefix + item)) return;
                        used.Add(Items.UsePrefix + item);
                        ab.Interactable = false;
                        UIRoot.I.Toast(Items.Hint(item), Theme.Red, 2.5f);
                    };
                }
                else if (extras.Item == Items.Shield) UIKit.Chip(pb.transform, "Štit te čuva", Theme.Blue, 24);
                foreach (var (pu, count) in powerups)
                {
                    var p = pu;
                    var b = UIKit.Button(pb.transform, $"{pu.Label}  x{count}", Theme.Purple, null, 26);
                    b.Size(-1, 66, 1);
                    b.OnClick += () => UsePowerup(p, b);
                }
            }

            // pitanje
            var qt = UIKit.Label(col.transform, q.Pitanje, 44, Theme.Ink, TextAnchor.MiddleLeft, true);
            qt.Size(-1, -1).minHeight = 70;
            qt.FitHeight().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            body = UIKit.Rect(col.transform, "Tijelo");
            body.Size(-1, 260, 1, 1);

            foot = (RectTransform)UIKit.VStack(col.transform, 10, "Podnožje").transform;
            foot.gameObject.SetActive(false);

            view = QuestionView.For(q.Tip);
            view.Mount(q, body, rng, this, Finish);

            // ulazna animacija
            StartCoroutine(Tw.Run(0.3f, k => card.localScale = Vector3.one * (0.92f + 0.08f * Tw.EaseOutBack(k)), true));
            if (!string.IsNullOrEmpty(TutorialHint)) UIRoot.I.Toast(TutorialHint, Theme.Gold, 4);
        }

        void UsePowerup(Powerup pu, GameButton b)
        {
            if (finished || used.Contains(pu.Id)) return;
            if (pu.Kind == "fifty" && !view.CanFifty) { UIRoot.I.Toast("50/50 radi samo na pitanjima sa ponuđenim odgovorima.", Theme.Purple); return; }
            if (OnUsePowerup != null && !OnUsePowerup(pu.Id)) return;
            used.Add(pu.Id);
            b.Interactable = false;
            if (pu.Kind == "fifty") view.Fifty();
            if (pu.Kind == "freeze") frozen = 5;
            UIRoot.I.Toast(pu.Label + "!", Theme.Purple, 1.5f);
        }

        void Update()
        {
            if (finished) return;
            float dt = Time.unscaledDeltaTime;
            if (frozen > 0) { frozen -= dt; timerFill.color = Theme.Blue; }
            else { elapsed += dt; timerFill.color = limit - elapsed < 5 ? Theme.Red : Theme.Go; }
            float left = Mathf.Max(0, limit - elapsed);
            UIKit.SetFill(timerFill, left / limit);
            timerNum.text = Mathf.CeilToInt(left).ToString();
            view.Tick(elapsed, limit);
            ReadKeys();
            if (left <= 0) Finish(view.Timeout(), true);
        }

        void ReadKeys()
        {
            try
            {
                for (int n = 1; n <= 4; n++)
                    if (Input.GetKeyDown(KeyCode.Alpha0 + n) || Input.GetKeyDown(KeyCode.Keypad0 + n)) view.OnKey(n);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) view.OnConfirmKey();
            }
            catch (InvalidOperationException) { /* uključen je samo novi Input System; tastatura preko UI-ja i dalje radi */ }
        }

        void Finish(QuizResult r) => Finish(r, false);

        void Finish(QuizResult r, bool timedOut)
        {
            if (finished) return;
            finished = true;
            view.Lock();
            r.TimedOut = timedOut;
            r.Limit = limit;
            r.TimeUsed = timedOut ? limit : Mathf.Min(limit, elapsed);
            r.Accuracy = Mathx.Clamp(r.Accuracy, 0, 1);
            r.Tiles = QuizTypes.TilesFor(r.Accuracy, r.TimeUsed, limit);
            r.UsedPowerups.AddRange(used);
            Result = r;
            view.Reveal(r);
            if (r.Accuracy >= 0.2) AudioKit.I?.Correct(); else AudioKit.I?.Wrong();
            Haptics.Tap(r.Accuracy >= 0.75 ? 20 : 60);
            if (puBar != null) puBar.gameObject.SetActive(false);
        }

        void OnDestroy() => Hud.ShowSide(true);

        public IEnumerator WaitAnswer() { while (!finished) yield return null; }

        // Presuda, zanimljivost i tabla runde; vraća se kad igrač tapne „Kreni”.
        public IEnumerator ShowBoard(List<RoundEntry> entries)
        {
            yield return Tw.Wait(0.9f, true);
            foot.gameObject.SetActive(true);
            var r = Result;
            var good = r.Accuracy >= 0.75 ? Theme.Right : r.Accuracy >= 0.2 ? Theme.Gold : Theme.Wrong;
            var row = UIKit.HStack(foot, 16, "Presuda");
            row.Size(-1, 84);
            var v = UIKit.Box(row.transform, good, UIKit.Pill, "Presuda", 1f);
            v.Size(-1, 76, 1);
            var vt = UIKit.Label(v.transform, QuizTypes.Verdict(r) + "   " + (r.Tiles > 0 ? $"+{r.Tiles} {(r.Tiles == 1 ? "polje" : "polja")}" : "bez pomaka"), 34, Color.white, TextAnchor.MiddleLeft, true);
            vt.rectTransform.Stretch(30, 0, 20, 0);
            goBtn = UIKit.Button(row.transform, "Kreni", Theme.Primary, () => go = true, 38);
            goBtn.Size(240, 84);
            // tajmer više ne treba: oslobodi mjesto za presudu
            if (timerRow != null) timerRow.gameObject.SetActive(false);
            // tabla runde: posebna kartica desno, a pitanje se pomjeri ulijevo (oba se vide); zanimljivost ide na tablu
            bool hasInfo = !string.IsNullOrEmpty(q.Info);
            var boardFace = UIKit.Card(transform, "Tabla runde", new Color(1, 1, 1, 0.97f));
            var boardRt = (RectTransform)boardFace.rectTransform.parent;
            float safeH = UIRoot.I.Safe.rect.height > 0 ? UIRoot.I.Safe.rect.height : 1080;
            boardRt.Anchor(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 36), new Vector2(620, Mathf.Min(hasInfo ? 700 : 520, safeH - 160)));
            var bcol = UIKit.VStack(boardFace.transform, 10, "Redovi", TextAnchor.UpperLeft, new RectOffset(26, 26, 22, 22));
            ((RectTransform)bcol.transform).Stretch();
            bcol.childForceExpandHeight = false;
            FitInside.Add((RectTransform)bcol.transform);
            UIKit.Label(bcol.transform, $"Runda {roundNo}", 30, Theme.InkSoft, TextAnchor.MiddleLeft, true).Size(-1, 44);
            foreach (var e in Race.SortForBoard(entries))
            {
                var cell = UIKit.Box(bcol.transform, e.Racer.Human ? Col.Hex("#fff3d6") : Theme.PaperSoft, UIKit.RoundSmall, "Igrač", 0.8f);
                cell.Size(-1, 92);
                var hr = UIKit.HStack(cell.transform, 12, "Red", TextAnchor.MiddleLeft, new RectOffset(16, 16, 0, 0));
                ((RectTransform)hr.transform).Stretch();
                UIKit.Box(hr.transform, Col.Hex(e.Racer.Color), UIKit.Circle, "Boja").Size(24, 24);
                var txt = UIKit.VStack(hr.transform, 0, "Tekst");
                txt.Size(-1, 88, 1);
                txt.childAlignment = TextAnchor.MiddleLeft;
                UIKit.Label(txt.transform, e.Racer.Human ? Lang.L("Ti") : e.Racer.Name, 28, Theme.Ink, TextAnchor.MiddleLeft, e.Racer.Human).Size(-1, 40);
                UIKit.Label(txt.transform, e.Text + "  ·  " + Fmt.Sec(e.Time), 24, Theme.InkSoft).Size(-1, 34);
                var t = UIKit.Label(hr.transform, e.Tiles > 0 ? "+" + e.Tiles : "0", 44, e.Tiles >= 3 ? Theme.Right : e.Tiles > 0 ? Theme.Ink : Theme.Wrong, TextAnchor.MiddleRight, true);
                t.Size(80, 88);
            }
            if (hasInfo)
            {
                var info = UIKit.Label(bcol.transform, q.Info, 26, Theme.InkSoft);
                info.horizontalOverflow = HorizontalWrapMode.Wrap;
                info.Size(-1, 150);
                info.resizeTextForBestFit = true; info.resizeTextMinSize = 20; info.resizeTextMaxSize = 26; info.verticalOverflow = VerticalWrapMode.Truncate;
            }
            var from = card.anchoredPosition;
            float safeW = UIRoot.I.Safe.rect.width > 0 ? UIRoot.I.Safe.rect.width : 1920;
            float toX = -safeW / 2 + 24 + card.sizeDelta.x / 2;
            StartCoroutine(Tw.Run(0.35f, k =>
            {
                card.anchoredPosition = Vector2.Lerp(from, new Vector2(toX, from.y), Tw.EaseInOut(k));
                boardRt.localScale = Vector3.one * Tw.EaseOutBack(k);
            }, true));
            go = false;
            float wait = 0;
            while (!go)
            {
                wait += Time.unscaledDeltaTime;
                try { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) go = true; } catch (InvalidOperationException) { }
                if (App.AutoPlay && wait > 0.3f) go = true;
                yield return null;
            }
        }

        public void Close()
        {
            Hud.ShowSide(true);
            view?.Dispose();
            Destroy(gameObject);
        }

        // Za automatske testove: odgovori kao da je igrač tapnuo.
        public void AutoAnswer(Rng rng)
        {
            if (finished) return;
            var r = view.Timeout();
            if (q.Tip == "izbor" || q.Tip == "mutna" || q.Tip == "melodija")
            {
                var pick = rng.Chance(0.7) ? q.TacnoText : rng.Pick(q.Opcije);
                r = new QuizResult { Accuracy = QuizTypes.ScoreChoice(q, pick), Chosen = pick };
            }
            else r.Accuracy = rng.Next();
            if (extras.Item != null && Items.IsAttack(extras.Item) && rng.Chance(0.7)) used.Add(Items.UsePrefix + extras.Item);
            elapsed = limit * 0.4f;
            Finish(r, false);
        }
    }
}
