// Testovi logike igre (bez grafike). Vrte se u Unity-ju (Test Runner → EditMode) i u običnom .NET-u (tools/unity-core-tests).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TrkaZnanja.Core;

namespace TrkaZnanja.Tests
{
    static class TestData
    {
        static string root;
        public static string Dir
        {
            get
            {
                if (root != null) return root;
                var env = Environment.GetEnvironmentVariable("TRKA_DATA");
                if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return root = env;
                var d = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (d != null)
                {
                    var p = Path.Combine(d.FullName, "Assets", "TrkaZnanja", "Resources", "Data");
                    if (Directory.Exists(p)) return root = p;
                    p = Path.Combine(d.FullName, "unity", "Assets", "TrkaZnanja", "Resources", "Data");
                    if (Directory.Exists(p)) return root = p;
                    d = d.Parent;
                }
                throw new DirectoryNotFoundException("Ne nalazim Resources/Data");
            }
        }
        public static string Read(string name) => File.ReadAllText(Path.Combine(Dir, name));

        static List<Question> questions;
        public static List<Question> Questions => questions ??= QuestionBank.Parse(Read("pitanja.json"));
    }

    public class JsonTests
    {
        [Test]
        public void ParsesAndWritesRoundTrip()
        {
            var src = "{\"a\":1,\"b\":[true,false,null],\"c\":\"Đurđevića \\\"Tara\\\"\\n\",\"d\":{\"e\":-2.5e1}}";
            var o = Json.Obj(Json.Parse(src));
            Assert.AreEqual(1.0, Json.Num(o, "a"));
            Assert.AreEqual(3, Json.Arr(o["b"]).Count);
            Assert.AreEqual("Đurđevića \"Tara\"\n", Json.Str(o, "c"));
            Assert.AreEqual(-25.0, Json.Num(Json.Obj(o["d"]), "e"));
            var again = Json.Obj(Json.Parse(Json.Write(o)));
            Assert.AreEqual(Json.Str(o, "c"), Json.Str(again, "c"));
        }

        [Test]
        public void RejectsBrokenJson()
        {
            Assert.Throws<FormatException>(() => Json.Parse("{\"a\":1,"));
            Assert.Throws<FormatException>(() => Json.Parse("[1 2]"));
        }
    }

    public class QuestionTests
    {
        [Test]
        public void AllQuestionsLoadAndAreValid()
        {
            var q = TestData.Questions;
            Assert.Greater(q.Count, 5000, "očekivano preko 5000 pitanja");
            foreach (var x in q) Assert.IsTrue(x.IsValid(), x.ToString());
            var types = q.Select(x => x.Tip).Distinct().ToList();
            foreach (var t in new[] { "izbor", "spoji", "poredaj", "procjena", "mutna", "mapa", "melodija" })
                Assert.IsTrue(types.Contains(t), "nedostaje tip " + t);
        }

        [Test]
        public void EveryAreaHasEnoughKidsQuestions()
        {
            foreach (var area in TestData.Questions.Select(x => x.Oblast).Distinct())
            {
                var kids = TestData.Questions.Count(x => x.Oblast == area && x.Tezina == 1);
                Assert.GreaterOrEqual(kids, 20, "malo dječjih pitanja u oblasti " + area);
            }
        }

        [Test]
        public void TypeSpecificFieldsParse()
        {
            var est = TestData.Questions.First(x => x.Tip == "procjena");
            Assert.Greater(est.Max, est.Min);
            Assert.Greater(est.Tolerancija, 0);
            var map = TestData.Questions.First(x => x.Tip == "mapa");
            Assert.IsNotNull(map.Tacka);
            var match = TestData.Questions.First(x => x.Tip == "spoji");
            Assert.GreaterOrEqual(match.Parovi.Count, 2);
            var mel = TestData.Questions.First(x => x.Tip == "melodija");
            Assert.Greater(Synth.ParseMelody(mel.Note).Count, 3);
        }
    }

    public class ScoringTests
    {
        [Test]
        public void TilesForMatchesWebVersion()
        {
            Assert.AreEqual(0, QuizTypes.TilesFor(0.1, 1, 15));
            Assert.AreEqual(4, QuizTypes.TilesFor(1, 0, 15));
            Assert.AreEqual(2, QuizTypes.TilesFor(1, 15, 15));
            Assert.AreEqual(1, QuizTypes.TilesFor(0.25, 10, 15));
        }

        [Test]
        public void OrderScore()
        {
            var c = new List<string> { "a", "b", "c", "d" };
            Assert.AreEqual(1.0, QuizTypes.ScoreOrder(c, c));
            Assert.AreEqual(0.0, QuizTypes.ScoreOrder(new List<string> { "d", "c", "b", "a" }, c));
            Assert.AreEqual(2.0 / 3, QuizTypes.ScoreOrder(new List<string> { "b", "a", "c", "d" }, c), 1e-9);
        }

        [Test]
        public void EstimateScoreAndSnap()
        {
            var q = new Question { Tip = "procjena", Min = 0, Max = 100, Korak = 5, TacnoBroj = 50, Tolerancija = 20 };
            Assert.AreEqual(1.0, QuizTypes.ScoreEstimate(q, 50));
            Assert.AreEqual(0.5, QuizTypes.ScoreEstimate(q, 60), 1e-9);
            Assert.AreEqual(0.0, QuizTypes.ScoreEstimate(q, 99));
            Assert.AreEqual(55.0, QuizTypes.SnapEstimate(q, 53.2));
            Assert.AreEqual(100.0, QuizTypes.SnapEstimate(q, 140));
        }

        [Test]
        public void MapScoreUsesDistance()
        {
            var q = new Question { Tip = "mapa", Mapa = "cg", Tacka = new MapPoint { Lon = 19.123, Lat = 43.154 } };
            Assert.AreEqual(1.0, QuizTypes.ScoreMap(q, 3));
            Assert.AreEqual(0.0, QuizTypes.ScoreMap(q, 60));
            var km = Geo.Haversine(19.262, 42.441, 19.123, 43.154);   // Podgorica – Žabljak
            Assert.That(km, Is.InRange(75.0, 85.0));
        }

