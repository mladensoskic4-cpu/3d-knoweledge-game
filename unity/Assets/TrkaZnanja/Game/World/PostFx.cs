// Post-processing (URP): bloom, ACES tonemapping, boje, vinjeta i zamućenje pozadine dok je pitanje na ekranu.
// I kvalitet grafike: Auto / Nisko / Srednje / Visoko (rezolucija, MSAA, sjenke, efekti).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrkaZnanja.Game
{
    public sealed class PostFx : MonoBehaviour
    {
        Volume volume;
        VolumeProfile profile;
        Bloom bloom;
        ColorAdjustments color;
        Vignette vignette;
        DepthOfField dof;
        WhiteBalance white;
        float dofWeight, dofTarget;
        public static string Level { get; private set; } = "visoko";
        static UniversalRenderPipelineAsset runtimeAsset;

        public static PostFx Create(Transform parent, Camera cam)
        {
            var go = new GameObject("Post-processing");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<PostFx>();
            fx.volume = go.AddComponent<Volume>();
            fx.volume.isGlobal = true;
            fx.volume.priority = 10;
            fx.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            fx.volume.sharedProfile = fx.profile;

            var tone = fx.profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            fx.bloom = fx.profile.Add<Bloom>(true);
            fx.bloom.threshold.Override(0.95f);
            fx.bloom.scatter.Override(0.65f);
            fx.color = fx.profile.Add<ColorAdjustments>(true);
            fx.color.contrast.Override(12f);
            fx.vignette = fx.profile.Add<Vignette>(true);
            fx.vignette.intensity.Override(0.22f);
            fx.vignette.smoothness.Override(0.45f);
            fx.white = fx.profile.Add<WhiteBalance>(true);
            fx.dof = fx.profile.Add<DepthOfField>(true);
            fx.dof.mode.Override(DepthOfFieldMode.Gaussian);
            fx.dof.gaussianStart.Override(5f);
            fx.dof.gaussianEnd.Override(14f);
            fx.dof.gaussianMaxRadius.Override(1.2f);
            fx.dof.active = false;

            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.requiresDepthTexture = true;
            }
            return fx;
        }

        public void Apply(AtmospherePreset a)
        {
            bloom.intensity.Override(Level == "nisko" ? 0 : a.Bloom);
            bloom.tint.Override(a.Id == "noc" ? new Color(1f, 0.9f, 0.75f) : Color.white);
            color.saturation.Override(a.Saturation);
            color.postExposure.Override(a.Exposure);
            white.temperature.Override(a.Temperature);
        }

        // Zamuti pozadinu dok je kartica sa pitanjem otvorena (staza se vidi, ali ne smeta čitanju).
        public void BlurBackground(bool on) => dofTarget = on && Level != "nisko" ? 1 : 0;

        void Update()
        {
            dofWeight = Mathf.MoveTowards(dofWeight, dofTarget, Time.unscaledDeltaTime * 2.5f);
            dof.active = dofWeight > 0.01f;
            dof.gaussianMaxRadius.Override(Mathf.Lerp(0.2f, 1.3f, dofWeight));
            dof.gaussianStart.Override(Mathf.Lerp(30, 4.6f, dofWeight));
        }

        // ---- kvalitet grafike ----
        public static string AutoLevel()
        {
            if (!Application.isMobilePlatform) return "visoko";
            int mem = SystemInfo.systemMemorySize;
            if (mem >= 6000) return "visoko";
            if (mem >= 3500) return "srednje";
            return "nisko";
        }

        public static void ApplyQuality(string setting)
        {
            Level = setting == "nisko" || setting == "srednje" || setting == "visoko" ? setting : AutoLevel();
            Application.targetFrameRate = Level == "nisko" ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (current == null) return;     // URP nije uključen (vidi PROCITAJ.md)
            if (runtimeAsset == null)
            {
                runtimeAsset = Object.Instantiate(current);
                runtimeAsset.name = "Trka URP (runtime)";
                QualitySettings.renderPipeline = runtimeAsset;
            }
            var a = runtimeAsset;
            switch (Level)
            {
                case "nisko":
                    a.renderScale = Application.isMobilePlatform ? 0.75f : 0.9f;
                    a.msaaSampleCount = 1;
                    a.shadowDistance = 25;
                    a.shadowCascadeCount = 1;
                    break;
                case "srednje":
                    a.renderScale = Application.isMobilePlatform ? 0.9f : 1f;
                    a.msaaSampleCount = 2;
                    a.shadowDistance = 40;
                    a.shadowCascadeCount = 2;
                    break;
                default:
                    a.renderScale = 1f;
                    a.msaaSampleCount = 4;
                    a.shadowDistance = 55;
                    a.shadowCascadeCount = 2;
                    break;
            }
            a.supportsHDR = Level != "nisko";
        }
    }
}
