// Pravljenje igre jednim klikom: Trka znanja → Napravi Android APK / Napravi PC verziju.
// APK ide u unity/Build/Android/TrkaZnanja.apk (instalira se na telefon direktno, bez Play prodavnice).
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrkaZnanja.EditorTools
{
    public static class BuildTools
    {
        const string Scene = "Assets/TrkaZnanja/Scenes/Igra.unity";

        [MenuItem("Trka znanja/Napravi Android APK", priority = 20)]
        public static void Android()
        {
            ProjectSetup.Run(false);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ClearNativeLibCache();
            EditorUserBuildSettings.buildAppBundle = false;     // .apk (ne .aab), da se instalira direktno
            PlayerSettings.Android.useCustomKeystore = false;    // testni potpis (debug)
            Directory.CreateDirectory("Build/Android");
            Run(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = "Build/Android/TrkaZnanja.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
        }

        [MenuItem("Trka znanja/Napravi PC verziju", priority = 21)]
        public static void Windows()
        {
            ProjectSetup.Run(false);
            Directory.CreateDirectory("Build");
            Run(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = "Build/Trka znanja.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
        }

        // Gradle međufajlovi sa .so bibliotekama: ako ih OneDrive (ili stari Gradle proces) drži zaključane,
        // drugi build pada sa AccessDeniedException. Brišemo ih prije builda (Gradle ih napravi ponovo).
        static void ClearNativeLibCache()
        {
            const string dir = "Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/build/intermediates/merged_native_libs";
            if (!Directory.Exists(dir)) return;
            try { Directory.Delete(dir, true); }
            catch (System.Exception e)
            {
                Debug.LogWarning("Trka znanja: ne mogu da obrišem stari Gradle keš (" + e.Message + "). " +
                    "Projekat je u OneDrive folderu: pauziraj OneDrive sinhronizaciju dok traje build, pa pokušaj ponovo.");
            }
        }

        static void Run(BuildPlayerOptions o)
        {
            var report = BuildPipeline.BuildPlayer(o);
            var s = report.summary;
            var msg = s.result == BuildResult.Succeeded
                ? $"Gotovo: {o.locationPathName} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalMinutes:0.0} min)"
                : $"Build nije uspio ({s.result}, grešaka: {s.totalErrors}). Pogledaj Console.";
            Debug.Log("Trka znanja: " + msg);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Trka znanja", msg, "U redu");
            if (s.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(o.locationPathName);
        }
    }
}
