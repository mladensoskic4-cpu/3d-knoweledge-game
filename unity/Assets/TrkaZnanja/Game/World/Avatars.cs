// Igrači: životinja (15 vrsta) u vozilu (12 vrsta), sa kapom i naočarima. Sve se modeluje u kodu, stilizovano,
// kao igračke. Model gleda naprijed (+z). AvatarRig animira točkove, poskakivanje, propeler i ples na pobjedi.
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class Avatars
    {
        public static GameObject Build(Look look, Transform parent, string name = "Igrač")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<AvatarRig>();
            var vehicle = Catalog.VehicleOf(look.Vozilo);
            var animal = Catalog.AnimalOf(look.Zivotinja);
            var paint = Col.Hex(string.IsNullOrEmpty(look.Boja) ? vehicle.DefaultPaint : look.Boja);

            var bodyGo = new GameObject("Tijelo");
            bodyGo.transform.SetParent(root.transform, false);
            rig.Body = bodyGo.transform;

            var kit = new MeshKit();
            var wheels = new List<(Vector3 pos, float r, float w)>();
            var spots = new List<DecoSpot>();
            Vector3 seat; float seatScale;
            Vehicles.Draw(kit, vehicle.Id, paint, wheels, out seat, out seatScale, rig, spots);
            Decor.Draw(kit, spots, look);
            kit.Push(seat, Quaternion.identity, Vector3.one * seatScale);
            Animals.Draw(kit, animal, look);
            kit.Pop();
            kit.ToObject("Model", bodyGo.transform);

            foreach (var w in wheels)
            {
                var wk = new MeshKit();
                wk.Wheel(Vector3.zero, w.r, w.w, MeshKit.C("#23262c"), MeshKit.C("#d9dde2"));
                // žbica da se vidi okretanje
                wk.Box(Vector3.zero, new Vector3(w.w + 0.012f, w.r * 1.5f, w.r * 0.18f), MeshKit.C("#9aa1aa"));
                var wg = wk.ToObject("Točak", bodyGo.transform);
                wg.transform.localPosition = w.pos;
                rig.Wheels.Add(wg.transform);
                rig.WheelRadius.Add(w.r);
            }
            rig.Flies = vehicle.Flies;
            rig.Floats = vehicle.Floats;
            rig.Bounce = (float)vehicle.Bounce;
            Trails.Attach(rig, look.Trag);
            return root;
        }
    }

    public sealed class AvatarRig : MonoBehaviour
    {
        public Transform Body, Propeller, Balloon;
        public readonly List<Transform> Wheels = new List<Transform>();
        public readonly List<float> WheelRadius = new List<float>();
        public bool Flies, Floats;
        public float Bounce = 0.25f;
        public float Speed;            // trenutna brzina (jedinica/s), postavlja RacerView
        public bool Dancing;
        public ParticleSystem Trail;   // trag u vožnji (ukras iz garaže)
        public bool Preview;           // garaža i meni: trag se pušta lagano i kad vozilo stoji
        float phase, dance;
        Vector3 bodyBase;

        void Start() { bodyBase = Body != null ? Body.localPosition : Vector3.zero; phase = Random.value * 10; }

        void Update()
        {
            float dt = Time.deltaTime;
            phase += dt;
            for (int i = 0; i < Wheels.Count; i++)
                Wheels[i].localRotation *= Quaternion.Euler(Speed * dt / Mathf.Max(0.02f, WheelRadius[i]) * Mathf.Rad2Deg, 0, 0);
            if (Propeller != null) Propeller.localRotation *= Quaternion.Euler(0, 0, (600 + Speed * 400) * dt);
            if (Trail != null)
            {
                var em = Trail.emission;
                em.rateOverTime = Speed > 0.1f ? 26 + Speed * 6 : Preview ? 7 : 0;
            }
            if (Body == null) return;

            float lift = Flies ? 0.9f + Mathf.Sin(phase * 1.6f) * 0.06f : 0;
            float bob = Mathf.Abs(Mathf.Sin(phase * (Speed > 0.1f ? 14 : 3))) * (Speed > 0.1f ? 0.03f * Bounce * 4 : 0.006f);
            float sway = Floats ? Mathf.Sin(phase * 2.2f) * 4 : 0;
            if (Dancing)
            {
                dance += dt;
                float jump = Mathf.Abs(Mathf.Sin(dance * 5)) * 0.18f;
                Body.localPosition = bodyBase + new Vector3(0, lift + jump, 0);
                Body.localRotation = Quaternion.Euler(0, dance * 220, Mathf.Sin(dance * 10) * 10);
                return;
            }
            dance = 0;
            Body.localPosition = bodyBase + new Vector3(0, lift + bob, 0);
            Body.localRotation = Quaternion.Euler(Speed > 0.1f ? -3 : 0, 0, sway);
            if (Balloon != null) Balloon.localRotation = Quaternion.Euler(Mathf.Sin(phase) * 3, 0, Mathf.Cos(phase * 0.8f) * 3);
        }
    }

    // ---------------- vozila ----------------
    static class Vehicles
    {
        static Color32 C(string h, float glow = 0) => MeshKit.C(h, glow);
        static Color32 P(Color c, float k = 1) => MeshKit.C(c * k);

        // seat = gdje sjedi životinja (dno trupa), seatScale = veličina životinje
        public static void Draw(MeshKit k, string id, Color paint, List<(Vector3, float, float)> wheels, out Vector3 seat, out float seatScale, AvatarRig rig, List<DecoSpot> spots)
        {
            var dark = C("#2b3038");
            var chrome = C("#dfe4ea");
            var lamp = C("#fff6c8", 0.9f);
            seatScale = 1;
            switch (id)
            {
                case "bicikl":
                    {
                        var frame = P(paint);
                        Tube(k, new Vector3(0, 0.12f, -0.16f), new Vector3(0, 0.24f, 0.0f), 0.012f, frame);
                        Tube(k, new Vector3(0, 0.24f, 0.0f), new Vector3(0, 0.12f, 0.16f), 0.012f, frame);
                        Tube(k, new Vector3(0, 0.12f, -0.16f), new Vector3(0, 0.12f, 0.16f), 0.012f, frame);
                        Tube(k, new Vector3(0, 0.24f, 0.0f), new Vector3(0, 0.27f, -0.02f), 0.01f, dark);
                        Tube(k, new Vector3(0, 0.12f, 0.16f), new Vector3(0, 0.29f, 0.14f), 0.012f, frame);
                        Tube(k, new Vector3(-0.07f, 0.29f, 0.14f), new Vector3(0.07f, 0.29f, 0.14f), 0.01f, dark);
                        k.Box(new Vector3(0, 0.275f, -0.03f), new Vector3(0.05f, 0.015f, 0.08f), dark);
                        wheels.Add((new Vector3(0, 0.12f, -0.17f), 0.12f, 0.02f));
                        wheels.Add((new Vector3(0, 0.12f, 0.17f), 0.12f, 0.02f));
                        DecoSpot.Sides(spots, 0.016f, 0.165f, 0.0f, 0.12f, 0.07f, true);
                        seat = new Vector3(0, 0.27f, -0.03f); seatScale = 0.95f;
                        return;
                    }
                case "motor":
                    {
                        k.Sphere(new Vector3(0, 0.2f, 0.04f), new Vector3(0.07f, 0.06f, 0.12f), P(paint), 6, 10);
                        k.Box(new Vector3(0, 0.15f, -0.02f), new Vector3(0.08f, 0.08f, 0.2f), dark);
                        k.Box(new Vector3(0, 0.2f, -0.1f), new Vector3(0.09f, 0.03f, 0.14f), C("#1b1b1b"));
                        Tube(k, new Vector3(0, 0.12f, 0.17f), new Vector3(0, 0.27f, 0.12f), 0.012f, chrome);
                        Tube(k, new Vector3(-0.08f, 0.27f, 0.12f), new Vector3(0.08f, 0.27f, 0.12f), 0.01f, dark);
                        k.Sphere(new Vector3(0, 0.24f, 0.17f), 0.025f, lamp, 4, 6);
                        Tube(k, new Vector3(0.05f, 0.1f, -0.05f), new Vector3(0.05f, 0.12f, -0.22f), 0.012f, chrome);
                        wheels.Add((new Vector3(0, 0.1f, -0.17f), 0.1f, 0.05f));
                        wheels.Add((new Vector3(0, 0.1f, 0.17f), 0.1f, 0.04f));
                        DecoSpot.Sides(spots, 0.072f, 0.2f, 0.04f, 0.13f, 0.06f);
                        DecoSpot.Top(spots, 0, 0.262f, 0.05f, 0.1f, 0.06f, true);
                        seat = new Vector3(0, 0.21f, -0.08f);
                        return;
                    }
                case "skuter":
                    {
                        k.Box(new Vector3(0, 0.08f, 0.02f), new Vector3(0.12f, 0.03f, 0.22f), P(paint));
                        k.Sphere(new Vector3(0, 0.14f, -0.12f), new Vector3(0.09f, 0.08f, 0.11f), P(paint), 6, 10);
                        k.Box(new Vector3(0, 0.2f, -0.1f), new Vector3(0.09f, 0.025f, 0.12f), C("#5a3a22"));
                        k.Box(new Vector3(0, 0.18f, 0.13f), new Vector3(0.13f, 0.2f, 0.03f), P(paint));
                        Tube(k, new Vector3(0, 0.28f, 0.14f), new Vector3(0, 0.3f, 0.12f), 0.012f, chrome);
                        Tube(k, new Vector3(-0.08f, 0.3f, 0.12f), new Vector3(0.08f, 0.3f, 0.12f), 0.01f, dark);
                        k.Sphere(new Vector3(0, 0.27f, 0.155f), 0.025f, lamp, 4, 6);
                        wheels.Add((new Vector3(0, 0.06f, -0.14f), 0.06f, 0.04f));
                        wheels.Add((new Vector3(0, 0.06f, 0.16f), 0.06f, 0.04f));
                        DecoSpot.Front(spots, 0, 0.19f, 0.146f, 0.12f, 0.16f);
                        DecoSpot.Sides(spots, 0.091f, 0.14f, -0.12f, 0.16f, 0.07f);
                        seat = new Vector3(0, 0.2f, -0.08f);
                        return;
                    }
                case "kolica":
                    {
                        k.RoundBox(new Vector3(0, 0.07f, 0), new Vector3(0.24f, 0.05f, 0.44f), 0.02f, P(paint));
                        k.Box(new Vector3(0, 0.07f, 0.24f), new Vector3(0.26f, 0.05f, 0.04f), dark);
                        k.Box(new Vector3(0, 0.12f, -0.07f), new Vector3(0.14f, 0.06f, 0.12f), C("#1b1b1b"));
                        Tube(k, new Vector3(0, 0.1f, 0.12f), new Vector3(0, 0.2f, 0.06f), 0.01f, dark);
                        k.Cylinder(new Vector3(0, 0.2f, 0.06f), 0.04f, 0.04f, 0.01f, 10, dark);
                        k.Box(new Vector3(0, 0.12f, -0.2f), new Vector3(0.22f, 0.03f, 0.03f), P(paint, 0.8f));
                        wheels.Add((new Vector3(-0.15f, 0.05f, -0.15f), 0.055f, 0.06f));
                        wheels.Add((new Vector3(0.15f, 0.05f, -0.15f), 0.055f, 0.06f));
                        wheels.Add((new Vector3(-0.14f, 0.045f, 0.15f), 0.045f, 0.05f));
                        wheels.Add((new Vector3(0.14f, 0.045f, 0.15f), 0.045f, 0.05f));
                        DecoSpot.Sides(spots, 0.122f, 0.07f, 0.0f, 0.4f, 0.045f);
                        DecoSpot.Top(spots, 0, 0.096f, 0.135f, 0.13f, 0.2f);
                        seat = new Vector3(0, 0.1f, -0.07f);
                        return;
                    }
                case "traktor":
                    {
                        k.RoundBox(new Vector3(0, 0.17f, 0.08f), new Vector3(0.16f, 0.13f, 0.22f), 0.025f, P(paint));
                        k.Box(new Vector3(0, 0.13f, -0.1f), new Vector3(0.18f, 0.06f, 0.16f), P(paint, 0.85f));
                        k.Box(new Vector3(0, 0.19f, -0.13f), new Vector3(0.1f, 0.04f, 0.08f), C("#1b1b1b"));
                        Tube(k, new Vector3(0.05f, 0.22f, 0.12f), new Vector3(0.05f, 0.36f, 0.12f), 0.012f, dark);
                        k.Box(new Vector3(0, 0.2f, 0.195f), new Vector3(0.12f, 0.06f, 0.01f), C("#9aa1aa"));
                        k.Sphere(new Vector3(-0.05f, 0.24f, 0.19f), 0.018f, lamp, 4, 6);
                        k.Sphere(new Vector3(0.05f, 0.24f, 0.19f), 0.018f, lamp, 4, 6);
                        wheels.Add((new Vector3(-0.14f, 0.12f, -0.12f), 0.12f, 0.06f));
                        wheels.Add((new Vector3(0.14f, 0.12f, -0.12f), 0.12f, 0.06f));
                        wheels.Add((new Vector3(-0.1f, 0.06f, 0.15f), 0.06f, 0.04f));
                        wheels.Add((new Vector3(0.1f, 0.06f, 0.15f), 0.06f, 0.04f));
                        DecoSpot.Sides(spots, 0.082f, 0.17f, 0.08f, 0.2f, 0.1f);
                        DecoSpot.Top(spots, 0, 0.236f, 0.08f, 0.2f, 0.14f);
                        seat = new Vector3(0, 0.21f, -0.12f);
                        return;
                    }
                case "kamion":
                    {
                        k.RoundBox(new Vector3(0, 0.17f, 0.14f), new Vector3(0.24f, 0.2f, 0.18f), 0.03f, P(paint));
                        k.Box(new Vector3(0, 0.22f, 0.235f), new Vector3(0.2f, 0.08f, 0.01f), C("#7fb8e0", 0.25f));
                        k.Box(new Vector3(0, 0.1f, -0.1f), new Vector3(0.24f, 0.05f, 0.34f), dark);
                        k.Box(new Vector3(0, 0.17f, -0.12f), new Vector3(0.22f, 0.12f, 0.28f), C("#e9e1d0"));
                        k.Box(new Vector3(0, 0.235f, -0.12f), new Vector3(0.23f, 0.015f, 0.29f), P(paint, 0.85f));
                        k.Sphere(new Vector3(-0.08f, 0.12f, 0.235f), 0.02f, lamp, 4, 6);
                        k.Sphere(new Vector3(0.08f, 0.12f, 0.235f), 0.02f, lamp, 4, 6);
                        k.Box(new Vector3(0, 0.08f, 0.235f), new Vector3(0.24f, 0.03f, 0.02f), chrome);
                        wheels.Add((new Vector3(-0.13f, 0.06f, 0.14f), 0.06f, 0.05f));
                        wheels.Add((new Vector3(0.13f, 0.06f, 0.14f), 0.06f, 0.05f));
                        wheels.Add((new Vector3(-0.13f, 0.06f, -0.15f), 0.06f, 0.05f));
                        wheels.Add((new Vector3(0.13f, 0.06f, -0.15f), 0.06f, 0.05f));
                        DecoSpot.Sides(spots, 0.112f, 0.17f, -0.12f, 0.26f, 0.11f);
                        DecoSpot.Top(spots, 0, 0.2435f, -0.12f, 0.27f, 0.2f);
                        seat = new Vector3(0, 0.26f, 0.12f); seatScale = 0.9f;   // vozač gleda kroz krovni otvor
                        return;
                    }
                case "fico":
                    {
                        k.RoundBox(new Vector3(0, 0.11f, 0), new Vector3(0.24f, 0.09f, 0.42f), 0.04f, P(paint));
                        k.Sphere(new Vector3(0, 0.16f, -0.02f), new Vector3(0.115f, 0.08f, 0.17f), P(paint), 7, 12);
                        k.Box(new Vector3(0, 0.18f, 0.08f), new Vector3(0.18f, 0.05f, 0.04f), C("#7fb8e0", 0.25f));
                        k.Sphere(new Vector3(-0.08f, 0.12f, 0.21f), 0.024f, lamp, 4, 6);
                        k.Sphere(new Vector3(0.08f, 0.12f, 0.21f), 0.024f, lamp, 4, 6);
                        k.Box(new Vector3(0, 0.07f, 0.215f), new Vector3(0.24f, 0.02f, 0.02f), chrome);
                        k.Box(new Vector3(0, 0.07f, -0.215f), new Vector3(0.24f, 0.02f, 0.02f), chrome);
                        wheels.Add((new Vector3(-0.12f, 0.055f, -0.13f), 0.055f, 0.04f));
                        wheels.Add((new Vector3(0.12f, 0.055f, -0.13f), 0.055f, 0.04f));
                        wheels.Add((new Vector3(-0.12f, 0.055f, 0.13f), 0.055f, 0.04f));
                        wheels.Add((new Vector3(0.12f, 0.055f, 0.13f), 0.055f, 0.04f));
                        DecoSpot.Sides(spots, 0.122f, 0.11f, 0.0f, 0.36f, 0.055f);
                        seat = new Vector3(0, 0.15f, -0.03f); seatScale = 0.95f;
                        return;
                    }
                case "zaprega":
                    {
                        // konj ispred, kola iza (životinja vozi kola)
                        Animals.Horse(k, new Vector3(0, 0, 0.2f), Col.Hex("#8b5a2b"), Col.Hex("#2b1d12"));
                        k.Box(new Vector3(0, 0.12f, -0.12f), new Vector3(0.22f, 0.06f, 0.26f), C("#9a6b3e"));
                        k.Box(new Vector3(-0.115f, 0.17f, -0.12f), new Vector3(0.01f, 0.06f, 0.26f), C("#7a5232"));
                        k.Box(new Vector3(0.115f, 0.17f, -0.12f), new Vector3(0.01f, 0.06f, 0.26f), C("#7a5232"));
                        Tube(k, new Vector3(-0.06f, 0.12f, 0.0f), new Vector3(-0.05f, 0.14f, 0.2f), 0.008f, C("#7a5232"));
                        Tube(k, new Vector3(0.06f, 0.12f, 0.0f), new Vector3(0.05f, 0.14f, 0.2f), 0.008f, C("#7a5232"));
                        wheels.Add((new Vector3(-0.14f, 0.09f, -0.13f), 0.09f, 0.025f));
                        wheels.Add((new Vector3(0.14f, 0.09f, -0.13f), 0.09f, 0.025f));
                        DecoSpot.Sides(spots, 0.122f, 0.17f, -0.12f, 0.24f, 0.055f);
                        seat = new Vector3(0, 0.15f, -0.14f); seatScale = 0.85f;
                        return;
                    }
                case "brod":
                    {
                        // trup: suženi pramac
                        k.Box(new Vector3(0, 0.08f, -0.05f), new Vector3(0.24f, 0.1f, 0.34f), P(paint));
                        k.Push(new Vector3(0, 0.08f, 0.12f), Quaternion.Euler(0, 45, 0));
                        k.Box(Vector3.zero, new Vector3(0.17f, 0.1f, 0.17f), P(paint));
                        k.Pop();
                        k.Box(new Vector3(0, 0.035f, 0), new Vector3(0.25f, 0.03f, 0.5f), C("#2b7de9"));
                        k.Box(new Vector3(0, 0.135f, -0.05f), new Vector3(0.2f, 0.01f, 0.3f), C("#c59a6a"));
                        k.Box(new Vector3(0, 0.2f, 0.06f), new Vector3(0.16f, 0.08f, 0.01f), C("#7fb8e0", 0.25f));
                        k.Sphere(new Vector3(0, 0.2f, -0.24f), new Vector3(0.04f, 0.05f, 0.04f), C("#3b3b3b"), 4, 6);
                        DecoSpot.Sides(spots, 0.122f, 0.085f, -0.05f, 0.32f, 0.08f);
                        seat = new Vector3(0, 0.14f, -0.06f);
                        return;
                    }
                case "balon":
                    {
                        k.Box(new Vector3(0, 0.08f, 0), new Vector3(0.2f, 0.14f, 0.2f), C("#a87a45"));
                        k.Box(new Vector3(0, 0.155f, 0), new Vector3(0.22f, 0.02f, 0.22f), C("#7a5232"));
                        for (int i = 0; i < 4; i++)
                        {
                            var x = (i % 2 == 0 ? -1 : 1) * 0.09f; var z = (i < 2 ? -1 : 1) * 0.09f;
                            Tube(k, new Vector3(x, 0.16f, z), new Vector3(x * 1.6f, 0.52f, z * 1.6f), 0.004f, C("#5a3a22"));
                        }
                        var bal = new MeshKit();
                        for (int s = 0; s < 8; s++)
                        {
                            var col = s % 2 == 0 ? P(paint) : C("#ffd34d");
                            bal.Push(Vector3.zero, Quaternion.Euler(0, s * 45, 0));
                            bal.Sphere(new Vector3(0, 0, 0), new Vector3(0.15f, 0.3f, 0.3f), col, 8, 4);
                            bal.Pop();
                        }
                        bal.Cylinder(new Vector3(0, -0.3f, 0), 0.06f, 0.13f, 0.08f, 10, P(paint, 0.8f), false);
                        var go = bal.ToObject("Balon", rig.transform);
                        go.transform.localPosition = new Vector3(0, 0.82f, 0);
                        rig.Balloon = go.transform;
                        DecoSpot.Sides(spots, 0.102f, 0.08f, 0, 0.18f, 0.12f);
                        seat = new Vector3(0, 0.12f, 0); seatScale = 0.85f;
                        return;
                    }
                case "avion":
                    {
                        k.Sphere(new Vector3(0, 0.15f, 0), new Vector3(0.07f, 0.07f, 0.24f), P(paint), 8, 12);
                        k.Box(new Vector3(0, 0.13f, 0.03f), new Vector3(0.56f, 0.02f, 0.12f), P(paint, 0.9f));
                        k.Box(new Vector3(0, 0.16f, -0.2f), new Vector3(0.18f, 0.015f, 0.06f), P(paint, 0.9f));
                        k.Box(new Vector3(0, 0.21f, -0.21f), new Vector3(0.015f, 0.08f, 0.06f), C("#e2283e"));
                        k.Box(new Vector3(-0.27f, 0.13f, 0.03f), new Vector3(0.03f, 0.025f, 0.12f), C("#e2283e"));
                        k.Box(new Vector3(0.27f, 0.13f, 0.03f), new Vector3(0.03f, 0.025f, 0.12f), C("#e2283e"));
                        k.Cone(new Vector3(0, 0.15f, 0.24f), 0.035f, 0.05f, 8, C("#e2283e"));
                        var prop = new MeshKit();
                        prop.Box(Vector3.zero, new Vector3(0.24f, 0.025f, 0.01f), dark);
                        var pg = prop.ToObject("Propeler", rig.transform);
                        pg.transform.localPosition = new Vector3(0, 0.15f, 0.27f);
                        rig.Propeller = pg.transform;
                        DecoSpot.Top(spots, 0.18f, 0.141f, 0.03f, 0.11f, 0.16f);
                        DecoSpot.Top(spots, -0.18f, 0.141f, 0.03f, 0.11f, 0.16f);
                        seat = new Vector3(0, 0.17f, -0.05f); seatScale = 0.9f;
                        return;
                    }
                default: // auto (kabriolet)
                    {
                        k.RoundBox(new Vector3(0, 0.1f, 0), new Vector3(0.26f, 0.09f, 0.46f), 0.04f, P(paint));
                        k.RoundBox(new Vector3(0, 0.15f, 0.12f), new Vector3(0.24f, 0.05f, 0.16f), 0.03f, P(paint));
                        k.RoundBox(new Vector3(0, 0.15f, -0.16f), new Vector3(0.24f, 0.05f, 0.1f), 0.03f, P(paint));
                        k.Box(new Vector3(0, 0.2f, 0.05f), new Vector3(0.22f, 0.06f, 0.012f), C("#9fd2f2", 0.2f));
                        k.Box(new Vector3(0, 0.125f, -0.04f), new Vector3(0.18f, 0.03f, 0.16f), C("#1b1b1b"));
                        k.Sphere(new Vector3(-0.08f, 0.11f, 0.23f), 0.024f, lamp, 4, 6);
                        k.Sphere(new Vector3(0.08f, 0.11f, 0.23f), 0.024f, lamp, 4, 6);
                        k.Sphere(new Vector3(-0.09f, 0.12f, -0.23f), 0.018f, C("#ff3b3b", 0.7f), 4, 6);
                        k.Sphere(new Vector3(0.09f, 0.12f, -0.23f), 0.018f, C("#ff3b3b", 0.7f), 4, 6);
                        k.Box(new Vector3(0, 0.065f, 0.235f), new Vector3(0.27f, 0.025f, 0.02f), chrome);
                        wheels.Add((new Vector3(-0.13f, 0.06f, -0.14f), 0.06f, 0.045f));
                        wheels.Add((new Vector3(0.13f, 0.06f, -0.14f), 0.06f, 0.045f));
                        wheels.Add((new Vector3(-0.13f, 0.06f, 0.14f), 0.06f, 0.045f));
                        wheels.Add((new Vector3(0.13f, 0.06f, 0.14f), 0.06f, 0.045f));
                        DecoSpot.Sides(spots, 0.132f, 0.1f, -0.01f, 0.36f, 0.055f);
                        DecoSpot.Top(spots, 0, 0.176f, 0.12f, 0.14f, 0.2f);
                        seat = new Vector3(0, 0.13f, -0.04f);
                        return;
                    }
            }
        }

        // Cijev između dvije tačke (ram bicikla, upravljač…).
        public static void Tube(MeshKit k, Vector3 a, Vector3 b, float r, Color32 col)
        {
            var d = b - a;
            k.Push(a, Quaternion.FromToRotation(Vector3.up, d.normalized));
            k.Cylinder(Vector3.zero, r, r, d.magnitude, 6, col, true);
            k.Pop();
        }
    }

    // ---------------- životinje ----------------
    static class Animals
    {
        static Color32 C(string h, float glow = 0) => MeshKit.C(h, glow);
        static Color32 P(Color c, float k = 1) => MeshKit.C(new Color(c.r * k, c.g * k, c.b * k));

        // Vozač: tijelo do pojasa + glava; (0,0,0) je sjedište, gleda +z.
        public static void Draw(MeshKit k, Animal a, Look look)
        {
            var body = Col.Hex(a.Body);
            var acc = Col.Hex(a.Accent);
            bool bird = a.Shape == "ptica";
            // tijelo i ruke (krila) prema volanu
            k.Sphere(new Vector3(0, 0.07f, 0), new Vector3(0.075f, 0.08f, 0.065f), P(body), 7, 10);
            k.Sphere(new Vector3(0, 0.06f, 0.035f), new Vector3(0.05f, 0.06f, 0.035f), P(acc), 6, 8);
            if (bird)
            {
                k.Sphere(new Vector3(-0.07f, 0.08f, 0.0f), new Vector3(0.025f, 0.06f, 0.05f), P(body, 0.85f), 5, 7);
                k.Sphere(new Vector3(0.07f, 0.08f, 0.0f), new Vector3(0.025f, 0.06f, 0.05f), P(body, 0.85f), 5, 7);
            }
            else
            {
                Vehicles.Tube(k, new Vector3(-0.06f, 0.1f, 0.0f), new Vector3(-0.04f, 0.09f, 0.09f), 0.018f, P(body));
                Vehicles.Tube(k, new Vector3(0.06f, 0.1f, 0.0f), new Vector3(0.04f, 0.09f, 0.09f), 0.018f, P(body));
            }

            // glava
            var head = new Vector3(0, 0.2f, 0.01f);
            float hr = a.Shape == "medvjed" || a.Shape == "panda" ? 0.075f : 0.068f;
            k.Sphere(head, new Vector3(hr, hr * 0.95f, hr), P(body), 8, 12);
            var face = head + new Vector3(0, -0.01f, hr * 0.8f);

            switch (a.Shape)
            {
                case "pas":
                    k.Sphere(face + new Vector3(0, -0.005f, 0.015f), new Vector3(0.035f, 0.028f, 0.04f), P(acc), 6, 8);
                    k.Sphere(face + new Vector3(0, 0.01f, 0.05f), 0.012f, C("#1b1b1b"), 4, 6);
                    if (a.Id == "kuce") { Ear(k, head, -1, body, true); Ear(k, head, 1, body, true); }
                    else { PointyEar(k, head, -1, body, acc, 0.05f); PointyEar(k, head, 1, body, acc, 0.05f); }
                    break;
                case "macka":
                    k.Sphere(face + new Vector3(0, -0.012f, 0.005f), new Vector3(0.03f, 0.02f, 0.025f), P(acc), 6, 8);
                    k.Sphere(face + new Vector3(0, 0.0f, 0.03f), 0.008f, C("#e86a8a"), 4, 6);
                    var big = a.Id == "lisica" ? 0.06f : 0.042f;
                    PointyEar(k, head, -1, body, acc, big); PointyEar(k, head, 1, body, acc, big);
                    if (a.Id == "lisica") k.Sphere(face + new Vector3(0, -0.01f, 0.03f), new Vector3(0.025f, 0.02f, 0.035f), P(body), 5, 7);
                    break;
                case "medvjed":
                case "panda":
                    k.Sphere(face + new Vector3(0, -0.012f, 0.0f), new Vector3(0.035f, 0.026f, 0.025f), P(a.Shape == "panda" ? Color.white : acc), 6, 8);
                    k.Sphere(face + new Vector3(0, 0.0f, 0.02f), 0.012f, C("#1b1b1b"), 4, 6);
                    var earCol = a.Shape == "panda" ? Col.Hex("#1d1d1f") : body;
                    k.Sphere(head + new Vector3(-0.055f, 0.055f, 0), 0.025f, P(earCol), 5, 7);
                    k.Sphere(head + new Vector3(0.055f, 0.055f, 0), 0.025f, P(earCol), 5, 7);
                    if (a.Shape == "panda")
                    {
                        k.Sphere(head + new Vector3(-0.028f, 0.012f, hr * 0.78f), new Vector3(0.02f, 0.025f, 0.012f), C("#1d1d1f"), 5, 7);
                        k.Sphere(head + new Vector3(0.028f, 0.012f, hr * 0.78f), new Vector3(0.02f, 0.025f, 0.012f), C("#1d1d1f"), 5, 7);
                    }
                    break;
                case "zec":
                    k.Sphere(face + new Vector3(0, -0.01f, 0.0f), new Vector3(0.028f, 0.02f, 0.02f), P(acc), 5, 7);
                    k.Sphere(face + new Vector3(0, 0.002f, 0.018f), 0.009f, C("#e86a8a"), 4, 6);
                    k.Push(head + new Vector3(-0.025f, 0.06f, -0.01f), Quaternion.Euler(-10, 0, 12));
                    k.Sphere(new Vector3(0, 0.06f, 0), new Vector3(0.018f, 0.07f, 0.012f), P(body), 5, 7);
                    k.Pop();
                    k.Push(head + new Vector3(0.025f, 0.06f, -0.01f), Quaternion.Euler(-10, 0, -12));
                    k.Sphere(new Vector3(0, 0.06f, 0), new Vector3(0.018f, 0.07f, 0.012f), P(body), 5, 7);
                    k.Pop();
                    break;
                case "ptica":
                    k.Push(face + new Vector3(0, -0.008f, -0.004f), Quaternion.Euler(78, 0, 0));   // kljun gleda naprijed
                    k.Cone(Vector3.zero, 0.02f, 0.05f, 6, C("#f2b630"));
                    k.Pop();
                    if (a.Id == "sova")
                    {
                        PointyEar(k, head, -1, body, acc, 0.03f); PointyEar(k, head, 1, body, acc, 0.03f);
                        k.Sphere(head + new Vector3(-0.026f, 0.012f, hr * 0.8f), 0.022f, P(acc), 5, 7);
                        k.Sphere(head + new Vector3(0.026f, 0.012f, hr * 0.8f), 0.022f, P(acc), 5, 7);
                    }
                    else if (a.Id == "orao") k.Sphere(head + new Vector3(0, 0.0f, 0), new Vector3(hr * 1.02f, hr * 0.97f, hr * 1.02f), P(Color.white), 8, 12);
                    break;
                case "koza":
                case "ovca":
                    k.Sphere(face + new Vector3(0, -0.02f, 0.0f), new Vector3(0.03f, 0.03f, 0.035f), P(a.Shape == "ovca" ? Col.Hex("#3b3534") : acc), 6, 8);
                    if (a.Shape == "koza")
                    {
                        Horn(k, head, -1); Horn(k, head, 1);
                        k.Cone(face + new Vector3(0, -0.07f, 0.0f), 0.012f, 0.035f, 5, P(acc));
                    }
                    else
                        for (int i = 0; i < 6; i++)
                            k.Sphere(head + new Vector3(Mathf.Cos(i) * 0.045f, 0.05f + Mathf.Sin(i * 2) * 0.01f, Mathf.Sin(i) * 0.03f - 0.01f), 0.03f, P(body), 5, 7);
                    Ear(k, head, -1, body, false); Ear(k, head, 1, body, false);
                    break;
                case "konj":
                    k.Sphere(face + new Vector3(0, -0.02f, 0.02f), new Vector3(0.03f, 0.03f, 0.05f), P(body, 0.92f), 6, 8);
                    if (a.Id == "magarac")
                        foreach (var sd in new[] { -1, 1 })
                        {
                            k.Push(head + new Vector3(sd * 0.03f, 0.055f, -0.01f), Quaternion.Euler(-5, 0, -sd * 22));
                            k.Sphere(new Vector3(0, 0.045f, 0), new Vector3(0.017f, 0.055f, 0.012f), P(body), 5, 7);
                            k.Pop();
                        }
                    else { PointyEar(k, head, -1, body, acc, 0.04f); PointyEar(k, head, 1, body, acc, 0.04f); }
                    for (int i = 0; i < 4; i++) k.Sphere(head + new Vector3(0, 0.05f - i * 0.03f, -0.05f), 0.02f, P(acc), 4, 6);
                    break;
                case "lav":
                    for (int i = 0; i < 12; i++)
                    {
                        var ang = i / 12f * Mathf.PI * 2;
                        k.Sphere(head + new Vector3(Mathf.Cos(ang) * 0.07f, Mathf.Sin(ang) * 0.07f, -0.015f), 0.032f, P(acc), 5, 7);
                    }
                    k.Sphere(face + new Vector3(0, -0.012f, 0.0f), new Vector3(0.032f, 0.024f, 0.022f), P(Col.Hex("#f6dca0")), 6, 8);
                    k.Sphere(face + new Vector3(0, 0.0f, 0.02f), 0.011f, C("#5a2e1a"), 4, 6);
                    break;
            }

            // oči
            float ey = 0.015f, ex = a.Id == "sova" ? 0.026f : 0.025f;
            foreach (var s in new[] { -1, 1 })
            {
                var e = head + new Vector3(s * ex, ey, hr * 0.84f);
                k.Sphere(e, 0.014f, C("#ffffff"), 5, 7);
                k.Sphere(e + new Vector3(0, 0, 0.009f), 0.008f, C("#141414"), 4, 6);
            }

            // kapa i naočare
            var top = head + new Vector3(0, hr * 0.85f, 0);
            switch (look.Kapa)
            {
                case "kacket":
                    k.Sphere(top, new Vector3(hr * 0.95f, hr * 0.5f, hr * 0.95f), C("#c8102e"), 6, 10);
                    k.Box(top + new Vector3(0, 0.0f, hr * 0.9f), new Vector3(hr * 1.2f, 0.008f, hr * 0.8f), C("#c8102e"));
                    break;
                case "sesir":
                    k.Cylinder(top + new Vector3(0, -0.005f, 0), hr * 1.6f, hr * 1.6f, 0.008f, 14, C("#3b2a1a"));
                    k.Cylinder(top, hr * 0.85f, hr * 0.75f, hr * 0.8f, 12, C("#3b2a1a"));
                    break;
                case "kapa":
                    k.Cylinder(top + new Vector3(0, -0.01f, 0), hr * 0.95f, hr * 0.95f, hr * 0.55f, 12, C("#111111"));
                    k.Disc(top + new Vector3(0, hr * 0.55f - 0.009f, 0), hr * 0.9f, 12, C("#c8102e"), true);
                    k.Disc(top + new Vector3(0, hr * 0.55f - 0.007f, 0), hr * 0.4f, 10, C("#d3ae3b"), true);
                    break;
                case "kaciga":
                    k.Sphere(top + new Vector3(0, -hr * 0.3f, 0), new Vector3(hr * 1.12f, hr * 0.85f, hr * 1.12f), C("#f2b630"), 7, 12);
                    break;
                case "kruna":
                    k.Cylinder(top, hr * 0.8f, hr * 0.85f, hr * 0.4f, 10, C("#f2c230", 0.2f), false);
                    for (int i = 0; i < 5; i++)
                    {
                        var ang = i / 5f * Mathf.PI * 2;
                        k.Cone(top + new Vector3(Mathf.Cos(ang) * hr * 0.8f, hr * 0.4f, Mathf.Sin(ang) * hr * 0.8f), 0.012f, 0.03f, 4, C("#f2c230", 0.2f));
                    }
                    break;
            }
            if (look.Naocare)
            {
                foreach (var s in new[] { -1, 1 })
                    k.Box(head + new Vector3(s * 0.026f, ey, hr * 0.93f), new Vector3(0.034f, 0.022f, 0.01f), C("#111111"));
                k.Box(head + new Vector3(0, ey + 0.004f, hr * 0.94f), new Vector3(0.02f, 0.005f, 0.008f), C("#111111"));
            }
        }

        static void PointyEar(MeshKit k, Vector3 head, int side, Color body, Color inner, float size)
        {
            k.Push(head + new Vector3(side * 0.04f, 0.05f, -0.005f), Quaternion.Euler(0, 0, -side * 18));
            k.Cone(Vector3.zero, size * 0.5f, size, 4, P(body));
            k.Cone(new Vector3(0, 0.002f, 0.008f), size * 0.3f, size * 0.7f, 4, P(inner));
            k.Pop();
        }

        static void Ear(MeshKit k, Vector3 head, int side, Color body, bool floppy)
        {
            k.Push(head + new Vector3(side * 0.065f, floppy ? 0.0f : 0.02f, 0), Quaternion.Euler(0, 0, side * (floppy ? 15 : 70)));
            k.Sphere(new Vector3(0, -0.03f, 0), new Vector3(0.016f, 0.04f, 0.026f), P(body, 0.85f), 5, 7);
            k.Pop();
        }

        static void Horn(MeshKit k, Vector3 head, int side)
        {
            k.Push(head + new Vector3(side * 0.03f, 0.055f, -0.01f), Quaternion.Euler(-35, 0, -side * 15));
            k.Cone(Vector3.zero, 0.012f, 0.06f, 5, C("#8a7a63"));
            k.Pop();
        }

        // Konj za zapregu (stoji na zemlji, gleda +z).
        public static void Horse(MeshKit k, Vector3 at, Color body, Color mane)
        {
            k.Push(at);
            k.Sphere(new Vector3(0, 0.16f, 0), new Vector3(0.06f, 0.06f, 0.11f), P(body), 7, 10);
            foreach (var x in new[] { -0.035f, 0.035f })
                foreach (var z in new[] { -0.07f, 0.07f })
                    k.Cylinder(new Vector3(x, 0, z), 0.014f, 0.016f, 0.13f, 6, P(body, 0.85f));
            Vehicles.Tube(k, new Vector3(0, 0.19f, 0.08f), new Vector3(0, 0.27f, 0.13f), 0.03f, P(body));
            k.Sphere(new Vector3(0, 0.28f, 0.16f), new Vector3(0.03f, 0.035f, 0.055f), P(body), 6, 8);
            for (int i = 0; i < 4; i++) k.Sphere(new Vector3(0, 0.29f - i * 0.025f, 0.11f - i * 0.02f), 0.015f, P(mane), 4, 6);
            k.Sphere(new Vector3(-0.02f, 0.3f, 0.19f), 0.008f, C("#141414"), 4, 6);
            k.Sphere(new Vector3(0.02f, 0.3f, 0.19f), 0.008f, C("#141414"), 4, 6);
            k.Pop();
        }
    }
}