        [Test]
        public void MatchScore()
        {
            var q = new Question { Tip = "spoji" };
            q.Parovi.Add(new KeyValuePair<string, string>("a", "1"));
            q.Parovi.Add(new KeyValuePair<string, string>("b", "2"));
            var (acc, good) = QuizTypes.ScoreMatch(q, new Dictionary<string, string> { ["a"] = "1", ["b"] = "1" });
            Assert.AreEqual(0.5, acc);
            Assert.AreEqual(1, good);
        }

        [Test]
        public void EndLabelsPickEarliestWord()
        {
            CollectionAssert.AreEqual(new[] { "najveći", "najmanji" }, QuizTypes.EndLabels("Poredaj od najvećeg do najmanjeg.", false));
            CollectionAssert.AreEqual(new[] { "najstariji", "najnoviji" }, QuizTypes.EndLabels("Poredaj filmove po godini izlaska, od najstarijeg.", false));
            CollectionAssert.AreEqual(new[] { "first", "last" }, QuizTypes.EndLabels("Nešto bez ključne riječi", true));
        }
    }

    public class DeckTests
    {
        [Test]
        public void MultipleChoiceNeverAboveFortyPercent()
        {
            var areas = TestData.Questions.Select(q => q.Oblast).Distinct().ToList();
            var deck = new Deck(TestData.Questions, areas, new Rng(1));
            int mc = 0;
            var seen = new HashSet<string>();
            for (int i = 1; i <= 200; i++)
            {
                var q = deck.Next();
                if (q.Tip == "izbor") mc++;
                Assert.LessOrEqual(mc, 0.4 * i + 1);
                seen.Add(q.Id);
            }
            Assert.AreEqual(200, seen.Count, "pitanja se ne smiju ponavljati u jednoj trci");
        }

        [Test]
        public void KidsGetOnlyEasyQuestions()
        {
            var deck = new Deck(TestData.Questions, new[] { "geografija", "priroda" }, new Rng(2), maxTezina: 1);
            for (int i = 0; i < 80; i++) Assert.AreEqual(1, deck.Next().Tezina);
        }

        [Test]
        public void SameSeedSameDeck()
        {
            var areas = new[] { "sport", "film" };
            var a = new Deck(TestData.Questions, areas, new Rng(5));
            var b = new Deck(TestData.Questions, areas, new Rng(5));
            for (int i = 0; i < 30; i++) Assert.AreEqual(a.Next().Id, b.Next().Id);
        }
    }

    public class RaceTests
    {
        static Race NewRace(int seed, int tiles = 32)
        {
            var rng = new Rng(seed);
            var human = new Racer { Id = "ti", Name = "Ti", Human = true, Look = new Look() };
            return new Race(new[] { human }.Concat(Bots.Create("srednje", rng)), tiles, rng, TileKinds.Generate(tiles, seed));
        }

        [Test]
        public void TileKindsAreDeterministicAndSpaced()
        {
            foreach (var n in new[] { 15, 32, 58, 84 })
            {
                var k = TileKinds.Generate(n, 123);
                CollectionAssert.AreEqual(k, TileKinds.Generate(n, 123), "isto seme = ista staza");
                Assert.AreEqual(TileKinds.Start, k[0]); Assert.AreEqual(TileKinds.Finish, k[n - 1]);
                for (int i = 1; i < n - 1; i++)
                    if (k[i] != TileKinds.Plain) Assert.AreEqual(TileKinds.Plain, k[i - 1], "posebna polja nisu jedno do drugog");
                if (n >= 32)
                {
                    Assert.GreaterOrEqual(k.Count(x => x == TileKinds.Box), 3, "ima kutija");
                    Assert.Greater(k.Count(x => x != TileKinds.Plain), n / 6);
                }
            }
            Assert.IsFalse(TileKinds.Generate(58, 1).SequenceEqual(TileKinds.Generate(58, 2)));
        }

        static Race FixedRace(params string[] kinds)
        {
            var list = Enumerable.Repeat(TileKinds.Plain, 20).ToList();
            list[0] = TileKinds.Start; list[19] = TileKinds.Finish;
            for (int i = 0; i + 1 < kinds.Length; i += 2) list[int.Parse(kinds[i])] = kinds[i + 1];
            var rng = new Rng(9);
            var players = new List<Racer> { new Racer { Id = "ti", Name = "Ti", Human = true, Look = new Look() } };
            players.AddRange(Bots.Create("srednje", rng));
            return new Race(players, 20, rng, list);
        }

        [Test]
        public void LandingTilesWork()
        {
            var r = FixedRace("5", TileKinds.Shortcut, "8", TileKinds.Mud, "11", TileKinds.Double, "14", TileKinds.Box);
            var me = r.Human;
            var m = r.AdvanceTarget(me, 5); r.Apply(new[] { m });
            var extra = r.Landings(new[] { m });
            Assert.AreEqual(7, extra[0].Target, "prečica +2");
            r.Apply(extra);
            m = r.AdvanceTarget(me, 1); r.Apply(new[] { m });
            extra = r.Landings(new[] { m });
            Assert.AreEqual(7, extra[0].Target, "blato −1");
            r.Apply(extra);
            m = r.AdvanceTarget(me, 4); r.Apply(new[] { m });
            Assert.AreEqual(0, r.Landings(new[] { m }).Count);
            Assert.IsTrue(me.DoubleNext, "dupla polja");
            m = r.AdvanceTarget(me, 3); r.Apply(new[] { m });
            r.Landings(new[] { m });
            Assert.IsTrue(Items.All.Contains(me.Item), "kutija daje predmet");
            Assert.AreEqual(0, r.Landings(new[] { new Move { Racer = me, From = 14, Target = 14 } }).Count, "ko stoji, ne aktivira polje");
        }

