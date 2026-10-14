// Ikonice za dugmad i ukrase (originalni crtež igre): bijeli oblici na providnoj pozadini, boja se daje u UI-ju.
// Koordinate su od 0 do 1 (y raste nadolje), pa se svaka ikonica može nacrtati u bilo kojoj veličini.
// Iste figure (zvijezda, srce, munja…) koriste se i za nalijepnice na vozilima i za tragove u vožnji.
using System;
using System.Collections.Generic;

namespace TrkaZnanja.Core
{
    public static class Icons
    {
        public static readonly string[] All =
        {
            "igraj", "ljudi", "prijatelj", "podesavanja", "pehar", "auto", "globus", "zvijezda", "srce", "munja", "plamen",
            "katanac", "novcic", "nazad", "preskoci", "okreni", "boja", "nalijepnica", "iskra", "sapa", "kapa", "naocare",
            "mapa", "sat", "kvacica", "zatvori", "cvijet", "smajli", "kruna", "duga", "sara", "plus", "minus", "lupa",
            "sedam", "zastava", "pruge", "tacke", "sah",
        };

        static readonly Rgba W = new Rgba(255, 255, 255);
        static readonly Rgba Hole = new Rgba(0, 0, 0, 0);

        public static Canvas32 Draw(string name, int size)
        {
            var cv = new Canvas32(size, size);
            var p = new P(cv, size);
            switch (name)
            {
                case "igraj": p.Poly(0.3, 0.18, 0.84, 0.5, 0.3, 0.82); break;
                case "ljudi":
                    p.Circle(0.36, 0.33, 0.13); p.Poly(Arc(0.36, 0.86, 0.26, 0.24, 180, 360, true));
                    p.Circle(0.67, 0.36, 0.11); p.Poly(Arc(0.69, 0.84, 0.22, 0.2, 180, 360, true));
                    break;
                case "prijatelj":
                    p.Circle(0.4, 0.34, 0.14); p.Poly(Arc(0.4, 0.88, 0.27, 0.26, 180, 360, true));
                    p.Rect(0.7, 0.44, 0.24, 0.07); p.Rect(0.785, 0.355, 0.07, 0.24);
                    break;
                case "podesavanja":
                    {
                        var gear = new List<Vec2d>();
                        for (int i = 0; i < 48; i++)
                        {
                            double a = i * Math.PI * 2 / 48;
                            double r = (i % 6) < 3 ? 0.44 : 0.34;
                            gear.Add(new Vec2d(0.5 + Math.Cos(a) * r, 0.5 + Math.Sin(a) * r));
                        }
                        p.Poly(gear);
                        p.Circle(0.5, 0.5, 0.14, Hole);
                        break;
                    }
                case "pehar":
                    p.Poly(0.24, 0.14, 0.76, 0.14, 0.72, 0.42, 0.6, 0.56, 0.4, 0.56, 0.28, 0.42);
                    p.Ring(0.21, 0.3, 0.1, 0.05); p.Ring(0.79, 0.3, 0.1, 0.05);
                    p.Rect(0.45, 0.55, 0.1, 0.17); p.Rect(0.3, 0.72, 0.4, 0.1);
                    break;
                case "auto":
                    p.RoundRect(0.1, 0.42, 0.8, 0.26, 0.08);
                    p.Poly(0.26, 0.44, 0.34, 0.24, 0.66, 0.24, 0.76, 0.44);
                    p.Poly(new[] { 0.37, 0.3, 0.48, 0.3, 0.48, 0.42, 0.33, 0.42 }, Hole);
                    p.Poly(new[] { 0.52, 0.3, 0.63, 0.3, 0.67, 0.42, 0.52, 0.42 }, Hole);
                    p.Circle(0.3, 0.7, 0.1); p.Circle(0.7, 0.7, 0.1);
                    p.Circle(0.3, 0.7, 0.04, Hole); p.Circle(0.7, 0.7, 0.04, Hole);
                    break;
                case "globus":
                    p.Ring(0.5, 0.5, 0.36, 0.07);
                    p.Line(0.14, 0.5, 0.86, 0.5, 0.06);
                    p.Ellipse(0.5, 0.5, 0.15, 0.36, 0.06);
                    p.Line(0.22, 0.32, 0.78, 0.32, 0.05); p.Line(0.22, 0.68, 0.78, 0.68, 0.05);
                    break;
                case "zvijezda": p.Poly(Star(0.5, 0.53, 0.44, 0.19, 5)); break;
                case "srce": p.Poly(Heart(0.5, 0.52, 0.42)); break;
                case "munja": p.Poly(0.58, 0.06, 0.2, 0.56, 0.46, 0.56, 0.38, 0.94, 0.8, 0.4, 0.53, 0.4, 0.66, 0.06); break;
                case "plamen": p.Poly(Flame(0.5, 0.5, 0.45)); p.Poly(Flame(0.5, 0.62, 0.22), Hole); break;
                case "katanac":
                    p.Ring(0.5, 0.38, 0.17, 0.08);
                    p.RoundRect(0.24, 0.44, 0.52, 0.42, 0.07);
                    p.Circle(0.5, 0.6, 0.06, Hole); p.Rect(0.475, 0.6, 0.05, 0.14, Hole);
                    break;
                case "novcic":
                    p.Circle(0.5, 0.5, 0.42); p.Ring(0.5, 0.5, 0.32, 0.05, Hole);
                    p.Poly(new[] { 0.5, 0.3, 0.56, 0.44, 0.7, 0.45, 0.59, 0.54, 0.62, 0.69, 0.5, 0.61, 0.38, 0.69, 0.41, 0.54, 0.3, 0.45, 0.44, 0.44 }, Hole);
                    break;
                case "nazad": p.Line(0.72, 0.5, 0.3, 0.5, 0.12); p.Line(0.3, 0.5, 0.52, 0.27, 0.12); p.Line(0.3, 0.5, 0.52, 0.73, 0.12); break;
                case "preskoci": p.Poly(0.16, 0.22, 0.5, 0.5, 0.16, 0.78); p.Poly(0.48, 0.22, 0.82, 0.5, 0.48, 0.78); break;
                case "okreni":
                    p.Poly(Arc(0.5, 0.5, 0.34, 0.34, 200, 520, false), 0.08);
                    p.Poly(0.66, 0.06, 0.86, 0.22, 0.6, 0.3);
                    break;
                case "boja":
                    p.Poly(Blob(0.5, 0.5, 0.42));
                    p.Circle(0.33, 0.38, 0.07, Hole); p.Circle(0.5, 0.28, 0.07, Hole); p.Circle(0.67, 0.38, 0.07, Hole); p.Circle(0.62, 0.66, 0.1, Hole);
                    break;
                case "nalijepnica":
                    p.Poly(0.16, 0.16, 0.84, 0.16, 0.84, 0.58, 0.58, 0.84, 0.16, 0.84);
                    p.Poly(new[] { 0.6, 0.84, 0.6, 0.6, 0.84, 0.6 }, Hole);
                    break;
                case "iskra":
                    p.Poly(Star(0.42, 0.44, 0.34, 0.08, 4));
                    p.Poly(Star(0.76, 0.76, 0.16, 0.04, 4));
                    p.Poly(Star(0.78, 0.22, 0.1, 0.03, 4));
                    break;
                case "sapa":
                    p.Ellipse(0.5, 0.66, 0.2, 0.17); p.Circle(0.26, 0.42, 0.08); p.Circle(0.41, 0.28, 0.085); p.Circle(0.59, 0.28, 0.085); p.Circle(0.74, 0.42, 0.08);
                    break;
                case "kapa":
                    p.Poly(Arc(0.5, 0.62, 0.3, 0.36, 180, 360, true));
                    p.RoundRect(0.12, 0.6, 0.76, 0.1, 0.05);
                    break;
                case "naocare":
                    p.Ring(0.3, 0.55, 0.15, 0.07); p.Ring(0.7, 0.55, 0.15, 0.07);
                    p.Line(0.44, 0.52, 0.56, 0.52, 0.06); p.Line(0.15, 0.5, 0.06, 0.4, 0.05); p.Line(0.85, 0.5, 0.94, 0.4, 0.05);
                    break;
                case "mapa":
                    p.Poly(0.1, 0.22, 0.36, 0.14, 0.64, 0.24, 0.9, 0.16, 0.9, 0.78, 0.64, 0.86, 0.36, 0.76, 0.1, 0.84);
                    p.Line(0.36, 0.18, 0.36, 0.74, 0.04, Hole); p.Line(0.64, 0.28, 0.64, 0.82, 0.04, Hole);
                    break;
                case "sat": p.Ring(0.5, 0.52, 0.36, 0.08); p.Line(0.5, 0.52, 0.5, 0.3, 0.07); p.Line(0.5, 0.52, 0.66, 0.6, 0.07); p.Rect(0.42, 0.06, 0.16, 0.08); break;
                case "kvacica": p.Line(0.2, 0.52, 0.42, 0.74, 0.13); p.Line(0.42, 0.74, 0.82, 0.28, 0.13); break;
                case "zatvori": p.Line(0.24, 0.24, 0.76, 0.76, 0.12); p.Line(0.76, 0.24, 0.24, 0.76, 0.12); break;
                case "plus": p.Line(0.2, 0.5, 0.8, 0.5, 0.14); p.Line(0.5, 0.2, 0.5, 0.8, 0.14); break;
                case "minus": p.Line(0.2, 0.5, 0.8, 0.5, 0.14); break;
                case "lupa": p.Ring(0.42, 0.42, 0.24, 0.08); p.Line(0.6, 0.6, 0.84, 0.84, 0.12); break;
                case "cvijet":
                    for (int i = 0; i < 5; i++) { double a = -Math.PI / 2 + i * Math.PI * 2 / 5; p.Circle(0.5 + Math.Cos(a) * 0.22, 0.5 + Math.Sin(a) * 0.22, 0.17); }
                    p.Circle(0.5, 0.5, 0.11, Hole); p.Circle(0.5, 0.5, 0.07);
                    break;
                case "smajli":
                    p.Circle(0.5, 0.5, 0.44);
                    p.Circle(0.36, 0.4, 0.06, Hole); p.Circle(0.64, 0.4, 0.06, Hole);
                    p.Poly(Arc(0.5, 0.56, 0.22, 0.2, 10, 170, false), 0.07, Hole);
                    break;
                case "kruna": p.Poly(0.12, 0.3, 0.32, 0.5, 0.5, 0.18, 0.68, 0.5, 0.88, 0.3, 0.8, 0.78, 0.2, 0.78); p.Circle(0.5, 0.6, 0.06, Hole); break;
                case "duga":
                    p.Poly(Arc(0.5, 0.78, 0.4, 0.4, 180, 360, false), 0.09);
                    p.Poly(Arc(0.5, 0.78, 0.27, 0.27, 180, 360, false), 0.09);
                    p.Poly(Arc(0.5, 0.78, 0.14, 0.14, 180, 360, false), 0.09);
                    break;
                case "sara":
                    p.RoundRect(0.1, 0.18, 0.8, 0.64, 0.1);
                    p.Poly(new[] { 0.3, 0.18, 0.42, 0.18, 0.24, 0.82, 0.12, 0.82 }, Hole);
                    p.Poly(new[] { 0.58, 0.18, 0.7, 0.18, 0.52, 0.82, 0.4, 0.82 }, Hole);
                    break;
                case "sedam":
                    p.Circle(0.5, 0.5, 0.44);
                    p.Poly(new[] { 0.32, 0.26, 0.7, 0.26, 0.7, 0.36, 0.5, 0.78, 0.38, 0.78, 0.57, 0.37, 0.32, 0.37 }, Hole);
                    break;
                case "zastava":
                    p.RoundRect(0.08, 0.2, 0.84, 0.6, 0.04);
                    p.Poly(new[] { 0.16, 0.28, 0.84, 0.28, 0.84, 0.72, 0.16, 0.72 }, Hole);
                    p.Circle(0.5, 0.5, 0.12);
                    break;
                case "pruge":
                    p.RoundRect(0.1, 0.18, 0.8, 0.64, 0.1);
                    p.Rect(0.1, 0.37, 0.8, 0.07, Hole); p.Rect(0.1, 0.56, 0.8, 0.07, Hole);
                    break;
                case "tacke":
                    for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) p.Circle(0.22 + i * 0.28 + (j % 2) * 0.06, 0.22 + j * 0.28, 0.1);
                    break;
                case "sah":
                    for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) if ((i + j) % 2 == 0) p.Rect(0.1 + i * 0.2, 0.1 + j * 0.2, 0.2, 0.2);
                    break;
                default: p.Circle(0.5, 0.5, 0.4); break;
            }
            return cv;
        }

        // ---- oblici (koordinate 0..1, y nadolje) ----
        public static List<Vec2d> Star(double cx, double cy, double r, double ri, int points)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i < points * 2; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / points;
                double rr = i % 2 == 0 ? r : ri;
                l.Add(new Vec2d(cx + Math.Cos(a) * rr, cy + Math.Sin(a) * rr));
            }
            return l;
        }

        public static List<Vec2d> Heart(double cx, double cy, double s)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i < 48; i++)
            {
                double t = i * Math.PI * 2 / 48;
                double x = 16 * Math.Pow(Math.Sin(t), 3);
                double y = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
                l.Add(new Vec2d(cx + x / 17.0 * s, cy - y / 17.0 * s - s * 0.08));
            }
            return l;
        }

        public static List<Vec2d> Flame(double cx, double cy, double s)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i < 40; i++)
            {
                double t = i * Math.PI * 2 / 40;
                double x = Math.Sin(t) * 0.62;
                double y = -Math.Cos(t);
                if (y < 0) { x *= 1 + y * 0.9; y *= 1.35; x += Math.Sin(y * 3.2) * 0.12 * -y; }
                l.Add(new Vec2d(cx + x * s, cy + y * s * 0.8 + s * 0.12));
            }
            return l;
        }

        static List<Vec2d> Blob(double cx, double cy, double r)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i < 40; i++)
            {
                double a = i * Math.PI * 2 / 40;
                double rr = r * (1 + 0.06 * Math.Sin(a * 3)) * (a > 0.6 && a < 1.3 ? 0.84 : 1);
                l.Add(new Vec2d(cx + Math.Cos(a) * rr, cy + Math.Sin(a) * rr * 0.86));
            }
            return l;
        }

        // Luk elipse od a0 do a1 stepeni (y nadolje); closed = dodaj centar (isječak).
        static List<Vec2d> Arc(double cx, double cy, double rx, double ry, double a0, double a1, bool closed)
        {
            var l = new List<Vec2d>();
            int n = Math.Max(8, (int)(Math.Abs(a1 - a0) / 8));
            for (int i = 0; i <= n; i++)
            {
                double a = (a0 + (a1 - a0) * i / n) * Math.PI / 180;
                l.Add(new Vec2d(cx + Math.Cos(a) * rx, cy + Math.Sin(a) * ry));
            }
            if (closed) l.Add(new Vec2d(cx, cy));
            return l;
        }

        sealed class P
        {
            readonly Canvas32 cv; readonly double s;
            public P(Canvas32 cv, double s) { this.cv = cv; this.s = s; }

            List<Vec2d> Sc(IList<Vec2d> pts) { var o = new List<Vec2d>(pts.Count); foreach (var q in pts) o.Add(new Vec2d(q.X * s, q.Y * s)); return o; }

            public void Poly(params double[] xy) => Poly(xy, W);
            public void Poly(double[] xy, Rgba c)
            {
                var l = new List<Vec2d>();
                for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vec2d(xy[i], xy[i + 1]));
                Poly(l, c);
            }
            public void Poly(IList<Vec2d> pts) => Poly(pts, W);
            public void Poly(IList<Vec2d> pts, Rgba c)
            {
                if (c.A == 0) Cut(Sc(pts));
                else cv.Fill(new List<IList<Vec2d>> { Sc(pts) }, c);
            }
            // otvorena linija (luk) date debljine
            public void Poly(IList<Vec2d> pts, double width, Rgba? c = null)
            {
                var col = c ?? W;
                for (int i = 0; i + 1 < pts.Count; i++) Line(pts[i].X, pts[i].Y, pts[i + 1].X, pts[i + 1].Y, width, col);
            }

            public void Circle(double x, double y, double r) => Circle(x, y, r, W);
            public void Circle(double x, double y, double r, Rgba c)
            {
                var l = new List<Vec2d>();
                for (int i = 0; i < 40; i++) { double a = i * Math.PI * 2 / 40; l.Add(new Vec2d(x + Math.Cos(a) * r, y + Math.Sin(a) * r)); }
                Poly(l, c);
            }
            public void Ellipse(double x, double y, double rx, double ry) => Poly(Ell(x, y, rx, ry));
            public void Ellipse(double x, double y, double rx, double ry, double width)
            {
                var e = Ell(x, y, rx, ry); e.Add(e[0]);
                Poly(e, width);
            }
            static List<Vec2d> Ell(double x, double y, double rx, double ry)
            {
                var l = new List<Vec2d>();
                for (int i = 0; i < 40; i++) { double a = i * Math.PI * 2 / 40; l.Add(new Vec2d(x + Math.Cos(a) * rx, y + Math.Sin(a) * ry)); }
                return l;
            }
            public void Ring(double x, double y, double r, double w) => Ring(x, y, r, w, W);
            public void Ring(double x, double y, double r, double w, Rgba c)
            {
                var e = Ell(x, y, r, r); e.Add(e[0]);
                Poly(e, w, c);
            }
            public void Line(double x1, double y1, double x2, double y2, double w) => Line(x1, y1, x2, y2, w, W);
            public void Line(double x1, double y1, double x2, double y2, double w, Rgba c)
            {
                if (c.A == 0) { CutLine(x1 * s, y1 * s, x2 * s, y2 * s, w * s); return; }
                cv.Line(new Vec2d(x1 * s, y1 * s), new Vec2d(x2 * s, y2 * s), w * s, c);
            }
            public void Rect(double x, double y, double w, double h) => Rect(x, y, w, h, W);
            public void Rect(double x, double y, double w, double h, Rgba c)
            {
                if (w <= 0 || h <= 0) return;
                Poly(new[] { x, y, x + w, y, x + w, y + h, x, y + h }, c);
            }
            public void RoundRect(double x, double y, double w, double h, double r)
            {
                var l = new List<Vec2d>();
                void Corner(double cx, double cy, double a0)
                {
                    for (int i = 0; i <= 6; i++) { double a = (a0 + i * 15) * Math.PI / 180; l.Add(new Vec2d(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r)); }
                }
                Corner(x + w - r, y + r, 270); Corner(x + w - r, y + h - r, 0); Corner(x + r, y + h - r, 90); Corner(x + r, y + r, 180);
                Poly(l);
            }

            // brisanje (rupa): pikseli unutar oblika postaju providni
            void Cut(List<Vec2d> ring)
            {
                var mask = new Canvas32(cv.W, cv.H);
                mask.Fill(new List<IList<Vec2d>> { ring }, W);
                ApplyCut(mask);
            }
            void CutLine(double x1, double y1, double x2, double y2, double w)
            {
                var mask = new Canvas32(cv.W, cv.H);
                mask.Line(new Vec2d(x1, y1), new Vec2d(x2, y2), w, W);
                ApplyCut(mask);
            }
            void ApplyCut(Canvas32 mask)
            {
                for (int i = 3; i < cv.Data.Length; i += 4)
                {
                    int m = mask.Data[i];
                    if (m > 0) cv.Data[i] = (byte)(cv.Data[i] * (255 - m) / 255);
                }
            }
        }
    }
}

