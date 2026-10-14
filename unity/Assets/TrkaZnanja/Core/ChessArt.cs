// Šahovske figure nacrtane kodom (originalni crtež igre): bijele su svijetle sa tamnim obrisom, crne tamne sa svijetlim detaljima.
// Koordinate oblika su od 0 do 1 (y raste nadolje), pa se figure mogu nacrtati u bilo kojoj veličini.
using System;
using System.Collections.Generic;

namespace TrkaZnanja.Core
{
    public static class ChessArt
    {
        static readonly Rgba WhiteFill = Rgba.Hex("#fbf8ef"), WhiteLine = Rgba.Hex("#26313b"),
            BlackFill = Rgba.Hex("#2b3640"), BlackLine = Rgba.Hex("#0f161c"), BlackDetail = Rgba.Hex("#e9edf0");

        public static Canvas32 Draw(char piece, int size)
        {
            var cv = new Canvas32(size, size);
            bool white = char.IsUpper(piece);
            var p = new Painter(cv, size, white ? WhiteFill : BlackFill, white ? WhiteLine : BlackLine, white ? WhiteLine : BlackDetail);
            switch (char.ToUpperInvariant(piece))
            {
                case 'P': Pawn(p); break;
                case 'R': Rook(p); break;
                case 'N': Knight(p); break;
                case 'B': Bishop(p); break;
                case 'Q': Queen(p); break;
                default: King(p); break;
            }
            return cv;
        }

        sealed class Painter
        {
            readonly Canvas32 cv; readonly double s; readonly Rgba fill, line, detail;
            public double Lw => s * 0.034;
            public Painter(Canvas32 cv, double s, Rgba fill, Rgba line, Rgba detail) { this.cv = cv; this.s = s; this.fill = fill; this.line = line; this.detail = detail; }

            List<Vec2d> Sc(IList<Vec2d> pts) { var o = new List<Vec2d>(pts.Count); foreach (var q in pts) o.Add(new Vec2d(q.X * s, q.Y * s)); return o; }

            // oblik sa obrisom
            public void Shape(params double[] xy)
            {
                var r = Sc(P(xy));
                cv.Fill(new List<IList<Vec2d>> { r }, fill);
                cv.Stroke(r, Lw, line);
            }

            public void Ellipse(double cx, double cy, double rx, double ry)
            {
                var r = Sc(EllipsePts(cx, cy, rx, ry));
                cv.Fill(new List<IList<Vec2d>> { r }, fill);
                cv.Stroke(r, Lw, line);
            }

            // tanka linija detalja (kod crnih figura svijetla)
            public void Detail(double x1, double y1, double x2, double y2, double k = 0.7) => cv.Line(new Vec2d(x1 * s, y1 * s), new Vec2d(x2 * s, y2 * s), Lw * k, detail);
            public void Dot(double x, double y, double r) => cv.Disc(new Vec2d(x * s, y * s), r * s, detail);
        }

