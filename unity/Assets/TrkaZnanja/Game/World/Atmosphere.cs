// Atmosfera: dan, zalazak, noć, kiša i snijeg. Postavlja nebo, sunce, ambijent, maglu, vodu, post-processing i padavine.
using UnityEngine;
using UnityEngine.Rendering;

namespace TrkaZnanja.Game
{
    public sealed class AtmospherePreset
    {
        public string Id;
        public Color SkyTop, SkyHorizon, SkyBottom, Sun, AmbientSky, AmbientEquator, AmbientGround, Fog, ShadowTint;
        public Color WaterDeep, WaterShallow, Grass, GrassDry, Sand, Rock, Snow;
        public float SunIntensity, SunPitch, SunYaw, Stars, Glow, FogStart, FogEnd, Exposure, Saturation, Bloom, Temperature;
        public float SnowLine = 3.3f;
        public string Weather;          // null, "kisa", "snijeg"

        public static AtmospherePreset Get(string id)
        {
            switch (id)
            {
                case "zalazak": return Sunset();
                case "noc": return Night();
                case "kisa": return Rain();
                case "snijeg": return SnowDay();
                default: return Day();
            }
        }

        static AtmospherePreset Base() => new AtmospherePreset
        {
            WaterDeep = Col.Hex("#0b5fa0"), WaterShallow = Col.Hex("#36c2cf"),
            Grass = Col.Hex("#7fc95a"), GrassDry = Col.Hex("#b6cf5e"), Sand = Col.Hex("#f0dca6"), Rock = Col.Hex("#9a9384"), Snow = Col.Hex("#fbfdff"),
            ShadowTint = Col.Hex("#6a78b8"), Exposure = 0, Saturation = 18, Bloom = 0.6f, Temperature = 0,
        };

        public static AtmospherePreset Day()
        {
            var p = Base();
            p.Id = "dan";
            p.SkyTop = Col.Hex("#3d8ee8"); p.SkyHorizon = Col.Hex("#bfe6ff"); p.SkyBottom = Col.Hex("#8cc6e6");
            p.Sun = Col.Hex("#fff3dc"); p.SunIntensity = 1.35f; p.SunPitch = 48; p.SunYaw = -35;
            p.AmbientSky = Col.Hex("#a9d4ff"); p.AmbientEquator = Col.Hex("#cfe6d8"); p.AmbientGround = Col.Hex("#6f8f55");
            p.Fog = Col.Hex("#cfe9fb"); p.FogStart = 45; p.FogEnd = 150;
            return p;
        }

        public static AtmospherePreset Sunset()
        {
            var p = Base();
            p.Id = "zalazak";
            p.SkyTop = Col.Hex("#4a5fb8"); p.SkyHorizon = Col.Hex("#ffb27a"); p.SkyBottom = Col.Hex("#d88a7a");
            p.Sun = Col.Hex("#ffb070"); p.SunIntensity = 1.25f; p.SunPitch = 14; p.SunYaw = -70;
            p.AmbientSky = Col.Hex("#8f8fd8"); p.AmbientEquator = Col.Hex("#f2a98a"); p.AmbientGround = Col.Hex("#5d5a58");
            p.Fog = Col.Hex("#f2b39a"); p.FogStart = 40; p.FogEnd = 140;
            p.WaterDeep = Col.Hex("#26407e"); p.WaterShallow = Col.Hex("#e08a7a");
            p.ShadowTint = Col.Hex("#6550a8"); p.Temperature = 18; p.Bloom = 0.9f;
            return p;
        }

        public static AtmospherePreset Night()
        {
            var p = Base();
            p.Id = "noc";
            p.SkyTop = Col.Hex("#050a24"); p.SkyHorizon = Col.Hex("#1f3168"); p.SkyBottom = Col.Hex("#0b1230");
            p.Sun = Col.Hex("#9fb8ff"); p.SunIntensity = 0.55f; p.SunPitch = 40; p.SunYaw = 30;   // mjesečina
            p.AmbientSky = Col.Hex("#33467e"); p.AmbientEquator = Col.Hex("#26345c"); p.AmbientGround = Col.Hex("#141a2a");
            p.Fog = Col.Hex("#16224a"); p.FogStart = 35; p.FogEnd = 120;
            p.WaterDeep = Col.Hex("#0a1d44"); p.WaterShallow = Col.Hex("#1f4e7a");
            p.Stars = 1; p.Glow = 1; p.ShadowTint = Col.Hex("#2a3a7a"); p.Bloom = 1.4f; p.Saturation = 10; p.Temperature = -12;
            return p;
        }

