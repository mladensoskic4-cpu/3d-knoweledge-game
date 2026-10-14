// Profil igrača: čuva se na uređaju (JSON), a kad ima interneta šalje se i na Supabase.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class Look
    {
        public string Zivotinja = "vuk", Vozilo = "auto", Boja = "#e0312f", Kapa = "nema";
        public bool Naocare;
        // ukrasi vozila: šara (i njena boja), nalijepnica i trag koji ostavlja u vožnji
        public string Sara = "nema", Boja2 = "#ffffff", Nalijepnica = "nema", Trag = "nema";
        public Look Clone() => (Look)MemberwiseClone();
    }

    public sealed class GameSettings
    {
        public string Jezik = "";          // "" = po jeziku telefona; lat, cyr, en
        public string Uzrast = "odrasli", Duzina = "10", Nivo = "srednje", Mapa = "cg", Kvalitet = "auto";
        public List<string> Oblasti;       // null = miks
        public bool Miks = true, Muzika = true, Efekti = true, Vibracija = true, UputstvoGotovo;
    }

    public sealed class RaceStats { public int Trke, Pobjede, Tacni, Odgovori; }

    public sealed class RaceRecord
    {
        public string Datum, Mapa;
        public int Mjesto, Bodovi, Tacni, Odgovori;
        public bool Poslato;               // poslato na server (offline igre se šalju kasnije)
    }

    public sealed class Profile
    {
        public string Ime = "", Grad = "", Skola = "";
        public int Novcici = 150, Bodovi;
        public List<string> Kupljeno = new List<string>(Catalog.FreeItems);
        public Dictionary<string, int> Powerupi = new Dictionary<string, int> { ["pola"] = 1, ["zamrzni"] = 1, ["precica"] = 0, ["uspori"] = 0 };
        public List<RaceRecord> Istorija = new List<RaceRecord>();
        public Look Izgled = new Look();
        public GameSettings Podesavanja = new GameSettings();
        public RaceStats Statistika = new RaceStats();
        public List<string> Vidjena = new List<string>();     // id-jevi pitanja iz ranijih trka (da se ne ponavljaju)
        public const int VidjenaMax = 4000;

        public void MarkSeen(string questionId)
        {
            if (string.IsNullOrEmpty(questionId)) return;
            Vidjena.Add(questionId);
            if (Vidjena.Count > VidjenaMax) Vidjena.RemoveRange(0, Vidjena.Count - VidjenaMax);
        }

        public Rank Rank => Catalog.RankOf(Bodovi);

        // ---- kupovina i oprema ----
        public bool Owns(string itemId) => Kupljeno.Contains(itemId);

        public bool CanBuy(ShopItem item) => !Owns(item.Id) && Catalog.RankAllows(item.Rank, Bodovi) && Novcici >= item.Price;

        public bool Buy(ShopItem item)
        {
            if (Owns(item.Id)) return true;
            if (!CanBuy(item)) return false;
            Novcici -= item.Price;
            Kupljeno.Add(item.Id);
            return true;
        }

        public bool IsEquipped(ShopItem item)
        {
            switch (item.Slot)
            {
                case "zivotinja": return Izgled.Zivotinja == item.Value;
                case "vozilo": return Izgled.Vozilo == item.Value;
                case "boja": return Izgled.Boja == item.Value;
                case "kapa": return Izgled.Kapa == item.Value;
                case "naocare": return Izgled.Naocare == (item.Value == "da");
                case "sara": return Izgled.Sara == item.Value;
                case "boja2": return Izgled.Boja2 == item.Value;
                case "nalijepnica": return Izgled.Nalijepnica == item.Value;
                case "trag": return Izgled.Trag == item.Value;
                default: return false;
            }
        }

        public static Look Apply(Look look, ShopItem item)
        {
            var l = look.Clone();
            switch (item.Slot)
            {
                case "zivotinja": l.Zivotinja = item.Value; break;
                case "vozilo": l.Vozilo = item.Value; break;
                case "boja": l.Boja = item.Value; break;
                case "kapa": l.Kapa = item.Value; break;
                case "naocare": l.Naocare = item.Value == "da"; break;
                case "sara": l.Sara = item.Value; break;
                case "boja2": l.Boja2 = item.Value; break;
                case "nalijepnica": l.Nalijepnica = item.Value; break;
                case "trag": l.Trag = item.Value; break;
            }
            return l;
        }

        public bool Equip(ShopItem item)
        {
            if (!Owns(item.Id)) return false;
            Izgled = Apply(Izgled, item);
            return true;
        }

        public bool BuyPowerup(string id)
        {
            var pu = Catalog.Powerups.FirstOrDefault(p => p.Id == id);
            if (pu == null || Novcici < pu.Price) return false;
            Novcici -= pu.Price;
            Powerupi[id] = (Powerupi.TryGetValue(id, out var n) ? n : 0) + 1;
            return true;
        }

        public void UsePowerup(string id)
        {
            if (Powerupi.TryGetValue(id, out var n) && n > 0) Powerupi[id] = n - 1;
        }

        // ---- nagrade poslije trke ----
        public sealed class Award { public int Pts, Coins; public Rank RankUp; public List<RaceMap> NewMaps = new List<RaceMap>(); }

        public Award AwardRace(int place, int correct, int answered, int raceCoins, string mapa, DateTime? now = null)
        {
            place = Mathx.Clamp(place, 0, 3);
            var before = Catalog.RankOf(Bodovi);
            var unlockedBefore = Catalog.Maps.Where(m => Catalog.RankAllows(m.Rank, Bodovi)).Select(m => m.Id).ToList();
            int pts = new[] { 100, 60, 35, 15 }[place] + correct * 4;
            int placeCoins = new[] { 60, 40, 25, 10 }[place];
            Bodovi += pts;
            Novcici += raceCoins + placeCoins;
            Statistika.Trke++;
            if (place == 0) Statistika.Pobjede++;
            Statistika.Tacni += correct;
            Statistika.Odgovori += answered;
            Istorija.Insert(0, new RaceRecord
            {
                Datum = (now ?? DateTime.UtcNow).ToString("o"), Mapa = mapa, Mjesto = place + 1, Bodovi = pts, Tacni = correct, Odgovori = answered
            });
            if (Istorija.Count > 50) Istorija.RemoveRange(50, Istorija.Count - 50);
            var after = Catalog.RankOf(Bodovi);
            var award = new Award { Pts = pts, Coins = raceCoins + placeCoins, RankUp = after.Id != before.Id ? after : null };
            award.NewMaps.AddRange(Catalog.Maps.Where(m => Catalog.RankAllows(m.Rank, Bodovi) && !unlockedBefore.Contains(m.Id)));
            return award;
        }

        public bool MapUnlocked(RaceMap m) => Catalog.RankAllows(m.Rank, Bodovi);

        // ---- JSON ----
        public string ToJson()
        {
            var d = new Dictionary<string, object>
            {
                ["verzija"] = 1.0,
                ["ime"] = Ime, ["grad"] = Grad, ["skola"] = Skola,
                ["novcici"] = (double)Novcici, ["bodovi"] = (double)Bodovi,
                ["kupljeno"] = Kupljeno.Cast<object>().ToList(),
                ["powerupi"] = Powerupi.ToDictionary(k => k.Key, k => (object)(double)k.Value),
                ["izgled"] = LookToDict(Izgled),
                ["podesavanja"] = new Dictionary<string, object>
                {
                    ["jezik"] = Podesavanja.Jezik, ["uzrast"] = Podesavanja.Uzrast, ["duzina"] = Podesavanja.Duzina,
                    ["nivo"] = Podesavanja.Nivo, ["mapa"] = Podesavanja.Mapa, ["kvalitet"] = Podesavanja.Kvalitet,
                    ["oblasti"] = Podesavanja.Oblasti?.Cast<object>().ToList(), ["miks"] = Podesavanja.Miks,
                    ["muzika"] = Podesavanja.Muzika, ["efekti"] = Podesavanja.Efekti, ["vibracija"] = Podesavanja.Vibracija,
                    ["uputstvo"] = Podesavanja.UputstvoGotovo,
                },
                ["statistika"] = new Dictionary<string, object>
                {
                    ["trke"] = (double)Statistika.Trke, ["pobjede"] = (double)Statistika.Pobjede,
                    ["tacni"] = (double)Statistika.Tacni, ["odgovori"] = (double)Statistika.Odgovori,
                },
                ["vidjena"] = Vidjena.Cast<object>().ToList(),
                ["istorija"] = Istorija.Select(r => (object)new Dictionary<string, object>
                {
                    ["datum"] = r.Datum, ["mapa"] = r.Mapa, ["mjesto"] = (double)r.Mjesto, ["bodovi"] = (double)r.Bodovi,
                    ["tacni"] = (double)r.Tacni, ["odgovori"] = (double)r.Odgovori, ["poslato"] = r.Poslato,
                }).ToList(),
            };
            return Json.Write(d);
        }

        public static Dictionary<string, object> LookToDict(Look l) => new Dictionary<string, object>
        {
            ["zivotinja"] = l.Zivotinja, ["vozilo"] = l.Vozilo, ["boja"] = l.Boja, ["kapa"] = l.Kapa, ["naocare"] = l.Naocare,
            ["sara"] = l.Sara, ["boja2"] = l.Boja2, ["nalijepnica"] = l.Nalijepnica, ["trag"] = l.Trag,
        };

        // Nepoznata ili pokvarena polja se preskaču, pa stari profil uvijek može da se učita.
        public static Profile FromJson(string json)
        {
            var p = new Profile();
            if (string.IsNullOrWhiteSpace(json)) return p;
            Dictionary<string, object> d;
            try { d = Json.Obj(Json.Parse(json)); } catch (FormatException) { return p; }
            if (d == null) return p;
            p.Ime = Json.Str(d, "ime", "");
            p.Grad = Json.Str(d, "grad", "");
            p.Skola = Json.Str(d, "skola", "");
            p.Novcici = Math.Max(0, Json.Int(d, "novcici", p.Novcici));
            p.Vidjena = Json.StrList(Json.Get(d, "vidjena"));
            if (p.Vidjena.Count > VidjenaMax) p.Vidjena.RemoveRange(0, p.Vidjena.Count - VidjenaMax);
            p.Bodovi = Math.Max(0, Json.Int(d, "bodovi", 0));
            var kup = Json.StrList(Json.Get(d, "kupljeno"));
            p.Kupljeno = Catalog.FreeItems.Union(kup.Where(id => Catalog.ShopItemOf(id) != null)).ToList();
            if (Json.Obj(Json.Get(d, "powerupi")) is Dictionary<string, object> pu)
                foreach (var kv in pu) if (kv.Value is double n) p.Powerupi[kv.Key] = Math.Max(0, (int)n);

            if (Json.Obj(Json.Get(d, "izgled")) is Dictionary<string, object> iz)
            {
                p.Izgled.Zivotinja = Json.Str(iz, "zivotinja", p.Izgled.Zivotinja);
                p.Izgled.Vozilo = Json.Str(iz, "vozilo", p.Izgled.Vozilo);
                p.Izgled.Boja = Json.Str(iz, "boja", p.Izgled.Boja);
                p.Izgled.Kapa = Json.Str(iz, "kapa", p.Izgled.Kapa);
                p.Izgled.Naocare = Json.Bool(iz, "naocare");
                p.Izgled.Sara = Json.Str(iz, "sara", p.Izgled.Sara);
                p.Izgled.Boja2 = Json.Str(iz, "boja2", p.Izgled.Boja2);
                p.Izgled.Nalijepnica = Json.Str(iz, "nalijepnica", p.Izgled.Nalijepnica);
                p.Izgled.Trag = Json.Str(iz, "trag", p.Izgled.Trag);
            }
            // oprema mora biti kupljena (zaštita od ručno izmijenjenog fajla)
            if (!p.Owns("z-" + p.Izgled.Zivotinja)) p.Izgled.Zivotinja = "vuk";
            if (!p.Owns("v-" + p.Izgled.Vozilo)) p.Izgled.Vozilo = "auto";
            if (!p.Owns("b-" + p.Izgled.Boja)) p.Izgled.Boja = "#e0312f";
            if (!p.Owns("h-" + p.Izgled.Kapa)) p.Izgled.Kapa = "nema";
            if (p.Izgled.Naocare && !p.Owns("n-da")) p.Izgled.Naocare = false;
            if (!p.Owns("s-" + p.Izgled.Sara)) p.Izgled.Sara = "nema";
            if (!p.Owns("c-" + p.Izgled.Boja2)) p.Izgled.Boja2 = "#ffffff";
            if (!p.Owns("l-" + p.Izgled.Nalijepnica)) p.Izgled.Nalijepnica = "nema";
            if (!p.Owns("t-" + p.Izgled.Trag)) p.Izgled.Trag = "nema";

            if (Json.Obj(Json.Get(d, "podesavanja")) is Dictionary<string, object> s)
            {
                var st = p.Podesavanja;
                st.Jezik = Json.Str(s, "jezik", st.Jezik);
                st.Uzrast = Catalog.Ages.Any(a => a.Id == Json.Str(s, "uzrast")) ? Json.Str(s, "uzrast") : "odrasli";
                st.Duzina = Catalog.Lengths.Any(l => l.Id == Json.Str(s, "duzina")) ? Json.Str(s, "duzina") : "10";
                st.Nivo = Bots.Levels.ContainsKey(Json.Str(s, "nivo", "")) ? Json.Str(s, "nivo") : "srednje";
                st.Mapa = Json.Str(s, "mapa", st.Mapa);
                st.Kvalitet = Json.Str(s, "kvalitet", st.Kvalitet);
                st.Oblasti = Json.Get(s, "oblasti") is List<object> ob ? Json.StrList(ob) : null;
                st.Miks = Json.Bool(s, "miks", true);
                st.Muzika = Json.Bool(s, "muzika", true);
                st.Efekti = Json.Bool(s, "efekti", true);
                st.Vibracija = Json.Bool(s, "vibracija", true);
                st.UputstvoGotovo = Json.Bool(s, "uputstvo");
            }
            if (Json.Obj(Json.Get(d, "statistika")) is Dictionary<string, object> stt)
            {
                p.Statistika.Trke = Json.Int(stt, "trke"); p.Statistika.Pobjede = Json.Int(stt, "pobjede");
                p.Statistika.Tacni = Json.Int(stt, "tacni"); p.Statistika.Odgovori = Json.Int(stt, "odgovori");
            }
            foreach (var o in Json.Arr(Json.Get(d, "istorija")) ?? new List<object>())
                if (o is Dictionary<string, object> r)
                    p.Istorija.Add(new RaceRecord
                    {
                        Datum = Json.Str(r, "datum"), Mapa = Json.Str(r, "mapa"), Mjesto = Json.Int(r, "mjesto"), Bodovi = Json.Int(r, "bodovi"),
                        Tacni = Json.Int(r, "tacni"), Odgovori = Json.Int(r, "odgovori"), Poslato = Json.Bool(r, "poslato"),
                    });
            return p;
        }
    }
}
