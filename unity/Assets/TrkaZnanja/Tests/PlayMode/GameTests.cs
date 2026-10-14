// PlayMode testovi: igra se pokrene, napravi svijet za svaku mapu i atmosferu, sve životinje i vozila,
// svaki tip pitanja, i odigra cijelu trku sama (AutoPlay). Svaka greška u konzoli obara test.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TrkaZnanja.Core;
using TrkaZnanja.Game;
using UnityEngine;
using UnityEngine.TestTools;

namespace TrkaZnanja.Tests
{
    public class GameTests
    {
        static IEnumerator FreshApp(bool autoPlay)
        {
            if (App.I != null) Object.Destroy(App.I.gameObject);
            yield return null;
            App.AutoPlay = autoPlay;
            App.TestMode = !autoPlay;
            App.ForcedSeed = 42;
            App.Boot();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            App.AutoPlay = false;
            App.TestMode = false;
            if (App.I != null) Object.Destroy(App.I.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootsLoadsQuestionsAndBuildsWorld()
        {
            yield return FreshApp(true);
            Assert.IsNotNull(App.I);
            Assert.Greater(GameData.Questions.Count, 5000);
            Assert.IsNotNull(Object.FindAnyObjectByType<WorldView>(), "svijet nije napravljen");
            Assert.IsNotNull(Camera.main, "nema glavne kamere");
            Assert.Greater(GameData.Countries.Count, 150);
            Assert.IsNotNull(GameData.Flag("me"), "zastava Crne Gore");
        }

        [UnityTest]
        public IEnumerator EveryMapAndAtmosphereBuilds()
        {
            yield return FreshApp(false);
            var root = new GameObject("Test svjetovi").transform;
            foreach (var map in Catalog.Maps)
                foreach (var atmo in Catalog.Atmosfere)
                {
                    var w = WorldView.Build(root, map, 32, atmo, 7);
                    var t = TrackView.Build(w);
                    Assert.AreEqual(32, t.Tiles.Count);
                    Assert.Greater(t.TotalArc, 20f, map.Id);
                    var meshes = w.GetComponentsInChildren<MeshFilter>();
                    Assert.Greater(meshes.Length, 3, map.Id + " " + atmo);
                    Assert.IsTrue(meshes.All(m => m.sharedMesh != null && m.sharedMesh.vertexCount > 0), map.Id + " " + atmo);
                    Object.Destroy(w.gameObject);
                    yield return null;
                }
            Object.Destroy(root.gameObject);
        }

        [UnityTest]
        public IEnumerator AllAnimalsAndVehiclesBuild()
        {
            yield return FreshApp(false);
            var root = new GameObject("Test likovi").transform;
            foreach (var a in Catalog.Animals)
                foreach (var v in Catalog.Vehicles.Where((x, i) => i % 3 == a.Id.Length % 3))
                {
                    var look = new Look { Zivotinja = a.Id, Vozilo = v.Id, Boja = v.DefaultPaint, Kapa = "kruna", Naocare = true };
                    var go = Avatars.Build(look, root, a.Id + "-" + v.Id);
                    var mf = go.GetComponentsInChildren<MeshFilter>();
                    Assert.Greater(mf.Sum(m => m.sharedMesh.vertexCount), 200, a.Id + " u " + v.Id);
                }
            foreach (var v in Catalog.Vehicles)
                Assert.IsNotNull(Avatars.Build(new Look { Vozilo = v.Id }, root, v.Id));
            yield return null;
            Object.Destroy(root.gameObject);
        }

        [UnityTest]
        public IEnumerator EveryQuestionTypeOpensAnswersAndCloses()
        {
            yield return FreshApp(false);
            var rng = new Rng(3);
            foreach (var tip in new[] { "izbor", "mutna", "melodija", "procjena", "poredaj", "spoji", "mapa" })
            {
                var q = GameData.Questions.First(x => x.Tip == tip);
                var qs = QuestionScreen.Open(UIRoot.I.Screen, q, 1, rng, Catalog.Powerups.Select(p => (p, 1)).ToList());
                yield return null;
                yield return null;
                qs.AutoAnswer(rng);
                Assert.IsNotNull(qs.Result, tip);
                Assert.That(qs.Result.Accuracy, Is.InRange(0.0, 1.0), tip);
                qs.Close();
                yield return null;
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EveryMinigamePlaysByItself()
        {
            yield return FreshApp(false);
            Minigame.TestAuto = true;
            try
            {
                Assert.AreEqual(11, Minigame.All.Count);
                Assert.AreEqual(11, Minigame.All.Select(m => m.Id).Distinct().Count(), "id-jevi mini igara su jedinstveni");
                var rng = new Rng(9);
                foreach (var mg in Minigame.All)
                {
                    Assert.IsTrue(mg.Ready, mg.Id + ": nema podataka");
                    var (bs, bl) = mg.SimulateBot(0.7, rng);
                    Assert.That(bs, Is.InRange(0.0, 1.0), mg.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(bl), mg.Id);
                    mg.Prepare();
                    var host = UIKit.Rect(UIRoot.I.Screen, "Test " + mg.Id);
                    host.Center(new Vector2(1160, 600));
                    yield return mg.Play(host, rng, 6);
                    Assert.That(mg.Score, Is.InRange(0.0, 1.0), mg.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(mg.ResultLabel), mg.Id);
                    Object.Destroy(host.gameObject);
                    yield return null;
                }
            }
            finally { Minigame.TestAuto = false; AudioKit.I?.StopNotes(); }
        }

        // Dva igrača u istoj sobi (lokalna veza, bez interneta): igra ide preko pravog toka poruka
        // (doprinosi, odluke domaćina, stanice i bonus runde) i mora stići do cilja.
        [UnityTest, Timeout(300000)]
        public IEnumerator TwoPlayerRaceOverLoopback()
        {
            yield return FreshApp(false);
            RealtimeChannel.Loopback = true;
            MpSession host = null, guest = null;
            try
            {
                App.I.Profile.Podesavanja.Duzina = "5";
                host = MpSession.Join("TESTA", App.I.Profile, App.I.transform, "domacin");
                yield return null;
                var other = new Profile { Ime = "Drugi" };
                // drugi igrač je samo „sesija” bez ekrana: na svaki doprinos domaćina odgovori svojim
                var gObj = new GameObject("Gost");
                guest = MpSession.Join("TESTA", other, gObj.transform, "gost", current: false);
                var rng = new Rng(4);
                var g = guest;
                g.OnContribution += (key, id) =>
                {
                    if (id == g.LocalId) return;
                    var round = key.Substring(key.IndexOf(':') + 1);
                    g.Contribute("q:" + round, new MpAnswer { Acc = rng.Chance(0.6) ? 1 : 0, Time = 5, Tiles = rng.Range(0, 5), Text = "gost" }.ToJson());
                    g.Contribute("st:" + round, new MpScore { Score = rng.Next(), Label = "gost" }.ToJson());
                    g.Contribute("bo:" + round, new MpScore { Score = rng.Next(), Label = "gost" }.ToJson());
                };
                float t = 0;
                while ((!host.Connected || !guest.Connected || host.Members.Count < 2) && t < 5) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.AreEqual(2, host.Members.Count, "oba igrača su u sobi");
                Assert.IsTrue(host.IsHost, "prvi u sobi je domaćin");
                Assert.IsFalse(guest.IsHost);
                App.AutoPlay = true;
                Minigame.TestAuto = true;
                App.I.StartMultiplayerRace(host);
                t = 0;
                while (guest.Setup == null && t < 3) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.IsNotNull(guest.Setup, "gost je dobio postavu trke");
                Assert.AreEqual(2, guest.Setup.Humans.Count);
                t = 0;
                while (App.I.RacesFinished < 1 && t < 280) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.GreaterOrEqual(App.I.RacesFinished, 1, "trka sa dva igrača se nije završila");
            }
            finally
            {
                Minigame.TestAuto = false;
                RealtimeChannel.Loopback = false;
                if (guest != null) Object.Destroy(guest.transform.parent != null ? guest.transform.parent.gameObject : guest.gameObject);
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator PlaysAWholeRaceByItself()
        {
            yield return FreshApp(true);
            App.I.Profile.Podesavanja.Duzina = "5";
            float t = 0;
            while (App.I.RacesFinished < 1 && t < 280)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.GreaterOrEqual(App.I.RacesFinished, 1, "trka se nije završila");
            Assert.Greater(App.I.Profile.Statistika.Trke, 0);
        }
    }
}