        [Test]
        public void AttacksShieldAndDoubleTiles()
        {
            var r = FixedRace();
            var me = r.Human;
            var a = r.Players[1]; var b = r.Players[2]; var c = r.Players[3];
            me.Pos = 3; a.Pos = 6; b.Pos = 9; c.Pos = 1;
            Assert.AreSame(a, r.TargetFor(me), "meta je najbliži ispred");
            Assert.AreSame(a, r.TargetFor(b), "prvi napada onoga odmah iza");
            var q = TestData.Questions.First(x => x.Tip == "izbor");
            r.NextRound();
            me.Item = Items.Back;
            var ans = new MpAnswer { Acc = 1, Time = 3, Tiles = 3, Text = "ok", Powerups = new List<string> { Items.UsePrefix + Items.Back } };
            var entries = r.ResolveRound(q, new Dictionary<string, MpAnswer> { ["ti"] = ans });
            Assert.IsNull(me.Item, "napad je potrošen");
            var ea = entries.First(e => e.Racer == a);
            Assert.IsTrue(ea.Text.Contains("napad"), ea.Text);
            Assert.IsTrue(r.Events.Any(e => e.Kind == "napad" && e.Target == a));

            // štit brani
            r.NextRound();
            me.Item = Items.Back; a.Item = Items.Shield;
            r.ResolveRound(q, new Dictionary<string, MpAnswer> { ["ti"] = ans });
            Assert.IsNull(a.Item, "štit je potrošen");
            Assert.IsTrue(r.Events.Any(e => e.Kind == "blokirano"));

            // dupla polja
            r.NextRound();
            me.DoubleNext = true;
            var e2 = r.ResolveRound(q, new Dictionary<string, MpAnswer> { ["ti"] = new MpAnswer { Acc = 1, Time = 3, Tiles = 3, Text = "ok" } });
            Assert.AreEqual(r.IsDoubleRound ? 12 : 6, e2.First(e => e.Racer == me).Tiles);
            Assert.IsFalse(me.DoubleNext);

            // napad može vratiti i ispod starta: ostaje 0
            c.Pos = 1;
            Assert.AreEqual(0, r.AdvanceTarget(c, -2).Target);
        }

        [Test]
        public void MinigameEveryFourRoundsAndDoubleRoundsAreRare()
        {
            var r = FixedRace();
            int doubles = 0;
            for (int i = 1; i <= 600; i++)
            {
                r.NextRound();
                Assert.AreEqual(i % 4 == 0, r.IsMinigameRound, "runda " + i);
                if (r.IsDoubleRound) doubles++;
                Assert.IsFalse(i < 3 && r.IsDoubleRound, "prve dvije runde nisu duple");
            }
            Assert.That((double)doubles, Is.InRange(60.0, 150.0), "otprilike 1 u 6");
        }

        [Test]
        public void DeckSkipsSeenQuestions()
        {
            var areas = Catalog.Areas.Select(a => a.Id).ToList();
            var first = new Deck(TestData.Questions, areas, new Rng(1));
            var seen = new HashSet<string>();
            for (int i = 0; i < 40; i++) seen.Add(first.Next().Id);
            var second = new Deck(TestData.Questions, areas, new Rng(1), 3, seen);
            int repeats = 0;
            for (int i = 0; i < 40; i++) if (seen.Contains(second.Next().Id)) repeats++;
            Assert.LessOrEqual(repeats, 2, "nova trka ne ponavlja viđena pitanja");
            var p = new Profile();
            for (int i = 0; i < Profile.VidjenaMax + 10; i++) p.MarkSeen("q" + i);
            Assert.AreEqual(Profile.VidjenaMax, p.Vidjena.Count);
            var back = Profile.FromJson(p.ToJson());
            Assert.AreEqual(Profile.VidjenaMax, back.Vidjena.Count);
            Assert.AreEqual("q10", back.Vidjena[0]);
        }

        [Test]
        public void NeverMovesPastFinish()
        {
            var r = NewRace(2);
            r.Human.Pos = r.Last - 1;
            Assert.AreEqual(r.Last, r.AdvanceTarget(r.Human, 4).Target);
        }

        [Test]
        public void WholeRaceFinishesAndRanksEveryone()
        {
            foreach (var seed in new[] { 1, 2, 3, 4, 5 })
            {
                var r = NewRace(seed, Catalog.LengthOf("10").Tiles);
                var deck = new Deck(TestData.Questions, Catalog.Areas.Select(a => a.Id).ToList(), r.Rng);
                bool done = false;
                for (int round = 0; round < 200 && !done; round++)
                {
                    r.NextRound();
                    var q = deck.Next();
                    var limit = QuizTypes.TimeLimitOf(q);
                    var acc = r.Rng.Next() < 0.7 ? 1 : 0;
                    var res = new QuizResult { Accuracy = acc, TimeUsed = limit / 2, Limit = limit, Tiles = QuizTypes.TilesFor(acc, limit / 2, limit) };
                    var moves = r.MovesFor(r.ResolveRound(q, res));
                    r.Apply(moves);
                    r.Apply(r.Landings(moves));
                    if (r.IsMinigameRound) r.Apply(r.Jumps(r.Bonus(r.Players.Select(p => (p, r.Rng.Next(), "x")))));
                    done = r.CheckFinish();
                }
                Assert.IsTrue(done, "trka mora da se završi");
                var rank = r.Ranking();
                Assert.AreEqual(4, rank.Count);
                Assert.AreEqual(r.Last, rank[0].Pos);
                Assert.That(r.Round, Is.InRange(8, 60));
            }
        }

        [Test]
        public void BotNamesAreDistinctAndShort()
        {
            var rng = new Rng(9);
            for (int i = 0; i < 50; i++)
            {
                var bots = Bots.Create("tesko", rng);
                Assert.AreEqual(3, bots.Select(b => b.Name).Distinct().Count());
                foreach (var b in bots) Assert.LessOrEqual(b.Name.Length, 14);
                foreach (var b in bots) Assert.IsFalse(Catalog.VehicleOf(b.Look.Vozilo).Flies);
            }
        }
    }