namespace TrkaZnanja.Core
{
    // Trouglovi za ravan poligon (odsijecanje „ušiju”), za nalijepnice i šare na 3D vozilima.
    public static class Triangulator
    {
        public static List<int> Triangulate(IList<Vec2d> pts)
        {
            var res = new List<int>();
            int n = pts.Count;
            if (n < 3) return res;
            var idx = new List<int>();
            for (int i = 0; i < n; i++) idx.Add(i);
            if (Area(pts) < 0) idx.Reverse();      // radimo u smjeru suprotnom od kazaljke (pozitivna površina)
            int guard = 0;
            while (idx.Count > 3 && guard++ < n * n)
            {
                bool cut = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    if (Cross(pts[a], pts[b], pts[c]) <= 1e-12) continue;          // udubljen ugao
                    bool inside = false;
                    for (int j = 0; j < idx.Count && !inside; j++)
                    {
                        int q = idx[j];
                        if (q == a || q == b || q == c) continue;
                        inside = InTri(pts[q], pts[a], pts[b], pts[c]);
                    }
                    if (inside) continue;
                    res.Add(a); res.Add(b); res.Add(c);
                    idx.RemoveAt(i);
                    cut = true;
                    break;
                }
                if (!cut) break;   // degenerisan oblik: ostatak kao lepeza
            }
            for (int i = 1; i + 1 < idx.Count; i++) { res.Add(idx[0]); res.Add(idx[i]); res.Add(idx[i + 1]); }
            return res;
        }

        public static double Area(IList<Vec2d> p)
        {
            double s = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += (p[j].X * p[i].Y - p[i].X * p[j].Y);
            return s / 2;
        }

        static double Cross(Vec2d a, Vec2d b, Vec2d c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        static bool InTri(Vec2d p, Vec2d a, Vec2d b, Vec2d c)
        {
            double d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
            return d1 >= 0 && d2 >= 0 && d3 >= 0;
        }
    }
}
