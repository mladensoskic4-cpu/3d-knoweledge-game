// Svijet za mini igre: države (Natural Earth, Kosovo u sastavu Srbije), kopno, veliki gradovi i rijeke,
// udaljenosti za bodovanje, projekcija mape svijeta i okvir za „Pogodi državu po obliku”. Isto kao js/world.js.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class WorldCountry
    {
        public string Code, Name, En, Continent;
        public double Area;
        public Vec2d Center;
        public List<IList<IList<Vec2d>>> Polygons = new List<IList<IList<Vec2d>>>();
    }

    public sealed class WorldPlace { public string Name, En; public double Lon, Lat; }
    public sealed class WorldRiver { public string Name, En; public List<Vec2d> Points = new List<Vec2d>(); }

    public static class WorldGeo
    {
        // drzave.json: [{ code, name, en, continent, area, c: [lon, lat], b, poly }]
        public static List<WorldCountry> ParseCountries(string json)
        {
            var res = new List<WorldCountry>();
            foreach (var o in Json.Arr(Json.Parse(json)) ?? new List<object>())
            {
                var d = Json.Obj(o);
                if (d == null) continue;
                var c = new WorldCountry
                {
                    Code = Json.Str(d, "code"), Name = Json.Str(d, "name"), En = Json.Str(d, "en"),
                    Continent = Json.Str(d, "continent"), Area = Json.Num(d, "area"),
                    Polygons = Geo.ParsePolygons(Json.Get(d, "poly")),
                };
                var cc = Json.Arr(Json.Get(d, "c"));
                if (cc != null && cc.Count >= 2 && cc[0] is double x && cc[1] is double y) c.Center = new Vec2d(x, y);
                else if (c.Polygons.Count > 0) c.Center = Centroid(c.Polygons[0][0]);
                if (c.Polygons.Count > 0) res.Add(c);
            }
            return res;
        }

        // kopno-svijet.json: lista poligona [[ring, rupa...], ...]
        public static List<IList<IList<Vec2d>>> ParseLand(string json) => Geo.ParsePolygons(Json.Parse(json));

        // svijet-gradovi-rijeke.json: { gradovi: [{name, lon, lat, en}], rijeke: [{name, en, pts}] }
        public static void ParsePlaces(string json, out List<WorldPlace> cities, out List<WorldRiver> rivers)
        {
            cities = new List<WorldPlace>();
            rivers = new List<WorldRiver>();
            var d = Json.Obj(Json.Parse(json));
            if (d == null) return;
            foreach (var o in Json.Arr(Json.Get(d, "gradovi")) ?? new List<object>())
            {
                var c = Json.Obj(o);
                cities.Add(new WorldPlace { Name = Json.Str(c, "name"), En = Json.Str(c, "en"), Lon = Json.Num(c, "lon"), Lat = Json.Num(c, "lat") });
            }
            foreach (var o in Json.Arr(Json.Get(d, "rijeke")) ?? new List<object>())
            {
                var r = Json.Obj(o);
                var river = new WorldRiver { Name = Json.Str(r, "name"), En = Json.Str(r, "en"), Points = Geo.ParseRing(Json.Get(r, "pts")) };
                if (river.Points.Count >= 2) rivers.Add(river);
            }
        }

        static Vec2d Centroid(IList<Vec2d> ring)
        {
            double x = 0, y = 0;
            foreach (var p in ring) { x += p.X; y += p.Y; }
            return new Vec2d(x / ring.Count, y / ring.Count);
        }

        public static bool InCountry(double lon, double lat, WorldCountry c)
        {
            foreach (var rings in c.Polygons) if (Geo.InRings(lon, lat, rings)) return true;
            return false;
        }

        // Udaljenost (km) od tačke do države: 0 ako je unutra, inače do najbliže (svake druge) tačke obrisa.
        public static double DistToCountry(double lon, double lat, WorldCountry c)
        {
            if (InCountry(lon, lat, c)) return 0;
            double best = double.PositiveInfinity;
            foreach (var rings in c.Polygons)
            {
                var r = rings[0];
                for (int i = 0; i < r.Count; i += 2) best = Math.Min(best, Geo.Haversine(lon, lat, r[i].X, r[i].Y));
            }
            return best;
        }

        // Udaljenost (km) do izlomljene linije (rijeka), sa 10 međutačaka po dužini.
        public static double DistToLine(double lon, double lat, IList<Vec2d> pts)
        {
            double best = double.PositiveInfinity;
            for (int i = 0; i + 1 < pts.Count; i++)
                for (int k = 0; k <= 10; k++)
                {
                    double t = k / 10.0;
                    best = Math.Min(best, Geo.Haversine(lon, lat, pts[i].X + (pts[i + 1].X - pts[i].X) * t, pts[i].Y + (pts[i + 1].Y - pts[i].Y) * t));
                }
            return best;
        }

        // Bodovi za „Pronađi na mapi svijeta”.
        public static double ScoreCountry(double km) => km == 0 ? 1 : Mathx.Clamp(1 - km / 900, 0, 0.8);
        public static double ScoreCity(double km) => km < 150 ? 1 : Mathx.Clamp(1 - (km - 150) / 1500, 0, 1);
        public static double ScoreRiver(double km) => km < 120 ? 1 : Mathx.Clamp(1 - (km - 120) / 1200, 0, 1);
        // „Gdje je ova fotografija?”: do 100 km pun bod, pa sve manje do 2.100 km.
        public static double ScoreForKm(double km) => km <= 100 ? 1 : Mathx.Clamp(1 - (km - 100) / 2000, 0, 1);

        // Prsten bez skokova od 360° (Rusija, Fidži…), pomjeren tako da mu je sredina blizu centerLon.
        public static List<Vec2d> Unwrap(IList<Vec2d> ring, double centerLon)
        {
            var o = new List<Vec2d>(ring.Count);
            double prev = double.NaN, lo = double.PositiveInfinity, hi = double.NegativeInfinity;
            foreach (var p in ring)
            {
                double x = p.X;
                if (!double.IsNaN(prev)) { while (x - prev > 180) x -= 360; while (x - prev < -180) x += 360; }
                prev = x; lo = Math.Min(lo, x); hi = Math.Max(hi, x);
                o.Add(new Vec2d(x, p.Y));
            }
            double sh = Math.Round((centerLon - (lo + hi) / 2) / 360) * 360;
            if (sh != 0) for (int i = 0; i < o.Count; i++) o[i] = new Vec2d(o[i].X + sh, o[i].Y);
            return o;
        }

        public static double[] Box(IList<Vec2d> ring)
        {
            var b = new[] { double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity };
            foreach (var p in ring) { b[0] = Math.Min(b[0], p.X); b[1] = Math.Min(b[1], p.Y); b[2] = Math.Max(b[2], p.X); b[3] = Math.Max(b[3], p.Y); }
            return b;
        }
    }

    // Mapa svijeta (ekvirektangularna): lon -170..190, lat -58..80. U = 0..1 slijeva, V = 0..1 odozgo.
    public static class WorldFrame
    {
        public const double MinLon = -170, MaxLon = 190, MinLat = -58, MaxLat = 80;
        public static double Aspect => (MaxLat - MinLat) / (MaxLon - MinLon);
        public static double U(double lon) => ((lon < MinLon ? lon + 360 : lon) - MinLon) / (MaxLon - MinLon);
        public static double V(double lat) => (MaxLat - lat) / (MaxLat - MinLat);
        public static Vec2d ToLonLat(double u, double v)
        {
            double lon = MinLon + u * (MaxLon - MinLon);
            if (lon > 180) lon -= 360;
            return new Vec2d(lon, MaxLat - v * (MaxLat - MinLat));
        }
    }

    // Okvir za crtanje jedne države sa okolinom (mini igra „Pogodi državu po obliku”), kao u js/minigames/shape.js.
    public sealed class ShapeFrame
    {
        public readonly double X0, Y1, Cos, S, CenterLon;
        public readonly int W, H;

        public ShapeFrame(WorldCountry d, int w, int h)
        {
            W = w; H = h;
            CenterLon = d.Center.X;
            // glavni dio države + dijelovi u blizini (bez dalekih prekomorskih teritorija)
            var parts = d.Polygons.Select(p => WorldGeo.Box(WorldGeo.Unwrap(p[0], CenterLon))).ToList();
            var main = parts.OrderByDescending(b => (b[2] - b[0]) * (b[3] - b[1])).First();
            double mx = (main[0] + main[2]) / 2, my = (main[1] + main[3]) / 2;
            double reach = Math.Max(12, Math.Max((main[2] - main[0]) * 1.2, (main[3] - main[1]) * 1.2));
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            foreach (var b in parts)
            {
                if (Math.Abs((b[0] + b[2]) / 2 - mx) >= reach || Math.Abs((b[1] + b[3]) / 2 - my) >= reach) continue;
                minX = Math.Min(minX, b[0]); minY = Math.Min(minY, b[1]); maxX = Math.Max(maxX, b[2]); maxY = Math.Max(maxY, b[3]);
            }
            Cos = Math.Max(0.25, Math.Cos((minY + maxY) / 2 * Math.PI / 180));
            double sx = Math.Max((maxX - minX) * Cos, 3.5 * Cos) * 1.7, sy = Math.Max(maxY - minY, 3.5) * 1.7;
            if (sx / sy > w / (double)h) sy = sx * h / w; else sx = sy * w / h;
            double ccx = (minX + maxX) / 2, ccy = (minY + maxY) / 2;
            X0 = ccx - sx / Cos / 2;
            Y1 = ccy + sy / 2;
            S = w / sx;
        }

        public Vec2d P(double lon, double lat) => new Vec2d((lon - X0) * Cos * S, (Y1 - lat) * S);

        public bool InView(double[] box)
        {
            var a = P(box[0], box[3]); var c = P(box[2], box[1]);
            return a.X < W && c.X > 0 && a.Y < H && c.Y > 0;
        }

        // Prsten u pikselima okvira (odmotan oko države).
        public List<Vec2d> Project(IList<Vec2d> ring)
        {
            var u = WorldGeo.Unwrap(ring, CenterLon);
            for (int i = 0; i < u.Count; i++) u[i] = P(u[i].X, u[i].Y);
            return u;
        }

        public bool RingInView(IList<Vec2d> ring) => InView(WorldGeo.Box(WorldGeo.Unwrap(ring, CenterLon)));
    }

    // Ponuđeni odgovori za mini igre: tačan + slični (isti kontinent / grupa / sistem), da ne bude prelako.
    public static class Options
    {
        public static List<string> Pick(Rng rng, string correct, IList<string> pool, int n = 4)
        {
            var others = pool.Where(x => x != correct).Distinct().ToList();
            var outList = new List<string> { correct };
            rng.Shuffle(others);
            foreach (var o in others) { if (outList.Count >= n) break; outList.Add(o); }
            return rng.Shuffle(outList);
        }

        // Kao optionsFor u js/slike.js: 2 iz iste grupe (key), ostalo iz bilo koje.
        public static List<string> Similar<T>(Rng rng, T item, IList<T> list, Func<T, string> key, Func<T, string> name, int n = 4) where T : class
        {
            string k = key(item);
            var same = list.Where(x => x != item && !string.IsNullOrEmpty(k) && key(x) == k && name(x) != name(item)).ToList();
            var other = list.Where(x => x != item && !same.Contains(x) && name(x) != name(item)).ToList();
            rng.Shuffle(same); rng.Shuffle(other);
            var picked = new List<T>(same.Take(2));
            foreach (var x in other.Concat(same.Skip(2))) { if (picked.Count >= n - 1) break; if (!picked.Any(p => name(p) == name(x))) picked.Add(x); }
            var names = new List<string> { name(item) };
            names.AddRange(picked.Select(name).Distinct().Take(n - 1));
            return rng.Shuffle(names);
        }
    }

    // Mat u jednom potezu: FEN pozicija i rješenje (od, do, SAN), problemi iz data/sah.json.
    public sealed class ChessPuzzle
    {
        public string Fen, From, To, San;
        public bool Promo;
        public const string Files = "abcdefgh";

        public static List<ChessPuzzle> ParseAll(string json)
        {
            var res = new List<ChessPuzzle>();
            foreach (var o in Json.Arr(Json.Parse(json)) ?? new List<object>())
            {
                var d = Json.Obj(o);
                if (d == null) continue;
                var p = new ChessPuzzle { Fen = Json.Str(d, "fen"), From = Json.Str(d, "from"), To = Json.Str(d, "to"), San = Json.Str(d, "san"), Promo = Json.Get(d, "promo") is string };
                if (!string.IsNullOrEmpty(p.Fen) && p.From?.Length == 2 && p.To?.Length == 2) res.Add(p);
            }
            return res;
        }

        // Polje → figura (K Q R B N P bijeli, mala slova crni).
        public static Dictionary<string, char> ParseFen(string fen)
        {
            var board = new Dictionary<string, char>();
            var rows = fen.Split(' ')[0].Split('/');
            for (int ri = 0; ri < rows.Length && ri < 8; ri++)
            {
                int f = 0;
                foreach (var ch in rows[ri])
                {
                    if (char.IsDigit(ch)) f += ch - '0';
                    else { if (f < 8) board["" + Files[f] + (8 - ri)] = ch; f++; }
                }
            }
            return board;
        }

        public static bool IsWhite(char piece) => char.IsUpper(piece);

        // Naš naziv figure (za prikaz na tabli bez šahovskih znakova u fontu): K D T L S p
        public static string Letter(char piece)
        {
            switch (char.ToUpperInvariant(piece))
            {
                case 'K': return "K";
                case 'Q': return "D";
                case 'R': return "T";
                case 'B': return "L";
                case 'N': return "S";
                default: return "p";
            }
        }

        // SAN zapis našim slovima (Qh7# → Dh7#).
        public static string LocalSan(string san)
        {
            if (string.IsNullOrEmpty(san)) return san;
            var map = new Dictionary<char, char> { ['K'] = 'K', ['Q'] = 'D', ['R'] = 'T', ['B'] = 'L', ['N'] = 'S' };
            var chars = san.ToCharArray();
            for (int i = 0; i < chars.Length; i++) if (map.TryGetValue(chars[i], out var c)) chars[i] = c;
            return new string(chars);
        }
    }
}

