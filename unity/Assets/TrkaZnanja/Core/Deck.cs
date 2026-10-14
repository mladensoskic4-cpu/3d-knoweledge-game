// Špil pitanja: bira tip sa najvećim „dugom” prema ciljnom udjelu; višestruki izbor nikad ne prelazi 40%.
// Unutar tipa bira oblast koja je najmanje korištena, a pitanja se ne ponavljaju dok se tip ne potroši.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class Deck
    {
        readonly Rng rng;
        readonly Dictionary<string, List<Question>> originals = new Dictionary<string, List<Question>>();
        readonly Dictionary<string, List<Question>> left = new Dictionary<string, List<Question>>();
        readonly Dictionary<string, double> weight = new Dictionary<string, double>();
        readonly Dictionary<string, int> used = new Dictionary<string, int>();
        readonly Dictionary<string, int> usedArea = new Dictionary<string, int>();
        readonly List<string> types;
        readonly List<string> others;
        int n;

        public int Size { get; }

        // seen: pitanja koja je igrač već vidio u ranijim trkama; preskaču se dok ima dovoljno novih (po tipu).
        public Deck(IEnumerable<Question> questions, ICollection<string> areas, Rng rng, int maxTezina = 3, ICollection<string> seen = null)
        {
            this.rng = rng;
            var all = questions.Where(q => QuizTypes.Get(q.Tip) != null && areas.Contains(q.Oblast)).ToList();
            var pool = all.Where(q => q.Tezina <= maxTezina).ToList();
            if (pool.Count < 20) pool = all;
            if (seen != null && seen.Count > 0)
            {
                // po tipu: ako ima bar 8 neviđenih, uzimaju se samo neviđena
                pool = pool.GroupBy(q => q.Tip).SelectMany(g =>
                {
                    var fresh = g.Where(q => q.Id == null || !seen.Contains(q.Id)).ToList();
                    return fresh.Count >= 8 ? fresh : g.ToList();
                }).ToList();
            }
            if (pool.Count == 0) throw new InvalidOperationException("Nema pitanja za izabrane oblasti.");
            Size = pool.Count;

            foreach (var q in pool)
            {
                if (!originals.TryGetValue(q.Tip, out var l)) originals[q.Tip] = l = new List<Question>();
                l.Add(q);
            }
            foreach (var t in originals.Keys) Refill(t);
            types = originals.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            others = types.Where(t => t != QuizTypes.MultipleChoice).ToList();

            if (types.Contains(QuizTypes.MultipleChoice))
                weight[QuizTypes.MultipleChoice] = others.Count > 0 ? QuizTypes.MultipleChoiceShareMax : 1;
            // tipovi sa malo pitanja dobijaju manju težinu, da se ne ponavljaju
            var raw = others.ToDictionary(t => t, t => Math.Min(1.0, originals[t].Count / 15.0));
            var rawSum = raw.Values.Sum();
            if (rawSum <= 0) rawSum = 1;
            var mc = weight.TryGetValue(QuizTypes.MultipleChoice, out var w) ? w : 0;
            foreach (var t in others) weight[t] = (1 - mc) * raw[t] / rawSum;
        }

        void Refill(string t) => left[t] = rng.Shuffle(new List<Question>(originals[t]));

        public Question Next()
        {
            string best = null;
            double bestScore = double.NegativeInfinity;
            foreach (var t in types)
            {
                used.TryGetValue(t, out var u);
                if (t == QuizTypes.MultipleChoice && others.Count > 0 && u + 1 > QuizTypes.MultipleChoiceShareMax * (n + 1) + 1e-9) continue;
                var score = weight[t] * (n + 1) - u + rng.Next() * 0.01;
                if (score > bestScore) { bestScore = score; best = t; }
            }
            if (left[best].Count == 0) Refill(best);
            var list = left[best];
            int idx = 0, min = int.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                usedArea.TryGetValue(list[i].Oblast, out var ua);
                if (ua < min) { min = ua; idx = i; }
            }
            var q = list[idx];
            list.RemoveAt(idx);
            used[best] = (used.TryGetValue(best, out var ub) ? ub : 0) + 1;
            usedArea[q.Oblast] = (usedArea.TryGetValue(q.Oblast, out var a) ? a : 0) + 1;
            n++;
            return q;
        }
    }
}
