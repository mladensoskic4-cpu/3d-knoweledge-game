// Jednim klikom (ili samo, pri prvom otvaranju) podešava projekat: URP sa post-processingom i mekim sjenkama,
// scenu „Igra”, listu scena za build, polegnut ekran, ime i ID aplikacije, IL2CPP/ARM64 za Android.
// Meni: Trka znanja → Podesi projekat.
using System.IO;
using TrkaZnanja.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TrkaZnanja.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Dir = "Assets/TrkaZnanja/Settings";
        const string AssetPath = Dir + "/TrkaURP.asset";
        const string RendererPath = Dir + "/TrkaRenderer.asset";
        const string ScenePath = "Assets/TrkaZnanja/Scenes/Igra.unity";
        const string AppId = "me.trkaznanja.igra";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("trka-setup-done", false)) return;
                SessionState.SetBool("trka-setup-done", true);
                if (NeedsSetup()) Run(false);
            };
        }

        static bool NeedsSetup() =>
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath) == null
            || GraphicsSettings.defaultRenderPipeline == null
            || !File.Exists(ScenePath);

        [MenuItem("Trka znanja/Podesi projekat (URP, scena, build)", priority = 1)]
        public static void Menu() => Run(true);

        // Za CI: Unity -batchmode -executeMethod TrkaZnanja.EditorTools.ProjectSetup.RunBatch -quit
        public static void RunBatch() => Run(false);

        public static void Run(bool interactive)
        {
            Directory.CreateDirectory(Dir);
            var asset = CreateOrLoadUrp();
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);

            ConfigurePlayer();
            CreateScene(interactive);
            AssetDatabase.SaveAssets();
            Debug.Log("Trka znanja: projekat podešen (URP, scena Igra, build lista, polegnut ekran). Pritisni Play.");
            if (interactive && !Application.isBatchMode)
                EditorUtility.DisplayDialog("Trka znanja", "Projekat je podešen.\n\nOtvori scenu Assets/TrkaZnanja/Scenes/Igra i pritisni Play.", "U redu");
        }

        static UniversalRenderPipelineAsset CreateOrLoadUrp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (asset == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                var ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (ppd != null) renderer.postProcessData = ppd;
                AssetDatabase.CreateAsset(renderer, RendererPath);
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            asset.renderScale = 1f;
            asset.shadowDistance = 55f;
            asset.shadowCascadeCount = 2;
            asset.supportsCameraDepthTexture = true;
            var so = new SerializedObject(asset);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1);       // po pikselu (svjetlo u garaži)
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void SetBool(SerializedObject so, string name, bool v) { var p = so.FindProperty(name); if (p != null) p.boolValue = v; }
        static void SetInt(SerializedObject so, string name, int v) { var p = so.FindProperty(name); if (p != null) p.intValue = v; }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Mladen";
            PlayerSettings.productName = "Trka znanja";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.runInBackground = false;
            PlayerSettings.bundleVersion = "0.1.0";
        }

        static void CreateScene(bool interactive)
        {
            if (!File.Exists(ScenePath))
            {
                if (interactive && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Trka znanja (pokretanje)");
                go.AddComponent<Bootstrap>();
                SceneManager.MoveGameObjectToScene(go, scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }

    // Uvoz: zastave sa mipmapama (za zamućenje), fontovi sa rezervnim fontovima sistema.
    public sealed class TrkaImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Zastave/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.maxTextureSize = 256;
            ti.alphaIsTransparency = true;
        }

        void OnPreprocessAsset()
        {
            if (!assetPath.EndsWith(".ttf") || !assetPath.Contains("/TrkaZnanja/")) return;
            if (assetImporter is TrueTypeFontImporter fi)
            {
                fi.includeFontData = true;
                fi.fontNames = new[] { "Nunito", "Roboto", "Noto Sans", "Arial", "Segoe UI Symbol", "Arial Unicode MS" };
            }
        }
    }
}