namespace TrkaZnanja.Core
{
    // Crtanje mape svijeta u Canvas32 (osnova za mini igre „Pronađi na mapi svijeta” i „Gdje je ova fotografija?”).
    public static class WorldPainter
    {
        public static readonly Rgba Sea = Rgba.Hex("#bfe3f2"), Land = Rgba.Hex("#e3eedb"), Border = Rgba.Hex("#94b384"),
            Grid = Rgba.Hex("#aed8ea");

        // Prsten u pikselima mape širine w (i kopije pomjerene za ±w kad prelazi ivicu mape).
        public static List<List<Vec2d>> Project(IList<Vec2d> ring, int w, int h)
        {
            double first = ring[0].X < WorldFrame.MinLon ? ring[0].X + 360 : ring[0].X;
            var u = WorldGeo.Unwrap(ring, first);
            double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
            var pts = new List<Vec2d>(u.Count);
            foreach (var p in u)
            {
                var q = new Vec2d((p.X - WorldFrame.MinLon) / (WorldFrame.MaxLon - WorldFrame.MinLon) * w, WorldFrame.V(p.Y) * h);
                lo = Math.Min(lo, q.X); hi = Math.Max(hi, q.X);
                pts.Add(q);
            }
            var res = new List<List<Vec2d>> { pts };
            if (hi > w) res.Add(pts.ConvertAll(p => new Vec2d(p.X - w, p.Y)));
            if (lo < 0) res.Add(pts.ConvertAll(p => new Vec2d(p.X + w, p.Y)));
            return res;
        }

