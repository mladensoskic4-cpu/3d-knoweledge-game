// Veza sa Supabase-om (ista baza kao web verzija): prijava kao gost, profil, rezultati trka i odobrena pitanja.
// Bez interneta igra radi normalno; rezultati se čuvaju i šalju kad se veza vrati.
// U kodu je samo javni (publishable) ključ. Service role ključ NIKAD ne ide u igru.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace TrkaZnanja.Game
{
    public static class Net
    {
        public const string Url = "https://soyaqpuronnhxcbwhnwt.supabase.co";
        public const string AnonKey = "sb_publishable_RoIgUlgm6tyatqSPQOa-Og_ZBVsGUYz";

        const string KeyAccess = "trka-sb-access", KeyRefresh = "trka-sb-refresh", KeyUser = "trka-sb-user", KeyExpires = "trka-sb-expires";
        static string access, refresh, userId;
        static long expires;
        public static bool Online => !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(access);
        public static string UserId => userId;
        public static string AccessToken => access;

        static UnityWebRequest Request(string method, string path, string body, bool auth)
        {
            var req = new UnityWebRequest(Url + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 12 };
            if (body != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)) { contentType = "application/json" };
            req.SetRequestHeader("apikey", AnonKey);
            req.SetRequestHeader("Content-Type", "application/json");
            if (auth && !string.IsNullOrEmpty(access)) req.SetRequestHeader("Authorization", "Bearer " + access);
            return req;
        }

        static bool Ok(UnityWebRequest r) => r.result == UnityWebRequest.Result.Success && r.responseCode >= 200 && r.responseCode < 300;

        static void SaveSession(string json)
        {
            var d = Json.Obj(Json.Parse(json));
            access = Json.Str(d, "access_token");
            refresh = Json.Str(d, "refresh_token");
            userId = Json.Str(Json.Obj(Json.Get(d, "user")), "id");
            expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)Json.Num(d, "expires_in", 3600) - 60;
            PlayerPrefs.SetString(KeyAccess, access ?? "");
            PlayerPrefs.SetString(KeyRefresh, refresh ?? "");
            PlayerPrefs.SetString(KeyUser, userId ?? "");
            PlayerPrefs.SetString(KeyExpires, expires.ToString());
            PlayerPrefs.Save();
        }

        // Prijava: postojeća sesija → osvježi token; inače novi gost (anonimna prijava).
        public static IEnumerator Init(Action<bool> done)
        {
            access = PlayerPrefs.GetString(KeyAccess, "");
            refresh = PlayerPrefs.GetString(KeyRefresh, "");
            userId = PlayerPrefs.GetString(KeyUser, "");
            long.TryParse(PlayerPrefs.GetString(KeyExpires, "0"), out expires);

            if (!string.IsNullOrEmpty(refresh) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
            {
                using (var r = Request("POST", "/auth/v1/token?grant_type=refresh_token", Json.Write(new Dictionary<string, object> { ["refresh_token"] = refresh }), false))
                {
                    yield return r.SendWebRequest();
                    if (Ok(r)) SaveSession(r.downloadHandler.text);
                    else { access = null; userId = null; }
                }
            }
            if (!Online)
            {
                using (var r = Request("POST", "/auth/v1/signup", "{\"data\":{}}", false))
                {
                    yield return r.SendWebRequest();
                    if (Ok(r)) SaveSession(r.downloadHandler.text);
                }
            }
            done?.Invoke(Online);
        }

        public static IEnumerator PushProfile(Profile p)
        {
            if (!Online) yield break;
            var row = new Dictionary<string, object>
            {
                ["id"] = userId, ["ime"] = string.IsNullOrEmpty(p.Ime) ? "Igrač" : p.Ime,
                ["grad"] = string.IsNullOrEmpty(p.Grad) ? null : p.Grad, ["skola"] = string.IsNullOrEmpty(p.Skola) ? null : p.Skola,
                ["bodovi"] = (double)p.Bodovi, ["novcici"] = (double)p.Novcici, ["izgled"] = Profile.LookToDict(p.Izgled),
                ["kupljeno"] = p.Kupljeno.Cast<object>().ToList(),
                ["powerupi"] = p.Powerupi.ToDictionary(k => k.Key, k => (object)(double)k.Value),
                ["uzrast"] = p.Podesavanja.Uzrast, ["updated_at"] = DateTime.UtcNow.ToString("o"),
            };
            using (var r = Request("POST", "/rest/v1/profiles", Json.Write(row), true))
            {
                r.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");
                yield return r.SendWebRequest();
            }
        }

        // Šalje sve trke koje još nisu poslate (igrane bez interneta).
        public static IEnumerator SendPendingResults(Profile p, Action changed)
        {
            if (!Online) yield break;
            bool any = false;
            foreach (var rec in p.Istorija.Where(x => !x.Poslato).Reverse().ToList())
            {
                var row = new Dictionary<string, object>
                {
                    ["user_id"] = userId, ["mapa"] = rec.Mapa, ["mjesto"] = (double)rec.Mjesto,
                    ["bodovi"] = (double)rec.Bodovi, ["tacni"] = (double)rec.Tacni, ["odgovori"] = (double)rec.Odgovori,
                };
                using (var r = Request("POST", "/rest/v1/rezultati", Json.Write(row), true))
                {
                    r.SetRequestHeader("Prefer", "return=minimal");
                    yield return r.SendWebRequest();
                    if (!Ok(r)) break;
                    rec.Poslato = true;
                    any = true;
                }
            }
            if (any) changed?.Invoke();
        }

        public static IEnumerator ApprovedQuestions(Action<List<Question>> done)
        {
            var list = new List<Question>();
            if (Online)
                using (var r = Request("GET", "/rest/v1/odobrena_pitanja?select=*&limit=1000", null, true))
                {
                    yield return r.SendWebRequest();
                    if (Ok(r))
                        foreach (var o in Json.Arr(Json.Parse(r.downloadHandler.text)) ?? new List<object>())
                        {
                            var d = Json.Obj(o);
                            var q = Question.FromJson(new Dictionary<string, object>
                            {
                                ["id"] = "igr-" + Json.Str(d, "id"), ["oblast"] = Json.Get(d, "oblast"), ["tip"] = Json.Get(d, "tip"),
                                ["tezina"] = 2.0, ["pitanje"] = Json.Get(d, "pitanje"), ["opcije"] = Json.Get(d, "opcije"),
                                ["tacno"] = Json.Get(d, "tacno"), ["info"] = "Pitanje je predložio igrač.",
                            });
                            if (q.IsValid() && QuizTypes.Get(q.Tip) != null) list.Add(q);
                        }
                }
            done?.Invoke(list);
        }
    }

    // Čuvanje profila na uređaju (JSON u persistentDataPath).
    public static class ProfileStore
    {
        public static bool Test;
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, Test ? "profil-test.json" : "profil.json");

        public static Profile Load()
        {
            if (Test) return new Profile();
            try
            {
                if (System.IO.File.Exists(Path)) return Profile.FromJson(System.IO.File.ReadAllText(Path));
            }
            catch (Exception e) { Debug.LogWarning("Profil se nije učitao: " + e.Message); }
            return new Profile();
        }

        public static void Save(Profile p)
        {
            try
            {
                var tmp = Path + ".tmp";
                System.IO.File.WriteAllText(tmp, p.ToJson());
                if (System.IO.File.Exists(Path)) System.IO.File.Delete(Path);
                System.IO.File.Move(tmp, Path);
            }
            catch (Exception e) { Debug.LogWarning("Profil se nije sačuvao: " + e.Message); }
        }
    }
}
