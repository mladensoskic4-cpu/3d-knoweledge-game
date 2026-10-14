// Ukrasi na vozilu: šare (pruge, plamen, tačkice, šahovnica, munje, zvjezdice) u boji šare i nalijepnice
// (zvijezda, srce, munja, broj 7, cvijet, smajli, zastava CG, krunica). Sve su tanki ravni oblici zalijepljeni
// na „mjesta” koja svako vozilo prijavi (bok lijevo/desno, hauba), pa rade na svih 12 vozila.
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public struct DecoSpot
    {
        public Vector3 Pos, Normal;
        public Vector3 A, C;          // A = duž vozila (naprijed), C = poprijeko (gore na boku, desno na haubi)
        public Vector3 U, V;          // osa slike nalijepnice: U desno, V gore (kako je vidi posmatrač)
        public float Len, Wid;        // dužina (duž A) i širina (duž C)
        public bool StickerOnly;      // premalo mjesto za šaru

        // bok vozila: x = ±strana, normalno na stranu
        public static void Sides(List<DecoSpot> l, float x, float y, float z, float len, float h, bool stickerOnly = false)
        {
            foreach (var s in new[] { 1, -1 })
                l.Add(new DecoSpot
                {
                    Pos = new Vector3(s * x, y, z), Normal = new Vector3(s, 0, 0),
                    A = Vector3.forward, C = Vector3.up, U = s > 0 ? Vector3.forward : Vector3.back, V = Vector3.up,
                    Len = len, Wid = h, StickerOnly = stickerOnly,
                });
        }

        // gornja površina (hauba, krov): gleda se odozgo, sa zadnje strane
        public static void Top(List<DecoSpot> l, float x, float y, float z, float len, float w, bool stickerOnly = false)
        {
            l.Add(new DecoSpot
            {
                Pos = new Vector3(x, y, z), Normal = Vector3.up, A = Vector3.forward, C = Vector3.right, U = Vector3.right, V = Vector3.forward,
                Len = len, Wid = w, StickerOnly = stickerOnly,
            });
        }

        // prednja površina (štit skutera): samo nalijepnica
        public static void Front(List<DecoSpot> l, float x, float y, float z, float w, float h)
        {
            l.Add(new DecoSpot
            {
                Pos = new Vector3(x, y, z), Normal = Vector3.forward, A = Vector3.up, C = Vector3.left, U = Vector3.left, V = Vector3.up,
                Len = h, Wid = w, StickerOnly = true,
            });
        }
    }

    public static class Decor
    {
        static Color32 C(string h, float glow = 0) => MeshKit.C(h, glow);
        const float Eps = 0.0022f, Layer = 0.0014f;

        public static void Draw(MeshKit k, List<DecoSpot> spots, Look look)
        {
            if (look == null) return;
            var col = C(string.IsNullOrEmpty(look.Boja2) ? "#ffffff" : look.Boja2);
            if (!string.IsNullOrEmpty(look.Sara) && look.Sara != "nema")
                foreach (var s in spots) if (!s.StickerOnly) Pattern(k, s, look.Sara, col);
            if (!string.IsNullOrEmpty(look.Nalijepnica) && look.Nalijepnica != "nema")
            {
                // na bokovima (i na haubi, ako je ima); kod malih mjesta samo jedna po strani
                foreach (var s in spots) Sticker(k, s, look.Nalijepnica, look.Sara != "nema" && !s.StickerOnly);
            }
        }

        // ---- šare (koordinate a = duž vozila, c = poprijeko, u metrima od centra mjesta) ----
        static void Pattern(MeshKit k, DecoSpot s, string id, Color32 col)
        {
            float L = s.Len, W = s.Wid;
            switch (id)
            {
                case "pruge":
                    if (s.Normal.y > 0.5f) { Rect(k, s, 0, -W * 0.16f, L, W * 0.16f, col, 0); Rect(k, s, 0, W * 0.16f, L, W * 0.16f, col, 0); }
                    else { Rect(k, s, 0, W * 0.18f, L, W * 0.14f, col, 0); Rect(k, s, 0, -W * 0.12f, L, W * 0.1f, col, 0); }
                    break;
                case "tacke":
                    {
                        float r = Mathf.Min(W, L) * 0.13f;
                        int nx = Mathf.Max(1, Mathf.RoundToInt(L / (r * 3.2f))), ny = Mathf.Max(1, Mathf.RoundToInt(W / (r * 3.2f)));
                        for (int i = 0; i < nx; i++)
                            for (int j = 0; j < ny; j++)
                            {
                                float a = (i + 0.5f) / nx * L - L / 2 + ((j % 2) * 0.5f - 0.25f) * (L / nx);
                                float c = (j + 0.5f) / ny * W - W / 2;
                                Disc(k, s, a, c, r, col, 0);
                            }
                        break;
                    }
                case "plamen":
                    {
                        // jezici plamena polaze od prednjeg dijela i ližu unazad
                        int n = 3;
                        for (int i = 0; i < n; i++)
                        {
                            float c = (i - (n - 1) / 2f) * W * 0.3f;
                            float len = L * (i == 1 ? 0.7f : 0.52f);
                            var pts = new List<Vector2>();
                            float a0 = L / 2 - L * 0.06f;
                            int seg = 10;
                            for (int q = 0; q <= seg; q++)
                            {
                                float t = q / (float)seg;
                                float half = W * 0.17f * Mathf.Sin(Mathf.PI * Mathf.Pow(1 - t, 0.6f) * 0.5f + 0.2f) * (1 - t * 0.85f);
                                pts.Add(new Vector2(a0 - t * len, c + half + Mathf.Sin(t * 9) * W * 0.03f));
                            }
                            for (int q = seg; q >= 0; q--)
                            {
                                float t = q / (float)seg;
                                float half = W * 0.17f * Mathf.Sin(Mathf.PI * Mathf.Pow(1 - t, 0.6f) * 0.5f + 0.2f) * (1 - t * 0.85f);
                                pts.Add(new Vector2(a0 - t * len, c - half + Mathf.Sin(t * 9) * W * 0.03f));
                            }
                            Poly(k, s, pts, col, 0);
                            Poly(k, s, Shrink(pts, 0.5f, new Vector2(a0 - len * 0.25f, c)), C("#ffd23f"), 1);
                        }
                        break;
                    }
                case "sah":
                    {
                        float sq = Mathf.Min(W * 0.28f, L / 8);
                        int n = Mathf.Max(2, Mathf.FloorToInt(L / sq));
                        float start = -n * sq / 2;
                        for (int i = 0; i < n; i++)
                            for (int j = 0; j < 2; j++)
                                Rect(k, s, start + (i + 0.5f) * sq, (j - 0.5f) * sq, sq * 0.98f, sq * 0.98f, (i + j) % 2 == 0 ? col : C("#14232e"), 0);
                        break;
                    }
                case "munje":
                    {
                        // cik-cak traka duž vozila
                        var pts = new List<Vector2>();
                        int z = 5;
                        float h = W * 0.5f, t = W * 0.17f;
                        for (int i = 0; i <= z; i++) pts.Add(new Vector2(-L / 2 + i * L / z, (i % 2 == 0 ? h : -h) * 0.5f + t));
                        for (int i = z; i >= 0; i--) pts.Add(new Vector2(-L / 2 + i * L / z, (i % 2 == 0 ? h : -h) * 0.5f - t));
                        for (int i = 0; i < z; i++)
                        {
                            var quad = new List<Vector2> { pts[i], pts[i + 1], pts[pts.Count - 2 - i], pts[pts.Count - 1 - i] };
                            Poly(k, s, quad, col, 0);
                        }
                        break;
                    }
                case "zvijezde":
                    {
                        var rng = new System.Random(7);
                        int n = Mathf.Clamp(Mathf.RoundToInt(L * W / 0.0025f), 2, 7);
                        for (int i = 0; i < n; i++)
                        {
                            float a = (i + 0.5f) / n * L - L / 2;
                            float c = ((float)rng.NextDouble() - 0.5f) * W * 0.5f;
                            float r = Mathf.Min(W, L) * (0.16f + (float)rng.NextDouble() * 0.1f);
                            Poly(k, s, Shape(Icons.Star(0, 0, 1, 0.45, 5), a, c, r, s.Normal.y > 0.5f), col, 0);
                        }
                        break;
                    }
            }
        }

        // ---- nalijepnice (slika gleda posmatrača: U desno, V gore) ----
        static void Sticker(MeshKit k, DecoSpot s, string id, bool overPattern)
        {
            float size = Mathf.Min(s.Wid * 0.95f, s.Len * 0.55f);
            int lay = overPattern ? 2 : 0;
            // centar: na boku malo iza sredine (da ne prekrije točak), na haubi u sredini
            Vector2 at = Vector2.zero;
            void L(IList<Vec2d> icon, Color32 c, int layer, float scale = 1, float dx = 0, float dy = 0) => SPoly(k, s, icon, at + new Vector2(dx, dy) * size, size * scale, c, lay + layer);
            switch (id)
            {
                case "zvijezda":
                    L(Icons.Star(0, 0, 0.5, 0.22, 5), C("#14232e"), 0, 1.12f);
                    L(Icons.Star(0, 0, 0.5, 0.22, 5), C("#ffd23f"), 1);
                    break;
                case "srce":
                    L(Icons.Heart(0, 0, 0.5), C("#ffffff"), 0, 1.15f);
                    L(Icons.Heart(0, 0, 0.5), C("#ff3b6b"), 1);
                    break;
                case "munja":
                    {
                        var bolt = Pts(0.58, 0.06, 0.2, 0.56, 0.46, 0.56, 0.38, 0.94, 0.8, 0.4, 0.53, 0.4, 0.66, 0.06);
                        L(Centered(bolt), C("#14232e"), 0, 1.12f);
                        L(Centered(bolt), C("#ffd23f"), 1);
                        break;
                    }
                case "broj":
                    L(Circle(0.5), C("#ffffff"), 0);
                    SRing(k, s, at, size, 0.5f, 0.41f, C("#e0312f"), lay + 1);
                    L(Centered(Pts(0.3, 0.22, 0.72, 0.22, 0.72, 0.32, 0.5, 0.8, 0.38, 0.8, 0.58, 0.33, 0.3, 0.33)), C("#14232e"), 1, 0.8f);
                    break;
                case "cvijet":
                    for (int i = 0; i < 5; i++)
                    {
                        float a = -Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                        L(Circle(0.2), C("#ff6fae"), 0, 1, Mathf.Cos(a) * 0.24f, -Mathf.Sin(a) * 0.24f);
                    }
                    L(Circle(0.16), C("#ffd23f"), 1);
                    break;
                case "smajli":
                    L(Circle(0.5), C("#14232e"), 0, 1.1f);
                    L(Circle(0.5), C("#ffd23f"), 1);
                    L(Circle(0.07), C("#14232e"), 2, 1, -0.15f, 0.12f);
                    L(Circle(0.07), C("#14232e"), 2, 1, 0.15f, 0.12f);
                    L(Smile(), C("#14232e"), 2);
                    break;
                case "cg":
                    L(Centered(Pts(0, 0, 1.5, 0, 1.5, 1, 0, 1)), C("#d3ae3b"), 0, 0.82f);
                    L(Centered(Pts(0, 0, 1.5, 0, 1.5, 1, 0, 1)), C("#c8102e"), 1, 0.7f);
                    L(Circle(0.16), C("#d3ae3b"), 2);
                    break;
                case "kruna":
                    {
                        var crown = Pts(0.12, 0.3, 0.32, 0.5, 0.5, 0.18, 0.68, 0.5, 0.88, 0.3, 0.8, 0.78, 0.2, 0.78);
                        L(Centered(crown), C("#14232e"), 0, 1.12f);
                        L(Centered(crown), C("#f2c230"), 1);
                        break;
                    }
            }
        }

        // ---- pomoćno: oblici u ravni mjesta ----
        static Vector3 At(DecoSpot s, float a, float c, int layer) => s.Pos + s.A * a + s.C * c + s.Normal * (Eps + layer * Layer);
        static Vector3 AtUV(DecoSpot s, Vector2 uv, int layer) => s.Pos + s.U * uv.x + s.V * uv.y + s.Normal * (Eps + layer * Layer);

        static void Tri(MeshKit k, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal, Color32 col)
        {
            // prednja strana prema spolja (normali mjesta)
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) > 0) k.Triangle(p0, p1, p2, col);
            else k.Triangle(p0, p2, p1, col);
        }

        static void Poly(MeshKit k, DecoSpot s, IList<Vector2> ac, Color32 col, int layer)
        {
            var pts = new List<Vec2d>(ac.Count);
            foreach (var p in ac) pts.Add(new Vec2d(p.x, p.y));
            var tris = Triangulator.Triangulate(pts);
            for (int i = 0; i + 2 < tris.Count; i += 3)
                Tri(k, At(s, ac[tris[i]].x, ac[tris[i]].y, layer), At(s, ac[tris[i + 1]].x, ac[tris[i + 1]].y, layer), At(s, ac[tris[i + 2]].x, ac[tris[i + 2]].y, layer), s.Normal, col);
        }

        // ikonica (koordinate centrirane, y nadolje) u ravni slike nalijepnice
        static void SPoly(MeshKit k, DecoSpot s, IList<Vec2d> icon, Vector2 center, float size, Color32 col, int layer)
        {
            var uv = new List<Vector2>(icon.Count);
            foreach (var p in icon) uv.Add(center + new Vector2((float)p.X, -(float)p.Y) * size);
            var pts = new List<Vec2d>(uv.Count);
            foreach (var p in uv) pts.Add(new Vec2d(p.x, p.y));
            var tris = Triangulator.Triangulate(pts);
            for (int i = 0; i + 2 < tris.Count; i += 3)
                Tri(k, AtUV(s, uv[tris[i]], layer), AtUV(s, uv[tris[i + 1]], layer), AtUV(s, uv[tris[i + 2]], layer), s.Normal, col);
        }

        static void Rect(MeshKit k, DecoSpot s, float a, float c, float len, float wid, Color32 col, int layer)
        {
            var p = new List<Vector2> { new Vector2(a - len / 2, c - wid / 2), new Vector2(a + len / 2, c - wid / 2), new Vector2(a + len / 2, c + wid / 2), new Vector2(a - len / 2, c + wid / 2) };
            Poly(k, s, p, col, layer);
        }

        static void Disc(MeshKit k, DecoSpot s, float a, float c, float r, Color32 col, int layer)
        {
            var p = new List<Vector2>();
            for (int i = 0; i < 14; i++) { float t = i * Mathf.PI * 2 / 14; p.Add(new Vector2(a + Mathf.Cos(t) * r, c + Mathf.Sin(t) * r)); }
            Poly(k, s, p, col, layer);
        }

        static List<Vector2> Shape(IList<Vec2d> icon, float a, float c, float r, bool top)
        {
            var l = new List<Vector2>(icon.Count);
            foreach (var p in icon) l.Add(new Vector2(a + (float)p.X * r, c - (float)p.Y * r * (top ? 1 : 1)));
            return l;
        }

        static List<Vector2> Shrink(List<Vector2> pts, float k, Vector2 toward)
        {
            var l = new List<Vector2>(pts.Count);
            foreach (var p in pts) l.Add(Vector2.Lerp(toward, p, k));
            return l;
        }

        static List<Vec2d> Pts(params double[] xy)
        {
            var l = new List<Vec2d>();
            for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vec2d(xy[i], xy[i + 1]));
            return l;
        }

        // pomjeri oblik iz 0..1 tako da mu je centar (0,0)
        static List<Vec2d> Centered(List<Vec2d> pts)
        {
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var p in pts) { minX = System.Math.Min(minX, p.X); maxX = System.Math.Max(maxX, p.X); minY = System.Math.Min(minY, p.Y); maxY = System.Math.Max(maxY, p.Y); }
            double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2, sc = 1 / System.Math.Max(maxX - minX, maxY - minY);
            var l = new List<Vec2d>(pts.Count);
            foreach (var p in pts) l.Add(new Vec2d((p.X - cx) * sc, (p.Y - cy) * sc));
            return l;
        }

        static List<Vec2d> Circle(double r, int n = 20)
        {
            var l = new List<Vec2d>(n);
            for (int i = 0; i < n; i++) { double t = i * System.Math.PI * 2 / n; l.Add(new Vec2d(System.Math.Cos(t) * r, System.Math.Sin(t) * r)); }
            return l;
        }

        // prsten (obod nalijepnice) kao niz četvorouglova
        static void SRing(MeshKit k, DecoSpot s, Vector2 center, float size, float r, float ri, Color32 col, int layer, int n = 24)
        {
            for (int i = 0; i < n; i++)
            {
                float t0 = i * Mathf.PI * 2 / n, t1 = (i + 1) * Mathf.PI * 2 / n;
                Vector2 o0 = center + new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * r * size, o1 = center + new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * r * size;
                Vector2 i0 = center + new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * ri * size, i1 = center + new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * ri * size;
                Tri(k, AtUV(s, o0, layer), AtUV(s, o1, layer), AtUV(s, i1, layer), s.Normal, col);
                Tri(k, AtUV(s, o0, layer), AtUV(s, i1, layer), AtUV(s, i0, layer), s.Normal, col);
            }
        }

        static List<Vec2d> Smile()
        {
            var l = new List<Vec2d>();
            for (int i = 0; i <= 10; i++) { double t = (20 + i * 14) * System.Math.PI / 180; l.Add(new Vec2d(System.Math.Cos(t) * 0.27, 0.05 + System.Math.Sin(t) * 0.22)); }
            for (int i = 10; i >= 0; i--) { double t = (20 + i * 14) * System.Math.PI / 180; l.Add(new Vec2d(System.Math.Cos(t) * 0.2, 0.05 + System.Math.Sin(t) * 0.14)); }
            return l;
        }
    }
}
