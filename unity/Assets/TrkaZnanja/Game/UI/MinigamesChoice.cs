// Mini igre sa 4 ponuđena odgovora: Glavni gradovi, Koji je ovo instrument? (zvuk i fotografija),
// Koja je ovo životinja?, Gdje je ovo?, Koji je ovo dio tijela? i Pogodi državu po obliku.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    public sealed class Capitals : Minigame
    {
        const int Goal = 11;
        public override string Id => "gradovi";
        public override string Name => "Glavni gradovi";
        public override int Duration => 25;
        public override string Intro(int s) => $"Pojavljuje se država, a ti biraš njen glavni grad. Odgovori na što više za {s} sekundi. Greška oduzima pola boda.";
        public override bool Ready => GameData.Countries.Count(c => !string.IsNullOrEmpty(c.Capital)) >= 8;
        public override (double score, string label) SimulateBot(double skill, Rng rng) => Simulate(skill, rng, Goal, "gradova");

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var list = GameData.Countries.Where(c => !string.IsNullOrEmpty(c.Capital)).ToList();
            var bag = new List<Country>();
            return ChoiceRounds(host, rng, seconds, Goal, "gradova", () =>
            {
                if (bag.Count == 0) bag.AddRange(rng.Shuffle(list.ToList()));
                var c = bag[bag.Count - 1];
                bag.RemoveAt(bag.Count - 1);
                // pogrešni odgovori: prvo glavni gradovi sa istog kontinenta
                var same = rng.Shuffle(list.Where(x => x != c && x.Continent == c.Continent).Select(x => x.Capital).ToList());
                var rest = rng.Shuffle(list.Where(x => x != c && x.Continent != c.Continent).Select(x => x.Capital).ToList());
                var wrong = same.Concat(rest).Where(x => x != c.Capital).Distinct().Take(3);
                var opts = rng.Shuffle(new List<string> { c.Capital }.Concat(wrong).ToList());
                return new ChoiceRound
                {
                    Options = opts, Answer = c.Capital,
                    Stage = st =>
                    {
                        var box = UIKit.Box(st, Theme.PaperSoft, UIKit.Round, "Država", 0.8f);
                        box.rectTransform.Stretch();
                        var flag = UIKit.Raw(box.transform, GameData.Flag(c.Code), "Zastava");
                        flag.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(330, 220));
                        var arf = flag.gameObject.AddComponent<AspectRatioFitter>();
                        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                        arf.aspectRatio = flag.texture != null ? flag.texture.width / (float)flag.texture.height : 1.5f;
                        flag.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.2f);
                        var name = UIKit.Label(box.transform, c.Name, 52, Theme.Ink, TextAnchor.MiddleCenter, true);
                        name.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(-40, 90));
                        name.resizeTextForBestFit = true; name.resizeTextMinSize = 26; name.resizeTextMaxSize = 52;
                    },
                };
            });
        }
    }

    // Fotografija + 4 ponuđena odgovora iz iste grupe (kontinent / sistem / grupa / vrsta).
    public class PhotoChoiceGame : Minigame
    {
        readonly string id, name, intro, kat, unit;
        readonly int goal, duration;
        public PhotoChoiceGame(string id, string name, string intro, string kat, int goal, int duration = 25, string unit = "tačnih")
        { this.id = id; this.name = name; this.intro = intro; this.kat = kat; this.goal = goal; this.duration = duration; this.unit = unit; }

        public static PhotoChoiceGame Places() => new PhotoChoiceGame("mjesta", "Gdje je ovo?",
            "Pojavljuje se fotografija poznatog mjesta ili spomenika. Izaberi koje je to mjesto. Ako ne znaš, preskoči (bez kazne). Što više tačnih za {0} sekundi.", "mjesta", 10, 45);
        public static PhotoChoiceGame Body() => new PhotoChoiceGame("tijelo", "Koji je ovo dio tijela?",
            "Pojavljuje se slika dijela ljudskog tijela. Izaberi koji je. Što više tačnih za {0} sekundi.", "tijelo", 9, 35);

        public override string Id => id;
        public override string Name => name;
        public override int Duration => duration;
        public override string Intro(int s) => string.Format(intro, s);
        public override bool Ready => MinigameData.Photos(kat).Count >= 6;
        public override (double score, string label) SimulateBot(double skill, Rng rng) => Simulate(skill, rng, goal, unit);

        protected List<PhotoItem> Bag(Rng rng, List<PhotoItem> bag)
        {
            if (bag.Count == 0) bag.AddRange(rng.Shuffle(MinigameData.Photos(kat).ToList()));
            return bag;
        }

        protected ChoiceRound PhotoRound(Rng rng, List<PhotoItem> bag)
        {
            var list = MinigameData.Photos(kat);
            Texture2D tex = null;
            PhotoItem it = null;
            for (int k = 0; k < 5 && tex == null; k++)
            {
                Bag(rng, bag);
                it = bag[bag.Count - 1];
                bag.RemoveAt(bag.Count - 1);
                tex = MinigameData.Photo(it);
            }
            var opts = Options.Similar(rng, it, list, x => x.Group, x => x.Display);
            return new ChoiceRound { Options = opts, Answer = it.Display, Stage = st => PhotoFrame(st, it, tex) };
        }

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var bag = new List<PhotoItem>();
            return ChoiceRounds(host, rng, seconds, goal, unit, () => PhotoRound(rng, bag));
        }
    }

    public sealed class AnimalsGame : PhotoChoiceGame
    {
        public AnimalsGame() : base("zivotinje", "Koja je ovo životinja?",
            "Pojavljuje se fotografija životinje. Izaberi koja je. Što više tačnih za {0} sekundi, greška oduzima pola boda.", "zivotinje", 9, 32) { }
    }

    // Naizmjenično zvuk instrumenta (4 note, FluidR3 GM, CC BY 3.0) i fotografija instrumenta.
    public sealed class InstrumentsGame : PhotoChoiceGame
    {
        public InstrumentsGame() : base("instrumenti", "Koji je ovo instrument?",
            "Naizmjenično čuješ zvuk instrumenta ili vidiš njegovu fotografiju. Izaberi koji je instrument. Što više tačnih za {0} sekundi.", "instrumenti", 8, 32) { }

        public override bool Ready => MinigameData.Instruments.Count >= 6 || base.Ready;

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var bag = new List<PhotoItem>();
            var sounds = MinigameData.Instruments;
            var sbag = new List<InstrumentSound>();
            bool photos = MinigameData.Photos("instrumenti").Count >= 6;
            int turn = 0;
            return ChoiceRounds(host, rng, seconds, 7, "tačnih", () =>
            {
                bool sound = sounds.Count >= 6 && AudioKit.I != null && AudioKit.I.SfxOn && (turn % 2 == 0 || !photos);
                turn++;
                if (!sound) return PhotoRound(rng, bag);
                if (sbag.Count == 0) sbag.AddRange(rng.Shuffle(sounds.ToList()));
                var s = sbag[sbag.Count - 1];
                sbag.RemoveAt(sbag.Count - 1);
                void PlayIt() => AudioKit.I?.PlayNotes(MinigameData.NotesFor(s));
                return new ChoiceRound
                {
                    Options = Options.Pick(rng, s.Name, sounds.Select(x => x.Name).ToList()), Answer = s.Name,
                    Stage = st => SoundStage(st, PlayIt),
                    OnShow = PlayIt,
                    OnDone = () => AudioKit.I?.StopNotes(),
                };
            });
        }

        static void SoundStage(RectTransform st, System.Action replay)
        {
            var box = UIKit.Box(st, Col.Hex("#1f2b3a"), UIKit.Round, "Zvuk", 0.8f);
            box.rectTransform.Stretch();
            var eq = UIKit.HStack(box.transform, 14, "Ekvilajzer", TextAnchor.MiddleCenter);
            ((RectTransform)eq.transform).Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(420, 180));
            eq.childControlHeight = false; eq.childForceExpandHeight = false;
            var bars = new List<RectTransform>();
            for (int i = 0; i < 9; i++)
            {
                var b = UIKit.Box(eq.transform, Color.Lerp(Theme.Gold, Theme.Red, i / 8f), UIKit.Pill, "Stub", 1f);
                b.Size(28, 60);
                bars.Add(b.rectTransform);
            }
            box.gameObject.AddComponent<EqualizerAnim>().Bars = bars;
            var hint = UIKit.Label(box.transform, "Slušaj: šta je ovo?", 30, Color.white, TextAnchor.MiddleCenter, true);
            hint.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(-40, 50));
            var again = UIKit.Button(box.transform, "Pusti ponovo", Theme.Purple, () => replay(), 28);
            ((RectTransform)again.transform).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 84));
        }
    }

    public sealed class EqualizerAnim : MonoBehaviour
    {
        public List<RectTransform> Bars;
        void Update()
        {
            if (Bars == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < Bars.Count; i++)
            {
                if (Bars[i] == null) continue;
                float h = 40 + 120 * Mathf.Abs(Mathf.Sin(t * (3.1f + i * 0.7f) + i * 1.3f)) * (0.6f + 0.4f * Mathf.Sin(t * 1.7f + i));
                Bars[i].sizeDelta = new Vector2(Bars[i].sizeDelta.x, h);
            }
        }
    }

    // Država zeleno, susjedi blijedo i more oko nje; sljedeći obris se crta unaprijed u pozadini.
    public sealed class ShapeGame : Minigame
    {
        const int Goal = 9, TexW = 680, TexH = 500;
        public override string Id => "oblik";
        public override string Name => "Pogodi državu po obliku";
        public override int Duration => 28;
        public override string Intro(int s) => $"Pojavljuje se država (zeleno) sa susjedima i morem oko nje. Izaberi koja je to država. Što više tačnih za {s} sekundi, greška oduzima pola boda.";
        public override bool Ready => MinigameData.World.Count > 20;
        public override (double score, string label) SimulateBot(double skill, Rng rng) => Simulate(skill, rng, Goal, "tačnih");

        static string Display(WorldCountry c) => Lang.Mode == Lang.En && !string.IsNullOrEmpty(c.En) ? c.En : c.Name;

        static Task<Canvas32> Render(WorldCountry d)
        {
            var all = MinigameData.World; var land = MinigameData.Land;
#if UNITY_WEBGL && !UNITY_EDITOR
            return Task.FromResult(WorldPainter.Shape(d, all, land, TexW, TexH));
#else
            return Task.Run(() => WorldPainter.Shape(d, all, land, TexW, TexH));
#endif
        }

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            // bez sasvim sitnih država (obris im se ne vidi)
            var list = MinigameData.World.Where(d => d.Area > 8000).ToList();
            var bag = rng.Shuffle(list.ToList());
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false) { name = "Obris države", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            WorldCountry Take() { if (bag.Count == 0) bag.AddRange(rng.Shuffle(list.ToList())); var d = bag[bag.Count - 1]; bag.RemoveAt(bag.Count - 1); return d; }
            var nextD = Take();
            var nextJob = Render(nextD);
            var routine = ChoiceRounds(host, rng, seconds, Goal, "tačnih", () =>
            {
                var d = nextD;
                var cv = nextJob.Result;
                nextD = Take();
                nextJob = Render(nextD);
                MinigameData.Upload(tex, cv);
                var same = rng.Shuffle(list.Where(x => x != d && x.Continent == d.Continent).ToList());
                var rest = rng.Shuffle(list.Where(x => x != d && x.Continent != d.Continent).ToList());
                var opts = rng.Shuffle(new List<WorldCountry> { d }.Concat(same.Concat(rest).Take(3)).Select(Display).ToList());
                return new ChoiceRound
                {
                    Options = opts, Answer = Display(d),
                    Stage = st =>
                    {
                        var frame = UIKit.Box(st, Col.Hex("#9fd3ec"), UIKit.Round, "Okvir", 0.8f);
                        frame.rectTransform.Stretch();
                        frame.gameObject.AddComponent<RectMask2D>();
                        var img = UIKit.Raw(frame.transform, tex, "Obris");
                        img.rectTransform.Stretch(6, 6, 6, 6);
                        var arf = img.gameObject.AddComponent<AspectRatioFitter>();
                        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                        arf.aspectRatio = TexW / (float)TexH;
                        img.raycastTarget = false;
                    },
                };
            });
            while (routine.MoveNext()) yield return routine.Current;
            Object.Destroy(tex);
        }
    }
}
