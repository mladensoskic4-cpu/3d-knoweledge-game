// Botovi: nasumična imena (liče na prave igrače), profil znanja po oblasti, brzina i tri nivoa težine.
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrkaZnanja.Core
{
    public sealed class BotLevel { public string Id, Label; public double Acc, Time; }

    public sealed class BotProfile
    {
        public string Archetype, Tagline;
        public double Speed;                 // prosječni udio vremena koji potroši na odgovor (manje = brže)
        public Dictionary<string, double> Skills;
    }

    public sealed class Racer
    {
        public string Id, Name, Color;
        public bool Human;
        public Look Look;
        public BotProfile Bot;
        public string Level = "srednje";
        // stanje u trci
        public int Pos, Coins, LastCheckpoint, Correct, Answered;
        public int? FinishedRound;
        public double LastScore;
    }

    public static class Bots
    {
        public static readonly Dictionary<string, BotLevel> Levels = new Dictionary<string, BotLevel>
        {
            ["lako"] = new BotLevel { Id = "lako", Label = "Lako", Acc = 0.72, Time = 1.25 },
            ["srednje"] = new BotLevel { Id = "srednje", Label = "Srednje", Acc = 0.9, Time = 1.0 },
            ["tesko"] = new BotLevel { Id = "tesko", Label = "Teško", Acc = 1.08, Time = 0.8 },
        };

        public static readonly BotProfile[] Archetypes =
        {
            new BotProfile { Archetype = "brzi", Tagline = "Brz na obaraču", Speed = 0.42, Skills = new Dictionary<string, double>
                { ["geografija"] = 0.6, ["istorija"] = 0.4, ["sport"] = 0.88, ["muzika"] = 0.65, ["priroda"] = 0.5, ["hrana"] = 0.6, ["logika"] = 0.5, ["film"] = 0.75, ["nauka"] = 0.5, ["jezici"] = 0.55, ["tehnologija"] = 0.85 } },
            new BotProfile { Archetype = "putnik", Tagline = "Zna svaku uvalu", Speed = 0.58, Skills = new Dictionary<string, double>
                { ["geografija"] = 0.88, ["istorija"] = 0.65, ["sport"] = 0.4, ["muzika"] = 0.55, ["priroda"] = 0.8, ["hrana"] = 0.7, ["logika"] = 0.6, ["film"] = 0.5, ["nauka"] = 0.7, ["jezici"] = 0.85, ["tehnologija"] = 0.5 } },
            new BotProfile { Archetype = "temeljni", Tagline = "Spor, ali temeljan", Speed = 0.7, Skills = new Dictionary<string, double>
                { ["geografija"] = 0.6, ["istorija"] = 0.9, ["sport"] = 0.62, ["muzika"] = 0.4, ["priroda"] = 0.55, ["hrana"] = 0.65, ["logika"] = 0.85, ["film"] = 0.6, ["nauka"] = 0.88, ["jezici"] = 0.6, ["tehnologija"] = 0.65 } },
        };

        public static readonly string[] BotColors = { "#2b7de9", "#16a36a", "#f08a1c", "#9b4bd6", "#e3b23c", "#0f8a8a" };

        static readonly string[] FirstNames =
        {
            "Marko", "Nikola", "Luka", "Stefan", "Filip", "Vuk", "Andrej", "Lazar", "Petar", "Danilo", "Matija", "Ognjen", "Uroš", "Aleksa",
            "Milica", "Jelena", "Ana", "Sara", "Teodora", "Jovana", "Marija", "Tamara", "Nina", "Ivana", "Mina", "Lana", "Dunja", "Sofija",
            "Amar", "Emir", "Lejla", "Amila", "Ilma", "Tarik", "Elma", "Dino", "Iva", "Ena", "Mia", "Toni", "Bojan", "Dejan", "Goran"
        };
        static readonly string[] Tags = { "pro", "brzi", "cg", "ns", "bg", "pg", "kviz", "gamer", "ace", "zmaj", "vuk", "lav", "sova" };

        // Ime kao kod pravih igrača: "Marko", "jelena.k", "Vuk_07", "milica99", "Luka cg"…
        public static string RandomName(Rng rng, ICollection<string> taken = null)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var first = rng.Pick(FirstNames);
                string name;
                switch (rng.Range(0, 6))
                {
                    case 0: name = first; break;
                    case 1: name = first.ToLowerInvariant() + "." + char.ToLowerInvariant(rng.Pick(FirstNames)[0]); break;
                    case 2: name = first + "_" + rng.Range(1, 99).ToString("00"); break;
                    case 3: name = first.ToLowerInvariant() + rng.Range(1, 100); break;
                    case 4: name = first + " " + rng.Pick(Tags); break;
                    default: name = rng.Pick(Tags) + "_" + first.ToLowerInvariant(); break;
                }
                if (name.Length > 14) name = name.Substring(0, 14);
                if (taken == null || !taken.Contains(name)) return name;
            }
            return "Igrač" + rng.Range(10, 99);
        }

        public static List<Racer> Create(string level, Rng rng, int count = 3, string avoidAnimal = null)
        {
            var taken = new HashSet<string>();
            var animals = Catalog.Animals.Select(a => a.Id).Where(a => a != avoidAnimal).ToList();
            rng.Shuffle(animals);
            var archetypes = rng.Shuffle(Archetypes.ToList());
            var list = new List<Racer>();
            for (int i = 0; i < count; i++)
            {
                var name = RandomName(rng, taken);
                taken.Add(name);
                var vehicle = rng.Pick(Catalog.Vehicles.Where(v => !v.Flies).ToList());
                var color = BotColors[i % BotColors.Length];
                list.Add(new Racer
                {
                    Id = "bot" + i, Name = name, Color = color, Level = Levels.ContainsKey(level) ? level : "srednje",
                    Bot = archetypes[i % archetypes.Count],
                    Look = new Look { Zivotinja = animals[i % animals.Count], Vozilo = vehicle.Id, Boja = color, Kapa = rng.Chance(0.3) ? rng.Pick(new[] { "kacket", "sesir", "kapa" }) : "nema", Naocare = rng.Chance(0.2) },
                });
            }
            return list;
        }

        public static (string strong, string weak) StrongWeak(BotProfile bot, IList<string> areas)
        {
            var list = areas.Select(a => (a, v: bot.Skills.TryGetValue(a, out var s) ? s : 0.5)).OrderByDescending(x => x.v).ToList();
            return (list[0].a, list[list.Count - 1].a);
        }

        public static double Skill(Racer bot, string area, int tezina = 2)
        {
            var b = bot.Bot.Skills.TryGetValue(area ?? "", out var s) ? s : 0.5;
            return Mathx.Clamp(b * Levels[bot.Level].Acc - (tezina - 2) * 0.07, 0.05, 0.96);
        }

        public static (double accuracy, double time, bool timedOut) Answer(Racer bot, Question q, double limit, Rng rng)
        {
            var p = Skill(bot, q.Oblast, q.Tezina);
            var time = limit * bot.Bot.Speed * Levels[bot.Level].Time * (0.6 + rng.Next() * 0.8);
            time = Mathx.Clamp(time, 1.2, limit * 1.05);
            if (time >= limit) return (0, limit, true);
            return (QuizTypes.Get(q.Tip).SimulateBot(q, p, rng), time, false);
        }

        public static double MinigameSkill(Racer bot)
        {
            var avg = bot.Bot.Skills.Values.Average();
            return Mathx.Clamp(avg * Levels[bot.Level].Acc, 0.05, 0.95);
        }
    }
}
