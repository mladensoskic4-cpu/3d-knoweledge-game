// Geografija: tačke (lon, lat), udaljenost na Zemlji, tačka u poligonu, projekcija za 2D mapu i obrisi Balkana.
using System;
using System.Collections.Generic;

namespace TrkaZnanja.Core
{
    public struct Vec2d
    {
        public double X, Y;   // X = lon, Y = lat (ili x, z u svijetu)
        public Vec2d(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    public struct Vec3d
    {
        public double X, Y, Z;
        public Vec3d(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vec3d operator +(Vec3d a, Vec3d b) => new Vec3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3d operator -(Vec3d a, Vec3d b) => new Vec3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3d operator *(Vec3d a, double k) => new Vec3d(a.X * k, a.Y * k, a.Z * k);
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double DistanceTo(Vec3d o) => (this - o).Length;
        public Vec3d Normalized { get { var l = Length; return l < 1e-12 ? new Vec3d(0, 0, 0) : this * (1 / l); } }
        public static Vec3d Lerp(Vec3d a, Vec3d b, double t) => a + (b - a) * t;
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }

    public sealed class GeoLabel
    {
        public string Text; public double Lon, Lat;
        public GeoLabel(string t, double lon, double lat) { Text = t; Lon = lon; Lat = lat; }
    }

    // Postavke 2D mape za pitanja „tapni na mapi”: granice, puni bod do `Full` km, nula od `Zero` km.
    public sealed class GeoMap
    {
        public string Id, Label, Focus;
        public double MinLon, MaxLon, MinLat, MaxLat, Full, Zero, Grid;
        public GeoLabel[] Labels;
    }

    public static class GeoMaps
    {
        public static readonly GeoMap Cg = new GeoMap { Id = "cg", Label = "Crna Gora", Focus = "Crna Gora", MinLon = 18.35, MaxLon = 20.45, MinLat = 41.78, MaxLat = 43.62, Full = 4, Zero = 55, Grid = 0.5, Labels = GeoData.CgLabels };
        public static readonly GeoMap Srb = new GeoMap { Id = "srb", Label = "Srbija", Focus = "Srbija", MinLon = 18.7, MaxLon = 23.1, MinLat = 41.8, MaxLat = 46.3, Full = 8, Zero = 110, Grid = 1, Labels = GeoData.SrbLabels };
        public static readonly GeoMap Balkan = new GeoMap { Id = "balkan", Label = "Balkan", Focus = null, MinLon = 13.0, MaxLon = 29.7, MinLat = 38.5, MaxLat = 46.8, Full = 20, Zero = 280, Grid = 2, Labels = GeoData.CountryLabels };

        public static GeoMap Get(string id) => id == "srb" ? Srb : id == "balkan" ? Balkan : Cg;
    }

    public static class Geo
    {
        const double R = 6371;

        public static double Haversine(double lon1, double lat1, double lon2, double lat2)
        {
            double ToR(double d) => d * Math.PI / 180;
            double dLat = ToR(lat2 - lat1), dLon = ToR(lon2 - lon1);
            var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(ToR(lat1)) * Math.Cos(ToR(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static bool PointInPolygon(double x, double y, IList<Vec2d> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                double xi = poly[i].X, yi = poly[i].Y, xj = poly[j].X, yj = poly[j].Y;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        // Poligon sa rupama: prvi prsten je spoljni, ostali su rupe.
        public static bool InRings(double x, double y, IList<IList<Vec2d>> rings)
        {
            if (rings.Count == 0 || !PointInPolygon(x, y, rings[0])) return false;
            for (int i = 1; i < rings.Count; i++) if (PointInPolygon(x, y, rings[i])) return false;
            return true;
        }

        // Ekvirektangularna projekcija sa korekcijom cos(lat), centrirana u pravougaonik w×h (y raste nadolje).
        public sealed class Projector
        {
            readonly double minLon, maxLat, cos, s, ox, oy;
            public readonly double Aspect;
            public Projector(GeoMap b, double w, double h, double pad = 12)
            {
                minLon = b.MinLon; maxLat = b.MaxLat;
                cos = Math.Cos((b.MinLat + b.MaxLat) / 2 * Math.PI / 180);
                double spanX = (b.MaxLon - b.MinLon) * cos, spanY = b.MaxLat - b.MinLat;
                s = Math.Min((w - 2 * pad) / spanX, (h - 2 * pad) / spanY);
                ox = (w - spanX * s) / 2; oy = (h - spanY * s) / 2;
                Aspect = spanY / spanX;
            }
            public Vec2d ToXY(double lon, double lat) => new Vec2d(ox + (lon - minLon) * cos * s, oy + (maxLat - lat) * s);
            public Vec2d ToLonLat(double x, double y) => new Vec2d(minLon + (x - ox) / (cos * s), maxLat - (y - oy) / s);
        }

        public static List<Vec2d> ParseRing(object o)
        {
            var list = new List<Vec2d>();
            foreach (var p in Json.Arr(o) ?? new List<object>())
            {
                var a = Json.Arr(p);
                if (a != null && a.Count >= 2 && a[0] is double x && a[1] is double y) list.Add(new Vec2d(x, y));
            }
            return list;
        }

        // [[ring, rupa...], ...] → lista poligona
        public static List<IList<IList<Vec2d>>> ParsePolygons(object o)
        {
            var res = new List<IList<IList<Vec2d>>>();
            foreach (var poly in Json.Arr(o) ?? new List<object>())
            {
                var rings = new List<IList<Vec2d>>();
                foreach (var r in Json.Arr(poly) ?? new List<object>()) rings.Add(ParseRing(r));
                if (rings.Count > 0 && rings[0].Count >= 3) res.Add(rings);
            }
            return res;
        }
    }

    public static class GeoData
    {
        // Jezera koja se crtaju na mapama Crne Gore i Balkana (Natural Earth obris ih nema dovoljno krupno).
        public static IList<IList<Vec2d>> LakesFor(string mapId) =>
            mapId == "cg" || mapId == "balkan"
                ? new List<IList<Vec2d>> { JezeroSkadar, JezeroOhrid, JezeroPrespa, JezeroPlav }
                : new List<IList<Vec2d>>();

        static Vec2d[] P(params double[] xy)
        {
            var a = new Vec2d[xy.Length / 2];
            for (int i = 0; i < a.Length; i++) a[i] = new Vec2d(xy[i * 2], xy[i * 2 + 1]);
            return a;
        }

        public static readonly Vec2d[] BalkanLand = P(13, 46.8, 13, 45.75, 13.53, 45.8, 13.75, 45.65, 13.6, 45.45, 13.55, 45.1, 13.85, 44.83, 14.1, 45.2, 14.45, 45.32, 14.9, 45.0, 15.2, 44.45, 15.23, 44.11, 15.9, 43.73, 16.44, 43.5, 17.0, 43.3, 17.6, 42.93, 18.1, 42.64, 18.52, 42.43, 18.58, 42.39, 18.7, 42.37, 18.84, 42.28, 19.1, 42.06, 19.21, 41.92, 19.37, 41.86, 19.55, 41.6, 19.45, 41.32, 19.45, 40.9, 19.49, 40.47, 19.4, 40.3, 19.98, 39.9, 20.0, 39.7, 20.3, 39.4, 20.75, 38.95, 21.1, 38.5, 23.0, 38.5, 23.4, 38.9, 22.95, 39.36, 22.6, 40.0, 22.95, 40.62, 23.4, 40.25, 23.9, 40.35, 23.85, 40.75, 24.41, 40.94, 25.0, 40.95, 25.87, 40.85, 26.08, 40.72, 26.2, 40.6, 26.67, 40.41, 27.5, 40.97, 28.98, 41.0, 29.1, 41.2, 28.1, 41.63, 27.98, 41.97, 27.47, 42.5, 27.73, 42.66, 27.92, 43.2, 28.47, 43.36, 28.58, 43.82, 28.65, 44.17, 29.6, 45.0, 29.7, 45.2, 29.7, 46.8);
        public static readonly Vec2d[] Italy = P(13, 43.85, 13.5, 43.62, 14.2, 42.46, 15.0, 42.0, 15.9, 41.95, 16.2, 41.75, 16.87, 41.12, 17.94, 40.64, 18.49, 40.15, 18.36, 39.8, 17.2, 40.45, 16.6, 40.0, 16.6, 38.5, 13, 38.5);
        public static readonly Vec2d[] CgShape = P(18.46, 42.48, 18.56, 42.57, 18.55, 42.68, 18.53, 42.88, 18.58, 43.02, 18.68, 43.2, 18.84, 43.33, 19.02, 43.45, 19.22, 43.53, 19.42, 43.38, 19.53, 43.32, 19.7, 43.18, 19.95, 43.1, 20.08, 43.0, 20.35, 42.9, 20.35, 42.83, 20.1, 42.7, 19.98, 42.53, 19.75, 42.48, 19.58, 42.52, 19.4, 42.38, 19.38, 42.22, 19.32, 42.1, 19.4, 41.98, 19.37, 41.86, 19.21, 41.92, 19.1, 42.06, 18.95, 42.2, 18.84, 42.28, 18.7, 42.37, 18.58, 42.39, 18.52, 42.43);
        public static readonly Vec2d[] SrbShape = P(18.85, 45.92, 19.3, 46.1, 19.7, 46.18, 20.25, 46.13, 20.75, 45.75, 21.0, 45.45, 21.5, 45.15, 21.4, 44.78, 22.0, 44.6, 22.45, 44.72, 22.7, 44.55, 22.68, 44.22, 22.5, 43.9, 22.55, 43.5, 22.95, 43.1, 22.75, 42.85, 22.45, 42.5, 22.36, 42.31, 21.9, 42.3, 21.6, 42.25, 21.1, 42.15, 20.6, 41.88, 20.55, 42.2, 20.25, 42.35, 19.98, 42.53, 20.1, 42.7, 20.35, 42.83, 20.35, 42.9, 20.08, 43.0, 19.95, 43.1, 19.7, 43.18, 19.53, 43.32, 19.42, 43.38, 19.22, 43.53, 19.5, 43.72, 19.45, 44.05, 19.12, 44.4, 19.2, 44.85, 19.0, 45.15, 19.0, 45.35, 18.95, 45.55);
        public static readonly Vec2d[] KotorBay = P(18.53, 42.435, 18.6, 42.45, 18.66, 42.47, 18.72, 42.49, 18.775, 42.47, 18.77, 42.425, 18.72, 42.41, 18.68, 42.43, 18.6, 42.425);
        public static readonly Vec2d[] JezeroSkadar = P(19.05, 42.3, 19.15, 42.36, 19.27, 42.3, 19.38, 42.22, 19.45, 42.12, 19.4, 42.06, 19.3, 42.12, 19.2, 42.18, 19.1, 42.24);
        public static readonly Vec2d[] JezeroOhrid = P(20.68, 41.15, 20.78, 41.2, 20.84, 41.1, 20.8, 40.92, 20.7, 40.95);
        public static readonly Vec2d[] JezeroPrespa = P(20.95, 41.05, 21.05, 41.0, 21.05, 40.8, 20.95, 40.82);
        public static readonly Vec2d[] JezeroPlav = P(19.91, 42.615, 19.94, 42.62, 19.955, 42.6, 19.93, 42.585, 19.91, 42.595);
        public static readonly GeoLabel[] CountryLabels = { new GeoLabel("Slovenija", 14.8, 46.15), new GeoLabel("Hrvatska", 15.9, 45.45), new GeoLabel("BiH", 17.9, 44.25), new GeoLabel("Srbija", 20.9, 44.0), new GeoLabel("Crna Gora", 19.3, 42.75), new GeoLabel("S. Makedonija", 21.7, 41.6), new GeoLabel("Albanija", 20.1, 40.8), new GeoLabel("Grčka", 22.0, 39.7), new GeoLabel("Bugarska", 25.2, 42.7), new GeoLabel("Rumunija", 24.6, 45.8), new GeoLabel("Italija", 15.2, 40.6), new GeoLabel("Jadransko more", 16.8, 42.4) };
        public static readonly GeoLabel[] CgLabels = { new GeoLabel("Hrvatska", 18.42, 42.58), new GeoLabel("BiH", 18.5, 43.35), new GeoLabel("Srbija", 20.0, 43.45), new GeoLabel("Albanija", 19.8, 42.1), new GeoLabel("Jadransko more", 18.75, 41.98) };
        public static readonly GeoLabel[] SrbLabels = { new GeoLabel("Mađarska", 20.6, 46.24), new GeoLabel("Rumunija", 22.3, 45.35), new GeoLabel("Bugarska", 22.85, 43.65), new GeoLabel("BiH", 18.95, 44.2), new GeoLabel("Hrvatska", 18.85, 45.25), new GeoLabel("Crna Gora", 19.55, 42.95), new GeoLabel("S. Makedonija", 21.9, 42.0), new GeoLabel("Albanija", 20.15, 42.05) };
    }
}
