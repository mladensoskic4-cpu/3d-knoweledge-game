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
