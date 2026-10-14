// Logika jedne trke, bez grafike: igrači, runde, pomjeranje po putu, polja (prečica, blato, dupla, kutija),
// napadi (nazad, zbunjivanje, štit), dupla runda, mini igra za sve svake 4 runde i poredak na cilju.
// Ekran (Unity) samo pita ovu klasu šta se desilo i to animira. Sve slučajno ide iz Rng-a trke (isto seme = ista trka).
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class RoundEntry
    {
        public Racer Racer;
        public double Accuracy, Time;
        public int Tiles;
        public string Text;
    }

    public sealed class Move
    {
        public Racer Racer;
        public int From, Target;
        public string Note;      // npr. "prečica +2", "blato −1", "napad −2"
    }

    public sealed class StationOutcome
    {
        public Racer Racer;
        public double Score;
        public string Label;
        public int Coins, Jump;
    }

    // Šta se desilo u rundi pored odgovora (za poruke na ekranu).
    public sealed class RaceEvent
    {
        public Racer Who, Target;
        public string Kind;      // napad, blokirano, kutija, precica, blato, dupla
        public string Item;
        public string Text;
    }

    public static class TileKinds
    {
        public const string Start = "start", Finish = "cilj", Plain = "polje", Shortcut = "precica", Mud = "blato", Double = "dupla", Box = "kutija";

        // Raspored posebnih polja iz semena trke (isti na svim uređajima). Nema ih na prva 2 i zadnja 2 polja
        // i dva posebna nikad nisu jedno do drugog.
        public static List<string> Generate(int count, int seed)
        {
            var rng = new Rng(unchecked(seed * 31 + 7));
            var res = new List<string>(count);
            int last = count - 1;
            for (int i = 0; i < count; i++)
            {
                string k = Plain;
                if (i == 0) k = Start;
                else if (i == last) k = Finish;
                else if (i >= 3 && i <= last - 2 && res[i - 1] == Plain)
                {
                    var r = rng.Next();
                    if (r < 0.17) k = Box;
                    else if (r < 0.24) k = Shortcut;
                    else if (r < 0.31) k = Mud;
                    else if (r < 0.37) k = Double;
                }
                res.Add(k);
            }
            // bar nekoliko kutija na svakoj stazi
            for (int i = 4; i < last - 2 && res.Count(x => x == Box) < Math.Max(2, count / 10); i += 5)
                if (res[i] == Plain && res[i - 1] == Plain && res[i + 1] == Plain) res[i] = Box;
            return res;
        }
    }

    public static class Items
    {
        public const string Back = "nazad", Confuse = "zbuni", Shield = "stit";
        public static readonly string[] All = { Back, Confuse, Shield };
        public const string UsePrefix = "napad:";

        public static string Label(string id) => id == Back ? "Vrati 2 polja" : id == Confuse ? "Zbuni protivnika" : id == Shield ? "Štit" : id;
        public static string Hint(string id) => id == Back ? "Igrač ispred tebe ide 2 polja nazad."
            : id == Confuse ? "Igrač ispred tebe u sljedećem pitanju ima manje vremena i mutne odgovore."
            : id == Shield ? "Sam te brani od jednog napada." : "";
        public static bool IsAttack(string id) => id == Back || id == Confuse;
    }

    public sealed class Race
    {
        public readonly List<Racer> Players;
        public readonly int TileCount;
        public readonly List<string> Kinds;
        public int Last => TileCount - 1;
        public int Round { get; private set; }
        public bool IsDoubleRound { get; private set; }
        public readonly Rng Rng;
        public readonly List<RaceEvent> Events = new List<RaceEvent>();   // događaji posljednje runde

        public Race(Racer human, IEnumerable<Racer> bots, int tileCount, Rng rng) : this(new[] { human }.Concat(bots), tileCount, rng) { }

        // Svi igrači u redosljedu koji je isti na svim uređajima (multiplayer); tačno jedan ima Human (ovaj uređaj).
        public Race(IEnumerable<Racer> players, int tileCount, Rng rng, IList<string> kinds = null)
        {
            if (tileCount < 8) throw new ArgumentException("Staza je prekratka.", nameof(tileCount));
            TileCount = tileCount;
            Rng = rng;
            Kinds = kinds != null && kinds.Count == tileCount ? kinds.ToList() : TileKinds.Generate(tileCount, 1);
            Players = players.ToList();
            if (Players.Count(p => p.Human) != 1) throw new ArgumentException("Tačno jedan igrač mora biti na ovom uređaju.", nameof(players));
            foreach (var p in Players)
            {
                p.Pos = 0; p.Coins = 0; p.Correct = 0; p.Answered = 0; p.FinishedRound = null; p.LastScore = 0;
                p.Item = null; p.DoubleNext = false; p.Confused = false;
            }
        }

        public Racer Human => Players.First(p => p.Human);

        public string KindAt(int i) => Kinds[Math.Max(0, Math.Min(Last, i))];

        // Nova runda: ponekad je dupla (sva polja ×2), najavi se prije pitanja.
        public int NextRound()
        {
            Round++;
            IsDoubleRound = Round >= 3 && Rng.Next() < 1 / 6.0;
            return Round;
        }

        // Svake 4 runde svi igraju istu mini igru (ako trka još traje).
        public bool IsMinigameRound => Round % Catalog.MinigameEvery == 0 && Players.All(p => p.Pos < Last);

        // Ko je „ispred” igrača p (meta napada): najbliži ispred njega; ako je on prvi, onaj odmah iza.
        public Racer TargetFor(Racer p)
        {
            var ahead = Players.Where(o => o != p && o.Pos > p.Pos).OrderBy(o => o.Pos).ThenBy(o => Players.IndexOf(o)).FirstOrDefault();
            if (ahead != null) return ahead;
            return Players.Where(o => o != p).OrderByDescending(o => o.Pos).ThenBy(o => Players.IndexOf(o)).FirstOrDefault();
        }

        // Rezultati runde za sve igrače (čovjek je već odgovorio, botovi se simuliraju).
        public List<RoundEntry> ResolveRound(Question q, QuizResult human)
        {
            var type = QuizTypes.Get(q.Tip);
            var text = human.TimedOut && human.Accuracy < 0.2 ? "isteklo vrijeme" : type.Summarize(q, human);
            return ResolveRound(q, new Dictionary<string, MpAnswer> { [Human.Id] = MpAnswer.From(human, text) });
        }

        // Odgovori svih ljudi (ključ = Id igrača); ko nema odgovor, isteklo mu je vrijeme. Botovi se simuliraju
        // istim Rng-om, pa je rezultat isti na svakom uređaju. Ovdje se primjenjuju i dupla polja i napadi.
        public List<RoundEntry> ResolveRound(Question q, IDictionary<string, MpAnswer> people)
        {
            Events.Clear();
            var type = QuizTypes.Get(q.Tip);
            var limit = QuizTypes.TimeLimitOf(q);
            var answers = new Dictionary<Racer, MpAnswer>();
            foreach (var p in Players.Where(p => p.Person))
                answers[p] = people.TryGetValue(p.Id, out var got) && got != null ? got : MpAnswer.Timeout(limit);
            // usporavanje (power-up): svako ko ga upotrijebi usporava sve ostale za 1 polje
            var slowers = Players.Where(p => p.Person && answers[p].Powerups.Contains("uspori")).ToList();
            var list = new List<RoundEntry>();
            foreach (var p in Players)
            {
                RoundEntry e;
                bool shortcut = false;
                if (p.Person)
                {
                    var a = answers[p];
                    shortcut = a.Powerups.Contains("precica");
                    e = new RoundEntry { Racer = p, Accuracy = a.Acc, Time = a.Time, Tiles = a.Tiles, Text = a.Text };
                }
                else
                {
                    var b = Bots.Answer(p, q, limit, Rng);
                    var r = new QuizResult { Accuracy = b.accuracy, TimeUsed = b.time, Limit = limit, TimedOut = b.timedOut };
                    int tiles = QuizTypes.TilesFor(b.accuracy, b.time, limit);
                    if (p.Confused && tiles > 0) tiles -= 1;       // zbunjen bot sporije odgovara
                    e = new RoundEntry { Racer = p, Accuracy = b.accuracy, Time = b.time, Tiles = tiles, Text = b.timedOut ? "isteklo vrijeme" : type.Summarize(q, r) };
                }
                p.Confused = false;
                p.Answered++;
                if (e.Accuracy >= 0.75) p.Correct++;
                if (shortcut) { e.Tiles += 2; e.Text += " · prečica +2"; }
                if (slowers.Any(s => s != p) && e.Tiles > 0) { e.Tiles -= 1; e.Text += " · usporen"; }
                if (p.DoubleNext)
                {
                    p.DoubleNext = false;
                    if (e.Tiles > 0) { e.Tiles *= 2; e.Text += " · dupla polja"; }
                }
                if (IsDoubleRound && e.Tiles > 0) { e.Tiles *= 2; e.Text += " · DUPLA RUNDA"; }
                p.LastScore = e.Tiles * 100 - e.Time;
                list.Add(e);
            }

            // napadi: ljudi koji su ih upotrijebili u ovom pitanju, botovi kad im se prohtije (isti Rng)
            foreach (var p in Players)
            {
                if (p.Item == null || !Items.IsAttack(p.Item)) continue;
                bool use = p.Person ? answers[p].Powerups.Contains(Items.UsePrefix + p.Item) : Rng.Chance(0.6);
                if (!use) continue;
                var item = p.Item;
                p.Item = null;
                var target = TargetFor(p);
                if (target == null) continue;
                if (target.Item == Items.Shield)
                {
                    target.Item = null;
                    Events.Add(new RaceEvent { Who = p, Target = target, Kind = "blokirano", Item = item, Text = $"{target.Name} se odbranio štitom od napada ({p.Name})." });
                    continue;
                }
                var te = list.First(x => x.Racer == target);
                if (item == Items.Back)
                {
                    te.Tiles -= 2;
                    te.Text += " · napad −2";
                    Events.Add(new RaceEvent { Who = p, Target = target, Kind = "napad", Item = item, Text = $"{p.Name} vraća igrača {target.Name} 2 polja nazad!" });
                }
                else
                {
                    target.Confused = true;
                    Events.Add(new RaceEvent { Who = p, Target = target, Kind = "napad", Item = item, Text = $"{p.Name} zbunjuje igrača {target.Name} za sljedeće pitanje!" });
                }
            }
            return list;
        }

        public static List<RoundEntry> SortForBoard(IEnumerable<RoundEntry> list) =>
            list.OrderByDescending(e => e.Tiles).ThenBy(e => e.Time).ToList();

        // Pomjeranje naprijed ili nazad (napad), unutar staze.
        public Move AdvanceTarget(Racer p, int steps, string note = null) =>
            new Move { Racer = p, From = p.Pos, Target = Math.Max(0, Math.Min(Last, p.Pos + steps)), Note = note };

        public List<Move> MovesFor(IEnumerable<RoundEntry> entries) => entries.Select(e => AdvanceTarget(e.Racer, e.Tiles)).ToList();

        public void Apply(IEnumerable<Move> moves)
        {
            foreach (var m in moves) m.Racer.Pos = m.Target;
        }

        // Poslije vožnje: šta rade polja na kojima su se igrači zaustavili. Vraća dodatna pomjeranja (prečica, blato);
        // dupla polja i kutije samo mijenjaju stanje igrača. Događaji idu u Events.
        public List<Move> Landings(IEnumerable<Move> moves)
        {
            var extra = new List<Move>();
            foreach (var m in moves)
            {
                var p = m.Racer;
                if (m.Target == m.From || p.Pos >= Last) continue;
                switch (KindAt(p.Pos))
                {
                    case TileKinds.Shortcut:
                        extra.Add(AdvanceTarget(p, 2, "prečica +2"));
                        Events.Add(new RaceEvent { Who = p, Kind = "precica", Text = $"{p.Name} nalazi prečicu: +2 polja!" });
                        break;
                    case TileKinds.Mud:
                        extra.Add(AdvanceTarget(p, -1, "blato −1"));
                        Events.Add(new RaceEvent { Who = p, Kind = "blato", Text = $"{p.Name} je upao u blato: −1 polje." });
                        break;
                    case TileKinds.Double:
                        p.DoubleNext = true;
                        Events.Add(new RaceEvent { Who = p, Kind = "dupla", Text = $"{p.Name}: sljedeći tačan odgovor donosi dupla polja!" });
                        break;
                    case TileKinds.Box:
                        if (p.Item == null)
                        {
                            p.Item = Items.All[Rng.Range(0, Items.All.Length)];
                            Events.Add(new RaceEvent { Who = p, Kind = "kutija", Item = p.Item, Text = $"{p.Name} otvara kutiju: {Items.Label(p.Item)}" });
                        }
                        break;
                }
            }
            return extra;
        }

        // Zajednička mini igra: nagrade po mjestu (polja i novčići).
        public static readonly (int jump, int coins)[] BonusPrize = { (3, 50), (2, 30), (1, 15), (0, 5) };

        public List<StationOutcome> Bonus(IEnumerable<(Racer racer, double score, string label)> results)
        {
            var sorted = results.OrderByDescending(r => r.score).ToList();
            var outcomes = new List<StationOutcome>();
            for (int i = 0; i < sorted.Count; i++)
            {
                var prize = BonusPrize[Math.Min(i, BonusPrize.Length - 1)];
                sorted[i].racer.Coins += prize.coins;
                outcomes.Add(new StationOutcome { Racer = sorted[i].racer, Score = sorted[i].score, Label = sorted[i].label, Coins = prize.coins, Jump = prize.jump });
            }
            return outcomes;
        }

        public List<Move> Jumps(IEnumerable<StationOutcome> outcomes) =>
            outcomes.Select(o => AdvanceTarget(o.Racer, o.Jump)).ToList();

        // Trka je gotova čim neko stigne na cilj (na kraju runde).
        public bool CheckFinish()
        {
            var finishers = Players.Where(p => p.Pos >= Last).ToList();
            foreach (var p in finishers) if (p.FinishedRound == null) p.FinishedRound = Round;
            return finishers.Count > 0;
        }

        public List<Racer> Ranking() => Players
            .OrderBy(p => p.FinishedRound ?? int.MaxValue)
            .ThenByDescending(p => p.Pos)
            .ThenByDescending(p => p.LastScore)
            .ToList();

        // Mjesto u trci uživo (za HUD): ko je dalje, ide gore.
        public List<Racer> Standings() => Players.OrderByDescending(p => p.Pos).ThenByDescending(p => p.LastScore).ToList();
    }
}