        public static AtmospherePreset Rain()
        {
            var p = Base();
            p.Id = "kisa";
            p.SkyTop = Col.Hex("#5d6f84"); p.SkyHorizon = Col.Hex("#a9b6c2"); p.SkyBottom = Col.Hex("#7d8b97");
            p.Sun = Col.Hex("#dfe6ee"); p.SunIntensity = 0.8f; p.SunPitch = 60; p.SunYaw = -20;
            p.AmbientSky = Col.Hex("#93a4b8"); p.AmbientEquator = Col.Hex("#8d9aa3"); p.AmbientGround = Col.Hex("#4b5a48");
            p.Fog = Col.Hex("#a3afba"); p.FogStart = 25; p.FogEnd = 110;
            p.WaterDeep = Col.Hex("#2a5577"); p.WaterShallow = Col.Hex("#5f9aa8");
            p.Grass = Col.Hex("#5fae55"); p.Weather = "kisa"; p.Saturation = 0; p.Glow = 0.35f;
            return p;
        }

        public static AtmospherePreset SnowDay()
        {
            var p = Base();
            p.Id = "snijeg";
            p.SkyTop = Col.Hex("#7fa6d6"); p.SkyHorizon = Col.Hex("#e4eef7"); p.SkyBottom = Col.Hex("#cfdbe6");
            p.Sun = Col.Hex("#fff8ee"); p.SunIntensity = 1.1f; p.SunPitch = 30; p.SunYaw = -50;
            p.AmbientSky = Col.Hex("#c8dcf2"); p.AmbientEquator = Col.Hex("#dfe7ee"); p.AmbientGround = Col.Hex("#9aa7b0");
            p.Fog = Col.Hex("#e6eef5"); p.FogStart = 30; p.FogEnd = 120;
            p.Grass = Col.Hex("#9cc7a0"); p.GrassDry = Col.Hex("#dfe9e6"); p.SnowLine = 1.7f; p.Weather = "snijeg"; p.Saturation = 5;
            return p;
        }

        // Postavi nebo, svjetlo i maglu u sceni.
        public void Apply(Light sun)
        {
            var sky = Mats.Sky;
            sky.SetColor("_TopColor", SkyTop);
            sky.SetColor("_HorizonColor", SkyHorizon);
            sky.SetColor("_BottomColor", SkyBottom);
            sky.SetColor("_SunColor", Sun * (Id == "noc" ? 0.6f : 1f));
            sky.SetFloat("_Stars", Stars);
            sky.SetFloat("_SunSize", Id == "noc" ? 0.012f : Id == "zalazak" ? 0.05f : 0.03f);
            RenderSettings.skybox = sky;

            sun.color = Sun;
            sun.intensity = SunIntensity;
            sun.transform.rotation = Quaternion.Euler(SunPitch, SunYaw, 0);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = Id == "kisa" ? 0.55f : 0.85f;
            var toSun = -sun.transform.forward;
            sky.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0));

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            // ambijent za šejder (sferni harmonici): nebo odozgo, tlo odozdo, horizont sa strane
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(AmbientEquator);
            var up = new Color(AmbientSky.r - AmbientEquator.r, AmbientSky.g - AmbientEquator.g, AmbientSky.b - AmbientEquator.b);
            var down = new Color(AmbientGround.r - AmbientEquator.r, AmbientGround.g - AmbientEquator.g, AmbientGround.b - AmbientEquator.b);
            sh.AddDirectionalLight(Vector3.up, up, 0.9f);
            sh.AddDirectionalLight(Vector3.down, down, 0.9f);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Fog;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;

            Shader.SetGlobalFloat("_TrkaGlow", Glow);
            Shader.SetGlobalColor("_TrkaGlowColor", Col.Hex("#ffd27a"));

            var w = Mats.Water;
            w.SetColor("_DeepColor", WaterDeep);
            w.SetColor("_ShallowColor", WaterShallow);
            w.SetColor("_SkyColor", Color.Lerp(SkyHorizon, SkyTop, 0.3f));
            w.SetFloat("_WaveHeight", Weather == "kisa" ? 0.07f : 0.045f);
        }
    }
}