    public class ProfileTests
    {
        [Test]
        public void AwardsAndRankUp()
        {
            var p = new Profile { Bodovi = 240 };
            var a = p.AwardRace(0, 5, 10, 30, "cg");
            Assert.AreEqual(120, a.Pts);
            Assert.AreEqual(90, a.Coins);
            Assert.IsNotNull(a.RankUp);
            Assert.AreEqual("srebro", a.RankUp.Id);
            Assert.AreEqual(1, a.NewMaps.Count);
            Assert.AreEqual("balkan", a.NewMaps[0].Id);
        }

        [Test]
        public void SaveLoadRoundTrip()
        {
            var p = new Profile { Ime = "Mladen", Novcici = 1000 };
            var item = Catalog.ShopItemOf("z-mace");
            Assert.IsTrue(p.Buy(item));
            Assert.IsTrue(p.Equip(item));
            p.Podesavanja.Oblasti = new List<string> { "sport" };
            p.AwardRace(1, 3, 8, 10, "cg");
            var q = Profile.FromJson(p.ToJson());
            Assert.AreEqual("Mladen", q.Ime);
            Assert.AreEqual("mace", q.Izgled.Zivotinja);
            Assert.AreEqual(p.Novcici, q.Novcici);
            Assert.AreEqual(1, q.Istorija.Count);
            CollectionAssert.AreEqual(new[] { "sport" }, q.Podesavanja.Oblasti);
        }

        [Test]
        public void CannotEquipUnboughtOrBuyLockedItems()
        {
            var p = new Profile { Novcici = 5000 };
            Assert.IsFalse(p.Equip(Catalog.ShopItemOf("z-lav")));
            Assert.IsFalse(p.Buy(Catalog.ShopItemOf("z-lav")), "lav traži rang platina");
            var tampered = Profile.FromJson("{\"izgled\":{\"zivotinja\":\"lav\"}}");
            Assert.AreEqual("vuk", tampered.Izgled.Zivotinja);
            Assert.AreEqual(150, Profile.FromJson("ne valja").Novcici);
        }
    }

    public class LangTests
    {
        [Test]
        public void Cyrillic()
        {
            Assert.AreEqual("Њујорк и Џон", Translit.ToCyr("Njujork i Džon"));
            Assert.AreEqual("H₂O и Minecraft", Translit.ToCyr("H₂O i Minecraft"));
            Assert.AreEqual("Луј XIV", Translit.ToCyr("Luj XIV"));
        }

        [Test]
        public void English()
        {
            Lang.LoadEnglish(TestData.Read("prevodi-en.json"));
            Assert.AreEqual("Correct!", Lang.ToEn("Tačno!"));
            Assert.AreEqual("Round 3", Lang.ToEn("Runda 3"));
            Assert.AreEqual("Tap the river:", Lang.ToEn("Tapni rijeku:"));
            Assert.AreEqual("Wolf · 12 coins", Lang.ToEn("Vuk · 12 novčića"));
            Assert.AreEqual("lat", Lang.FromSystemLanguage("English"));
            Assert.AreEqual("lat", Lang.FromSystemLanguage("Serbian"));
        }
    }

    public class WorldTests
    {
        [Test]
        public void TrackTilesAreEvenlySpaced()
        {
            foreach (var map in Catalog.Maps)
            {
                var t = TrackLayout.Build(map, 58);
                Assert.AreEqual(58, t.Tiles.Count);
                var gaps = Enumerable.Range(1, 57).Select(i => t.Tiles[i].Pos.DistanceTo(t.Tiles[i - 1].Pos)).ToList();
                // razmak se mjeri po krivoj, pa je ravna linija u oštroj krivini malo kraća
                Assert.That(gaps.Max() / gaps.Average(), Is.LessThan(1.06), map.Id);
                Assert.That(gaps.Min() / gaps.Average(), Is.GreaterThan(0.5), map.Id);
                Assert.That(gaps.Average(), Is.InRange(1.2, 1.8), map.Id);
                Assert.AreEqual("start", t.Tiles[0].Type);
                Assert.AreEqual("cilj", t.Tiles[57].Type);
                Assert.AreEqual(map.Route[0].name, t.Tiles[0].City);
            }
        }

        [Test]
        public void TerrainKeepsTrackFlatAndHasSeaAndMountains()
        {
            var map = Catalog.MapOf("cg");
            var track = TrackLayout.Build(map, 32);
            var land = Geo.ParsePolygons(Json.Obj(Json.Parse(TestData.Read("Mape/evropa.json")))["polygons"]);
            var lakes = GeoData.LakesFor("cg");
            var T = TerrainBuilder.Build(track, land, lakes, new TerrainParams(), map.Id);
            foreach (var tile in track.Tiles.Where(x => T.IsLandAt(x.Pos.X, x.Pos.Z)))
                Assert.AreEqual(T.LandTop, T.SampleHeight(tile.Pos.X, tile.Pos.Z), 0.12, "polje " + tile.Index);
            Assert.IsTrue(T.Kind.Contains(Ground.Sea), "Jadransko more");
            Assert.IsTrue(T.Kind.Contains(Ground.Lake), "Skadarsko jezero");
            Assert.Greater(T.Height.Max(), 2.0f, "planine");
            Assert.Greater(T.Trees.Count, 100);
            Assert.Greater(T.Houses.Count, 30);
        }
    }

    public class SynthTests
    {
        [Test]
        public void NotesAndClips()
        {
            Assert.AreEqual(440.0, Synth.NoteFreq("A4"), 1e-9);
            Assert.AreEqual(261.63, Synth.NoteFreq("C4"), 0.01);
            var m = Synth.RenderMelody("E4:1 R:1 G4:.5", 120);
            Assert.That(m.Length, Is.InRange(Synth.Rate * 2, Synth.Rate * 3));
            Assert.That(m.Max(), Is.LessThanOrEqualTo(1f));
            var music = Synth.Music();
            Assert.Greater(music.Length, Synth.Rate * 30);
            Assert.That(music.Max(Math.Abs), Is.InRange(0.5f, 0.81f));
            foreach (var v in Catalog.Vehicles) Assert.Greater(Synth.VehicleHop(v.Id).Length, 100, v.Id);
        }
    }
    public class MinigameDataTests
    {
        static List<WorldCountry> world;
        static List<WorldCountry> World => world ??= WorldGeo.ParseCountries(TestData.Read("Mape/drzave.json"));

