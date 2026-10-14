// Katalog igre: oblasti, rangovi, uzrasti, dužine trke, mape (svjetovi), životinje, vozila, kape i power-upovi.
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class Area { public string Id, Label, Color; }
    public sealed class Rank { public string Id, Label, Color; public int Min; }
    public sealed class AgeGroup { public string Id, Label, Note; public int MaxTezina; public double BonusSeconds; }
    public sealed class TrackLength { public string Id, Label, Note; public int Tiles; }

    public sealed class RaceMap
    {
        public string Id, Name, Rank, Finish, Land;     // Land: ime fajla sa obrisom kopna u Resources/Data/Mape (Natural Earth)
        public string Atmosfera;                          // podrazumijevano vrijeme: dan, zalazak, noc, kisa, snijeg
        public (string name, double lon, double lat)[] Route;
    }

    public sealed class Animal
    {
        public string Id, Name, En, Body, Accent, Shape;  // Shape: pas, macka, medvjed, zec, ptica, koza, konj, panda, lav
        public int Price; public string Rank;
    }

    public sealed class Vehicle
    {
        public string Id, Name, En, DefaultPaint;
        public int Price; public string Rank;
        public double Bounce;                            // koliko poskakuje u vožnji (osobina vozila)
        public bool Floats, Flies;
    }

    public sealed class ShopItem
    {
        public string Id, Slot, Value, Label, Rank, For, Swatch;
        public int Price;
    }

    public sealed class Powerup { public string Id, Kind, Label, Opis; public int Price; }

    public static class Catalog
    {
        public static readonly Area[] Areas =
        {
            new Area { Id = "geografija", Label = "Geografija", Color = "#1f86c2" },
            new Area { Id = "istorija", Label = "Istorija", Color = "#a8452f" },
            new Area { Id = "sport", Label = "Sport", Color = "#2f9e5b" },
            new Area { Id = "muzika", Label = "Muzika i pop kultura", Color = "#9b4bd6" },
            new Area { Id = "priroda", Label = "Priroda i životinje", Color = "#5c8d2c" },
            new Area { Id = "hrana", Label = "Hrana i običaji", Color = "#d9822b" },
            new Area { Id = "logika", Label = "Logika i matematika", Color = "#3d5a80" },
            new Area { Id = "film", Label = "Filmovi i serije", Color = "#c2410c" },
            new Area { Id = "nauka", Label = "Nauka i svemir", Color = "#2a4d9b" },
            new Area { Id = "jezici", Label = "Jezici svijeta", Color = "#0f8a8a" },
            new Area { Id = "tehnologija", Label = "Tehnologija i gejming", Color = "#6b3fa0" },
        };
        public static Area AreaOf(string id) => Areas.FirstOrDefault(a => a.Id == id) ?? new Area { Id = id, Label = id, Color = "#555555" };

        public static readonly Rank[] Ranks =
        {
            new Rank { Id = "bronza", Label = "Bronza", Min = 0, Color = "#b0703c" },
            new Rank { Id = "srebro", Label = "Srebro", Min = 250, Color = "#8f9ba6" },
            new Rank { Id = "zlato", Label = "Zlato", Min = 700, Color = "#d9a21b" },
            new Rank { Id = "platina", Label = "Platina", Min = 1400, Color = "#3aa7a7" },
            new Rank { Id = "dijamant", Label = "Dijamant", Min = 2500, Color = "#5b7cfa" },
        };
        public static int RankIndex(string id) => System.Array.FindIndex(Ranks, r => r.Id == id);
        public static Rank RankOf(int points)
        {
            var r = Ranks[0];
            foreach (var x in Ranks) if (points >= x.Min) r = x;
            return r;
        }
        public static (double pct, Rank next, int need) RankProgress(int points)
        {
            int i = RankIndex(RankOf(points).Id);
            if (i + 1 >= Ranks.Length) return (1, null, 0);
            var cur = Ranks[i]; var next = Ranks[i + 1];
            return ((double)(points - cur.Min) / (next.Min - cur.Min), next, next.Min - points);
        }
        public static bool RankAllows(string required, int points) =>
            string.IsNullOrEmpty(required) || RankIndex(RankOf(points).Id) >= RankIndex(required);

        public static readonly AgeGroup[] Ages =
        {
            new AgeGroup { Id = "djeca", Label = "Djeca", Note = "5–12 godina · lakša pitanja, više vremena", MaxTezina = 1, BonusSeconds = 6 },
            new AgeGroup { Id = "odrasli", Label = "Odrasli", Note = "sva pitanja", MaxTezina = 3, BonusSeconds = 0 },
        };
        public static AgeGroup AgeOf(string id) => Ages.FirstOrDefault(a => a.Id == id) ?? Ages[1];

        // Oko 2,2 polja po rundi za vodećeg i oko 25 s po rundi (pitanje + vožnja).
        public static readonly TrackLength[] Lengths =
        {
            new TrackLength { Id = "5", Label = "5 min", Note = "brza trka", Tiles = 32 },
            new TrackLength { Id = "10", Label = "10 min", Note = "standardna trka", Tiles = 58 },
            new TrackLength { Id = "15", Label = "15 min", Note = "duga trka, više mini igara", Tiles = 84 },
        };
        public static TrackLength LengthOf(string id) => Lengths.FirstOrDefault(l => l.Id == id) ?? Lengths[1];

        public const int MinigameEvery = 4;          // svake 4 runde svi igraju istu mini igru
        public const double BonusTime = 1.6;

        public static readonly RaceMap[] Maps =
        {
            new RaceMap
            {
                Id = "cg", Name = "Crna Gora", Rank = "bronza", Finish = "Žabljak", Land = "evropa", Atmosfera = "dan",
                Route = new[]
                {
                    ("Herceg Novi", 18.537, 42.453), ("Kotor", 18.771, 42.424), ("Cetinje", 18.914, 42.39), ("Budva", 18.84, 42.289),
                    ("Bar", 19.1, 42.093), ("Ulcinj", 19.216, 41.929), ("Podgorica", 19.262, 42.441), ("Nikšić", 18.944, 42.773),
                    ("Plužine", 18.84, 43.153), ("Žabljak", 19.123, 43.154)
                }
            },
            new RaceMap
            {
                Id = "balkan", Name = "Balkan", Rank = "srebro", Finish = "Žabljak", Land = "evropa", Atmosfera = "zalazak",
                Route = new[]
                {
                    ("Ljubljana", 14.506, 46.056), ("Zagreb", 15.982, 45.815), ("Novi Sad", 19.845, 45.267), ("Beograd", 20.457, 44.787),
                    ("Kragujevac", 20.911, 44.012), ("Zlatibor", 19.7, 43.73), ("Sarajevo", 18.413, 43.856), ("Mostar", 17.807, 43.343),
                    ("Dubrovnik", 18.094, 42.65), ("Herceg Novi", 18.537, 42.453), ("Kotor", 18.771, 42.424), ("Budva", 18.84, 42.289),
                    ("Bar", 19.1, 42.093), ("Ulcinj", 19.216, 41.929), ("Podgorica", 19.262, 42.441), ("Kolašin", 19.522, 42.823),
                    ("Žabljak", 19.123, 43.154)
                }
            },
            new RaceMap
            {
                Id = "evropa", Name = "Evropa", Rank = "zlato", Finish = "Podgorica", Land = "evropa", Atmosfera = "kisa",
                Route = new[]
                {
                    ("Lisabon", -9.139, 38.722), ("Madrid", -3.704, 40.417), ("Pariz", 2.352, 48.857), ("London", -0.128, 51.507),
                    ("Amsterdam", 4.904, 52.368), ("Berlin", 13.405, 52.52), ("Prag", 14.438, 50.075), ("Beč", 16.373, 48.208),
                    ("Budimpešta", 19.04, 47.498), ("Beograd", 20.457, 44.787), ("Podgorica", 19.262, 42.441)
                }
            },
            new RaceMap
            {
                Id = "svijet", Name = "Svijet", Rank = "platina", Finish = "Sidnej", Land = "svijet", Atmosfera = "noc",
                Route = new[]
                {
                    ("Njujork", -74.006, 40.713), ("Rio de Žaneiro", -43.173, -22.907), ("Kejptaun", 18.424, -33.925), ("Najrobi", 36.822, -1.292),
                    ("Kairo", 31.236, 30.044), ("Podgorica", 19.262, 42.441), ("Moskva", 37.618, 55.756), ("Nju Delhi", 77.209, 28.614),
                    ("Peking", 116.407, 39.904), ("Tokio", 139.692, 35.69), ("Sidnej", 151.209, -33.869)
                }
            },
        };
        public static RaceMap MapOf(string id) => Maps.FirstOrDefault(m => m.Id == id) ?? Maps[0];

        public static readonly string[] Atmosfere = { "dan", "zalazak", "noc", "kisa", "snijeg" };

        public static readonly Animal[] Animals =
        {
            new Animal { Id = "vuk", Name = "Vuk", En = "Wolf", Shape = "pas", Body = "#8c8f96", Accent = "#e9e4da", Price = 0 },
            new Animal { Id = "zec", Name = "Zec", En = "Hare", Shape = "zec", Body = "#b58a62", Accent = "#f2e6d6", Price = 0 },
            new Animal { Id = "kuce", Name = "Kuče", En = "Puppy", Shape = "pas", Body = "#d7a25f", Accent = "#fff3e0", Price = 0 },
            new Animal { Id = "mace", Name = "Mače", En = "Kitten", Shape = "macka", Body = "#f0a04b", Accent = "#fff6ea", Price = 120 },
            new Animal { Id = "lisica", Name = "Lisica", En = "Fox", Shape = "macka", Body = "#e2672a", Accent = "#fff4ea", Price = 150 },
            new Animal { Id = "medvjed", Name = "Medvjed", En = "Bear", Shape = "medvjed", Body = "#6b4a32", Accent = "#c99d74", Price = 200 },
            new Animal { Id = "koza", Name = "Koza", En = "Goat", Shape = "koza", Body = "#f1ece2", Accent = "#c9b28f", Price = 150 },
            new Animal { Id = "ovca", Name = "Ovca", En = "Sheep", Shape = "ovca", Body = "#fbf8f2", Accent = "#3b3534", Price = 150 },
            new Animal { Id = "magarac", Name = "Magarac", En = "Donkey", Shape = "konj", Body = "#9c938a", Accent = "#e7e1da", Price = 180 },
            new Animal { Id = "konj", Name = "Konj", En = "Horse", Shape = "konj", Body = "#8b5a2b", Accent = "#2b1d12", Price = 250, Rank = "srebro" },
            new Animal { Id = "sova", Name = "Sova", En = "Owl", Shape = "ptica", Body = "#9a7b5a", Accent = "#f3e2c3", Price = 220 },
            new Animal { Id = "soko", Name = "Soko", En = "Falcon", Shape = "ptica", Body = "#6f6a66", Accent = "#f5efe6", Price = 300, Rank = "srebro" },
            new Animal { Id = "orao", Name = "Orao", En = "Eagle", Shape = "ptica", Body = "#5a3d25", Accent = "#ffffff", Price = 500, Rank = "zlato" },
            new Animal { Id = "panda", Name = "Panda", En = "Panda", Shape = "panda", Body = "#fbfbf8", Accent = "#1d1d1f", Price = 600, Rank = "zlato" },
            new Animal { Id = "lav", Name = "Lav", En = "Lion", Shape = "lav", Body = "#e3a743", Accent = "#9c5a1f", Price = 900, Rank = "platina" },
        };
        public static Animal AnimalOf(string id) => Animals.FirstOrDefault(a => a.Id == id) ?? Animals[0];

        public static readonly Vehicle[] Vehicles =
        {
            new Vehicle { Id = "auto", Name = "Auto", En = "Car", DefaultPaint = "#e0312f", Price = 0, Bounce = 0.25 },
            new Vehicle { Id = "bicikl", Name = "Bicikl", En = "Bicycle", DefaultPaint = "#2b7de9", Price = 0, Bounce = 0.35 },
            new Vehicle { Id = "kolica", Name = "Kolica", En = "Go-kart", DefaultPaint = "#f2b630", Price = 80, Bounce = 0.45 },
            new Vehicle { Id = "skuter", Name = "Skuter", En = "Scooter", DefaultPaint = "#5fc3c0", Price = 120, Bounce = 0.3 },
            new Vehicle { Id = "fico", Name = "Mali retro auto", En = "Little retro car", DefaultPaint = "#e8e2c8", Price = 150, Bounce = 0.4 },
            new Vehicle { Id = "motor", Name = "Motor", En = "Motorbike", DefaultPaint = "#14232e", Price = 220, Bounce = 0.2 },
            new Vehicle { Id = "traktor", Name = "Traktor", En = "Tractor", DefaultPaint = "#2f9e5b", Price = 260, Bounce = 0.5 },
            new Vehicle { Id = "zaprega", Name = "Konjska zaprega", En = "Horse cart", DefaultPaint = "#8b5a2b", Price = 300, Bounce = 0.45 },
            new Vehicle { Id = "kamion", Name = "Kamion", En = "Truck", DefaultPaint = "#f08a1c", Price = 450, Rank = "srebro", Bounce = 0.2 },
            new Vehicle { Id = "brod", Name = "Brod", En = "Boat", DefaultPaint = "#ffffff", Price = 600, Rank = "zlato", Bounce = 0.15, Floats = true },
            new Vehicle { Id = "balon", Name = "Balon", En = "Hot-air balloon", DefaultPaint = "#c8102e", Price = 750, Rank = "zlato", Bounce = 0.05, Flies = true },
            new Vehicle { Id = "avion", Name = "Avion", En = "Plane", DefaultPaint = "#f4f4f4", Price = 1000, Rank = "platina", Bounce = 0.05, Flies = true },
        };
        public static Vehicle VehicleOf(string id) => Vehicles.FirstOrDefault(v => v.Id == id) ?? Vehicles[0];

        public static readonly (string color, string label, int price, string rank)[] Paints =
        {
            ("#e0312f", "Crvena", 0, null), ("#2b7de9", "Plava", 0, null), ("#16a36a", "Zelena", 40, null), ("#f08a1c", "Narandžasta", 40, null),
            ("#9b4bd6", "Ljubičasta", 80, null), ("#14232e", "Crna", 100, null), ("#f4f4f4", "Bijela", 100, null), ("#ff6fae", "Roze", 60, null),
            ("#e3b23c", "Zlatna", 400, "zlato"), ("#5b7cfa", "Dijamantska", 800, "dijamant"),
        };

        public static readonly (string id, string label, int price, string rank)[] Hats =
        {
            ("nema", "Bez kape", 0, null), ("kacket", "Kačket", 60, null), ("sesir", "Šešir", 120, null),
            ("kapa", "Tradicionalna kapa", 150, null), ("kaciga", "Kaciga", 200, "srebro"), ("kruna", "Kruna", 1000, "dijamant"),
        };

        public static readonly Powerup[] Powerups =
        {
            new Powerup { Id = "pola", Kind = "fifty", Label = "50/50", Price = 40, Opis = "Uklanja dva netačna odgovora (pitanja sa ponuđenim odgovorima)." },
            new Powerup { Id = "zamrzni", Kind = "freeze", Label = "Zamrzni tajmer", Price = 50, Opis = "Zaustavlja vrijeme na 5 sekundi." },
            new Powerup { Id = "precica", Kind = "shortcut", Label = "Prečica", Price = 80, Opis = "U ovoj rundi ideš 2 polja više." },
            new Powerup { Id = "uspori", Kind = "slow", Label = "Uspori protivnike", Price = 70, Opis = "U ovoj rundi svaki protivnik ide polje manje." },
        };

        // Sve što se kupuje u garaži (životinje, vozila, boje, kape, naočare).
        public static readonly List<ShopItem> Shop = BuildShop();
        static List<ShopItem> BuildShop()
        {
            var s = new List<ShopItem>();
            foreach (var a in Animals) s.Add(new ShopItem { Id = "z-" + a.Id, Slot = "zivotinja", Value = a.Id, Label = a.Name, Price = a.Price, Rank = a.Rank, Swatch = a.Body });
            foreach (var v in Vehicles) s.Add(new ShopItem { Id = "v-" + v.Id, Slot = "vozilo", Value = v.Id, Label = v.Name, Price = v.Price, Rank = v.Rank, Swatch = v.DefaultPaint });
            foreach (var p in Paints) s.Add(new ShopItem { Id = "b-" + p.color, Slot = "boja", Value = p.color, Label = p.label, Price = p.price, Rank = p.rank, Swatch = p.color });
            foreach (var h in Hats) s.Add(new ShopItem { Id = "h-" + h.id, Slot = "kapa", Value = h.id, Label = h.label, Price = h.price, Rank = h.rank });
            s.Add(new ShopItem { Id = "n-ne", Slot = "naocare", Value = "ne", Label = "Bez naočara", Price = 0 });
            s.Add(new ShopItem { Id = "n-da", Slot = "naocare", Value = "da", Label = "Naočare za sunce", Price = 80 });
            return s;
        }
        public static ShopItem ShopItemOf(string id) => Shop.FirstOrDefault(i => i.Id == id);
        public static IEnumerable<string> FreeItems => Shop.Where(i => i.Price == 0 && string.IsNullOrEmpty(i.Rank)).Select(i => i.Id);

        // Nagrada za mini igru (stanica na stazi).
        public static (int coins, int jump) RewardFor(double score)
        {
            int coins = (int)System.Math.Round(score * 30, System.MidpointRounding.AwayFromZero);
            int jump = score >= 0.9 ? 3 : score >= 0.6 ? 2 : score >= 0.3 ? 1 : 0;
            return (coins, jump);
        }
    }
}
