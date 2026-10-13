// Efekti: vatromet, konfete i iskre (proslava na cilju, pogodak na stanici).
using System.Collections;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class Effects
    {
        static readonly Color[] Party = { Col.Hex("#e2283e"), Col.Hex("#2b7de9"), Col.Hex("#f6bd2a"), Col.Hex("#2fbf6b"), Col.Hex("#9b4bd6"), Col.Hex("#ff6fae"), Color.white };

        static ParticleSystem NewSystem(string name, Vector3 pos, Transform parent, bool additive)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(additive);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;
            return ps;
        }

        // Konfete: šareni papirići padaju i vrte se.
        public static ParticleSystem Confetti(Vector3 pos, Transform parent, float seconds = 4)
        {
            var ps = NewSystem("Konfete", pos, parent, false);
            var main = ps.main;
            main.duration = seconds;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.gravityModifier = 0.35f;
            main.maxParticles = 600;
            var grad = new Gradient();
            var keys = new GradientColorKey[Party.Length > 8 ? 8 : Party.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(Party[i], i / (float)(keys.Length - 1));
            grad.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 160), new ParticleSystem.Burst(0.6f, 90), new ParticleSystem.Burst(1.4f, 90) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 35;
            sh.radius = 0.3f;
            sh.rotation = new Vector3(-90, 0, 0);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6, 6);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(Mats.Particle(false)) { mainTexture = Tex.Square };
            ps.Play();
            Object.Destroy(ps.gameObject, seconds + 5);
            return ps;
        }

        // Jedna raketa vatrometa: penje se i eksplodira u šarene iskre (svijetle, za bloom).
        public static void Firework(Vector3 pos, Transform parent, Color color)
        {
            var ps = NewSystem("Vatromet", pos, parent, true);
            var main = ps.main;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
            main.gravityModifier = 0.45f;
            main.maxParticles = 300;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 140) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.25f;
            trails.widthOverTrail = 0.4f;
            ps.GetComponent<ParticleSystemRenderer>().trailMaterial = Mats.Particle(true);
            ps.Play();
            Object.Destroy(ps.gameObject, 4);
        }

        public static IEnumerator FireworkShow(Vector3 center, Transform parent, float seconds)
        {
            float t = 0;
            while (t < seconds)
            {
                var p = center + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(2.5f, 4.2f), Random.Range(-2.5f, 2.5f));
                Firework(p, parent, Party[Random.Range(0, Party.Length)]);
                AudioKit.I?.Play("vatromet", 0.5f);
                float wait = Random.Range(0.25f, 0.6f);
                t += wait;
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        // Iskre (npr. kad dobiješ polja ili stigneš na stanicu).
        public static void Sparkle(Vector3 pos, Transform parent, Color color)
        {
            var ps = NewSystem("Iskre", pos, parent, true);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = color;
            main.gravityModifier = -0.1f;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 40) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.2f;
            ps.Play();
            Object.Destroy(ps.gameObject, 2);
        }
    }
}