        [Test]
        public void WorldCountriesAndDistances()
        {
            Assert.Greater(World.Count, 180);
            var srb = World.First(c => c.Code == "rs");
            Assert.IsTrue(WorldGeo.InCountry(20.46, 44.82, srb), "Beograd je u Srbiji");
            Assert.IsTrue(WorldGeo.InCountry(21.17, 42.66, srb), "Priština je u sastavu Srbije");
            Assert.AreEqual(0.0, WorldGeo.DistToCountry(20.46, 44.82, srb), 1e-9);
            var fr = World.First(c => c.Code == "fr");
            Assert.That(WorldGeo.DistToCountry(-3.7, 40.42, fr), Is.InRange(200.0, 700.0), "Madrid je par stotina km od Francuske");
            Assert.AreEqual(1.0, WorldGeo.ScoreCountry(0), 1e-9);
            Assert.AreEqual(0.8, WorldGeo.ScoreCountry(100), 1e-9);
            Assert.AreEqual(0.0, WorldGeo.ScoreCountry(1000), 1e-9);
            Assert.AreEqual(1.0, WorldGeo.ScoreForKm(80), 1e-9);
            Assert.AreEqual(0.5, WorldGeo.ScoreForKm(1100), 1e-9);
            Assert.AreEqual(0.0, WorldGeo.ScoreForKm(5000), 1e-9);
            Assert.Greater(World.Count(c => c.Area > 8000), 150);
        }

        [Test]
        public void CitiesAndRivers()
        {
            WorldGeo.ParsePlaces(TestData.Read("svijet-gradovi-rijeke.json"), out var cities, out var rivers);
            Assert.GreaterOrEqual(cities.Count, 40);
            Assert.GreaterOrEqual(rivers.Count, 10);
            var dunav = rivers.First(r => r.Name == "Dunav");
            Assert.Less(WorldGeo.DistToLine(20.5, 44.8, dunav.Points), 60, "Dunav kod Beograda");
            Assert.Greater(WorldGeo.DistToLine(2.35, 48.86, dunav.Points), 400, "Pariz nije na Dunavu");
            Assert.AreEqual(1.0, WorldGeo.ScoreRiver(100), 1e-9);
            Assert.AreEqual(1.0, WorldGeo.ScoreCity(149), 1e-9);
        }

        [Test]
        public void WorldFrameRoundTrip()
        {
            foreach (var (lon, lat) in new[] { (0.0, 0.0), (20.5, 44.8), (-120.0, 35.0), (178.0, -18.0), (-175.0, -20.0) })
            {
                var p = WorldFrame.ToLonLat(WorldFrame.U(lon), WorldFrame.V(lat));
                Assert.AreEqual(lon, p.X, 1e-9); Assert.AreEqual(lat, p.Y, 1e-9);
                Assert.That(WorldFrame.U(lon), Is.InRange(0.0, 1.0));
            }
            var ring = new List<Vec2d> { new Vec2d(179, 0), new Vec2d(-179, 0), new Vec2d(-179, 1), new Vec2d(179, 1) };
            var u = WorldGeo.Unwrap(ring, 179);
            Assert.AreEqual(181.0, u[1].X, 1e-9, "prsten preko datumske granice se odmotava");
        }

        [Test]
        public void CanvasFillAndShape()
        {
            var cv = new Canvas32(20, 10);
            cv.Clear(new Rgba(0, 0, 255));
            cv.Fill(new List<IList<Vec2d>> { new List<Vec2d> { new Vec2d(2, 2), new Vec2d(10, 2), new Vec2d(10, 8), new Vec2d(2, 8) } }, new Rgba(255, 0, 0));
            Assert.AreEqual(255, (int)cv.Get(5, 5).R);
            Assert.AreEqual(255, (int)cv.Get(15, 5).B);
            Assert.AreEqual(0, (int)cv.Get(15, 5).R);
            Assert.AreEqual(255, (int)cv.Data[(4 * 20 + 5) * 4 + 0], "redovi u nizu idu odozdo (y = 5 je red 4)");
            Assert.AreEqual(0, (int)cv.Data[(0 * 20 + 5) * 4 + 0], "red 0 u nizu je dno slike (y = 9, plavo)");

            var land = WorldGeo.ParseLand(TestData.Read("Mape/kopno-svijet.json"));
            Assert.Greater(land.Count, 50);
            foreach (var code in new[] { "me", "ru", "fj", "nz", "cl", "id" })
            {
                var d = World.First(c => c.Code == code);
                var img = WorldPainter.Shape(d, World, land, 340, 250);
                int green = 0;
                for (int y = 0; y < img.H; y++) for (int x = 0; x < img.W; x++) { var p = img.Get(x, y); if (p.G > 90 && p.R < 80) green++; }
                Assert.That((double)green, Is.InRange(img.W * img.H / 100.0, img.W * img.H * 0.7), code + " zauzima razuman dio slike");
            }
        }

