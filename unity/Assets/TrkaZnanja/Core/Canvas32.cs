// Mali crtač u memoriji (RGBA, sa ublažavanjem ivica): mape i obrisi država za mini igre se crtaju ovdje,
// pa se jednim potezom prebace u Unity teksturu (redovi su odozdo nagore, kao što Unity očekuje).
// Koordinate su u pikselima, y raste nadolje (kao na ekranu i u web canvas-u).
using System;
using System.Collections.Generic;

namespace TrkaZnanja.Core
{
    public struct Rgba
    {
        public byte R, G, B, A;
        public Rgba(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
        public static Rgba Hex(string hex, byte a = 255)
        {
            hex = hex.TrimStart('#');
            return new Rgba(Convert.ToByte(hex.Substring(0, 2), 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16), a);
        }
        public Rgba WithAlpha(double a) => new Rgba(R, G, B, (byte)Math.Round(Mathx.Clamp(a, 0, 1) * 255));
    }

    public sealed class Canvas32
    {
        public readonly int W, H;
        public readonly byte[] Data;   // RGBA, red 0 = dno slike
        const int Sub = 4;             // podlinija po pikselu (ublažavanje ivica)
        float[] cover;

        public Canvas32(int w, int h)
        {
            W = w; H = h;
            Data = new byte[w * h * 4];
        }

        int Index(int x, int y) => ((H - 1 - y) * W + x) * 4;

        public Rgba Get(int x, int y)
        {
            int i = Index(x, y);
            return new Rgba(Data[i], Data[i + 1], Data[i + 2], Data[i + 3]);
        }

        public void Clear(Rgba c)
        {
            for (int i = 0; i < Data.Length; i += 4) { Data[i] = c.R; Data[i + 1] = c.G; Data[i + 2] = c.B; Data[i + 3] = c.A; }
        }

        public void Blend(int x, int y, Rgba c, double k)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || k <= 0) return;
            double a = c.A / 255.0 * Math.Min(1, k);
            int i = Index(x, y);
            double da = Data[i + 3] / 255.0;
            double oa = a + da * (1 - a);
            if (oa <= 0) return;
            Data[i] = (byte)Math.Round((c.R * a + Data[i] * da * (1 - a)) / oa);
            Data[i + 1] = (byte)Math.Round((c.G * a + Data[i + 1] * da * (1 - a)) / oa);
            Data[i + 2] = (byte)Math.Round((c.B * a + Data[i + 2] * da * (1 - a)) / oa);
            Data[i + 3] = (byte)Math.Round(oa * 255);
        }

        // Popuna poligona sa rupama (pravilo par-nepar), kao ctx.fill('evenodd').
        public void Fill(IList<IList<Vec2d>> rings, Rgba c)
        {
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity, minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
            foreach (var r in rings) foreach (var p in r) { minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y); minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); }
            if (maxY < 0 || minY >= H || maxX < 0 || minX >= W) return;
            int y0 = Math.Max(0, (int)Math.Floor(minY)), y1 = Math.Min(H - 1, (int)Math.Ceiling(maxY));
            if (cover == null || cover.Length < W + 1) cover = new float[W + 1];
            var xs = new List<double>();
            for (int y = y0; y <= y1; y++)
            {
                Array.Clear(cover, 0, W + 1);
                int lo = W, hi = -1;
                for (int s = 0; s < Sub; s++)
                {
                    double yc = y + (s + 0.5) / Sub;
                    xs.Clear();
                    foreach (var r in rings)
                        for (int i = 0, j = r.Count - 1; i < r.Count; j = i++)
                            if ((r[i].Y > yc) != (r[j].Y > yc)) xs.Add(r[i].X + (yc - r[i].Y) * (r[j].X - r[i].X) / (r[j].Y - r[i].Y));
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        double a = Math.Max(0, xs[k]), b = Math.Min(W, xs[k + 1]);
                        if (b <= a) continue;
                        int ia = (int)Math.Floor(a), ib = (int)Math.Floor(b);
                        lo = Math.Min(lo, ia); hi = Math.Max(hi, Math.Min(W - 1, ib));
                        if (ia == ib) { cover[ia] += (float)((b - a) / Sub); continue; }
                        cover[ia] += (float)((ia + 1 - a) / Sub);
                        for (int x = ia + 1; x < ib; x++) cover[x] += 1f / Sub;
                        if (ib < W) cover[ib] += (float)((b - ib) / Sub);
                    }
                }
                for (int x = Math.Max(0, lo); x <= hi; x++) if (cover[x] > 0.001f) Blend(x, y, c, cover[x]);
            }
        }

        // Debela linija (kapsula) od a do b.
        public void Line(Vec2d a, Vec2d b, double width, Rgba c)
        {
            double r = width / 2;
            int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.X, b.X) - r - 1)), x1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(a.X, b.X) + r + 1));
            int y0 = Math.Max(0, (int)Math.Floor(Math.Min(a.Y, b.Y) - r - 1)), y1 = Math.Min(H - 1, (int)Math.Ceiling(Math.Max(a.Y, b.Y) + r + 1));
            double dx = b.X - a.X, dy = b.Y - a.Y, len2 = dx * dx + dy * dy;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    double px = x + 0.5 - a.X, py = y + 0.5 - a.Y;
                    double t = len2 > 0 ? Mathx.Clamp((px * dx + py * dy) / len2, 0, 1) : 0;
                    double ex = px - dx * t, ey = py - dy * t;
                    double d = Math.Sqrt(ex * ex + ey * ey);
                    double k = r + 0.5 - d;
                    if (k > 0) Blend(x, y, c, Math.Min(1, k));
                }
        }

        public void Stroke(IList<Vec2d> ring, double width, Rgba c, bool closed = true)
        {
            int n = ring.Count;
            for (int i = 0; i + (closed ? 0 : 1) < n; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % n];
                // preskoči ivice preko cijele mape (prelaz datumske granice)
                if (Math.Abs(b.X - a.X) > W / 3.0) continue;
                Line(a, b, width, c);
            }
        }

        public void Disc(Vec2d p, double radius, Rgba c) => Line(p, p, radius * 2, c);
    }
}
