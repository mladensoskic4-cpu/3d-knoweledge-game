// Logika jedne trke, bez grafike: igrači, runde, pomjeranje po poljima, stanice, bonus runde i poredak na cilju.
// Ekran (Unity) samo pita ovu klasu šta se desilo i to animira.
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
        public bool Checkpoint;
    }

    public sealed class StationOutcome
    {
        public Racer Racer;
        public double Score;
        public string Label;
        public int Coins, Jump;
    }

    public sealed class Race
    {
        public readonly List<Racer> Players;
        public readonly int TileCount;
        public int Last => TileCount - 1;
        public int Round { get; private set; }
        public readonly Rng Rng;

        public Race(Racer human, IEnumerable<Racer> bots, int tileCount, Rng rng)
        {
            if (tileCount < 8) throw new ArgumentException("Staza je prekratka.", nameof(tileCount));
            TileCount = tileCount;
            Rng = rng;
            Players = new List<Racer> { human };
            Players.AddRange(bots);
            foreach (var p in Players)
            {
                p.Pos = 0; p.Coins = 0; p.LastCheckpoint = 0; p.Correct = 0; p.Answered = 0; p.FinishedRound = null; p.LastScore = 0;
            }
        }

        public Racer Human => Players.First(p => p.Human);

        public static string TileType(int i, int last) =>
            i == 0 ? "start" : i == last ? "cilj" : i % Catalog.CheckpointEvery == 0 ? "stanica" : "polje";

        public bool IsCheckpoint(int i) => i > 0 && i < Last && i % Catalog.CheckpointEvery == 0;

        public int NextRound() => ++Round;

        public bool IsBonusRound => Catalog.BonusRounds.Contains(Round) && Players.All(p => p.Pos < Last);

        // Rezultati runde za sve igrače (čovjek je već odgovorio, botovi se simuliraju).
        public List<RoundEntry> ResolveRound(Question q, QuizResult human)
        {
            var type = QuizTypes.Get(q.Tip);
            var limit = QuizTypes.TimeLimitOf(q);
            bool shortcut = human.UsedPowerups.Contains("precica"), slow = human.UsedPowerups.Contains("uspori");
            var list = new List<RoundEntry>();
            foreach (var p in Players)
            {
                RoundEntry e;
                if (p.Human)
                {
                    e = new RoundEntry
                    {
                        Racer = p, Accuracy = human.Accuracy, Time = human.TimeUsed, Tiles = human.Tiles,
                        Text = human.TimedOut && human.Accuracy < 0.2 ? "isteklo vrijeme" : type.Summarize(q, human)
                    };
                }
                else
                {
                    var b = Bots.Answer(p, q, limit, Rng);
                    var r = new QuizResult { Accuracy = b.accuracy, TimeUsed = b.time, Limit = limit, TimedOut = b.timedOut };
                    e = new RoundEntry
                    {
                        Racer = p, Accuracy = b.accuracy, Time = b.time, Tiles = QuizTypes.TilesFor(b.accuracy, b.time, limit),
                        Text = b.timedOut ? "isteklo vrijeme" : type.Summarize(q, r)
                    };
                }
                p.Answered++;
                if (e.Accuracy >= 0.75) p.Correct++;
                p.LastScore = e.Tiles * 100 - e.Time;
                if (p.Human && shortcut) { e.Tiles += 2; e.Text += " · prečica +2"; }
                if (!p.Human && slow && e.Tiles > 0) { e.Tiles -= 1; e.Text += " · usporen"; }
                list.Add(e);
            }
            return list;
        }

        public static List<RoundEntry> SortForBoard(IEnumerable<RoundEntry> list) =>
            list.OrderByDescending(e => e.Tiles).ThenBy(e => e.Time).ToList();

        // Pomjeranje staje na prvoj stanici koju igrač još nije posjetio.
        public Move AdvanceTarget(Racer p, int steps)
        {
            int target = Math.Min(Last, p.Pos + Math.Max(0, steps));
            bool cp = false;
            for (int i = p.Pos + 1; i <= target; i++)
                if (IsCheckpoint(i) && i > p.LastCheckpoint) { target = i; cp = true; break; }
            return new Move { Racer = p, From = p.Pos, Target = target, Checkpoint = cp };
        }

        public List<Move> MovesFor(IEnumerable<RoundEntry> entries) => entries.Select(e => AdvanceTarget(e.Racer, e.Tiles)).ToList();

        public void Apply(IEnumerable<Move> moves)
        {
            foreach (var m in moves)
            {
                m.Racer.Pos = m.Target;
                if (m.Checkpoint) m.Racer.LastCheckpoint = m.Target;
            }
        }

        public List<Racer> AtStation(IEnumerable<Move> moves) => moves.Where(m => m.Checkpoint && m.Racer.Pos < Last).Select(m => m.Racer).ToList();

        // Mini igra na stanici: rezultat → novčići i skok naprijed.
        public StationOutcome Station(Racer p, double score, string label)
        {
            var (coins, jump) = Catalog.RewardFor(score);
            p.Coins += coins;
            return new StationOutcome { Racer = p, Score = score, Label = label, Coins = coins, Jump = jump };
        }

        // Bonus runda: svi igraju istu mini igru; nagrade po mjestu.
        public static readonly (int jump, int coins)[] BonusPrize = { (3, 50), (1, 20), (0, 10), (0, 0) };

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
            outcomes.Select(o => new Move { Racer = o.Racer, From = o.Racer.Pos, Target = Math.Min(Last, o.Racer.Pos + o.Jump) }).ToList();

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