        [Test]
        public void ChessPuzzles()
        {
            var all = ChessPuzzle.ParseAll(TestData.Read("sah.json"));
            Assert.GreaterOrEqual(all.Count, 40);
            foreach (var p in all)
            {
                var b = ChessPuzzle.ParseFen(p.Fen);
                Assert.IsTrue(b.TryGetValue(p.From, out var pc) && ChessPuzzle.IsWhite(pc), p.Fen + ": na polju " + p.From + " je bijela figura");
                Assert.IsTrue(!b.TryGetValue(p.To, out var t) || !ChessPuzzle.IsWhite(t), p.Fen + ": cilj nije bijela figura");
                Assert.AreEqual(1, b.Values.Count(x => x == 'K'));
                Assert.AreEqual(1, b.Values.Count(x => x == 'k'));
            }
            Assert.AreEqual("Dh7#", ChessPuzzle.LocalSan("Qh7#"));
            Lang.LoadEnglish(TestData.Read("prevodi-en.json"));
            Assert.AreEqual("3.5 points · 12 s", Lang.ToEn("3,5 bodova · 12 s"));
            Assert.AreEqual("2 mates", Lang.ToEn("2 matova"));
            Assert.AreEqual("Mate!", Lang.ToEn("Mat!"));
            Assert.AreEqual("S", ChessPuzzle.Letter('n'));
            foreach (var pc in "KQRBNPkqrbnp")
            {
                var img = ChessArt.Draw(pc, 64);
                int filled = 0; double light = 0;
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { var c = img.Get(x, y); if (c.A > 200) { filled++; light += c.R; } }
                Assert.That((double)filled, Is.InRange(64 * 64 * 0.12, 64 * 64 * 0.6), pc + " zauzima razuman dio polja");
                Assert.IsTrue(char.IsUpper(pc) ? light / filled > 150 : light / filled < 110, pc + " bijele su svijetle, crne tamne");
                Assert.AreEqual(0, (int)img.Get(1, 1).A, pc + " ugao je providan");
            }
        }

        [Test]
        public void OptionsAreDistinctAndContainAnswer()
        {
            var rng = new Rng(5);
            var pool = new List<string> { "a", "b", "c", "d", "e", "f" };
            for (int i = 0; i < 50; i++)
            {
                var o = Options.Pick(rng, "c", pool);
                Assert.AreEqual(4, o.Count); Assert.IsTrue(o.Contains("c")); Assert.AreEqual(4, o.Distinct().Count());
            }
            var places = Json.Arr(Json.Parse(TestData.Read("slike-mjesta.json"))).Select(Json.Obj).ToList();
            Assert.Greater(places.Count, 150);
            for (int i = 0; i < 50; i++)
            {
                var it = rng.Pick(places);
                var o = Options.Similar(rng, it, places, x => Json.Str(x, "kontinent"), x => Json.Str(x, "naziv"));
                Assert.AreEqual(4, o.Count); Assert.IsTrue(o.Contains(Json.Str(it, "naziv"))); Assert.AreEqual(4, o.Distinct().Count());
            }
            foreach (var kat in new[] { "mjesta", "zivotinje", "tijelo", "instrumenti" })
                foreach (var x in Json.Arr(Json.Parse(TestData.Read("slike-" + kat + ".json"))).Select(Json.Obj))
                {
                    Assert.IsTrue(Json.Str(x, "slika").StartsWith("Slike/" + kat + "/"), kat);
                    Assert.IsTrue(File.Exists(Path.Combine(TestData.Dir, "..", Json.Str(x, "slika") + ".jpg")), Json.Str(x, "slika"));
                }
            var ins = Json.Arr(Json.Parse(TestData.Read("instrumenti-zvuk.json")));
            Assert.GreaterOrEqual(ins.Count, 20);
        }
    }
    public class MultiplayerTests
    {
        [Test]
        public void RoomCodesAreReadableAndNormalize()
        {
            var rng = new Rng(1);
            for (int i = 0; i < 200; i++)
            {
                var c = RoomCode.New(rng);
                Assert.AreEqual(5, c.Length);
                Assert.IsTrue(c.All(ch => RoomCode.Alphabet.IndexOf(ch) >= 0));
                Assert.AreEqual(c, RoomCode.Normalize(" " + c.ToLowerInvariant().Insert(2, "-") + " "));
            }
            Assert.IsNull(RoomCode.Normalize("ABC"));
            Assert.IsNull(RoomCode.Normalize("AB0CD"), "nula nije u abecedi");
            Assert.IsNull(RoomCode.Normalize(null));
        }

        [Test]
        public void PhoenixMessagesRoundTrip()
        {
            var join = Phoenix.Decode(Phoenix.Join("realtime:trka-soba-ABCDE", "u1", "tok", "1"));
            Assert.AreEqual("phx_join", join.Event);
            Assert.AreEqual("1", join.JoinRef);
            var cfg = Json.Obj(join.Payload["config"]);
            Assert.AreEqual("u1", Json.Str(Json.Obj(cfg["presence"]), "key"));
            Assert.AreEqual("tok", Json.Str(join.Payload, "access_token"));

            var ans = new MpAnswer { Acc = 0.8, Time = 4.25, Tiles = 4, Text = "Beograd", Powerups = new List<string> { "precica" } };
            var b = Phoenix.Decode(Phoenix.Broadcast("realtime:x", "m", new Dictionary<string, object> { ["t"] = "ans", ["a"] = ans.ToJson() }, "5", "1"));
            Assert.AreEqual("broadcast", b.Event);
            Assert.AreEqual("m", Json.Str(b.Payload, "event"));
            var back = MpAnswer.FromJson(Json.Get(Json.Obj(b.Payload["payload"]), "a"));
            Assert.AreEqual(0.8, back.Acc, 1e-9); Assert.AreEqual(4, back.Tiles); Assert.AreEqual("precica", back.Powerups[0]);
            Assert.AreEqual(12, MpAnswer.FromJson(Json.Parse("{\"tiles\": 99}")).Tiles, "varanje: najviše 12 polja");

            var state = Json.Obj(Json.Parse("{\"u1\":{\"metas\":[{\"phx_ref\":\"x\",\"name\":\"Ana\",\"since\":5}]}}"));
            var metas = Phoenix.PresenceMetas(state);
            Assert.AreEqual("Ana", Json.Str(metas["u1"], "name"));
            Assert.IsNull(Phoenix.Decode("nije json"));
        }