        // Poligon sa rupama → lista poligona u pikselima (sa kopijama preko ivice).
        public static List<IList<IList<Vec2d>>> ProjectPolygon(IList<IList<Vec2d>> rings, int w, int h)
        {
            var outer = Project(rings[0], w, h);
            var holes = new List<List<List<Vec2d>>>();
            for (int i = 1; i < rings.Count; i++) holes.Add(Project(rings[i], w, h));
            var res = new List<IList<IList<Vec2d>>>();
            for (int k = 0; k < outer.Count; k++)
            {
                var poly = new List<IList<Vec2d>> { outer[k] };
                foreach (var hcopies in holes) if (k < hcopies.Count) poly.Add(hcopies[k]);
                res.Add(poly);
            }
            return res;
        }

        public static Canvas32 Base(int w, IList<WorldCountry> countries, IList<IList<IList<Vec2d>>> land)
        {
            int h = (int)Math.Round(w * WorldFrame.Aspect);
            var cv = new Canvas32(w, h);
            cv.Clear(Sea);
            // mreža na svakih 30°
            for (int lon = -150; lon <= 180; lon += 30)
            {
                double x = WorldFrame.U(lon) * w;
                cv.Line(new Vec2d(x, 0), new Vec2d(x, h), 1, Grid);
            }
            for (int lat = -30; lat <= 60; lat += 30)
            {
                double y = WorldFrame.V(lat) * h;
                cv.Line(new Vec2d(0, y), new Vec2d(w, y), 1, Grid);
            }
            foreach (var p in land) foreach (var q in ProjectPolygon(p, w, h)) cv.Fill(q, Land);
            foreach (var c in countries) foreach (var p in c.Polygons) foreach (var q in ProjectPolygon(p, w, h)) cv.Fill(q, Land);
            double bw = Math.Max(1, w / 2400.0);
            foreach (var c in countries) foreach (var p in c.Polygons) foreach (var r in p) foreach (var q in Project(r, w, h)) cv.Stroke(q, bw, Border);
            return cv;
        }

