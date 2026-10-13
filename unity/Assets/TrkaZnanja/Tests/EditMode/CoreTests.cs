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
            return new Race(human, Bots.Create("srednje", rng), tiles, rng);
        }

        [Test]
        public void StopsAtFirstUnvisitedStation()
        {
            var r = NewRace(1);
            var me = r.Human;
            me.Pos = 5;
            var m = r.AdvanceTarget(me, 4);
            Assert.AreEqual(7, m.Target);
            Assert.IsTrue(m.Checkpoint);
            r.Apply(new[] { m });
            var m2 = r.AdvanceTarget(me, 3);
            Assert.AreEqual(10, m2.Target);
            Assert.IsFalse(m2.Checkpoint);
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
                    foreach (var p in r.AtStation(moves)) r.Apply(r.Jumps(new[] { r.Station(p, r.Rng.Next(), "test") }));
                    if (r.IsBonusRound) r.Apply(r.Jumps(r.Bonus(r.Players.Select(p => (p, r.Rng.Next(), "x")))));
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
}
