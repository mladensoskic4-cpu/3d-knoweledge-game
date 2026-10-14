// Multiplayer bez sopstvenog servera: Supabase Realtime kanal po sobi (broadcast + presence).
// Svi klijenti vrte istu trku iz istog semena (pitanja, botovi, mini igre), a šalju se samo odgovori ljudi.
// Domaćin sobe (host) skuplja odgovore i objavljuje konačan rezultat runde, pa svi primjenjuju isto.
// Ovdje je dio bez Unity-ja: kod sobe, poruke, Phoenix okvir, skupljanje odgovora, uparivanje i postava trke.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public static class RoomCode
    {
        // bez slova i cifara koje se lako pomiješaju (0/O, 1/I/L)
        public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        public const int Length = 5;

        public static string New(Rng rng)
        {
            var c = new char[Length];
            for (int i = 0; i < Length; i++) c[i] = Alphabet[rng.Range(0, Alphabet.Length)];
            return new string(c);
        }

        // Ono što igrač otkuca (mala slova, razmaci, crtice) → ispravan kod ili null.
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = new string(input.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (s.Length != Length || s.Any(ch => Alphabet.IndexOf(ch) < 0)) return null;
            return s;
        }

        public static string Topic(string code) => "trka-soba-" + code;
    }

    // Odgovor jednog čovjeka u rundi (šalje se mrežom).
    public sealed class MpAnswer
    {
        public double Acc, Time;
        public bool TimedOut;
        public int Tiles;
        public string Text = "";
        public List<string> Powerups = new List<string>();

        public static MpAnswer From(QuizResult r, string text) => new MpAnswer
        {
            Acc = r.Accuracy, Time = r.TimeUsed, TimedOut = r.TimedOut, Tiles = r.Tiles, Text = text ?? "", Powerups = r.UsedPowerups.ToList(),
        };

        public static MpAnswer Timeout(double limit) => new MpAnswer { Acc = 0, Time = limit, TimedOut = true, Tiles = 0, Text = "isteklo vrijeme" };

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            ["acc"] = Math.Round(Acc, 4), ["time"] = Math.Round(Time, 3), ["to"] = TimedOut, ["tiles"] = (double)Tiles, ["text"] = Text,
            ["pu"] = Powerups.Cast<object>().ToList(),
        };

        public static MpAnswer FromJson(object o)
        {
            var d = Json.Obj(o);
            if (d == null) return null;
            return new MpAnswer
            {
                Acc = Mathx.Clamp(Json.Num(d, "acc"), 0, 1), Time = Math.Max(0, Json.Num(d, "time")), TimedOut = Json.Bool(d, "to"),
                Tiles = Mathx.Clamp(Json.Int(d, "tiles"), 0, 12), Text = Json.Str(d, "text", ""), Powerups = Json.StrList(Json.Get(d, "pu")),
            };
        }
    }

    // Rezultat mini igre jednog čovjeka.
    public sealed class MpScore
    {
        public double Score;
        public string Label = "";
        public Dictionary<string, object> ToJson() => new Dictionary<string, object> { ["score"] = Math.Round(Score, 4), ["label"] = Label };
        public static MpScore FromJson(object o)
        {
            var d = Json.Obj(o);
            return d == null ? null : new MpScore { Score = Mathx.Clamp(Json.Num(d, "score"), 0, 1), Label = Json.Str(d, "label", "") };
        }
    }

    public sealed class MpPlayer
    {
        public string Id, Name;
        public Look Look = new Look();

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            ["id"] = Id, ["name"] = Name,
            ["look"] = new Dictionary<string, object>
                {
                    ["z"] = Look.Zivotinja, ["v"] = Look.Vozilo, ["b"] = Look.Boja, ["k"] = Look.Kapa, ["n"] = Look.Naocare,
                    ["s"] = Look.Sara, ["c2"] = Look.Boja2, ["l"] = Look.Nalijepnica, ["t"] = Look.Trag,
                },
        };

        public static MpPlayer FromJson(object o)
        {
            var d = Json.Obj(o);
            if (d == null) return null;
            var l = Json.Obj(Json.Get(d, "look"));
            var name = Json.Str(d, "name", "Igrač");
            if (name.Length > 20) name = name.Substring(0, 20);
            return new MpPlayer
            {
                Id = Json.Str(d, "id"), Name = name,
                Look = l == null ? new Look() : new Look
                {
                    Zivotinja = Json.Str(l, "z", "vuk"), Vozilo = Json.Str(l, "v", "auto"), Boja = Json.Str(l, "b", "#e0312f"), Kapa = Json.Str(l, "k", "nema"), Naocare = Json.Bool(l, "n"),
                    Sara = Json.Str(l, "s", "nema"), Boja2 = Json.Str(l, "c2", "#ffffff"), Nalijepnica = Json.Str(l, "l", "nema"), Trag = Json.Str(l, "t", "nema"),
                },
            };
        }
    }

    // Postava trke koju domaćin pošalje svima: seme, mapa, dužina, oblasti i ljudi po redu. Botove svako napravi sam iz semena.
    public sealed class RaceSetup
    {
        public int Seed;
        public string Map = "cg", Atmosphere = "dan", Level = "srednje";
        public int Tiles = 58, MaxTezina = 3;
        public double BonusSeconds;
        public List<string> Areas = new List<string>();
        public List<MpPlayer> Humans = new List<MpPlayer>();
        public const int Seats = 4;
        public static readonly string[] SeatColors = { "#e0312f", "#2b7de9", "#2fbf6b", "#9b4bd6" };

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            ["seed"] = (double)Seed, ["map"] = Map, ["atmo"] = Atmosphere, ["level"] = Level, ["tiles"] = (double)Tiles, ["maxT"] = (double)MaxTezina,
            ["bonusS"] = BonusSeconds, ["areas"] = Areas.Cast<object>().ToList(), ["humans"] = Humans.Select(h => (object)h.ToJson()).ToList(),
        };

        public static RaceSetup FromJson(object o)
        {
            var d = Json.Obj(o);
            if (d == null) return null;
            var s = new RaceSetup
            {
                Seed = Json.Int(d, "seed"), Map = Json.Str(d, "map", "cg"), Atmosphere = Json.Str(d, "atmo", "dan"), Level = Json.Str(d, "level", "srednje"),
                Tiles = Json.Int(d, "tiles", 58), MaxTezina = Json.Int(d, "maxT", 3), BonusSeconds = Json.Num(d, "bonusS"), Areas = Json.StrList(Json.Get(d, "areas")),
            };
            foreach (var h in Json.Arr(Json.Get(d, "humans")) ?? new List<object>())
            {
                var p = MpPlayer.FromJson(h);
                if (p != null && !string.IsNullOrEmpty(p.Id) && s.Humans.All(x => x.Id != p.Id) && s.Humans.Count < Seats) s.Humans.Add(p);
            }
            return s.Humans.Count > 0 && s.Tiles >= 8 ? s : null;
        }

        // Isti redosljed igrača na svim uređajima: ljudi po mjestima, pa botovi. Lokalni igrač je Human, ostali ljudi su Remote.
        public List<Racer> BuildPlayers(string localId, Rng shared)
        {
            var list = new List<Racer>();
            for (int i = 0; i < Humans.Count; i++)
            {
                var h = Humans[i];
                var look = h.Look.Clone();
                list.Add(new Racer { Id = h.Id, Name = h.Name, Color = SeatColors[i], Human = h.Id == localId, Remote = h.Id != localId, Look = look });
            }
            int bots = Seats - Humans.Count;
            if (bots > 0)
            {
                var made = Bots.Create(Level, shared, bots, Humans[0].Look.Zivotinja);
                for (int i = 0; i < made.Count; i++)
                {
                    made[i].Color = SeatColors[Humans.Count + i];
                    made[i].Look.Boja = made[i].Color;
                    list.Add(made[i]);
                }
            }
            return list;
        }
    }

    // Domaćin skuplja odgovore ljudi za jednu rundu ili mini igru; kad stignu svi (ili istekne rok), objavljuje konačno.
    public sealed class Collector<T> where T : class
    {
        readonly HashSet<string> expected;
        readonly Dictionary<string, T> got = new Dictionary<string, T>();
        public Collector(IEnumerable<string> expectedIds) { expected = new HashSet<string>(expectedIds); }
        public bool Add(string id, T value)
        {
            if (!expected.Contains(id) || got.ContainsKey(id) || value == null) return false;
            got[id] = value;
            return true;
        }
        public void Drop(string id) => expected.Remove(id);   // igrač je izašao: ne čekamo ga
        public bool Complete => expected.All(got.ContainsKey);
        public IReadOnlyDictionary<string, T> Got => got;
        public IEnumerable<string> Missing => expected.Where(id => !got.ContainsKey(id));
        // konačno: ko nije stigao, dobija zamjenu (npr. isteklo vrijeme)
        public Dictionary<string, T> Decide(Func<string, T> missing)
        {
            var res = new Dictionary<string, T>(got);
            foreach (var id in expected) if (!res.ContainsKey(id)) res[id] = missing(id);
            return res;
        }
    }

    // Brza igra: igrači čekaju u zajedničkom predvorju (presence). Odlučuje samo onaj koji najduže čeka (vođa):
    // on objavi grupu i sobu, a ostali je samo prate. Tako nema dvije različite odluke.
    public static class Matchmaker
    {
        public sealed class Waiting { public string Id; public double Since; }
        public const double MaxWait = 15;      // sekundi: posle toga krećemo i sa 2 igrača (ostalo botovi)

        public static string Leader(IEnumerable<Waiting> lobby) => Sorted(lobby).Select(w => w.Id).FirstOrDefault();

        static List<Waiting> Sorted(IEnumerable<Waiting> lobby) => lobby.Where(w => !string.IsNullOrEmpty(w.Id)).GroupBy(w => w.Id)
            .Select(g => g.OrderBy(x => x.Since).First()).OrderBy(w => w.Since).ThenBy(w => w.Id, StringComparer.Ordinal).ToList();

        // Vraća grupu (prvi je domaćin) ili null. now i Since su sekunde po satu vođe.
        public static List<string> Group(IEnumerable<Waiting> lobby, double now)
        {
            var list = Sorted(lobby);
            if (list.Count >= RaceSetup.Seats) return list.Take(RaceSetup.Seats).Select(w => w.Id).ToList();
            if (list.Count >= 2 && now - list[0].Since >= MaxWait) return list.Select(w => w.Id).ToList();
            return null;
        }

        // Soba za grupu: izvedena iz domaćina i vremena, isti rezultat na svim uređajima.
        public static string RoomFor(IList<string> group, double hostSince)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (var ch in group[0] + "|" + Math.Round(hostSince)) { h ^= ch; h *= 16777619; }
                return RoomCode.New(new Rng((int)h));
            }
        }
    }

    // Phoenix poruke (Supabase Realtime, protokol 1.0.0): { topic, event, payload, ref, join_ref }.
    public static class Phoenix
    {
        public sealed class Msg
        {
            public string Topic, Event, Ref, JoinRef;
            public Dictionary<string, object> Payload;
        }

        public static string Encode(string topic, string ev, Dictionary<string, object> payload, string reference, string joinRef = null)
        {
            var d = new Dictionary<string, object> { ["topic"] = topic, ["event"] = ev, ["payload"] = payload ?? new Dictionary<string, object>(), ["ref"] = reference };
            if (joinRef != null) d["join_ref"] = joinRef;
            return Json.Write(d);
        }

        public static Msg Decode(string text)
        {
            try
            {
                var d = Json.Obj(Json.Parse(text));
                if (d == null) return null;
                return new Msg { Topic = Json.Str(d, "topic"), Event = Json.Str(d, "event"), Ref = Json.Get(d, "ref")?.ToString(), JoinRef = Json.Get(d, "join_ref")?.ToString(), Payload = Json.Obj(Json.Get(d, "payload")) ?? new Dictionary<string, object>() };
            }
            catch (Exception) { return null; }
        }

        public static string Join(string topic, string presenceKey, string accessToken, string reference) =>
            Encode(topic, "phx_join", new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object>
                {
                    ["broadcast"] = new Dictionary<string, object> { ["ack"] = false, ["self"] = false },
                    ["presence"] = new Dictionary<string, object> { ["enabled"] = true, ["key"] = presenceKey ?? "" },
                    ["private"] = false,
                },
                ["access_token"] = accessToken,
            }, reference, reference);

        public static string Heartbeat(string reference) => Encode("phoenix", "heartbeat", new Dictionary<string, object>(), reference);

        public static string Broadcast(string topic, string ev, Dictionary<string, object> payload, string reference, string joinRef) =>
            Encode(topic, "broadcast", new Dictionary<string, object> { ["type"] = "broadcast", ["event"] = ev, ["payload"] = payload }, reference, joinRef);

        public static string Track(string topic, Dictionary<string, object> meta, string reference, string joinRef) =>
            Encode(topic, "presence", new Dictionary<string, object> { ["type"] = "presence", ["event"] = "track", ["payload"] = meta }, reference, joinRef);

        public static string Leave(string topic, string reference, string joinRef) => Encode(topic, "phx_leave", new Dictionary<string, object>(), reference, joinRef);

        // presence_state / presence_diff → ključ → prvi meta zapis
        public static Dictionary<string, Dictionary<string, object>> PresenceMetas(Dictionary<string, object> map)
        {
            var res = new Dictionary<string, Dictionary<string, object>>();
            if (map == null) return res;
            foreach (var kv in map)
            {
                var metas = Json.Arr(Json.Get(Json.Obj(kv.Value), "metas"));
                res[kv.Key] = metas != null && metas.Count > 0 ? Json.Obj(metas[0]) ?? new Dictionary<string, object>() : new Dictionary<string, object>();
            }
            return res;
        }
    }
}
