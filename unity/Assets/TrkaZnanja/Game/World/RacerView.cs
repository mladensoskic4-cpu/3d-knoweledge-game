// Figura jednog igrača na stazi: glatka vožnja po putu do ciljnog polja, prašina iza točkova,
// strelica iznad tvog vozila i ples na pobjedi.
using System.Collections;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class RacerView : MonoBehaviour
    {
        public Racer Racer { get; private set; }
        public AvatarRig Rig { get; private set; }
        TrackView track;
        float arc;                 // dužina puta od starta
        Vector3 lateral;           // pomak u stranu kad je više igrača na istom polju
        Transform marker;
        ParticleSystem dust;
        public bool Moving { get; private set; }

        public static RacerView Create(Racer r, TrackView track, Transform parent)
        {
            var go = new GameObject("Igrač · " + r.Name);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<RacerView>();
            v.Racer = r;
            v.track = track;
            var model = Avatars.Build(r.Look, go.transform, "Model");
            v.Rig = model.GetComponent<AvatarRig>();
            if (r.Human) v.AddMarker(Col.Hex(r.Color));
            v.AddDust();
            v.arc = track.ArcOfTile(r.Pos);
            v.Place();
            return v;
        }

        void AddMarker(Color c)
        {
            var k = new MeshKit();
            k.Push(Vector3.zero, Quaternion.Euler(180, 0, 0));
            k.Cone(Vector3.zero, 0.07f, 0.16f, 4, MeshKit.C(c, 0.4f));
            k.Pop();
            var m = k.ToObject("Strelica", transform, null, false);
            marker = m.transform;
        }

        void AddDust()
        {
            var go = new GameObject("Prašina");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0.03f, -0.2f);
            dust = go.AddComponent<ParticleSystem>();
            var main = dust.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new Color(0.92f, 0.86f, 0.74f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.gravityModifier = -0.05f;
            var em = dust.emission;
            em.rateOverTime = 0;
            var sh = dust.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.05f;
            var sz = dust.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var col = dust.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.6f, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(false);
            dust.Play();
        }

        void Place()
        {
            track.PoseAt(arc, out var p, out var fwd);
            transform.position = p + lateral;
            transform.rotation = Quaternion.LookRotation(fwd);
        }

        // Vožnja do polja `to`: ubrzanje, krivine, usporavanje. Brzina zavisi od dužine puta.
        public IEnumerator DriveTo(int to, float speedK = 1f)
        {
            float start = arc, end = track.ArcOfTile(to);
            if (end <= start + 1e-3f) yield break;
            Moving = true;
            var dustEm = dust.emission;
            dustEm.rateOverTime = Racer.Look != null && Catalog.VehicleOf(Racer.Look.Vozilo).Flies ? 0 : 40;
            var snd = Racer.Human ? AudioKit.I?.Vehicle(Racer.Look?.Vozilo) : null;
            float dist = end - start;
            float duration = Mathf.Clamp(0.55f + dist / 2.6f, 0.7f, 3.2f) / speedK;
            Vector3 lat0 = lateral;
            float t = 0, lastHop = -1;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = Tw.EaseInOut(k);
                float prev = arc;
                arc = Mathf.Lerp(start, end, e);
                lateral = Vector3.Lerp(lat0, Vector3.zero, Mathf.Clamp01(k * 3));
                Rig.Speed = (arc - prev) / Mathf.Max(1e-4f, Time.deltaTime);
                Place();
                // nagib u krivini
                track.PoseAt(arc + 0.3f, out _, out var ahead);
                var turn = Vector3.SignedAngle(transform.forward, ahead, Vector3.up);
                Rig.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-turn * 0.6f, -12, 12));
                // zvuk po pređenom polju
                if (snd != null && arc - lastHop > 1.2f) { lastHop = arc; snd.Hop(); }
                yield return null;
            }
            arc = end;
            Rig.Speed = 0;
            Rig.transform.localRotation = Quaternion.identity;
            dustEm.rateOverTime = 0;
            Place();
            Moving = false;
        }

        // Pomjeri u stranu (više igrača na istom polju).
        public IEnumerator SlideTo(Vector3 offset)
        {
            var from = lateral;
            yield return Tw.Run(0.25f, k => { lateral = Vector3.Lerp(from, offset, Tw.EaseInOut(k)); Place(); });
        }

        public void SetDancing(bool on) { if (Rig != null) Rig.Dancing = on; }

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;

        void Update()
        {
            if (marker != null)
            {
                marker.localPosition = new Vector3(0, 0.75f + Mathf.Sin(Time.time * 4) * 0.06f + (Rig != null && Rig.Flies ? 0.9f : 0), 0);
                marker.localRotation = Quaternion.Euler(0, Time.time * 120, 0);
            }
        }
    }
}
