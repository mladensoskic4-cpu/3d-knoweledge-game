// Podaci za mini igre: fotografije (mjesta, životinje, tijelo, instrumenti), zvukovi instrumenata,
// države svijeta sa obrisima, kopno, gradovi, rijeke i šahovski problemi. Sve se učitava tek kad zatreba.
// Mapa svijeta se crta u pozadinskoj niti (Canvas32), pa se samo prebaci u teksturu.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class PhotoItem
    {
        public string Id, Name, En, Group, Country, Author, License, Path;
        public double Lat = double.NaN, Lon = double.NaN;
        public bool HasPlace => !double.IsNaN(Lat) && !double.IsNaN(Lon);
        // na engleskom: naslov članka sa engleske Wikipedije (bez zagrada), kao u web verziji
        public string Display => Lang.Mode == Lang.En && !string.IsNullOrEmpty(En) ? System.Text.RegularExpressions.Regex.Replace(En, @"\s*\([^)]*\)\s*$", "") : Name;
        public string Credit
        {
            get
            {
                var a = System.Text.RegularExpressions.Regex.Replace(Author ?? "", @"\s+", " ").Trim();
                if (a.Length > 40) a = a.Substring(0, 38) + "…";
                return $"Foto: {(a.Length > 0 ? a : "nepoznat autor")} · {License}";
            }
        }
    }

    public sealed class InstrumentSound { public string Name, Folder; }

    public static class MinigameData
    {
        static readonly Dictionary<string, List<PhotoItem>> photos = new Dictionary<string, List<PhotoItem>>();
        static List<WorldCountry> world;
        static List<IList<IList<Vec2d>>> land;
        static List<WorldPlace> cities;
        static List<WorldRiver> rivers;
        static List<ChessPuzzle> chess;
        static List<InstrumentSound> instruments;
        static Task<Canvas32> worldJob;
        static Texture2D worldTex;
        public const int WorldTexWidth = 3072;

        // ključ po kojem se biraju slični ponuđeni odgovori
        static string GroupKey(string kat) => kat == "mjesta" ? "kontinent" : kat == "zivotinje" ? "grupa" : kat == "tijelo" ? "sistem" : "vrsta";

        public static List<PhotoItem> Photos(string kat)
        {
            if (photos.TryGetValue(kat, out var l)) return l;
            l = new List<PhotoItem>();
            var json = GameData.Text("slike-" + kat);
            foreach (var o in Json.Arr(json != null ? Json.Parse(json) : null) ?? new List<object>())
            {
                var d = Json.Obj(o);
                if (d == null) continue;
                var p = new PhotoItem
                {
                    Id = Json.Str(d, "id"), Name = Json.Str(d, "naziv"), En = Json.Str(d, "wikipedia"), Group = Json.Str(d, GroupKey(kat)),
                    Country = Json.Str(d, "drzava"), Author = Json.Str(d, "autor"), License = Json.Str(d, "licenca"), Path = Json.Str(d, "slika"),
                };
                if (Json.Get(d, "lat") is double la && Json.Get(d, "lon") is double lo) { p.Lat = la; p.Lon = lo; }
                if (!string.IsNullOrEmpty(p.Name) && !string.IsNullOrEmpty(p.Path)) l.Add(p);
            }
            photos[kat] = l;
            return l;
        }

        public static Texture2D Photo(PhotoItem p)
        {
            var t = Resources.Load<Texture2D>(p.Path);
            if (t != null) t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        public static List<InstrumentSound> Instruments
        {
            get
            {
                if (instruments != null) return instruments;
                instruments = new List<InstrumentSound>();
                var json = GameData.Text("instrumenti-zvuk");
                foreach (var o in Json.Arr(json != null ? Json.Parse(json) : null) ?? new List<object>())
                {
                    var d = Json.Obj(o);
                    if (d != null) instruments.Add(new InstrumentSound { Name = Json.Str(d, "naziv"), Folder = Json.Str(d, "folder") });
                }
                return instruments;
            }
        }

        public static string[] NotesFor(InstrumentSound s) => new[] { "C4", "E4", "G4", "C5" }.Select(n => $"Instrumenti/{s.Folder}/{n}").ToArray();

        public static List<WorldCountry> World
        {
            get
            {
                if (world != null) return world;
                var json = GameData.Text("Mape/drzave");
                world = json != null ? WorldGeo.ParseCountries(json) : new List<WorldCountry>();
                return world;
            }
        }

        public static List<IList<IList<Vec2d>>> Land
        {
            get
            {
                if (land != null) return land;
                var json = GameData.Text("Mape/kopno-svijet");
                land = json != null ? WorldGeo.ParseLand(json) : new List<IList<IList<Vec2d>>>();
                return land;
            }
        }

        static void LoadPlaces()
        {
            if (cities != null) return;
            var json = GameData.Text("svijet-gradovi-rijeke");
            if (json != null) WorldGeo.ParsePlaces(json, out cities, out rivers);
            else { cities = new List<WorldPlace>(); rivers = new List<WorldRiver>(); }
        }
        public static List<WorldPlace> Cities { get { LoadPlaces(); return cities; } }
        public static List<WorldRiver> Rivers { get { LoadPlaces(); return rivers; } }

        public static List<ChessPuzzle> Chess
        {
            get
            {
                if (chess != null) return chess;
                var json = GameData.Text("sah");
                chess = json != null ? ChessPuzzle.ParseAll(json) : new List<ChessPuzzle>();
                return chess;
            }
        }

        // Pokreni crtanje mape svijeta unaprijed (dok igrač čita uputstvo).
        public static void WarmWorldMap()
        {
            if (worldTex != null || worldJob != null) return;
            var w = World; var l = Land;     // JSON se čita na glavnoj niti (Resources)
#if UNITY_WEBGL && !UNITY_EDITOR
            worldJob = Task.FromResult(WorldPainter.Base(WorldTexWidth / 2, w, l));   // WebGL nema niti
#else
            worldJob = Task.Run(() => WorldPainter.Base(WorldTexWidth, w, l));
#endif
        }

        public static bool WorldMapReady => worldTex != null || (worldJob != null && worldJob.IsCompleted);

        public static Texture2D WorldMap
        {
            get
            {
                if (worldTex != null) return worldTex;
                WarmWorldMap();
                var cv = worldJob.Result;
                worldTex = ToTexture(cv, "Mapa svijeta");
                worldTex.wrapModeU = TextureWrapMode.Repeat;
                worldTex.wrapModeV = TextureWrapMode.Clamp;
                worldJob = null;
                return worldTex;
            }
        }

        public static Texture2D ToTexture(Canvas32 cv, string name, bool mip = true)
        {
            var t = new Texture2D(cv.W, cv.H, TextureFormat.RGBA32, mip) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            Upload(t, cv);
            return t;
        }

        public static void Upload(Texture2D t, Canvas32 cv)
        {
            if (t.mipmapCount > 1)
            {
                // sa mipmapama LoadRawTextureData traži sve nivoe, pa prvi nivo upisujemo preko SetPixels32
                var px = new Color32[cv.W * cv.H];
                for (int i = 0, j = 0; i < px.Length; i++, j += 4) px[i] = new Color32(cv.Data[j], cv.Data[j + 1], cv.Data[j + 2], cv.Data[j + 3]);
                t.SetPixels32(px);
                t.Apply(true);
            }
            else
            {
                t.LoadRawTextureData(cv.Data);
                t.Apply(false);
            }
        }
    }
}
