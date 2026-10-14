// Crta 2D mapu (Crna Gora, Srbija, Balkan) u teksturu: more sa mrežom, države (istaknuta zelenom), jezera i granice.
// Crta se jednom po mapi i čuva, pa je pitanje „tapni na mapi” odmah spremno.
using System;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class MapPainter
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        public const int Width = 2048;    // dovoljno oštro i kad se mapa uveća

        static readonly Color32 Sea = Col.Hex("#bfe3f2"), Grid = Col.Hex("#a6d4e8"), Land = Col.Hex("#e8f1df"), Edge = Col.Hex("#a9c49a"),
            Focus = Col.Hex("#9fd07e"), FocusEdge = Col.Hex("#3f7a3a"), Lake = Col.Hex("#8fcbe8");

        public static int HeightFor(GeoMap m) => Mathf.RoundToInt(Width * (float)new Geo.Projector(m, 100, 100, 0).Aspect);

        public static Texture2D Get(GeoMap m)
        {
            if (cache.TryGetValue(m.Id, out var t) && t != null) return t;
            int w = Width, h = HeightFor(m);
            var proj = new Geo.Projector(m, w, h, 0);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Sea;
            // mreža meridijana i paralela
            for (double lon = Math.Ceiling(m.MinLon / m.Grid) * m.Grid; lon < m.MaxLon; lon += m.Grid)
            {
                int x = (int)proj.ToXY(lon, m.MaxLat).X;
                for (int y = 0; y < h; y++) Put(px, w, h, x, y, Grid);
            }
            for (double lat = Math.Ceiling(m.MinLat / m.Grid) * m.Grid; lat < m.MaxLat; lat += m.Grid)
            {
                int y = (int)proj.ToXY(m.MinLon, lat).Y;
                for (int x = 0; x < w; x++) Put(px, w, h, x, y, Grid);
            }
            var b = GameData.Borders(m.Id);
            if (b.Countries.Count > 0)
            {
                foreach (var c in b.Countries)
                {
                    bool focus = m.Focus != null ? c.Name == m.Focus : (c.Name == "Crna Gora" || c.Name == "Srbija");
                    foreach (var poly in c.Polygons) Fill(px, w, h, proj, poly, focus ? Focus : Land);
                }
                foreach (var c in b.Countries)
                {
                    bool focus = m.Focus != null ? c.Name == m.Focus : (c.Name == "Crna Gora" || c.Name == "Srbija");
                    foreach (var poly in c.Polygons) foreach (var ring in poly) Stroke(px, w, h, proj, ring, focus ? FocusEdge : Edge, focus ? 3 : 2);
                }
            }
            else
            {
                Fill(px, w, h, proj, new List<IList<Vec2d>> { GeoData.BalkanLand }, Land);
                Fill(px, w, h, proj, new List<IList<Vec2d>> { GeoData.Italy }, Land);
            }
            foreach (var lake in GeoData.LakesFor("cg")) { Fill(px, w, h, proj, new List<IList<Vec2d>> { lake }, Lake); Stroke(px, w, h, proj, lake, Edge, 2); }

            // y u teksturi raste nagore, a projekcija nadolje: okreni
            var flipped = new Color32[px.Length];
            for (int y = 0; y < h; y++) Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
            t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Mapa " + m.Id, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels32(flipped);
            t.Apply();
            cache[m.Id] = t;
            return t;
        }

        static void Put(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
        }

        static void Fill(Color32[] px, int w, int h, Geo.Projector proj, IList<IList<Vec2d>> rings, Color32 col)
        {
            var pr = new List<List<Vector2>>();
            foreach (var r in rings)
            {
                var l = new List<Vector2>(r.Count);
                foreach (var p in r) { var q = proj.ToXY(p.X, p.Y); l.Add(new Vector2((float)q.X, (float)q.Y)); }
                pr.Add(l);
            }
            var xs = new List<float>();
            for (int y = 0; y < h; y++)
            {
                float yc = y + 0.5f;
                xs.Clear();
                foreach (var r in pr)
                    for (int i = 0, j = r.Count - 1; i < r.Count; j = i++)
                        if ((r[i].y > yc) != (r[j].y > yc)) xs.Add(r[i].x + (yc - r[i].y) * (r[j].x - r[i].x) / (r[j].y - r[i].y));
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    for (int x = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f)); x <= Mathf.Min(w - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f)); x++)
                        px[y * w + x] = col;
            }
        }

        static void Stroke(Color32[] px, int w, int h, Geo.Projector proj, IList<Vec2d> ring, Color32 col, int width)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                var a = proj.ToXY(ring[i].X, ring[i].Y);
                var b = proj.ToXY(ring[(i + 1) % ring.Count].X, ring[(i + 1) % ring.Count].Y);
                int steps = Mathf.Max(1, Mathf.CeilToInt((float)Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y))));
                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    int x = Mathf.RoundToInt((float)(a.X + (b.X - a.X) * t)), y = Mathf.RoundToInt((float)(a.Y + (b.Y - a.Y) * t));
                    for (int dy = -width / 2; dy <= width / 2; dy++)
                        for (int dx = -width / 2; dx <= width / 2; dx++)
                            Put(px, w, h, x + dx, y + dy, col);
                }
            }
        }
    }
}
