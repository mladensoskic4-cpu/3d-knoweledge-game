// Mali sintisajzer: melodije za pitanja „prepoznaj melodiju”, zvučni efekti i vesela muzika sa domaćim motivima.
// Sve se računa u nizove brojeva (-1..1); Unity od njih pravi AudioClip. Pravi CC0 zvukovi, kad se dodaju, imaju prednost.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TrkaZnanja.Core
{
    public struct Note { public double Freq; public double Beats; }   // Freq = 0 je pauza

    public static class Synth
    {
        public const int Rate = 22050;
        static readonly Dictionary<char, int> Semi = new Dictionary<char, int> { ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11 };
        static readonly Regex NoteRx = new Regex(@"^([A-G])([#b]?)(-?\d)$");

        public static double NoteFreq(string name)
        {
            var m = NoteRx.Match(name ?? "");
            if (!m.Success) return 0;
            int midi = 12 * (int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) + 1) + Semi[m.Groups[1].Value[0]]
                       + (m.Groups[2].Value == "#" ? 1 : m.Groups[2].Value == "b" ? -1 : 0);
            return 440 * Math.Pow(2, (midi - 69) / 12.0);
        }

        // "E4:1 D#5:.5 R:1" (R = pauza, broj = trajanje u dobama)
        public static List<Note> ParseMelody(string str)
        {
            var list = new List<Note>();
            foreach (var tok in (str ?? "").Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = tok.Split(':');
                double beats = 1;
                if (parts.Length > 1) double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out beats);
                if (beats <= 0) beats = 1;
                list.Add(new Note { Freq = parts[0] == "R" ? 0 : NoteFreq(parts[0]), Beats = beats });
            }
            return list;
        }

        // Melodija kao jedan krug (sa pauzom od 2 dobe na kraju, da se lijepo vrti u petlji).
        public static float[] RenderMelody(string notes, double tempo, double volume = 0.32)
        {
            var list = ParseMelody(notes);
            double beat = 60.0 / (tempo > 0 ? tempo : 110);
            double total = 2 * beat;
            foreach (var n in list) total += n.Beats * beat;
            var buf = new float[(int)(total * Rate) + 1];
            double t = 0;
            foreach (var n in list)
            {
                var dur = n.Beats * beat;
                if (n.Freq > 0) Tone(buf, t, dur, n.Freq, volume);
                t += dur;
            }
            return buf;
        }

        // Ton sa „klavirskim” obrisom: triangle + oktava sine.
        static void Tone(float[] buf, double start, double dur, double f, double vol)
        {
            int s0 = (int)(start * Rate), len = (int)((dur * 0.95 + 0.05) * Rate);
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
            {
                double tt = (double)k / Rate;
                double env = tt < 0.015 ? tt / 0.015 : Math.Max(0, Math.Exp(-(tt - 0.015) * 3.2) * 0.65 + 0.35 * Math.Max(0, 1 - tt / (dur + 0.05)));
                double ph = tt * f;
                double tri = 4 * Math.Abs(ph - Math.Floor(ph + 0.5)) - 1;
                double sin2 = Math.Sin(2 * Math.PI * 2 * f * tt) * 0.25;
                buf[s0 + k] += (float)((tri * 0.8 + sin2) * env * vol);
            }
        }

        // ---- zvučni efekti ----
        public static float[] Click() => Env(0.05, (t, i) => Math.Sin(2 * Math.PI * (1500 - 9000 * t) * t) * Math.Exp(-t * 90) * 0.5);

        public static float[] Correct() => Join(Blip(880, 0.09, 0.4), Blip(1318.5, 0.16, 0.4));

        public static float[] Wrong() => Env(0.32, (t, i) => Saw((220 - 120 * t) * t) * Math.Exp(-t * 6) * 0.3);

        public static float[] Coin() => Join(Blip(1567.98, 0.06, 0.35), Blip(2093, 0.18, 0.35));

        public static float[] Whoosh(int seed = 3)
        {
            var rng = new Rng(seed);
            double lp = 0;
            return Env(0.45, (t, i) =>
            {
                var a = 0.05 + 0.6 * Math.Sin(Math.PI * t / 0.45);
                lp += (rng.Next() * 2 - 1 - lp) * (0.08 + 0.25 * t);
                return lp * a * 0.9;
            });
        }

        // Kratak „korak” vozila za jedno polje: motor (auto, kamion, motor…), kopita (zaprega), pedale (bicikl).
        public static float[] VehicleHop(string vehicle, int seed = 1)
        {
            var rng = new Rng(seed);
            switch (vehicle)
            {
                case "zaprega":
                    return Join(Hoof(rng, 0.0), Hoof(rng, 0.0), Silence(0.04), Hoof(rng, 0.0));
                case "bicikl":
                case "kolica":
                case "skuter":
                    return Env(0.22, (t, i) => (Math.Sin(2 * Math.PI * 140 * t) * 0.4 + (rng.Next() * 2 - 1) * 0.15) * Math.Exp(-t * 9) * 0.35);
                case "brod":
                    double lpw = 0;
                    return Env(0.3, (t, i) => { lpw += (rng.Next() * 2 - 1 - lpw) * 0.05; return lpw * Math.Sin(Math.PI * t / 0.3) * 1.4; });
                case "avion":
                case "balon":
                    return Env(0.3, (t, i) => (Math.Sin(2 * Math.PI * (300 + 40 * t) * t) * 0.2 + (rng.Next() * 2 - 1) * 0.1) * Math.Sin(Math.PI * t / 0.3) * 0.5);
                default:
                    double baseF = vehicle == "kamion" || vehicle == "traktor" ? 55 : vehicle == "motor" ? 95 : 75;
                    return Env(0.24, (t, i) =>
                    {
                        var f = baseF * (1 + 0.6 * Math.Sin(Math.PI * t / 0.24));
                        return (Saw(f * t) * 0.5 + Math.Sin(2 * Math.PI * f * 0.5 * t) * 0.4) * Math.Sin(Math.PI * t / 0.24) * 0.32;
                    });
            }
        }

        static float[] Hoof(Rng rng, double _) => Env(0.07, (t, i) => ((rng.Next() * 2 - 1) * 0.6 + Math.Sin(2 * Math.PI * 300 * t)) * Math.Exp(-t * 60) * 0.5);

        public static float[] Fanfare() => Join(Blip(523.25, 0.12, 0.3), Blip(659.25, 0.12, 0.3), Blip(783.99, 0.12, 0.3), Blip(1046.5, 0.45, 0.35));

        public static float[] Firework(int seed = 5)
        {
            var rng = new Rng(seed);
            return Env(0.9, (t, i) => (rng.Next() * 2 - 1) * Math.Exp(-t * 5) * (t < 0.02 ? t / 0.02 : 1) * 0.5);
        }

        // ---- muzika: vesela, sa harmonikom/tamburicom i frulom, ritam 116 BPM, 16 taktova ----
        public static float[] Music(int seed = 11, double seconds = 33.1)
        {
            var rng = new Rng(seed);
            int n = (int)(seconds * Rate);
            var buf = new float[n];
            double bpm = 116, beat = 60 / bpm, bar = beat * 4;
            // A-mol sa durskom petom (balkanski prizvuk): Am – Dm – E – Am / F – G – E – Am
            string[][] chords =
            {
                new[] { "A3", "C4", "E4" }, new[] { "D3", "F3", "A3" }, new[] { "E3", "G#3", "B3" }, new[] { "A3", "C4", "E4" },
                new[] { "F3", "A3", "C4" }, new[] { "G3", "B3", "D4" }, new[] { "E3", "G#3", "B3" }, new[] { "A3", "C4", "E4" },
            };
            string[] scale = { "A4", "B4", "C5", "D5", "E5", "F5", "G#5", "A5" };
            int bars = (int)(seconds / bar);
            for (int b = 0; b < bars; b++)
            {
                var ch = chords[b % chords.Length];
                double t0 = b * bar;
                // bas (oom-pa) i harmonika (akord na 2 i 4, kao kolo)
                for (int k = 0; k < 4; k++)
                {
                    double tb = t0 + k * beat;
                    if (k % 2 == 0) Pluck(buf, tb, beat * 0.9, NoteFreq(ch[0]) / 2, 0.22);
                    else foreach (var c in ch) Reed(buf, tb, beat * 0.45, NoteFreq(c), 0.06);
                    Kick(buf, tb, k % 2 == 0 ? 0.5 : 0.0);
                    Shaker(buf, tb + beat / 2, rng, 0.05);
                }
                // frula: melodija od osmina, ide po lestvici oko tonova akorda
                if (b >= 2)
                {
                    int idx = (b * 3) % scale.Length;
                    for (int k = 0; k < 8; k++)
                    {
                        if (rng.Chance(0.18)) continue;
                        idx = Mathx.Clamp(idx + rng.Range(-2, 3), 0, scale.Length - 1);
                        double len = rng.Chance(0.25) ? beat : beat / 2;
                        Flute(buf, t0 + k * beat / 2, len * 0.95, NoteFreq(scale[idx]), 0.11);
                    }
                }
            }
            // blago zaokruži početak i kraj da petlja ne pucketa
            int fade = Rate / 50;
            for (int i = 0; i < fade && i < n; i++) { buf[i] *= i / (float)fade; buf[n - 1 - i] *= i / (float)fade; }
            Normalize(buf, 0.8f);
            return buf;
        }

        static void Pluck(float[] buf, double start, double dur, double f, double vol)
        {
            int s0 = (int)(start * Rate), len = (int)(dur * Rate);
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
            {
                double t = (double)k / Rate;
                buf[s0 + k] += (float)((Math.Sin(2 * Math.PI * f * t) + 0.3 * Math.Sin(2 * Math.PI * 2 * f * t)) * Math.Exp(-t * 7) * vol);
            }
        }

        static void Reed(float[] buf, double start, double dur, double f, double vol)
        {
            int s0 = (int)(start * Rate), len = (int)(dur * Rate);
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
            {
                double t = (double)k / Rate;
                double env = Math.Min(1, t / 0.01) * Math.Min(1, (dur - t) / 0.03);
                // dva malo raštimovana „jezička”, kao harmonika
                double v = Saw(f * t) + Saw(f * 1.004 * t);
                buf[s0 + k] += (float)(v * 0.5 * env * vol);
            }
        }

        static void Flute(float[] buf, double start, double dur, double f, double vol)
        {
            int s0 = (int)(start * Rate), len = (int)(dur * Rate);
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
            {
                double t = (double)k / Rate;
                double vib = 1 + 0.006 * Math.Sin(2 * Math.PI * 5.5 * t) * Math.Min(1, t / 0.15);
                double env = Math.Min(1, t / 0.03) * Math.Min(1, (dur - t) / 0.04);
                buf[s0 + k] += (float)((Math.Sin(2 * Math.PI * f * vib * t) + 0.12 * Math.Sin(4 * Math.PI * f * t)) * env * vol);
            }
        }

        static void Kick(float[] buf, double start, double vol)
        {
            if (vol <= 0) return;
            int s0 = (int)(start * Rate), len = (int)(0.18 * Rate);
            double ph = 0;
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
            {
                double t = (double)k / Rate;
                ph += (55 + 90 * Math.Exp(-t * 30)) / Rate;
                buf[s0 + k] += (float)(Math.Sin(2 * Math.PI * ph) * Math.Exp(-t * 14) * vol);
            }
        }

        static void Shaker(float[] buf, double start, Rng rng, double vol)
        {
            int s0 = (int)(start * Rate), len = (int)(0.05 * Rate);
            for (int k = 0; k < len && s0 + k < buf.Length; k++)
                buf[s0 + k] += (float)((rng.Next() * 2 - 1) * Math.Exp(-(double)k / Rate * 60) * vol);
        }

        // ---- pomoćnici ----
        static double Saw(double phase) => 2 * (phase - Math.Floor(phase + 0.5));

        static float[] Blip(double f, double dur, double vol) =>
            Env(dur, (t, i) => (Math.Sin(2 * Math.PI * f * t) * 0.8 + Math.Sin(2 * Math.PI * 2 * f * t) * 0.15) * Math.Min(1, t / 0.005) * Math.Exp(-t * 14) * vol);

        static float[] Silence(double dur) => new float[(int)(dur * Rate)];

        static float[] Env(double dur, Func<double, int, double> f)
        {
            var buf = new float[(int)(dur * Rate)];
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Mathx.Clamp(f((double)i / Rate, i), -1, 1);
            return buf;
        }

        static float[] Join(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var res = new float[n];
            int o = 0;
            foreach (var p in parts) { Array.Copy(p, 0, res, o, p.Length); o += p.Length; }
            return res;
        }

        public static void Normalize(float[] buf, float peak)
        {
            float max = 0;
            foreach (var v in buf) max = Math.Max(max, Math.Abs(v));
            if (max < 1e-6f) return;
            var k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }
    }
}
