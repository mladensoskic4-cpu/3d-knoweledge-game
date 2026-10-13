// Domaće znamenitosti u 3D (stilizovano): Sveti Stefan, Gospa od Škrpjela, Ostrog, Cetinjski manastir,
// most na Đurđevića Tari, Crno jezero, Milenijum most i Sahat kula. Postavljaju se na prave koordinate,
// malo odmaknute od staze da je ne zaklanjaju.
using System;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class Landmarks
    {
        sealed class Spot
        {
            public string Name; public double Lon, Lat; public float Scale; public bool OnWater;
            public Action<MeshKit, AtmospherePreset> Draw;
        }

        static readonly Spot[] Spots =
        {
            new Spot { Name = "Sveti Stefan", Lon = 18.8915, Lat = 42.2556, Scale = 1.0f, OnWater = true, Draw = SvetiStefan },
            new Spot { Name = "Gospa od Škrpjela", Lon = 18.6905, Lat = 42.4861, Scale = 0.9f, OnWater = true, Draw = Gospa },
            new Spot { Name = "Ostrog", Lon = 19.0302, Lat = 42.6750, Scale = 1.2f, Draw = Ostrog },
            new Spot { Name = "Cetinjski manastir", Lon = 18.9233, Lat = 42.3897, Scale = 1.0f, Draw = Cetinje },
            new Spot { Name = "Most na Đurđevića Tari", Lon = 19.2955, Lat = 43.1505, Scale = 1.3f, Draw = TaraBridge },
            new Spot { Name = "Crno jezero", Lon = 19.0890, Lat = 43.1460, Scale = 1.1f, Draw = BlackLake },
            new Spot { Name = "Milenijum most", Lon = 19.2443, Lat = 42.4442, Scale = 1.0f, Draw = Millennium },
            new Spot { Name = "Sahat kula", Lon = 19.2626, Lat = 42.4402, Scale = 0.9f, Draw = ClockTower },
        };

        public static readonly List<(string name, Vector3 pos)> Placed = new List<(string, Vector3)>();

        public static void Build(Transform parent, WorldView w)
        {
            Placed.Clear();
            if (w.Map.Id != "cg" && w.Map.Id != "balkan") return;
            float k = w.Map.Id == "cg" ? 1f : 0.75f;
            var used = new List<Vector3>();
            foreach (var s in Spots)
            {
                var p2 = w.Track.Proj.ToWorld(s.Lon, s.Lat);
                var pos = new Vector3((float)p2.X, 0, (float)p2.Y);
                if (!w.Bounds.Contains(new Vector3(pos.x, 0, pos.z))) continue;
                // odmakni od staze
                for (int i = 0; i < 12 && w.Track.DistanceToRoad(pos.x, pos.z) < 1.25 * k; i++)
                {
                    var away = AwayFromRoad(w.Track, pos);
                    pos += away * 0.25f;
                }
                // ne preklapaj druge znamenitosti
                foreach (var u in used)
                    if ((u - pos).Flat().magnitude < 1.4f * k) pos += (pos - u).Flat().normalized * 1.4f * k;
                used.Add(pos);
                bool water = !w.Terrain.IsLandAt(pos.x, pos.z);
                pos.y = water ? (float)w.Terrain.SeaLevel : w.GroundAt(pos);
                if (!s.OnWater && water) continue;     // npr. Ostrog u moru nema smisla na gruboj mapi
                var kit = new MeshKit();
                if (s.OnWater && water) Islet(kit, w.Atmo);
                s.Draw(kit, w.Atmo);
                var go = kit.ToObject(s.Name, parent);
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * s.Scale * k * 0.55f;
                go.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(pos.x, pos.z) * 30f, 0);
                Placed.Add((s.Name, pos + Vector3.up * 1.4f * k));
            }
        }

        static Vector3 AwayFromRoad(TrackLayout t, Vector3 p)
        {
            Vector3 best = Vector3.zero; double bd = double.MaxValue;
            foreach (var r in t.Road)
            {
                var d = (r.U() - p).Flat().sqrMagnitude;
                if (d < bd) { bd = d; best = r.U(); }
            }
            var away = (p - best).Flat();
            return away.sqrMagnitude < 1e-6f ? Vector3.right : away.normalized;
        }

        static Color32 C(string hex, float glow = 0) => MeshKit.C(hex, glow);

        static void Islet(MeshKit k, AtmospherePreset a)
        {
            k.Cylinder(new Vector3(0, -0.8f, 0), 1.45f, 1.2f, 0.95f, 16, MeshKit.C(a.Rock));
            k.Disc(new Vector3(0, 0.15f, 0), 1.2f, 16, MeshKit.C(a.Sand), true);
        }

        // Sveti Stefan: ostrvo prepuno kamenih kuća sa crvenim krovovima i nasip do obale.
        static void SvetiStefan(MeshKit k, AtmospherePreset a)
        {
            var rng = new Rng(3);
            k.Box(new Vector3(0, 0.08f, -1.9f), new Vector3(0.5f, 0.16f, 1.6f), MeshKit.C(a.Sand));   // nasip
            for (int i = 0; i < 22; i++)
            {
                var ang = rng.Range(0, Math.PI * 2);
                var r = rng.Range(0, 0.95);
                var p = new Vector3((float)(Math.Cos(ang) * r), 0.15f, (float)(Math.Sin(ang) * r));
                var h = 0.35f + (float)rng.Next() * 0.35f + (1 - (float)r) * 0.45f;
                k.Push(p, Quaternion.Euler(0, (float)rng.Range(0, 90), 0));
                k.Box(new Vector3(0, h / 2, 0), new Vector3(0.36f, h, 0.3f), C(rng.Chance(0.5) ? "#e8d9bd" : "#d9c6a2"));
                k.Cone(new Vector3(0, h, 0), 0.27f, 0.2f, 4, C(rng.Chance(0.5) ? "#c8553d" : "#b9472f"));
                k.Box(new Vector3(0, h * 0.6f, 0.155f), new Vector3(0.08f, 0.1f, 0.01f), C("#3b3b3b", 1f));
                k.Pop();
            }
            k.Cylinder(new Vector3(0.2f, 0.9f, 0.1f), 0.08f, 0.08f, 0.55f, 6, C("#e8d9bd"));   // zvonik
            k.Cone(new Vector3(0.2f, 1.45f, 0.1f), 0.11f, 0.22f, 6, C("#b9472f"));
        }

        // Gospa od Škrpjela: crkva sa kupolom i zvonikom na malom ostrvu.
        static void Gospa(MeshKit k, AtmospherePreset a)
        {
            k.Box(new Vector3(0, 0.55f, 0), new Vector3(1.0f, 0.8f, 0.6f), C("#efe9dc"));
            k.Cylinder(new Vector3(-0.15f, 0.95f, 0), 0.32f, 0.32f, 0.25f, 8, C("#efe9dc"));
            k.Sphere(new Vector3(-0.15f, 1.2f, 0), new Vector3(0.32f, 0.3f, 0.32f), C("#3f8a8a"), 6, 10);
            k.Cone(new Vector3(-0.15f, 1.45f, 0), 0.05f, 0.18f, 6, C("#d9b44a"));
            k.Box(new Vector3(0.45f, 0.9f, 0.1f), new Vector3(0.3f, 1.5f, 0.3f), C("#efe9dc"));      // zvonik
            k.Cone(new Vector3(0.45f, 1.65f, 0.1f), 0.22f, 0.4f, 8, C("#3f8a8a"));
            k.Box(new Vector3(0.45f, 1.35f, 0.26f), new Vector3(0.12f, 0.16f, 0.01f), C("#444444", 1f));
            for (int i = -1; i <= 1; i++) k.Box(new Vector3(i * 0.28f, 0.6f, 0.305f), new Vector3(0.1f, 0.22f, 0.01f), C("#4a5a6a", 1f));
        }

        // Ostrog: bijeli manastir uklesan u visoku liticu.
        static void Ostrog(MeshKit k, AtmospherePreset a)
        {
            var rock = MeshKit.C(a.Rock * 0.95f);
            k.Box(new Vector3(0, 1.6f, -0.45f), new Vector3(2.4f, 3.2f, 0.7f), rock);
            k.Sphere(new Vector3(-0.8f, 2.9f, -0.4f), new Vector3(0.9f, 0.7f, 0.6f), rock, 5, 8);
            k.Sphere(new Vector3(0.7f, 3.0f, -0.45f), new Vector3(1.0f, 0.6f, 0.6f), rock, 5, 8);
            k.Box(new Vector3(0, 1.0f, 0.12f), new Vector3(1.0f, 0.75f, 0.5f), C("#fbfaf6"));
            k.Box(new Vector3(0.25f, 1.55f, 0.1f), new Vector3(0.55f, 0.35f, 0.42f), C("#fbfaf6"));
            k.Box(new Vector3(0, 1.4f, 0.12f), new Vector3(1.05f, 0.06f, 0.52f), C("#c8553d"));
            for (int i = -1; i <= 1; i++) k.Box(new Vector3(i * 0.28f, 1.05f, 0.375f), new Vector3(0.1f, 0.18f, 0.01f), C("#4a5a6a", 1f));
            k.Box(new Vector3(0, 0.3f, 0.2f), new Vector3(1.6f, 0.6f, 0.9f), rock);
        }

        // Cetinjski manastir: kameni zidovi, crveni krov i zvonik.
        static void Cetinje(MeshKit k, AtmospherePreset a)
        {
            var stone = C("#e6dccb");
            k.Box(new Vector3(0, 0.35f, 0), new Vector3(1.6f, 0.7f, 0.9f), stone);
            k.Push(new Vector3(0, 0.7f, 0));
            k.Quad(new Vector3(0.82f, 0, -0.47f), new Vector3(-0.82f, 0, -0.47f), new Vector3(-0.82f, 0.35f, 0), new Vector3(0.82f, 0.35f, 0), C("#b9472f"));
            k.Quad(new Vector3(-0.82f, 0, 0.47f), new Vector3(0.82f, 0, 0.47f), new Vector3(0.82f, 0.35f, 0), new Vector3(-0.82f, 0.35f, 0), C("#b9472f"));
            k.Pop();
            k.Box(new Vector3(-0.95f, 0.7f, 0.2f), new Vector3(0.35f, 1.4f, 0.35f), stone);
            k.Cone(new Vector3(-0.95f, 1.4f, 0.2f), 0.26f, 0.35f, 4, C("#b9472f"));
            k.Box(new Vector3(-0.95f, 1.15f, 0.38f), new Vector3(0.14f, 0.2f, 0.01f), C("#333333", 1f));
            for (int i = -2; i <= 2; i++) k.Box(new Vector3(i * 0.3f, 0.42f, 0.455f), new Vector3(0.09f, 0.16f, 0.01f), C("#4a5a6a", 1f));
            k.Sphere(new Vector3(0.9f, 0.6f, -0.6f), 0.4f, MeshKit.C(a.Grass * 0.7f), 5, 8);
        }

        // Most na Đurđevića Tari: pet betonskih lukova preko kanjona.
        static void TaraBridge(MeshKit k, AtmospherePreset a)
        {
            var beton = C("#e9e5dc");
            var rock = MeshKit.C(a.Rock);
            k.Box(new Vector3(-2.7f, 0.2f, 0), new Vector3(0.8f, 1.6f, 1.6f), rock);
            k.Box(new Vector3(2.7f, 0.2f, 0), new Vector3(0.8f, 1.6f, 1.6f), rock);
            k.Box(new Vector3(0, 1.05f, 0), new Vector3(5.6f, 0.1f, 0.38f), beton);
            k.Box(new Vector3(0, 1.13f, 0.17f), new Vector3(5.6f, 0.06f, 0.03f), C("#c8553d"));
            k.Box(new Vector3(0, 1.13f, -0.17f), new Vector3(5.6f, 0.06f, 0.03f), C("#c8553d"));
            for (int arch = 0; arch < 5; arch++)
            {
                float cx = -2.0f + arch;
                for (int s = 0; s <= 8; s++)
                {
                    float t0 = s / 9f, t1 = (s + 1) / 9f;
                    float x0 = cx - 0.5f + t0, x1 = cx - 0.5f + t1;
                    float y0 = 1.0f - Mathf.Sin(t0 * Mathf.PI) * 0.0f - (1 - Mathf.Sin(t0 * Mathf.PI)) * 0.55f;
                    float y1 = 1.0f - (1 - Mathf.Sin(t1 * Mathf.PI)) * 0.55f;
                    k.Box(new Vector3((x0 + x1) / 2, (y0 + y1) / 2 - 0.05f, 0), new Vector3(x1 - x0 + 0.02f, 0.08f + Mathf.Abs(y1 - y0), 0.3f), beton);
                }
                k.Box(new Vector3(cx + 0.5f, 0.0f, 0), new Vector3(0.14f, 2.0f, 0.3f), beton);
            }
            k.Box(new Vector3(0, -0.75f, 0), new Vector3(4.8f, 0.1f, 1.2f), C("#2f8f9a"));   // rijeka Tara
        }

        // Crno jezero: tamna voda okružena borovima, Međed u pozadini.
        static void BlackLake(MeshKit k, AtmospherePreset a)
        {
            k.Disc(new Vector3(0, 0.03f, 0), 1.3f, 20, C("#1c3f52"), true);
            var rng = new Rng(8);
            for (int i = 0; i < 18; i++)
            {
                var ang = i / 18f * Mathf.PI * 2 + (float)rng.Range(-0.1, 0.1);
                var r = 1.45f + (float)rng.Range(0, 0.4);
                var leaf = MeshKit.C(a.Grass * (0.55f + (float)rng.Next() * 0.15f));
                k.Push(new Vector3(Mathf.Cos(ang) * r, 0, Mathf.Sin(ang) * r), Quaternion.identity, Vector3.one * (0.5f + (float)rng.Next() * 0.25f));
                k.Cylinder(Vector3.zero, 0.07f, 0.05f, 0.3f, 6, C("#6b4a2f"), false);
                k.Cone(new Vector3(0, 0.2f, 0), 0.38f, 0.6f, 7, leaf);
                k.Cone(new Vector3(0, 0.55f, 0), 0.28f, 0.5f, 7, leaf);
                k.Pop();
            }
            k.Cone(new Vector3(0.3f, 0, -2.4f), 1.4f, 2.4f, 7, MeshKit.C(a.Rock));
            k.Cone(new Vector3(0.3f, 1.6f, -2.4f), 0.55f, 0.8f, 7, MeshKit.C(a.Snow));
        }

        // Milenijum most: jedan kosi stub i lepeza kablova.
        static void Millennium(MeshKit k, AtmospherePreset a)
        {
            var white = C("#f4f4f2");
            k.Box(new Vector3(0, 0.45f, 0), new Vector3(4.2f, 0.12f, 0.5f), white);
            k.Box(new Vector3(0, -0.2f, 0), new Vector3(3.6f, 0.08f, 0.9f), C("#2c8fb0"));    // Morača
            k.Push(new Vector3(-1.2f, 0.45f, 0), Quaternion.Euler(0, 0, -18));
            k.Box(new Vector3(0, 1.3f, 0), new Vector3(0.16f, 2.6f, 0.16f), white);
            k.Pop();
            var top = new Vector3(-1.2f + Mathf.Sin(18 * Mathf.Deg2Rad) * 2.55f, 0.45f + Mathf.Cos(18 * Mathf.Deg2Rad) * 2.55f, 0);
            for (int i = 1; i <= 6; i++)
            {
                var deck = new Vector3(-1.2f + i * 0.45f, 0.5f, 0);
                var mid = (top + deck) / 2;
                var dir = deck - top;
                k.Push(mid, Quaternion.FromToRotation(Vector3.up, dir.normalized));
                k.Cylinder(new Vector3(0, -dir.magnitude / 2, 0), 0.015f, 0.015f, dir.magnitude, 4, C("#d9d9d9"), false);
                k.Pop();
            }
            k.Sphere(top, 0.08f, C("#ff6040", 1f), 4, 6);   // svjetlo na vrhu
        }

        // Sahat kula: kamena kula sa satom.
        static void ClockTower(MeshKit k, AtmospherePreset a)
        {
            var stone = C("#d8c9a8");
            k.Box(new Vector3(0, 0.9f, 0), new Vector3(0.5f, 1.8f, 0.5f), stone);
            k.Box(new Vector3(0, 1.9f, 0), new Vector3(0.58f, 0.2f, 0.58f), stone);
            k.Cylinder(new Vector3(0, 2.0f, 0), 0.06f, 0.06f, 0.25f, 6, C("#4a4a4a"));
            k.Cone(new Vector3(0, 2.25f, 0), 0.12f, 0.2f, 6, C("#c9a43a"));
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                k.Push(new Vector3(0, 1.55f, 0) + dir * 0.255f, Quaternion.FromToRotation(Vector3.up, dir));
                k.Disc(Vector3.zero, 0.14f, 12, C("#fff7e0", 0.6f), true);
                k.Pop();
            }
            k.Box(new Vector3(0, 0.25f, 0.255f), new Vector3(0.16f, 0.4f, 0.01f), C("#5a3a22"));
        }
    }
}
