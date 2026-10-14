// Mini igre na stanicama i u bonus rundama (11 igara, iste kao u web verziji). Svaka vraća rezultat 0..1 i kratak opis.
// Ovdje je osnova i „Zastave u letu”; igre sa 4 ponuđena odgovora su u MinigamesChoice.cs, a ostale u MinigamesSpecial.cs.
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
    public abstract partial class Minigame
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Intro(int seconds);
        public abstract int Duration { get; }
        public double Score { get; protected set; }
        public string ResultLabel { get; protected set; }
        public abstract IEnumerator Play(RectTransform host, Rng rng, int seconds);
        public abstract (double score, string label) SimulateBot(double skill, Rng rng);

        // igra sama (AutoPlay trke ili test pojedinačne mini igre)
        public static bool TestAuto;
        protected static bool Auto => App.AutoPlay || TestAuto;

        // false = nema podataka (npr. fotografija), pa se igra preskače
        public virtual bool Ready => true;
        // poziva se kad se pokaže uputstvo (npr. da se mapa svijeta nacrta unaprijed)
        public virtual void Prepare() { }

        public static readonly List<Minigame> All = new List<Minigame>
        {
            new FlyingFlags(), new Capitals(), new InstrumentsGame(), new AnimalsGame(),
            PhotoChoiceGame.Places(), new WorldPhotoMap(), PhotoChoiceGame.Body(), new FlagMemory(), new MateInOne(),
            new ShapeGame(), new WorldMapGame(),
        };
        static readonly List<Minigame> queue = new List<Minigame>();

        public static Minigame ById(string id) => All.FirstOrDefault(m => m.Id == id);

        // Smjenjivanje: svaka mini igra dođe na red prije nego što se neka ponovi.
        public static Minigame Pick(Rng rng)
        {
            var ready = All.Where(m => m.Ready).ToList();
            queue.RemoveAll(m => !ready.Contains(m));
            if (queue.Count == 0) queue.AddRange(rng.Shuffle(ready));
            var pick = queue[0];
            queue.RemoveAt(0);
            return pick;
        }

        protected static (double, string) Simulate(double skill, Rng rng, int goal, string unit)
        {
            var score = Mathx.Clamp(skill * 0.95 + rng.Gaussian() * 0.15, 0, 1);
            return (score, $"{Math.Round(score * goal)} {unit}");
        }
    }

    public sealed class FlyingFlags : Minigame
    {
        const int Goal = 7;
        public override string Id => "zastave";
        public override string Name => "Zastave u letu";
        public override int Duration => 20;
        public override string Intro(int s) => $"Čim se pojavi naziv države, njena zastava poleti preko ekrana. Tapni je prije nego što izađe, jer se ne vraća. Pogrešna zastava oduzima pola boda. Imaš {s} sekundi.";

        sealed class Flyer { public RectTransform Rt; public Country C; public float X, Y, Speed; public int Dir; public bool Gone; }

        public override (double score, string label) SimulateBot(double skill, Rng rng) => Simulate(skill, rng, Goal, "zastava");

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var countries = GameData.Countries.Where(c => GameData.Flag(c.Code) != null).ToList();
            if (countries.Count < 4) { Score = 0.5; ResultLabel = "—"; yield break; }
            var col = UIKit.VStack(host, 12, "Zastave", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            var trow = UIKit.HStack(col.transform, 14, "Tajmer");
            trow.Size(-1, 34);
            var bar = UIKit.Bar(trow.transform, Theme.Line, Theme.Go, out var fill, 20);
            bar.Size(-1, 20, 1);
            var status = UIKit.Label(trow.transform, "", 28, Theme.Ink, TextAnchor.MiddleRight, true);
            status.Size(320, 34);
            var prompt = UIKit.Box(col.transform, Theme.Ink, UIKit.Pill, "Zadatak", 1f);
            prompt.Size(-1, 84);
            var promptText = UIKit.Label(prompt.transform, "", 40, Color.white, TextAnchor.MiddleCenter, true);
            promptText.rectTransform.Stretch(20, 0, 20, 0);
            var field = UIKit.Box(col.transform, Col.Hex("#dff1fb"), UIKit.Round, "Nebo", 0.8f);
            field.Size(-1, 380, 1, 1);
            var fieldRt = field.rectTransform;
            fieldRt.gameObject.AddComponent<RectMask2D>();

            const int lanes = 4;
            var flyers = new List<Flyer>();
            int hits = 0, misses = 0;
            Country target = null;
            Flyer targetFlyer = null;
            var bag = new List<Country>();
            float t = 0, lastDecoy = 0;

            Flyer Spawn(Country c, int lane, float speedK)
            {
                var w = fieldRt.rect.width; var h = fieldRt.rect.height;
                var img = UIKit.Raw(fieldRt, GameData.Flag(c.Code), "Zastava " + c.Code);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.sizeDelta = new Vector2(152, 114);   // zastave su 4:3
                img.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.25f);
                int dir = rng.Chance(0.5) ? 1 : -1;
                var f = new Flyer { Rt = rt, C = c, Dir = dir, X = dir > 0 ? -160 : w + 10, Y = -(lane + 0.5f) * (h / lanes), Speed = (w + 160) / (3.6f + (float)rng.Next() * 1.4f) * speedK };
                var tap = img.gameObject.AddComponent<PointerDownHandler>();
                tap.Down = () => Tap(f);
                flyers.Add(f);
                return f;
            }

            void NewTarget()
            {
                if (bag.Count == 0) bag.AddRange(rng.Shuffle(countries.ToList()));
                target = bag[bag.Count - 1];
                bag.RemoveAt(bag.Count - 1);
                promptText.text = Lang.L("Tapni:") + "  " + Lang.L(target.Name);
                int lane = rng.Range(0, lanes);
                targetFlyer = Spawn(target, lane, 1);
                foreach (var l in rng.Shuffle(Enumerable.Range(0, lanes).Where(x => x != lane).ToList()).Take(2))
                    Spawn(rng.Pick(countries), l, 0.9f + (float)rng.Next() * 0.3f);
            }

            void Tap(Flyer f)
            {
                if (f.Gone || t >= seconds) return;
                if (f == targetFlyer)
                {
                    hits++; f.Gone = true;
                    AudioKit.I?.Correct();
                    UnityEngine.Object.Destroy(f.Rt.gameObject);
                    NewTarget();
                }
                else
                {
                    misses++;
                    AudioKit.I?.Wrong();
                    Haptics.Tap(40);
                    f.Rt.GetComponent<RawImage>().color = new Color(1, 0.6f, 0.6f);
                }
            }

            yield return null;    // raspored polja
            NewTarget();
            while (t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                UIKit.SetFill(fill, 1 - t / seconds);
                status.text = Lang.L($"{hits} pogodaka · {Mathf.CeilToInt(seconds - t)} s");
                if (t - lastDecoy > 0.9f) { lastDecoy = t; Spawn(rng.Pick(countries), rng.Range(0, lanes), 0.9f + (float)rng.Next() * 0.3f); }
                var w = fieldRt.rect.width;
                bool targetLeft = false;
                foreach (var f in flyers)
                {
                    if (f.Gone) continue;
                    f.X += f.Dir * f.Speed * dt;
                    f.Rt.anchoredPosition = new Vector2(f.X, f.Y);
                    if ((f.Dir > 0 && f.X > w) || (f.Dir < 0 && f.X < -160))
                    {
                        f.Gone = true;
                        UnityEngine.Object.Destroy(f.Rt.gameObject);
                        if (f == targetFlyer) targetLeft = true;
                    }
                }
                flyers.RemoveAll(f => f.Gone);
                if (targetLeft) { misses++; NewTarget(); }
                if (Auto && rng.Chance(dt * 1.2)) Tap(targetFlyer);
                yield return null;
            }
            foreach (var f in flyers) if (f.Rt != null) UnityEngine.Object.Destroy(f.Rt.gameObject);
            promptText.text = Lang.L($"{hits} pogodaka, {misses} promašaja");
            Score = Mathx.Clamp((hits - misses * 0.5) / (Goal * seconds / (double)Duration), 0, 1);
            ResultLabel = $"{hits} zastava";
        }
    }

    public sealed class PointerDownHandler : MonoBehaviour, IPointerDownHandler
    {
        public Action Down;
        public void OnPointerDown(PointerEventData e) => Down?.Invoke();
    }
}
