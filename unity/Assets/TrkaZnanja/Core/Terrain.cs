// Teren mape: mreža visina iz obrisa kopna (more, plaže, brda, planine, jezera) i raspored drveća i kuća.
// Staza ostaje ravna, a planine rastu što su dalje od nje, da ne zaklanjaju kameru.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public enum Ground : byte { Sea = 0, Sand = 1, Grass = 2, Rock = 3, Snow = 4, Lake = 5 }

    public sealed class TerrainParams
    {
        public double LandTop = 0.4;          // visina ravnice i staze
        public double SeaLevel = 0.12;        // visina mora
        public double Margin = 20;            // koliko terena oko staze
        public int MaxCells = 240;            // najviše ćelija po strani (performanse na telefonu)
        public double MountainHeight = 3.8;
        public double SnowLine = 3.3;         // za atmosferu „snijeg” se spušta
        public int Seed = 7;
    }

    public sealed class Scatter
    {
        public Vec3d Pos;
        public double Scale, Rot;
        public int Variant;
    }

    public sealed class TerrainGrid
    {
        public int Nx, Nz;                    // broj tačaka (ne ćelija)
        public double X0, Z0, Cell;
        public float[] Height;                // Nx*Nz
        public Ground[] Kind;
        public float[] Shade;                 // 0..1, za blagu varijaciju boje
        public bool[] Land, Lake;
        public List<Scatter> Trees = new List<Scatter>(), Rocks = new List<Scatter>(), Houses = new List<Scatter>();
        public double LandTop, SeaLevel;

        public int Idx(int ix, int iz) => iz * Nx + ix;

        // Visina terena u tački (bilinearno).
        public double SampleHeight(double x, double z)
        {
            double fx = (x - X0) / Cell, fz = (z - Z0) / Cell;
            int ix = Mathx.Clamp((int)Math.Floor(fx), 0, Nx - 2), iz = Mathx.Clamp((int)Math.Floor(fz), 0, Nz - 2);
            double tx = Mathx.Clamp(fx - ix, 0, 1), tz = Mathx.Clamp(fz - iz, 0, 1);
            double a = Height[Idx(ix, iz)], b = Height[Idx(ix + 1, iz)], c = Height[Idx(ix, iz + 1)], d = Height[Idx(ix + 1, iz + 1)];
            return Mathx.Lerp(Mathx.Lerp(a, b, tx), Mathx.Lerp(c, d, tx), tz);
        }

        public bool IsLandAt(double x, double z)
        {
            int ix = (int)Math.Round((x - X0) / Cell), iz = (int)Math.Round((z - Z0) / Cell);
            if (ix < 0 || iz < 0 || ix >= Nx || iz >= Nz) return false;
            var i = Idx(ix, iz);
            return Land[i] && !Lake[i];
        }
    }

    public static class Noise
    {
        static double Hash(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }

        public static double Value(double x, double z, int seed)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            double fx = x - ix, fz = z - iz;
            double sx = fx * fx * (3 - 2 * fx), sz = fz * fz * (3 - 2 * fz);
            double a = Hash(ix, iz, seed), b = Hash(ix + 1, iz, seed), c = Hash(ix, iz + 1, seed), d = Hash(ix + 1, iz + 1, seed);
            return Mathx.Lerp(Mathx.Lerp(a, b, sx), Mathx.Lerp(c, d, sx), sz);
        }

        public static double Fbm(double x, double z, int seed, int octaves = 4)
        {
            double sum = 0, amp = 0.5, norm = 0;
            for (int o = 0; o < octaves; o++)
            {
                sum += Value(x, z, seed + o * 31) * amp;
                norm += amp;
                x *= 2.03; z *= 2.03; amp *= 0.5;
            }
            return sum / norm;
        }

        // Grebeni (oštriji vrhovi planina).
        public static double Ridged(double x, double z, int seed, int octaves = 4)
        {
            double sum = 0, amp = 0.5, norm = 0;
            for (int o = 0; o < octaves; o++)
            {
                var n = 1 - Math.Abs(Value(x, z, seed + o * 17) * 2 - 1);
                sum += n * n * amp;
                norm += amp;
                x *= 2.1; z *= 2.1; amp *= 0.5;
            }
            return sum / norm;
        }
    }

    public static class TerrainBuilder
    {
        // land: poligoni kopna u (lon, lat); lakes: jezera u (lon, lat)
        public static TerrainGrid Build(TrackLayout track, IList<IList<IList<Vec2d>>> land, IList<IList<Vec2d>> lakes, TerrainParams s, string mapId = null)
        {
            var proj = track.Proj;
            double minX = track.Tiles.Min(t => t.Pos.X) - s.Margin, maxX = track.Tiles.Max(t => t.Pos.X) + s.Margin;
            double minZ = track.Tiles.Min(t => t.Pos.Z) - s.Margin, maxZ = track.Tiles.Max(t => t.Pos.Z) + s.Margin;
            var cell = Math.Max(0.45, Math.Max(maxX - minX, maxZ - minZ) / s.MaxCells);
            int nx = (int)Math.Ceiling((maxX - minX) / cell) + 1, nz = (int)Math.Ceiling((maxZ - minZ) / cell) + 1;
            var T = new TerrainGrid
            {
                Nx = nx, Nz = nz, X0 = minX, Z0 = minZ, Cell = cell, LandTop = s.LandTop, SeaLevel = s.SeaLevel,
                Height = new float[nx * nz], Kind = new Ground[nx * nz], Shade = new float[nx * nz], Land = new bool[nx * nz], Lake = new bool[nx * nz],
            };

            // 1) kopno i jezera (scanline u koordinatama svijeta)
            var landRings = new List<List<Vec2d>>();
            foreach (var poly in land) foreach (var ring in poly) landRings.Add(ring.Select(p => proj.ToWorld(p.X, p.Y)).ToList());
            Fill(T, landRings, T.Land);
            var lakeRings = lakes.Select(r => r.Select(p => proj.ToWorld(p.X, p.Y)).ToList()).ToList();
            Fill(T, lakeRings, T.Lake);

            // 2) udaljenosti: od puta i od obale (chamfer transformacija, brzo i za veliku mrežu)
            var roadSeed = new bool[nx * nz];
            foreach (var p in track.Road)
            {
                int ix = (int)Math.Round((p.X - minX) / cell), iz = (int)Math.Round((p.Z - minZ) / cell);
                if (ix >= 0 && iz >= 0 && ix < nx && iz < nz) roadSeed[T.Idx(ix, iz)] = true;
            }
            var dRoad = Distance(nx, nz, i => roadSeed[i], cell);
            var dCoastIn = Distance(nx, nz, i => !T.Land[i], cell);       // u kopnu: koliko daleko od mora
            var dCoastOut = Distance(nx, nz, i => T.Land[i], cell);       // u moru: koliko daleko od kopna
            var dLake = Distance(nx, nz, i => !T.Lake[i], cell);

            // 3) visine i vrsta tla
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = T.Idx(ix, iz);
                    double x = minX + ix * cell, z = minZ + iz * cell;
                    double h;
                    var shade = Noise.Fbm(x * 0.21, z * 0.21, s.Seed + 5);
                    T.Shade[i] = (float)shade;
                    if (!T.Land[i])
                    {
                        h = -0.45 - Mathx.SmoothStep(0, 3, dCoastOut[i]) * 0.9;
                        // put preko mora ide po nasipu (pješčani nasip, kao u Boki)
                        // (samo blizu obale; preko okeana staza ide po mostu)
                        var dike = dCoastOut[i] < 2.5 ? Mathx.SmoothStep(0.7, 1.3, dRoad[i]) : 1;
                        h = Mathx.Lerp(s.LandTop, h, dike);
                        T.Height[i] = (float)h;
                        T.Kind[i] = h > s.SeaLevel ? Ground.Sand : Ground.Sea;
                        continue;
                    }
                    var ll = proj.ToLonLat(x, z);
                    double region = 1;
                    if (mapId == "balkan")   // Panonska nizija ostaje ravna (blag prelaz, bez stepenika)
                        region = Mathx.Lerp(1, 0.12, Mathx.SmoothStep(44.5, 45.1, ll.Y) * Mathx.SmoothStep(17.4, 18.2, ll.X) * (1 - Mathx.SmoothStep(21.6, 22.4, ll.X)));
                    // planine rastu brzo čim se odmakne od staze, a uz obalu su niže (plaže, pa strme padine kao u Boki)
                    var coastK = Mathx.SmoothStep(0.3, 2.2, dCoastIn[i]);
                    var amp = Mathx.Clamp((dRoad[i] - 1.1) * 0.6, 0, s.MountainHeight) * region * (0.35 + 0.65 * coastK);
                    var hills = Noise.Fbm(x * 0.16, z * 0.16, s.Seed);
                    var ridge = Noise.Ridged(x * 0.085, z * 0.085, s.Seed + 11);
                    var detail = Noise.Fbm(x * 0.55, z * 0.55, s.Seed + 23);
                    var mountain = amp * (0.25 * hills + 0.95 * Math.Pow(ridge, 1.5) + 0.12 * detail)
                                   + Mathx.SmoothStep(0.9, 2.4, dRoad[i]) * 0.22 * detail * region;
                    h = s.LandTop + mountain;
                    // plaža i obala
                    var coast = Mathx.SmoothStep(0, 1.3, dCoastIn[i]);
                    h = Mathx.Lerp(s.SeaLevel - 0.25, h, coast);
                    // put ostaje ravan
                    var flat = Mathx.SmoothStep(0.72, 1.2, dRoad[i]);   // put je širok ~1,2
                    h = Mathx.Lerp(s.LandTop, h, flat);
                    // jezero
                    if (T.Lake[i]) h = s.LandTop - 0.2 - Mathx.SmoothStep(0, 1.5, dLake[i]) * 0.3;
                    T.Height[i] = (float)h;

                    if (T.Lake[i]) T.Kind[i] = Ground.Lake;
                    else if (h > s.SnowLine + (hills - 0.5) * 0.4) T.Kind[i] = Ground.Snow;
                    else if (mountain > 1.8 + hills * 0.6) T.Kind[i] = Ground.Rock;
                    else if (dCoastIn[i] < 0.9 && h < s.LandTop + 0.12) T.Kind[i] = Ground.Sand;
                    else T.Kind[i] = Ground.Grass;
                }

            // 4) drveće, kamenje i kuće
            var rng = new Rng(s.Seed * 97 + 13);
            double area = (maxX - minX) * (maxZ - minZ);
            int treeTarget = (int)Math.Min(900, area * 0.09), rockTarget = (int)Math.Min(160, area * 0.012);
            for (int tries = 0; T.Trees.Count < treeTarget && tries < treeTarget * 12; tries++)
            {
                double x = rng.Range(minX, maxX), z = rng.Range(minZ, maxZ);
                int ix = (int)Math.Round((x - minX) / cell), iz = (int)Math.Round((z - minZ) / cell);
                int i = T.Idx(Mathx.Clamp(ix, 0, nx - 1), Mathx.Clamp(iz, 0, nz - 1));
                if (!T.Land[i] || T.Lake[i] || dRoad[i] < 0.8 || dCoastIn[i] < 0.7) continue;
                if (T.Kind[i] == Ground.Snow || T.Kind[i] == Ground.Rock) continue;
                // šume u grupama
                if (Noise.Fbm(x * 0.15, z * 0.15, s.Seed + 99) < 0.45) continue;
                T.Trees.Add(new Scatter { Pos = new Vec3d(x, T.SampleHeight(x, z), z), Scale = rng.Range(0.7, 1.35), Rot = rng.Range(0, 360), Variant = rng.Range(0, 3) });
            }
            for (int tries = 0; T.Rocks.Count < rockTarget && tries < rockTarget * 20; tries++)
            {
                double x = rng.Range(minX, maxX), z = rng.Range(minZ, maxZ);
                int ix = (int)Math.Round((x - minX) / cell), iz = (int)Math.Round((z - minZ) / cell);
                int i = T.Idx(Mathx.Clamp(ix, 0, nx - 1), Mathx.Clamp(iz, 0, nz - 1));
                if (!T.Land[i] || T.Lake[i] || dRoad[i] < 0.9) continue;
                T.Rocks.Add(new Scatter { Pos = new Vec3d(x, T.SampleHeight(x, z), z), Scale = rng.Range(0.15, 0.45), Rot = rng.Range(0, 360), Variant = rng.Range(0, 2) });
            }
            foreach (var (name, pos) in track.Cities)
            {
                int houses = rng.Range(6, 11);
                for (int k = 0, tries = 0; k < houses && tries < 60; tries++)
                {
                    var ang = rng.Range(0, Math.PI * 2);
                    var r = rng.Range(0.75, 2.0);
                    double x = pos.X + Math.Cos(ang) * r, z = pos.Z + Math.Sin(ang) * r;
                    if (!T.IsLandAt(x, z) || track.DistanceToRoad(x, z) < 0.85) continue;
                    T.Houses.Add(new Scatter { Pos = new Vec3d(x, T.SampleHeight(x, z), z), Scale = rng.Range(0.8, 1.25), Rot = rng.Range(0, 360), Variant = rng.Range(0, 4) });
                    k++;
                }
            }
            return T;
        }

        static void Fill(TerrainGrid T, List<List<Vec2d>> rings, bool[] mask)
        {
            var xs = new List<double>();
            for (int iz = 0; iz < T.Nz; iz++)
            {
                double z = T.Z0 + iz * T.Cell;
                xs.Clear();
                foreach (var r in rings)
                    for (int i = 0, j = r.Count - 1; i < r.Count; j = i++)
                    {
                        double zi = r[i].Y, zj = r[j].Y;
                        if ((zi > z) != (zj > z)) xs.Add(r[i].X + (z - zi) * (r[j].X - r[i].X) / (zj - zi));
                    }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int a = (int)Math.Ceiling((xs[k] - T.X0) / T.Cell), b = (int)Math.Floor((xs[k + 1] - T.X0) / T.Cell);
                    for (int ix = Math.Max(0, a); ix <= Math.Min(T.Nx - 1, b); ix++) mask[T.Idx(ix, iz)] ^= true;
                }
            }
        }

        // Udaljenost (u jedinicama svijeta) od najbliže ćelije za koju je `source` tačno. Dvoprolazni chamfer 3-4.
        public static float[] Distance(int nx, int nz, Func<int, bool> source, double cell)
        {
            const float INF = 1e9f;
            var d = new float[nx * nz];
            for (int i = 0; i < d.Length; i++) d[i] = source(i) ? 0 : INF;
            float a = 1f, b = 1.4142f;
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    int i = z * nx + x; var v = d[i];
                    if (x > 0) v = Math.Min(v, d[i - 1] + a);
                    if (z > 0)
                    {
                        v = Math.Min(v, d[i - nx] + a);
                        if (x > 0) v = Math.Min(v, d[i - nx - 1] + b);
                        if (x < nx - 1) v = Math.Min(v, d[i - nx + 1] + b);
                    }
                    d[i] = v;
                }
            for (int z = nz - 1; z >= 0; z--)
                for (int x = nx - 1; x >= 0; x--)
                {
                    int i = z * nx + x; var v = d[i];
                    if (x < nx - 1) v = Math.Min(v, d[i + 1] + a);
                    if (z < nz - 1)
                    {
                        v = Math.Min(v, d[i + nx] + a);
                        if (x < nx - 1) v = Math.Min(v, d[i + nx + 1] + b);
                        if (x > 0) v = Math.Min(v, d[i + nx - 1] + b);
                    }
                    d[i] = v;
                }
            for (int i = 0; i < d.Length; i++) d[i] = (float)(d[i] * cell);
            return d;
        }
    }
}
