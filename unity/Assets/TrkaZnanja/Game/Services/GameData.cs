// Podaci igre iz Resources/Data: pitanja, obrisi kopna i granica, zastave, države i prevodi.
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class Country { public string Code, Name, Capital, Continent; }

    public sealed class BorderSet
    {
        public sealed class C { public string Name; public List<IList<IList<Vec2d>>> Polygons; }
        public List<C> Countries = new List<C>();
    }

    public static class GameData
    {
        static List<Question> questions;
        static readonly Dictionary<string, List<IList<IList<Vec2d>>>> land = new Dictionary<string, List<IList<IList<Vec2d>>>>();
        static readonly Dictionary<string, BorderSet> borders = new Dictionary<string, BorderSet>();
        static readonly Dictionary<string, Texture2D> flags = new Dictionary<string, Texture2D>();
        static List<Country> countries;
        static Font font, fontBlack;

        public static string Text(string name)
        {
            var ta = Resources.Load<TextAsset>("Data/" + name);
            return ta != null ? ta.text : null;
        }

        public static List<Question> Questions
        {
            get
            {
                if (questions != null) return questions;
                var json = Text("pitanja");
                questions = json != null ? QuestionBank.Parse(json) : new List<Question>();
                if (questions.Count == 0) Debug.LogError("Trka znanja: pitanja nisu učitana (Resources/Data/pitanja.json).");
                return questions;
            }
        }

        public static void AddQuestions(IEnumerable<Question> extra) => Questions.AddRange(extra.Where(q => q.IsValid()));

        public static void LoadTranslations()
        {
            var en = Text("prevodi-en");
            if (en != null) Lang.LoadEnglish(en);
            var names = Text("en-drzave");
            if (names == null) return;
            var codes = Json.Obj(Json.Parse(names));
            var map = new Dictionary<string, string>();
            foreach (var c in Countries)
                if (codes != null && codes.TryGetValue(c.Code, out var e) && e is string s) map[c.Name] = s;
            Lang.AddCountryNames(map);
        }

        public static List<IList<IList<Vec2d>>> Land(string file)
        {
            if (land.TryGetValue(file, out var l)) return l;
            var json = Text("Mape/" + file);
            l = json != null ? Geo.ParsePolygons(Json.Get(Json.Obj(Json.Parse(json)), "polygons")) : new List<IList<IList<Vec2d>>>();
            land[file] = l;
            return l;
        }

        // Granice država za 2D mape u pitanjima (cg, srb, balkan).
        public static BorderSet Borders(string mapId)
        {
            if (borders.TryGetValue(mapId, out var b)) return b;
            b = new BorderSet();
            var json = Text("Mape/granice-" + mapId);
            if (json != null)
                foreach (var c in Json.Arr(Json.Get(Json.Obj(Json.Parse(json)), "countries")) ?? new List<object>())
                {
                    var d = Json.Obj(c);
                    b.Countries.Add(new BorderSet.C { Name = Json.Str(d, "name"), Polygons = Geo.ParsePolygons(Json.Get(d, "polygons")) });
                }
            borders[mapId] = b;
            return b;
        }

        public static Texture2D Flag(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            if (flags.TryGetValue(code, out var t)) return t;
            t = Resources.Load<Texture2D>("Zastave/" + code);
            flags[code] = t;
            return t;
        }

        public static List<Country> Countries
        {
            get
            {
                if (countries != null) return countries;
                countries = new List<Country>();
                var json = Text("drzave-lista");
                foreach (var o in Json.Arr(json != null ? Json.Parse(json) : null) ?? new List<object>())
                {
                    var d = Json.Obj(o);
                    countries.Add(new Country { Code = Json.Str(d, "kod"), Name = Json.Str(d, "naziv"), Capital = Json.Str(d, "grad"), Continent = Json.Str(d, "kontinent") });
                }
                return countries;
            }
        }

        public static Font Font => font ??= Resources.Load<Font>("Fonts/Nunito-ExtraBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font FontBlack => fontBlack ??= Resources.Load<Font>("Fonts/Nunito-Black") ?? Font;
    }
}