        // Država istaknuta bojom na prozirnoj podlozi (sloj preko osnovne mape).
        public static void Highlight(Canvas32 cv, WorldCountry c, Rgba fill)
        {
            foreach (var p in c.Polygons) foreach (var q in ProjectPolygon(p, cv.W, cv.H)) cv.Fill(q, fill);
        }

        // Obris jedne države sa susjedima i morem (mini igra „Pogodi državu po obliku”).
        public static Canvas32 Shape(WorldCountry d, IList<WorldCountry> all, IList<IList<IList<Vec2d>>> land, int w, int h)
        {
            var f = new ShapeFrame(d, w, h);
            var cv = new Canvas32(w, h);
            cv.Clear(Rgba.Hex("#9fd3ec"));
            var landCol = Rgba.Hex("#eef1e6"); var edge = Rgba.Hex("#b4bfa6");
            foreach (var p in land) if (f.RingInView(p[0])) cv.Fill(p.Select(f.Project).Cast<IList<Vec2d>>().ToList(), landCol);
            foreach (var o in all)
            {
                if (o == d) continue;
                foreach (var p in o.Polygons)
                {
                    if (!f.RingInView(p[0])) continue;
                    var rings = p.Select(f.Project).Cast<IList<Vec2d>>().ToList();
                    cv.Fill(rings, landCol);
                    foreach (var r in rings) cv.Stroke(r, 1.2, edge);
                }
            }
            var green = Rgba.Hex("#2f9e5b"); var dark = Rgba.Hex("#17643a");
            foreach (var p in d.Polygons)
            {
                var rings = p.Select(f.Project).Cast<IList<Vec2d>>().ToList();
                cv.Fill(rings, green);
                foreach (var r in rings) cv.Stroke(r, 2, dark);
            }
            return cv;
        }
    }
}