        [Test]
        public void MatchmakerGroupsOldestFirst()
        {
            var lobby = new List<Matchmaker.Waiting> { new Matchmaker.Waiting { Id = "c", Since = 30 }, new Matchmaker.Waiting { Id = "a", Since = 10 } };
            Assert.AreEqual("a", Matchmaker.Leader(lobby));
            Assert.IsNull(Matchmaker.Group(lobby, 20), "čeka se još igrača");
            CollectionAssert.AreEqual(new[] { "a", "c" }, Matchmaker.Group(lobby, 26));
            lobby.Add(new Matchmaker.Waiting { Id = "d", Since = 31 });
            lobby.Add(new Matchmaker.Waiting { Id = "e", Since = 32 });
            lobby.Add(new Matchmaker.Waiting { Id = "f", Since = 33 });
            CollectionAssert.AreEqual(new[] { "a", "c", "d", "e" }, Matchmaker.Group(lobby, 11), "četvorica odmah");
            var g = Matchmaker.Group(lobby, 11);
            Assert.AreEqual(Matchmaker.RoomFor(g, 10), Matchmaker.RoomFor(g, 10.2));
        }

        [Test]
        public void CollectorWaitsForEveryoneThenDecides()
        {
            var c = new Collector<MpAnswer>(new[] { "a", "b", "c" });
            Assert.IsTrue(c.Add("a", new MpAnswer { Acc = 1, Tiles = 5 }));
            Assert.IsFalse(c.Add("a", new MpAnswer()), "drugi odgovor istog igrača se ne računa");
            Assert.IsFalse(c.Add("x", new MpAnswer()), "stranac");
            Assert.IsFalse(c.Complete);
            c.Drop("c");
            Assert.IsFalse(c.Complete);
            c.Add("b", new MpAnswer { Acc = 0.5, Tiles = 2 });
            Assert.IsTrue(c.Complete);
            var d = new Collector<MpAnswer>(new[] { "a", "b" });
            d.Add("a", new MpAnswer { Tiles = 3 });
            var res = d.Decide(id => MpAnswer.Timeout(20));
            Assert.AreEqual(0, res["b"].Tiles);
            Assert.IsTrue(res["b"].TimedOut);
        }

        // Tri uređaja igraju istu trku: svaki ima svoj Race, a domaćin objavljuje odgovore ljudi i rezultate mini igara.
        // Na kraju svako mora imati iste pozicije, novčiće i poredak.
        [Test]
        public void ThreeDevicesStayInSync()
        {
            var setup = new RaceSetup { Seed = 777, Map = "cg", Tiles = 32, Level = "srednje", Areas = Catalog.Areas.Select(a => a.Id).ToList(), MaxTezina = 3 };
            foreach (var (id, name) in new[] { ("u1", "Ana"), ("u2", "Bojan"), ("u3", "Ceca") })
                setup.Humans.Add(new MpPlayer { Id = id, Name = name, Look = new Look { Zivotinja = "lisica" } });
            var wire = RaceSetup.FromJson(Json.Parse(Json.Write(setup.ToJson())));   // preko mreže
            Assert.AreEqual(3, wire.Humans.Count);

            var devices = wire.Humans.Select(h =>
            {
                var shared = new Rng(wire.Seed);
                var players = wire.BuildPlayers(h.Id, shared);
                return (id: h.Id, race: new Race(players, wire.Tiles, shared, TileKinds.Generate(wire.Tiles, wire.Seed)), deck: new Deck(TestData.Questions, wire.Areas, shared, wire.MaxTezina), own: new Rng(h.Id.GetHashCode()));
            }).ToList();
            Assert.AreEqual(4, devices[0].race.Players.Count);
            Assert.AreEqual(1, devices[0].race.Players.Count(p => p.Human));
            Assert.AreEqual(2, devices[0].race.Players.Count(p => p.Remote));
            CollectionAssert.AreEqual(devices[0].race.Players.Select(p => p.Name).ToList(), devices[2].race.Players.Select(p => p.Name).ToList(), "isti botovi");

            bool done = false;
            for (int round = 0; round < 200 && !done; round++)
            {
                var qs = devices.Select(d => { d.race.NextRound(); return d.deck.Next(); }).ToList();
                Assert.IsTrue(qs.All(q => q.Id == qs[0].Id), "isto pitanje");
                var limit = QuizTypes.TimeLimitOf(qs[0]);
                // svaki čovjek odgovara na svom uređaju; u2 kasni pa domaćin odluči da mu je isteklo vrijeme
                var answers = new Collector<MpAnswer>(wire.Humans.Select(h => h.Id));
                foreach (var d in devices)
                {
                    if (d.id == "u2" && round % 5 == 3) continue;
                    double acc = d.own.Chance(0.7) ? 1 : 0, t = d.own.Range(2.0, limit);
                    var me = d.race.Players.First(p => p.Id == d.id);
                    var pu = new List<string>();
                    if (d.own.Chance(0.05)) pu.Add("precica");
                    if (me.Item != null && Items.IsAttack(me.Item) && d.own.Chance(0.5)) pu.Add(Items.UsePrefix + me.Item);
                    answers.Add(d.id, new MpAnswer { Acc = acc, Time = t, Tiles = QuizTypes.TilesFor(acc, t, limit), Text = "odgovor", Powerups = pu });
                }
                var decided = answers.Decide(id => MpAnswer.Timeout(limit));
                var wireRound = Json.Write(decided.ToDictionary(kv => kv.Key, kv => (object)kv.Value.ToJson()));
                List<Move> firstMoves = null;
                foreach (var d in devices)
                {
                    var got = Json.Obj(Json.Parse(wireRound)).ToDictionary(kv => kv.Key, kv => MpAnswer.FromJson(kv.Value));
                    var entries = d.race.ResolveRound(qs[0], got);
                    var moves = d.race.MovesFor(entries);
                    d.race.Apply(moves);
                    firstMoves ??= moves;
                    d.race.Apply(d.race.Landings(moves));
                    // svake 4 runde: mini igra za sve (ljudi iz odluke domaćina, botovi iz zajedničkog Rng-a)
                    if (d.race.IsMinigameRound)
                    {
                        var res = d.race.Players.Select(p => (p, p.Person ? (p.Id[1] * 0.11 + round * 0.07) % 1 : d.race.Rng.Next(), "b")).ToList();
                        d.race.Apply(d.race.Jumps(d.race.Bonus(res)));
                    }
                }
                foreach (var d in devices) done |= d.race.CheckFinish();
                var reference = devices[0].race.Players.Select(p => $"{p.Id}:{p.Pos}:{p.Coins}:{p.Item}:{p.DoubleNext}:{p.Confused}").ToList();
                foreach (var d in devices)
                    CollectionAssert.AreEqual(reference, d.race.Players.Select(p => $"{p.Id}:{p.Pos}:{p.Coins}:{p.Item}:{p.DoubleNext}:{p.Confused}").ToList(), "runda " + round + " na " + d.id);
            }
            Assert.IsTrue(done, "trka se završila");
            var rank = devices.Select(d => string.Join(",", d.race.Ranking().Select(p => p.Id))).Distinct().ToList();
            Assert.AreEqual(1, rank.Count, "isti poredak na svim uređajima");
            Assert.AreEqual("u1", devices[0].race.Human.Id);
            Assert.AreEqual("u3", devices[2].race.Human.Id);
        }
    }
}

