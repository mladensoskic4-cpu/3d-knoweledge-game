// Meni „Trka znanja → Provjeri izgled na telefonu”: pokrene PlayMode test sa snimcima ekrana
// za telefon 20:9 (sa notch-om), telefon 16:9 i tablet 4:3. Slike i izvještaj: unity/Logs/UiSnimci/.
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TrkaZnanja.Tests
{
    public static class UiCheckMenu
    {
        [MenuItem("Trka znanja/Provjeri izgled na telefonu", priority = 40)]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = new[] { "TrkaZnanja.Tests.UiSnapshotTests.PhoneLayoutsDoNotOverlap" },
            }));
            Debug.Log("Trka znanja: provjera izgleda pokrenuta — slike idu u Logs/UiSnimci.");
        }
    }
}
