// Put trke: projekcija (lon, lat) → svijet, glatka kriva kroz gradove (Catmull-Rom) i polja na jednakim razmacima.
// Svijet: x = istok, z = sjever (Unity: +z naprijed), y = visina.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class WorldProjection
    {
        public const double TileSpacing = 1.45;
        public double Lon0 = 19, Lat0 = 43, K = 8, Cos = Math.Cos(43 * Math.PI / 180);

        // Projekcija tako da ruta sa n polja ima razmak TileSpacing između polja.
        public static WorldProjection ForRoute(IList<(string name, double lon, double lat)> route, int tiles)
        {
            var p = new WorldProjection();
            double minLon = route.Min(r => r.lon), maxLon = route.Max(r => r.lon), minLat = route.Min(r => r.lat), maxLat = route.Max(r => r.lat);
            p.Lon0 = (minLon + maxLon) / 2;
            p.Lat0 = (minLat + maxLat) / 2;
            p.Cos = Math.Cos(p.Lat0 * Math.PI / 180);
            double len = 0;
            for (int i = 1; i < route.Count; i++)
                len += Math.Sqrt(Math.Pow((route[i].lon - route[i - 1].lon) * p.Cos, 2) + Math.Pow(route[i].lat - route[i - 1].lat, 2));
            // kriva je malo duža od izlomljene linije, zato 0.93
            p.K = (tiles - 1) * TileSpacing * 0.93 / Math.Max(1e-6, len);
            return p;
        }

        public Vec2d ToWorld(double lon, double lat) => new Vec2d((lon - Lon0) * K * Cos, (lat - Lat0) * K);
        public Vec2d ToLonLat(double x, double z) => new Vec2d(Lon0 + x / (K * Cos), Lat0 + z / K);
        // km po jedinici svijeta (za skaliranje planina i rastojanja)
        public double KmPerUnit => 111.0 / K;
    }

    public sealed class Tile
    {
        public int Index;
        public Vec3d Pos, Dir;          // Dir = pravac staze (vodoravno, normalizovan)
        public string Type;             // start, polje, precica, blato, dupla, kutija, cilj
        public string City;             // ime grada kod ovog polja (ako ga ima)
    }

    public static class CatmullRom
    {
        // Centripetalna Catmull-Rom kriva (isto kao THREE.CatmullRomCurve3 'centripetal').
        public static Vec3d Point(IList<Vec3d> pts, double t)
        {
            int n = pts.Count;
            var p = (n - 1) * t;
            int i = (int)Math.Floor(p);
            double w = p - i;
            if (i >= n - 1) { i = n - 2; w = 1; }
            if (i < 0) { i = 0; w = 0; }
            Vec3d p0 = i > 0 ? pts[i - 1] : pts[0] * 2 - pts[1];
            Vec3d p1 = pts[i], p2 = pts[i + 1];
            Vec3d p3 = i + 2 < n ? pts[i + 2] : pts[n - 1] * 2 - pts[n - 2];

            double dt0 = Math.Pow(Dist2(p0, p1), 0.25), dt1 = Math.Pow(Dist2(p1, p2), 0.25), dt2 = Math.Pow(Dist2(p2, p3), 0.25);
            if (dt1 < 1e-4) dt1 = 1.0;
            if (dt0 < 1e-4) dt0 = dt1;
            if (dt2 < 1e-4) dt2 = dt1;
            return new Vec3d(
                Nonuniform(p0.X, p1.X, p2.X, p3.X, dt0, dt1, dt2, w),
                Nonuniform(p0.Y, p1.Y, p2.Y, p3.Y, dt0, dt1, dt2, w),
                Nonuniform(p0.Z, p1.Z, p2.Z, p3.Z, dt0, dt1, dt2, w));
        }

        static double Dist2(Vec3d a, Vec3d b) { var d = a - b; return d.X * d.X + d.Y * d.Y + d.Z * d.Z; }

        static double Nonuniform(double x0, double x1, double x2, double x3, double dt0, double dt1, double dt2, double t)
        {
            double t1 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1;
            double t2 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;
            t1 *= dt1; t2 *= dt1;
            double c0 = x1, c1 = t1, c2 = -3 * x1 + 3 * x2 - 2 * t1 - t2, c3 = 2 * x1 - 2 * x2 + t1 + t2;
            double t2p = t * t, t3 = t2p * t;
            return c0 + c1 * t + c2 * t2p + c3 * t3;
        }

        // Gusto uzorkovana kriva (za crtanje puta).
        public static List<Vec3d> Sample(IList<Vec3d> pts, int count)
        {
            var res = new List<Vec3d>(count);
            for (int i = 0; i < count; i++) res.Add(Point(pts, (double)i / (count - 1)));
            return res;
        }

        // Tačke na jednakim razmacima duž krive (po dužini luka).
        public static List<Vec3d> Spaced(IList<Vec3d> pts, int count, int resolution = 0)
        {
            if (resolution <= 0) resolution = Math.Max(200, pts.Count * 60);
            var dense = Sample(pts, resolution + 1);
            var cum = new double[dense.Count];
            for (int i = 1; i < dense.Count; i++) cum[i] = cum[i - 1] + dense[i].DistanceTo(dense[i - 1]);
            var total = cum[cum.Length - 1];
            var res = new List<Vec3d>(count);
            int j = 0;
            for (int k = 0; k < count; k++)
            {
                var target = count == 1 ? 0 : total * k / (count - 1);
                while (j < cum.Length - 2 && cum[j + 1] < target) j++;
                var seg = cum[j + 1] - cum[j];
                var f = seg < 1e-12 ? 0 : (target - cum[j]) / seg;
                res.Add(Vec3d.Lerp(dense[j], dense[j + 1], Mathx.Clamp(f, 0, 1)));
            }
            return res;
        }
    }

    public sealed class TrackLayout
    {
        public WorldProjection Proj;
        public List<Tile> Tiles = new List<Tile>();
        public List<Vec3d> Road = new List<Vec3d>();     // gusta linija puta
        public List<(string name, Vec3d pos)> Cities = new List<(string, Vec3d)>();
        public int Last => Tiles.Count - 1;

        public static TrackLayout Build(RaceMap map, int tileCount, double y = 0, IList<string> kinds = null)
        {
            var t = new TrackLayout { Proj = WorldProjection.ForRoute(map.Route, tileCount) };
            var pts = map.Route.Select(r => { var w = t.Proj.ToWorld(r.lon, r.lat); return new Vec3d(w.X, y, w.Y); }).ToList();
            t.Cities = map.Route.Select((r, i) => (r.name, pts[i])).ToList();
            var sp = CatmullRom.Spaced(pts, tileCount);
            t.Road = CatmullRom.Sample(pts, Math.Max(200, tileCount * 12));
            int last = tileCount - 1;
            kinds ??= TileKinds.Generate(tileCount, 1);
            for (int i = 0; i < tileCount; i++)
            {
                var a = sp[Math.Max(0, i - 1)];
                var b = sp[Math.Min(last, i + 1)];
                var dir = new Vec3d(b.X - a.X, 0, b.Z - a.Z).Normalized;
                t.Tiles.Add(new Tile { Index = i, Pos = sp[i], Dir = dir, Type = kinds[i] });
            }
            // najbliže polje svakom gradu dobija ime grada
            foreach (var (name, pos) in t.Cities)
            {
                int best = 0; double bd = double.MaxValue;
                for (int i = 0; i < t.Tiles.Count; i++)
                {
                    var d = t.Tiles[i].Pos.DistanceTo(pos);
                    if (d < bd) { bd = d; best = i; }
                }
                if (t.Tiles[best].City == null) t.Tiles[best].City = name;
            }
            return t;
        }

        // Udaljenost tačke (x, z) od puta (u jedinicama svijeta).
        public double DistanceToRoad(double x, double z)
        {
            double best = double.MaxValue;
            for (int i = 0; i < Road.Count - 1; i++)
            {
                var a = Road[i]; var b = Road[i + 1];
                double vx = b.X - a.X, vz = b.Z - a.Z, wx = x - a.X, wz = z - a.Z;
                var len2 = vx * vx + vz * vz;
                var tt = len2 < 1e-12 ? 0 : Mathx.Clamp((wx * vx + wz * vz) / len2, 0, 1);
                double dx = a.X + vx * tt - x, dz = a.Z + vz * tt - z;
                var d = dx * dx + dz * dz;
                if (d < best) best = d;
            }
            return Math.Sqrt(best);
        }
    }
}