namespace TrkaZnanja.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using TrkaZnanja.Core;

    public class LookAndIconTests
    {
        [Test]
        public void EveryIconDrawsSomething()
        {
            foreach (var name in Icons.All)
            {
                var cv = Icons.Draw(name, 96);
                int filled = 0;
                for (int i = 3; i < cv.Data.Length; i += 4) if (cv.Data[i] > 128) filled++;
                Assert.Greater(filled, 96 * 96 / 40, name);
                Assert.Less(filled, 96 * 96 * 9 / 10, name);
                // za pregled ikonica (alat): TRKA_IKONE=folder sačuva sirove RGBA slike
                var dir = Environment.GetEnvironmentVariable("TRKA_IKONE");
                if (!string.IsNullOrEmpty(dir)) File.WriteAllBytes(Path.Combine(dir, name + ".rgba"), cv.Data);
            }
        }

        [Test]
        public void DecorationsAreInShopAndSurviveJson()
        {
            foreach (var slot in new[] { "sara", "boja2", "nalijepnica", "trag" })
            {
                Assert.Greater(Catalog.Shop.Count(i => i.Slot == slot), 3, slot);
                Assert.IsTrue(Catalog.Shop.Any(i => i.Slot == slot && i.Price == 0), slot + " ima besplatnu opciju");
            }
            Assert.AreEqual(Catalog.Shop.Count, Catalog.Shop.Select(i => i.Id).Distinct().Count(), "id-jevi u prodavnici su jedinstveni");
            var p = new Profile { Novcici = 5000, Bodovi = 100000 };
            foreach (var id in new[] { "s-plamen", "c-#ffd23f", "l-zvijezda", "t-duga" })
            {
                var item = Catalog.ShopItemOf(id);
                Assert.IsNotNull(item, id);
                Assert.IsTrue(p.Buy(item), id);
                Assert.IsTrue(p.Equip(item), id);
                Assert.IsTrue(p.IsEquipped(item), id);
            }
            var back = Profile.FromJson(p.ToJson());
            Assert.AreEqual("plamen", back.Izgled.Sara);
            Assert.AreEqual("#ffd23f", back.Izgled.Boja2);
            Assert.AreEqual("zvijezda", back.Izgled.Nalijepnica);
            Assert.AreEqual("duga", back.Izgled.Trag);

            // ukras koji nije kupljen se ne učitava
            var json = p.ToJson().Replace("\"t-duga\",", "").Replace(",\"t-duga\"", "");
            Assert.AreEqual("nema", Profile.FromJson(json).Izgled.Trag);

            // stari profil (bez ukrasa) daje podrazumijevane vrijednosti
            var old = Profile.FromJson("{\"izgled\":{\"zivotinja\":\"zec\"}}");
            Assert.AreEqual("nema", old.Izgled.Sara);
            Assert.AreEqual("#ffffff", old.Izgled.Boja2);

            // ukrasi idu i preko multiplayera
            var mp = MpPlayer.FromJson(new MpPlayer { Id = "a", Name = "A", Look = back.Izgled }.ToJson());
            Assert.AreEqual("plamen", mp.Look.Sara);
            Assert.AreEqual("duga", mp.Look.Trag);
        }
    }
}

namespace TrkaZnanja.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using TrkaZnanja.Core;

    public class TriangulatorTests
    {
        [Test]
        public void TrianglesCoverPolygonArea()
        {
            var shapes = new List<List<Vec2d>>
            {
                Icons.Star(0.5, 0.5, 0.4, 0.15, 5),
                Icons.Heart(0.5, 0.5, 0.4),
                Icons.Flame(0.5, 0.5, 0.4),
                new List<Vec2d> { new Vec2d(0.58, 0.06), new Vec2d(0.2, 0.56), new Vec2d(0.46, 0.56), new Vec2d(0.38, 0.94), new Vec2d(0.8, 0.4), new Vec2d(0.53, 0.4), new Vec2d(0.66, 0.06) },
            };
            foreach (var s in shapes)
            {
                var tris = Triangulator.Triangulate(s);
                Assert.AreEqual(0, tris.Count % 3);
                Assert.AreEqual(s.Count - 2, tris.Count / 3);
                double sum = 0;
                for (int i = 0; i < tris.Count; i += 3)
                    sum += System.Math.Abs(Triangulator.Area(new[] { s[tris[i]], s[tris[i + 1]], s[tris[i + 2]] }));
                Assert.AreEqual(System.Math.Abs(Triangulator.Area(s)), sum, 1e-6);
            }
        }
    }
}

namespace TrkaZnanja.Tests
{
    using NUnit.Framework;
    using TrkaZnanja.Core;

    public class PluralTests
    {
        [Test]
        public void PluralFormsFollowOurGrammar()
        {
            string R(int n) => Fmt.Plural(n, "runda", "runde", "rundi");
            Assert.AreEqual("runda", R(1));
            Assert.AreEqual("runde", R(3));
            Assert.AreEqual("rundi", R(5));
            Assert.AreEqual("rundi", R(12));
            Assert.AreEqual("runda", R(21));
            Assert.AreEqual("runde", R(24));
            Assert.AreEqual("rundi", R(111));
        }
    }
}