        static List<Vec2d> P(double[] xy)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vec2d(xy[i], xy[i + 1]));
            return l;
        }

        static List<Vec2d> EllipsePts(double cx, double cy, double rx, double ry, int n = 40)
        {
            var l = new List<Vec2d>(n);
            for (int i = 0; i < n; i++) { double a = i * 2 * Math.PI / n; l.Add(new Vec2d(cx + Math.Cos(a) * rx, cy + Math.Sin(a) * ry)); }
            return l;
        }

        // postolje: široka ploča i stepenik iznad nje
        static void Base(Painter p, double w = 0.30)
        {
            p.Shape(0.5 - w, 0.92, 0.5 + w, 0.92, 0.5 + w, 0.84, 0.5 - w, 0.84);
            p.Shape(0.5 - w + 0.05, 0.84, 0.5 + w - 0.05, 0.84, 0.5 + w - 0.08, 0.78, 0.5 - w + 0.08, 0.78);
        }

        static void Pawn(Painter p)
        {
            Base(p, 0.25);
            p.Shape(0.36, 0.78, 0.64, 0.78, 0.57, 0.50, 0.43, 0.50);
            p.Ellipse(0.5, 0.50, 0.15, 0.04);
            p.Ellipse(0.5, 0.35, 0.13, 0.13);
        }

        static void Rook(Painter p)
        {
            Base(p, 0.30);
            p.Shape(0.31, 0.78, 0.69, 0.78, 0.66, 0.40, 0.34, 0.40);
            p.Shape(0.27, 0.40, 0.73, 0.40, 0.73, 0.32, 0.27, 0.32);
            // kruna sa zupcima
            p.Shape(0.27, 0.32, 0.73, 0.32, 0.73, 0.16, 0.64, 0.16, 0.64, 0.23, 0.555, 0.23, 0.555, 0.16, 0.445, 0.16, 0.445, 0.23, 0.36, 0.23, 0.36, 0.16, 0.27, 0.16);
            p.Detail(0.34, 0.50, 0.66, 0.50);
            p.Detail(0.33, 0.66, 0.67, 0.66);
        }

        static void Bishop(Painter p)
        {
            Base(p, 0.28);
            p.Shape(0.37, 0.78, 0.63, 0.78, 0.56, 0.56, 0.44, 0.56);
            p.Ellipse(0.5, 0.56, 0.16, 0.04);
            // mitra: jaje sa šiljkom na vrhu
            var cap = new List<double>();
            for (int d = -50; d <= 230; d += 10)
            {
                double a = d * Math.PI / 180;
                cap.Add(0.5 + Math.Cos(a) * 0.14); cap.Add(0.41 + Math.Sin(a) * 0.14);
            }
            cap.Add(0.5); cap.Add(0.19);
            p.Shape(cap.ToArray());
            p.Ellipse(0.5, 0.16, 0.045, 0.045);
            // prorez
            p.Detail(0.45, 0.42, 0.57, 0.30, 0.9);
        }

        static void Knight(Painter p)
        {
            Base(p, 0.30);
            // glava konja (gleda ulijevo)
            p.Shape(0.30, 0.78, 0.36, 0.62, 0.32, 0.56, 0.22, 0.55, 0.15, 0.50, 0.14, 0.43, 0.20, 0.36, 0.30, 0.27, 0.36, 0.20,
                0.38, 0.10, 0.44, 0.17, 0.50, 0.14, 0.60, 0.17, 0.69, 0.26, 0.75, 0.40, 0.77, 0.56, 0.74, 0.70, 0.72, 0.78);
            p.Dot(0.33, 0.30, 0.025);
            p.Dot(0.18, 0.45, 0.015);
            // griva
            p.Detail(0.50, 0.20, 0.64, 0.30);
            p.Detail(0.55, 0.28, 0.69, 0.40);
            p.Detail(0.58, 0.38, 0.71, 0.52);
            p.Detail(0.30, 0.50, 0.40, 0.46);
        }

        static void Crown(Painter p, double top)
        {
            p.Shape(0.31, 0.78, 0.69, 0.78, 0.60, 0.50, 0.40, 0.50);
            p.Ellipse(0.5, 0.50, 0.17, 0.045);
            p.Shape(0.36, 0.46, 0.64, 0.46, 0.70, top, 0.30, top);
        }

        static void Queen(Painter p)
        {
            Base(p, 0.31);
            p.Shape(0.31, 0.78, 0.69, 0.78, 0.60, 0.50, 0.40, 0.50);
            p.Ellipse(0.5, 0.50, 0.17, 0.045);
            // kruna sa 5 šiljaka
            p.Shape(0.36, 0.46, 0.64, 0.46, 0.72, 0.22, 0.62, 0.33, 0.58, 0.16, 0.5, 0.32, 0.42, 0.16, 0.38, 0.33, 0.28, 0.22);
            foreach (var (x, y) in new[] { (0.28, 0.20), (0.42, 0.14), (0.58, 0.14), (0.72, 0.20) }) p.Ellipse(x, y, 0.04, 0.04);
            p.Detail(0.37, 0.41, 0.63, 0.41);
        }

        static void King(Painter p)
        {
            Base(p, 0.31);
            Crown(p, 0.28);
            p.Detail(0.37, 0.41, 0.63, 0.41);
            // krst
            p.Shape(0.47, 0.28, 0.53, 0.28, 0.53, 0.17, 0.60, 0.17, 0.60, 0.12, 0.53, 0.12, 0.53, 0.05, 0.47, 0.05, 0.47, 0.12, 0.40, 0.12, 0.40, 0.17, 0.47, 0.17);
        }
    }
}
