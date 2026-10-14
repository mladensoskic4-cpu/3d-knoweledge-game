// Trag koji vozilo ostavlja u vožnji (kupuje se u garaži): iskrice, srca, duga ili zvijezde.
// Čestice se puštaju samo dok se vozi; u garaži i meniju (Preview) lagano, da se vidi kako izgleda.
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class Trails
    {
        static readonly Dictionary<string, Texture2D> shapes = new Dictionary<string, Texture2D>();

        static Texture2D Shape(string icon)
        {
            if (shapes.TryGetValue(icon, out var t) && t != null) return t;
            t = MinigameData.ToTexture(Icons.Draw(icon, 64), "Trag " + icon, false);
            t.wrapMode = TextureWrapMode.Clamp;
            shapes[icon] = t;
            return t;
        }

        public static void Attach(AvatarRig rig, string id)
        {
            if (rig == null || string.IsNullOrEmpty(id) || id == "nema") return;
            var go = new GameObject("Trag");
            go.transform.SetParent(rig.transform, false);
            go.transform.localPosition = new Vector3(0, 0.12f, -0.24f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.gravityModifier = -0.03f;
            main.maxParticles = 200;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var em = ps.emission;
            em.rateOverTime = 0;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.06f;
            // čestice idu unazad (lokalno), pa trag teče iza vozila i kad stoji (garaža)
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            vel.z = new ParticleSystem.MinMaxCurve(-1.1f, -0.6f);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0.2f));
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0, 1) });
            fade.color = g;

            bool additive = false;
            string icon = "iskra";
            switch (id)
            {
                case "iskre":
                    additive = true;
                    main.startColor = new ParticleSystem.MinMaxGradient(Col.Hex("#ffd23f"), Col.Hex("#fff4b8"));
                    break;
                case "srca":
                    icon = "srce";
                    main.startColor = new ParticleSystem.MinMaxGradient(Col.Hex("#ff3b6b"), Col.Hex("#ff8fb0"));
                    main.gravityModifier = -0.08f;
                    break;
                case "duga":
                    {
                        icon = "krug";     // nepoznato ime = krug (Icons.Draw)
                        var rb = new Gradient();
                        rb.SetKeys(new[]
                        {
                            new GradientColorKey(Col.Hex("#ff3b3b"), 0), new GradientColorKey(Col.Hex("#ff9f1c"), 0.2f), new GradientColorKey(Col.Hex("#ffd23f"), 0.4f),
                            new GradientColorKey(Col.Hex("#3ddc97"), 0.6f), new GradientColorKey(Col.Hex("#2b7de9"), 0.8f), new GradientColorKey(Col.Hex("#9b4bd6"), 1),
                        }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                        main.startColor = new ParticleSystem.MinMaxGradient(rb) { mode = ParticleSystemGradientMode.RandomColor };
                        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
                        break;
                    }
                case "zvijezde":
                    icon = "zvijezda";
                    additive = true;
                    main.startColor = new ParticleSystem.MinMaxGradient(Col.Hex("#fff7d6"), Col.Hex("#ffd23f"));
                    break;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Mats.Particle(additive)) { name = "Trag " + id };
            mat.SetTexture("_MainTex", Shape(icon));
            r.sharedMaterial = mat;
            ps.Play();
            rig.Trail = ps;
        }
    }
}
