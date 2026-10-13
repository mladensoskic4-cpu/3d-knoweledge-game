// Tipovi pitanja: vrijeme, bodovanje (0..1), simulacija bota i kratak opis rezultata za tablu runde.
// Ekran (Unity UI) za svaki tip je u Game/UI/Questions; ovdje je samo logika, da se može testirati.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TrkaZnanja.Core
{
    public sealed class QuizResult
    {
        public double Accuracy, TimeUsed, Limit;
        public bool TimedOut;
        public int Tiles;
        public List<string> UsedPowerups = new List<string>();
        // detalji (zavisno od tipa)
        public string Chosen;
        public double? Value, Km;
        public int Good, Total;
        public List<string> Order;
    }

    public sealed class QuizType
    {
        public string Id, Label;
        public double TimeLimit;
        public bool HasOptions;                       // 50/50 radi samo za ove
        public Func<Question, double, Rng, double> SimulateBot;
        public Func<Question, QuizResult, string> Summarize;
    }

    public static class QuizTypes
    {
        public const string MultipleChoice = "izbor";
        public const double MultipleChoiceShareMax = 0.4;
        public const int MaxTiles = 4;

        static readonly Dictionary<string, QuizType> registry = new Dictionary<string, QuizType>();
        public static IEnumerable<QuizType> All => registry.Values;
        public static QuizType Get(string id) => id != null && registry.TryGetValue(id, out var t) ? t : null;

        static string RightWrong(Question q, QuizResult r) => r.Accuracy >= 1 ? "tačno" : "netačno";
        static double Coin(Question q, double p, Rng rng) => rng.Next() < p ? 1 : 0;
        static double Spread(Question q, double p, Rng rng) => Mathx.Clamp(p * 1.05 + rng.Gaussian() * 0.2, 0, 1);

        static QuizTypes()
        {
            Add(new QuizType { Id = "izbor", Label = "Višestruki izbor", TimeLimit = 15, HasOptions = true, SimulateBot = Coin, Summarize = RightWrong });
            Add(new QuizType { Id = "mutna", Label = "Mutna slika", TimeLimit = 15, HasOptions = true, SimulateBot = Coin, Summarize = RightWrong });
            Add(new QuizType { Id = "melodija", Label = "Prepoznaj melodiju", TimeLimit = 20, HasOptions = true, SimulateBot = Coin, Summarize = RightWrong });
            Add(new QuizType
            {
                Id = "procjena", Label = "Procijeni broj", TimeLimit = 20,
                SimulateBot = (q, p, rng) =>
                {
                    var err = rng.Gaussian() * q.Tolerancija * (1.1 - p) * 0.75;
                    return Math.Max(0, 1 - Math.Abs(err) / q.Tolerancija);
                },
                Summarize = (q, r) => r.Value.HasValue
                    ? $"{FormatEstimate(q, r.Value.Value)} ({Math.Round(r.Accuracy * 100)}%)"
                    : $"{Math.Round(r.Accuracy * 100)}% tačno"
            });
            Add(new QuizType
            {
                Id = "poredaj", Label = "Poredaj", TimeLimit = 25, SimulateBot = Spread,
                Summarize = (q, r) => r.Accuracy >= 1 ? "sve tačno" : $"{Math.Round(r.Accuracy * 100)}%"
            });
            Add(new QuizType
            {
                Id = "spoji", Label = "Spoji parove", TimeLimit = 25,
                SimulateBot = (q, p, rng) =>
                {
                    int n = q.Parovi.Count, good = 0;
                    for (int i = 0; i < n; i++) if (rng.Next() < p) good++;
                    return n == 0 ? 0 : (double)good / n;
                },
                Summarize = (q, r) => r.Total > 0
                    ? $"{r.Good}/{r.Total} parova"
                    : $"{Math.Round(r.Accuracy * q.Parovi.Count)}/{q.Parovi.Count} parova"
            });
            Add(new QuizType
            {
                Id = "mapa", Label = "Tapni na mapi", TimeLimit = 20, SimulateBot = Spread,
                Summarize = (q, r) => r.Km.HasValue ? $"{Fmt.Num(Math.Round(r.Km.Value))} km" : $"{Math.Round(r.Accuracy * 100)}% tačno"
            });
        }

        static void Add(QuizType t) => registry[t.Id] = t;

        public static double TimeLimitOf(Question q) => q.Vrijeme ?? Get(q.Tip)?.TimeLimit ?? 15;

        // Brz i tačan odgovor = do 4 polja, spor tačan = 2, netačan = 0.
        public static int TilesFor(double accuracy, double timeUsed, double limit)
        {
            if (accuracy < 0.2) return 0;
            var speed = 1 - Mathx.Clamp(timeUsed / limit, 0, 1);
            var s = accuracy * (0.55 + 0.45 * speed);
            return Mathx.Clamp((int)Math.Round(s * MaxTiles, MidpointRounding.AwayFromZero), 1, MaxTiles);
        }

        public static string Verdict(QuizResult r)
        {
            if (r.TimedOut && r.Accuracy < 0.2) return "Isteklo vrijeme";
            if (r.Accuracy >= 0.999) return "Tačno!";
            if (r.Accuracy >= 0.75) return "Odlično, blizu!";
            if (r.Accuracy >= 0.2) return "Djelimično";
            return "Netačno";
        }

        // ---- bodovanje po tipu ----
        public static double ScoreChoice(Question q, string chosen) => chosen != null && chosen == q.TacnoText ? 1 : 0;

        public static double ScoreEstimate(Question q, double value) => Math.Max(0, 1 - Math.Abs(value - q.TacnoBroj) / q.Tolerancija);

        public static double SnapEstimate(Question q, double v)
        {
            var step = q.Korak > 0 ? q.Korak : 1;
            var snapped = Math.Round(v / step) * step;
            snapped = Mathx.Clamp(snapped, q.Min, q.Max);
            return Math.Round(snapped, Fmt.DecimalsOf(step));
        }

        public static string FormatEstimate(Question q, double v)
        {
            var s = Fmt.Num(v, Fmt.DecimalsOf(q.Korak), q.Format != "godina");
            return q.Format == "godina" ? s + "." : s;
        }

        public static string EstimateUnit(Question q) => q.Format == "godina" ? "godina" : q.Jedinica ?? "";

        // Udio parova u tačnom redoslijedu; pola tačnih parova = 0 (kao nasumično), sve = 1.
        public static double ScoreOrder(IList<string> order, IList<string> correct)
        {
            var pos = new Dictionary<string, int>();
            for (int i = 0; i < correct.Count; i++) pos[correct[i]] = i;
            int ok = 0, total = 0;
            for (int i = 0; i < order.Count; i++)
                for (int j = i + 1; j < order.Count; j++)
                {
                    total++;
                    if (pos.TryGetValue(order[i], out var a) && pos.TryGetValue(order[j], out var b) && a < b) ok++;
                }
            if (total == 0) return 0;
            var c = (double)ok / total;
            return c >= 1 ? 1 : Math.Max(0, (c - 0.5) * 2);
        }

        // links: lijevi pojam → izabrani desni
        public static (double accuracy, int good) ScoreMatch(Question q, IDictionary<string, string> links)
        {
            int good = 0;
            foreach (var p in q.Parovi)
                if (links.TryGetValue(p.Key, out var r) && r == p.Value) good++;
            return (q.Parovi.Count == 0 ? 0 : (double)good / q.Parovi.Count, good);
        }

        public static double ScoreMap(Question q, double km)
        {
            var cfg = GeoMaps.Get(q.Mapa);
            double full = q.TolPun ?? cfg.Full, zero = q.TolNula ?? cfg.Zero;
            return km <= full ? 1 : km >= zero ? 0 : 1 - (km - full) / (zero - full);
        }

        // ---- natpisi za krajeve kod „poredaj” (izvedeni iz teksta pitanja) ----
        static readonly (Regex rx, string[] sr, string[] en)[] Ends =
        {
            (new Regex("najmanj|smallest|least|fewest", RegexOptions.IgnoreCase), new[] { "najmanji", "najveći" }, new[] { "smallest", "largest" }),
            (new Regex(@"najveć|largest|biggest|most\b", RegexOptions.IgnoreCase), new[] { "najveći", "najmanji" }, new[] { "largest", "smallest" }),
            (new Regex("najranij|earliest", RegexOptions.IgnoreCase), new[] { "najraniji", "najkasniji" }, new[] { "earliest", "latest" }),
            (new Regex("najstarij|hronolo|godini|redu izla|premijer|oldest|chronolog|release|year", RegexOptions.IgnoreCase), new[] { "najstariji", "najnoviji" }, new[] { "oldest", "newest" }),
            (new Regex("najbliž|closest|nearest", RegexOptions.IgnoreCase), new[] { "najbliži", "najdalji" }, new[] { "closest", "farthest" }),
            (new Regex("najviš|highest|tallest", RegexOptions.IgnoreCase), new[] { "najviši", "najniži" }, new[] { "highest", "lowest" }),
            (new Regex("najduž|longest", RegexOptions.IgnoreCase), new[] { "najduži", "najkraći" }, new[] { "longest", "shortest" }),
            (new Regex("najkrać|shortest", RegexOptions.IgnoreCase), new[] { "najkraći", "najduži" }, new[] { "shortest", "longest" }),
            (new Regex("najbrž|fastest", RegexOptions.IgnoreCase), new[] { "najbrži", "najsporiji" }, new[] { "fastest", "slowest" }),
            (new Regex("najšir|widest", RegexOptions.IgnoreCase), new[] { "najširi", "najuži" }, new[] { "widest", "narrowest" }),
            (new Regex("najljuć|hottest|spiciest", RegexOptions.IgnoreCase), new[] { "najljući", "najblaži" }, new[] { "hottest", "mildest" }),
            (new Regex("sjevera ka jugu|north to south", RegexOptions.IgnoreCase), new[] { "sjever", "jug" }, new[] { "north", "south" }),
            (new Regex("zapada ka istoku|west to east", RegexOptions.IgnoreCase), new[] { "zapad", "istok" }, new[] { "west", "east" }),
            (new Regex("uzlazno|ascending|lowest", RegexOptions.IgnoreCase), new[] { "najniži", "najviši" }, new[] { "lowest", "highest" }),
            (new Regex("tla naviše|ground up", RegexOptions.IgnoreCase), new[] { "dno", "vrh" }, new[] { "bottom", "top" }),
            (new Regex("od sunca|from the sun", RegexOptions.IgnoreCase), new[] { "najbliža Suncu", "najdalja" }, new[] { "closest to the Sun", "farthest" }),
        };

        // Pravilo čija se riječ prva pojavljuje u tekstu („od najvećeg do najmanjeg” → najveći).
        public static string[] EndLabels(string text, bool english)
        {
            text ??= "";
            string[] best = null;
            int at = int.MaxValue;
            foreach (var e in Ends)
            {
                var m = e.rx.Match(text);
                if (m.Success && m.Index < at) { at = m.Index; best = english ? e.en : e.sr; }
            }
            return best ?? (english ? new[] { "first", "last" } : new[] { "prvo", "posljednje" });
        }
    }
}
