// Pitanje iz data/questions.json (isti fajl koristi i web verzija).
using System.Collections.Generic;

namespace TrkaZnanja.Core
{
    public sealed class MapPoint
    {
        public double Lon, Lat;
        public string Naziv;
    }

    public sealed class Question
    {
        public string Id, Oblast, Tip, Pitanje, Info;
        public int Tezina = 1;
        public double Rang;
        public double? Vrijeme;            // posebno vrijeme za ovo pitanje (s)

        // višestruki izbor, mutna slika, melodija
        public List<string> Opcije = new List<string>();
        public string TacnoText;
        public string Zastava;             // mutna: kod zastave (npr. "me")
        public string Note;                // melodija: "E4:1 D4:.5 R:1"
        public double Tempo = 110;
        public string Djelo;

        // procjena
        public double Min, Max, Korak = 1, TacnoBroj, Tolerancija;
        public string Jedinica, Format;

        // poredaj
        public List<string> Redoslijed = new List<string>();
        public List<string> Oznake;

        // spoji
        public List<KeyValuePair<string, string>> Parovi = new List<KeyValuePair<string, string>>();

        // mapa
        public string Mapa = "cg";
        public MapPoint Tacka;
        public double? TolPun, TolNula;

        public Dictionary<string, object> En;   // engleska verzija polja (ako postoji)

        public static Question FromJson(Dictionary<string, object> d)
        {
            var q = new Question
            {
                Id = Json.Str(d, "id"),
                Oblast = Json.Str(d, "oblast"),
                Tip = Json.Str(d, "tip"),
                Pitanje = Json.Str(d, "pitanje", ""),
                Info = Json.Str(d, "info"),
                Tezina = Mathx.Clamp(Json.Int(d, "tezina", 1), 1, 3),
                Rang = Json.Num(d, "rang"),
                En = Json.Obj(Json.Get(d, "en")),
            };
            if (Json.Get(d, "vrijeme") is double v) q.Vrijeme = v;
            q.Fill(d);
            return q;
        }

        void Fill(Dictionary<string, object> d)
        {
            if (d.ContainsKey("pitanje")) Pitanje = Json.Str(d, "pitanje", Pitanje);
            if (d.ContainsKey("info")) Info = Json.Str(d, "info");
            if (d.ContainsKey("opcije")) Opcije = Json.StrList(Json.Get(d, "opcije"));
            if (d.ContainsKey("zastava")) Zastava = Json.Str(d, "zastava");
            if (d.ContainsKey("note")) Note = Json.Str(d, "note");
            if (d.ContainsKey("tempo")) Tempo = Json.Num(d, "tempo", 110);
            if (d.ContainsKey("djelo")) Djelo = Json.Str(d, "djelo");
            if (d.ContainsKey("min")) Min = Json.Num(d, "min");
            if (d.ContainsKey("max")) Max = Json.Num(d, "max");
            if (d.ContainsKey("korak")) Korak = Json.Num(d, "korak", 1);
            if (d.ContainsKey("jedinica")) Jedinica = Json.Str(d, "jedinica");
            if (d.ContainsKey("format")) Format = Json.Str(d, "format");
            if (d.ContainsKey("oznake")) Oznake = Json.StrList(Json.Get(d, "oznake"));
            if (d.ContainsKey("mapa")) Mapa = Json.Str(d, "mapa", "cg");

            var tol = Json.Get(d, "tolerancija");
            if (tol is double t) Tolerancija = t;
            else if (tol is Dictionary<string, object> td)
            {
                if (Json.Get(td, "pun") is double p) TolPun = p;
                if (Json.Get(td, "nula") is double n) TolNula = n;
            }

            if (!d.ContainsKey("tacno")) return;
            var tacno = Json.Get(d, "tacno");
            switch (Tip)
            {
                case "procjena": TacnoBroj = tacno is double x ? x : Json.Num(d, "tacno"); break;
                case "poredaj": Redoslijed = Json.StrList(tacno); break;
                case "spoji":
                    Parovi.Clear();
                    foreach (var p in Json.Arr(tacno) ?? new List<object>())
                    {
                        var pair = Json.StrList(p);
                        if (pair.Count >= 2) Parovi.Add(new KeyValuePair<string, string>(pair[0], pair[1]));
                    }
                    break;
                case "mapa":
                    if (tacno is Dictionary<string, object> m)
                        Tacka = new MapPoint { Lon = Json.Num(m, "lon"), Lat = Json.Num(m, "lat"), Naziv = Json.Str(m, "naziv", "") };
                    break;
                default: TacnoText = tacno as string ?? Json.Str(d, "tacno"); break;
            }
        }

        public Question Clone()
        {
            var c = (Question)MemberwiseClone();
            c.Opcije = new List<string>(Opcije);
            c.Redoslijed = new List<string>(Redoslijed);
            c.Parovi = new List<KeyValuePair<string, string>>(Parovi);
            return c;
        }

        // Kopija pitanja na traženom jeziku (engleska polja preko naših, ako postoje).
        public Question Localized(string lang)
        {
            if (lang != "en" || En == null) return this;
            var c = (Question)MemberwiseClone();
            c.Opcije = new List<string>(Opcije);
            c.Redoslijed = new List<string>(Redoslijed);
            c.Parovi = new List<KeyValuePair<string, string>>(Parovi);
            c.Fill(En);
            return c;
        }

        // Da li pitanje ima sve što njegov tip traži (loša pitanja se preskaču umjesto da sruše igru).
        public bool IsValid()
        {
            switch (Tip)
            {
                case "izbor":
                case "melodija":
                    return Opcije.Count >= 2 && Opcije.Contains(TacnoText) && (Tip != "melodija" || !string.IsNullOrEmpty(Note));
                case "mutna": return Opcije.Count >= 2 && Opcije.Contains(TacnoText) && !string.IsNullOrEmpty(Zastava);
                case "procjena": return Max > Min && Tolerancija > 0 && Korak > 0;
                case "poredaj": return Redoslijed.Count >= 2;
                case "spoji": return Parovi.Count >= 2;
                case "mapa": return Tacka != null;
                default: return false;
            }
        }

        public override string ToString() => $"{Id} [{Tip}] {Pitanje}";
    }

    public static class QuestionBank
    {
        // Učitava { "pitanja": [...] } ili samo niz pitanja.
        public static List<Question> Parse(string json)
        {
            var root = Json.Parse(json);
            var arr = root is List<object> a ? a : Json.Arr(Json.Get(Json.Obj(root), "pitanja"));
            var list = new List<Question>();
            if (arr == null) return list;
            foreach (var o in arr)
            {
                if (!(o is Dictionary<string, object> d)) continue;
                var q = Question.FromJson(d);
                if (QuizTypes.Get(q.Tip) != null && q.IsValid()) list.Add(q);
            }
            return list;
        }
    }
}
