// Soba za igru sa drugima: ko je u sobi (presence), start trke i razmjena odgovora.
// Svaki igrač pošalje svoj doprinos (odgovor na pitanje, rezultat mini igre) svima. Domaćin čeka sve prisutne
// (ili istek roka) i objavi konačnu odluku; svi primjenjuju samo odluke, pa trka ostaje ista na svim uređajima.
// Ako domaćin izađe, domaćin postaje sljedeći igrač i nastavlja odlučivanje od mjesta gdje je stalo.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class MpMember
    {
        public string Id, Name;
        public Look Look = new Look();
        public double Since;
    }

    public sealed class MpSession : MonoBehaviour
    {
        public static MpSession Current { get; private set; }
        public string Code { get; private set; }
        public string LocalId { get; private set; }
        public RealtimeChannel Channel { get; private set; }
        public RaceSetup Setup { get; private set; }
        public bool Lost { get; private set; }           // veza prekinuta u trci: dalje igramo sami (ostali = isteklo vrijeme)
        public List<string> Group;                       // brza igra: ko treba da dođe (prvi je domaćin)
        public List<MpMember> Members { get; } = new List<MpMember>();

        public event Action OnMembers;
        public event Action<RaceSetup> OnStart;
        public event Action<string> OnProblem;
        public event Action<string, string> OnContribution;   // ključ, ko (za testove i prikaz)

        readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> contributions = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> decided = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        double since;

        static string guestId;
        static string MyId() => !string.IsNullOrEmpty(Net.UserId) ? Net.UserId : guestId ??= "gost-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        public static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        public bool Connected => Channel != null && Channel.Status == RealtimeChannel.State.Joined;

        public static MpSession Join(string code, Profile p, Transform parent, string id = null, bool current = true)
        {
            if (current) Current?.Leave();
            var go = new GameObject("Soba " + code);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<MpSession>();
            if (current) Current = s;
            s.Code = code;
            s.LocalId = id ?? MyId();
            s.since = Now;
            s.Channel = new RealtimeChannel(RoomCode.Topic(code), s.LocalId);
            s.Channel.OnBroadcast += s.Receive;
            s.Channel.OnPresence += s.PresenceChanged;
            s.Channel.OnStatus += st =>
            {
                if (st == RealtimeChannel.State.Failed || st == RealtimeChannel.State.Closed)
                {
                    if (s.Setup != null) s.Lost = true;
                    s.OnProblem?.Invoke(s.Channel.Error ?? "Veza je prekinuta.");
                }
            };
            s.Channel.Track(MemberMeta(s.LocalId, p, s.since));
            s.Channel.Connect();
            return s;
        }

        public static Dictionary<string, object> MemberMeta(string id, Profile p, double since)
        {
            var me = new MpPlayer { Id = id, Name = string.IsNullOrEmpty(p.Ime) ? "Igrač" : p.Ime, Look = p.Izgled.Clone() }.ToJson();
            me["since"] = since;
            return me;
        }

        public void Leave()
        {
            Channel?.Close();
            if (Current == this) Current = null;
            if (this != null) Destroy(gameObject);
        }

        void Update() => Channel?.Pump();
        void OnDestroy() { Channel?.Close(); if (Current == this) Current = null; }

        void PresenceChanged()
        {
            Members.Clear();
            foreach (var kv in Channel.Presence)
            {
                var p = MpPlayer.FromJson(kv.Value);
                if (p == null) continue;
                Members.Add(new MpMember { Id = kv.Key, Name = p.Name, Look = p.Look, Since = Json.Num(kv.Value, "since") });
            }
            Members.Sort((a, b) => a.Since != b.Since ? a.Since.CompareTo(b.Since) : string.CompareOrdinal(a.Id, b.Id));
            OnMembers?.Invoke();
        }

        public bool Present(string id) => id == LocalId || Members.Any(m => m.Id == id);

        // Domaćin: prije starta prvi iz brze grupe ili onaj ko je najduže u sobi; u trci prvi prisutni po redu iz postave.
        public string HostId
        {
            get
            {
                if (Setup != null) return Setup.Humans.Select(h => h.Id).FirstOrDefault(Present) ?? LocalId;
                if (Group != null && Group.Count > 0 && Present(Group[0])) return Group[0];
                return Members.Count > 0 ? Members[0].Id : LocalId;
            }
        }
        public bool IsHost => HostId == LocalId;

        // ---------- start ----------
        public void StartRace(RaceSetup setup)
        {
            if (!IsHost || Setup != null) return;
            Channel.Broadcast("m", new Dictionary<string, object> { ["t"] = "start", ["s"] = setup.ToJson() });
            BeginRace(setup);
        }

        void BeginRace(RaceSetup setup)
        {
            if (Setup != null) return;
            Setup = setup;
            OnStart?.Invoke(setup);
        }

        // ---------- poruke ----------
        void Receive(string ev, Dictionary<string, object> m)
        {
            if (ev != "m" || m == null) return;
            switch (Json.Str(m, "t"))
            {
                case "start":
                    var s = RaceSetup.FromJson(Json.Get(m, "s"));
                    if (s != null && s.Humans.Any(h => h.Id == LocalId)) BeginRace(s);
                    break;
                case "c":   // doprinos jednog igrača
                    Store(contributions, Json.Str(m, "k"), Json.Str(m, "id"), Json.Obj(Json.Get(m, "v")));
                    OnContribution?.Invoke(Json.Str(m, "k"), Json.Str(m, "id"));
                    break;
                case "d":   // odluka domaćina
                    var k = Json.Str(m, "k");
                    if (k != null && !decided.ContainsKey(k))
                    {
                        var res = new Dictionary<string, Dictionary<string, object>>();
                        foreach (var kv in Json.Obj(Json.Get(m, "res")) ?? new Dictionary<string, object>())
                            if (Json.Obj(kv.Value) is Dictionary<string, object> v) res[kv.Key] = v;
                        decided[k] = res;
                    }
                    break;
            }
        }

        static void Store(Dictionary<string, Dictionary<string, Dictionary<string, object>>> where, string key, string id, Dictionary<string, object> v)
        {
            if (key == null || id == null || v == null) return;
            if (!where.TryGetValue(key, out var d)) where[key] = d = new Dictionary<string, Dictionary<string, object>>();
            if (!d.ContainsKey(id)) d[id] = v;
        }

        public void Contribute(string key, Dictionary<string, object> value)
        {
            Store(contributions, key, LocalId, value);
            if (!Lost) Channel.Broadcast("m", new Dictionary<string, object> { ["t"] = "c", ["k"] = key, ["id"] = LocalId, ["v"] = value });
        }

        // Čeka konačnu odluku za ključ (npr. "q:5" = pitanje u 5. rundi). Domaćin je donosi kad stignu svi prisutni
        // ili prođe rok; ostali čekaju njegovu poruku. Bez veze odlučujemo sami.
        public IEnumerator Await(string key, IList<string> expected, float timeout, Func<string, Dictionary<string, object>> missing,
            Action<Dictionary<string, Dictionary<string, object>>> done)
        {
            float t = 0;
            while (true)
            {
                if (decided.TryGetValue(key, out var d)) { done(Fill(d, expected, missing)); yield break; }
                contributions.TryGetValue(key, out var got);
                got ??= new Dictionary<string, Dictionary<string, object>>();
                bool allIn = expected.All(id => got.ContainsKey(id) || !Present(id));
                bool alone = Lost || !Connected;
                if (alone && t > 0.5f || IsHost && (allIn || t >= timeout))
                {
                    var res = Fill(got, expected, missing);
                    decided[key] = res;
                    if (!alone) Channel.Broadcast("m", new Dictionary<string, object> { ["t"] = "d", ["k"] = key, ["res"] = res.ToDictionary(kv => kv.Key, kv => (object)kv.Value) });
                    done(res);
                    yield break;
                }
                // domaćin se ne javlja predugo: veza je vjerovatno pukla
                if (!IsHost && t > timeout + 20) { Lost = true; OnProblem?.Invoke("Domaćin se ne javlja. Nastavljaš sam."); }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        static Dictionary<string, Dictionary<string, object>> Fill(Dictionary<string, Dictionary<string, object>> got, IList<string> expected,
            Func<string, Dictionary<string, object>> missing)
        {
            var res = new Dictionary<string, Dictionary<string, object>>();
            foreach (var id in expected) res[id] = got.TryGetValue(id, out var v) ? v : missing(id);
            return res;
        }

        // ---------- brza igra (predvorje) ----------
        public static IEnumerator QuickMatch(Profile p, Transform parent, Action<string> status, Action<MpSession> found, Func<bool> cancelled)
        {
            var lobbyId = MyId();
            var lobby = new RealtimeChannel("trka-predvorje-" + p.Podesavanja.Uzrast, lobbyId);
            double mySince = Now;
            string room = null; List<string> group = null;
            lobby.OnBroadcast += (ev, m) =>
            {
                if (ev != "m" || Json.Str(m, "t") != "match" || room != null) return;
                var g = Json.StrList(Json.Get(m, "group"));
                if (g.Contains(lobbyId)) { group = g; room = Json.Str(m, "room"); }
            };
            lobby.Track(MemberMeta(lobbyId, p, mySince));
            lobby.Connect();
            float t = 0, check = 0;
            while (room == null)
            {
                if (cancelled()) { lobby.Close(); yield break; }
                lobby.Pump();
                if (lobby.Status == RealtimeChannel.State.Failed || lobby.Status == RealtimeChannel.State.Closed)
                {
                    status("Ne mogu da se povežem sa serverom. Provjeri internet.");
                    yield break;
                }
                var waiting = lobby.Presence.Select(kv => new Matchmaker.Waiting { Id = kv.Key, Since = Json.Num(kv.Value, "since") }).ToList();
                status(lobby.Status == RealtimeChannel.State.Joined
                    ? $"Tražim igrače… ({Math.Max(1, waiting.Count)} u čekaonici, {Mathf.FloorToInt(t)} s)"
                    : "Povezujem se…");
                check += Time.unscaledDeltaTime;
                if (check > 1 && Matchmaker.Leader(waiting) == lobbyId)
                {
                    check = 0;
                    var g = Matchmaker.Group(waiting, Now);
                    if (g != null)
                    {
                        var code = Matchmaker.RoomFor(g, mySince);
                        lobby.Broadcast("m", new Dictionary<string, object> { ["t"] = "match", ["room"] = code, ["group"] = g.Cast<object>().ToList() });
                        group = g; room = code;
                    }
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // kratko sačekaj da poruka o grupi stigne do svih, pa izađi iz predvorja
            float w = 0;
            while (w < 0.4f) { lobby.Pump(); w += Time.unscaledDeltaTime; yield return null; }
            lobby.Close();
            var s = Join(room, p, parent, lobbyId);
            s.Group = group;
            found(s);
        }
    }
}
