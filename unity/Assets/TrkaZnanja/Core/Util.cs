// Sitne pomoćne stvari: generator slučajnih brojeva (može se zadati seme, pa su testovi ponovljivi),
// miješanje, normalna raspodjela i formatiranje brojeva na naš način (1.234,5).
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TrkaZnanja.Core
{
    public sealed class Rng
    {
        uint state;
        public Rng(int seed) { state = (uint)seed; }
        public Rng() : this(Environment.TickCount ^ Guid.NewGuid().GetHashCode()) { }

        // mulberry32: brz, dovoljno dobar za igru, isti rezultat na svim platformama
        public double Next()
        {
            unchecked
            {
                state += 0x6D2B79F5;
                uint t = state;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return ((t ^ (t >> 14)) >> 0) / 4294967296.0;
            }
        }

        public int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : minInclusive + (int)Math.Floor(Next() * (maxExclusive - minInclusive));

        public double Range(double a, double b) => a + (b - a) * Next();

        public T Pick<T>(IList<T> list) => list[Range(0, list.Count)];

        public bool Chance(double p) => Next() < p;

        // Normalna raspodjela (Box–Muller), srednja 0, devijacija 1.
        public double Gaussian()
        {
            double u = 0, v = 0;
            while (u == 0) u = Next();
            while (v == 0) v = Next();
            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }

        public List<T> Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)Math.Floor(Next() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }

    public static class Mathx
    {
        public static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static double Ease(double k) => k < 0.5 ? 2 * k * k : 1 - Math.Pow(-2 * k + 2, 2) / 2;
        public static double SmoothStep(double a, double b, double x)
        {
            var t = Clamp((x - a) / (b - a), 0, 1);
            return t * t * (3 - 2 * t);
        }
    }

    public static class Fmt
    {
        // Množina u našem jeziku: 1 runda, 2–4 runde, 5+ rundi (11–14 uvijek „rundi”).
        public static string Plural(long n, string one, string few, string many)
        {
            long a = Math.Abs(n) % 100, b = a % 10;
            if (a >= 11 && a <= 14) return many;
            if (b == 1) return one;
            if (b >= 2 && b <= 4) return few;
            return many;
        }

        // 1234.5 → "1.234,5"; godine se pišu bez tačke za hiljade.
        public static string Num(double v, int? decimals = null, bool group = true)
        {
            int d = decimals ?? (Math.Abs(v % 1) < 1e-9 ? 0 : Math.Min(6, DecimalsOf(v)));
            var s = Math.Abs(v).ToString("F" + d, CultureInfo.InvariantCulture);
            var parts = s.Split('.');
            var ip = parts[0];
            if (group && ip.Length > 3)
            {
                var chars = new List<char>();
                for (int i = 0; i < ip.Length; i++)
                {
                    if (i > 0 && (ip.Length - i) % 3 == 0) chars.Add('.');
                    chars.Add(ip[i]);
                }
                ip = new string(chars.ToArray());
            }
            return (v < 0 ? "−" : "") + ip + (parts.Length > 1 ? "," + parts[1] : "");
        }

        public static int DecimalsOf(double step)
        {
            var s = step.ToString("R", CultureInfo.InvariantCulture);
            int dot = s.IndexOf('.');
            return dot < 0 ? 0 : s.Length - dot - 1;
        }

        public static string Sec(double s) => Num(Math.Round(s * 10) / 10, 1) + " s";
    }
}
