// 3D svijet jedne mape: teren (more, plaže, brda, planine sa snijegom), voda sa pjenom, šume, sela, oblaci,
// padavine, mostovi preko mora i domaće znamenitosti. Sve se pravi u kodu iz obrisa kopna (Natural Earth).
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrkaZnanja.Game
{
    public sealed class WorldView : MonoBehaviour
    {
        public TrackLayout Track { get; private set; }
        public TerrainGrid Terrain { get; private set; }
        public AtmospherePreset Atmo { get; private set; }
        public RaceMap Map { get; private set; }
        public Bounds Bounds { get; private set; }

        readonly List<Transform> clouds = new List<Transform>();
        readonly List<float> cloudSpeed = new List<float>();
        ParticleSystem weather;
        Transform weatherFollow;

        public float RoadY => (float)(Terrain?.LandTop ?? 0.4) + 0.06f;

        public static WorldView Build(Transform parent, RaceMap map, int tiles, string atmosphere, int seed)
        {
            var go = new GameObject("Svijet · " + map.Name);
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<WorldView>();
            w.Create(map, tiles, atmosphere, seed);
            return w;
        }

        void Create(RaceMap map, int tiles, string atmosphere, int seed)
        {
            Map = map;
            Atmo = AtmospherePreset.Get(atmosphere);
            Track = TrackLayout.Build(map, tiles);
            var settings = new TerrainParams { Seed = seed, SnowLine = Atmo.SnowLine };
            Terrain = TerrainBuilder.Build(Track, GameData.Land(map.Land), GeoData.LakesFor(map.Id), settings, map.Id);
            var T = Terrain;
            Bounds = new Bounds(new Vector3((float)(T.X0 + (T.Nx - 1) * T.Cell / 2), 0, (float)(T.Z0 + (T.Nz - 1) * T.Cell / 2)),
                new Vector3((float)((T.Nx - 1) * T.Cell), 4, (float)((T.Nz - 1) * T.Cell)));

            BuildTerrain();
            BuildWater();
            BuildScenery(seed);
            BuildBridges();
            Landmarks.Build(transform, this);
            BuildClouds(seed);
            BuildWeather();
        }

        // ---------- teren ----------
        Color GroundColor(int i)
        {
            var T = Terrain;
            float shade = T.Shade[i];
            float h = T.Height[i];
            switch (T.Kind[i])
            {
                case Ground.Sea: return Color.Lerp(Atmo.Sand, Atmo.WaterDeep, 0.35f);
                case Ground.Lake: return Color.Lerp(Atmo.Sand, Atmo.WaterDeep, 0.5f);
                case Ground.Sand: return Atmo.Sand * (0.94f + shade * 0.12f);
                case Ground.Snow: return Atmo.Snow * (0.93f + shade * 0.07f);
                case Ground.Rock: return Color.Lerp(Atmo.Rock, Atmo.Rock * 0.8f, shade);
                default:
                    var g = Color.Lerp(Atmo.Grass, Atmo.GrassDry, Mathf.SmoothStep(0.35f, 0.75f, shade));
                    // više = tamnije zeleno (šuma), pa prelaz u stijenu
                    var hh = Mathf.Clamp01((h - (float)T.LandTop) / 2f);
                    g = Color.Lerp(g, Color.Lerp(g * 0.75f, Atmo.Rock, 0.35f), hh);
                    if (Atmo.Id == "snijeg") g = Color.Lerp(g, Atmo.Snow, Mathf.SmoothStep(0.15f, 0.6f, hh + shade * 0.3f));
                    return g;
            }
        }

        void BuildTerrain()
        {
            var T = Terrain;
            int nx = T.Nx, nz = T.Nz;
            var verts = new Vector3[nx * nz];
            var cols = new Color32[nx * nz];
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = T.Idx(ix, iz);
                    verts[i] = new Vector3((float)(T.X0 + ix * T.Cell), T.Height[i], (float)(T.Z0 + iz * T.Cell));
                    cols[i] = MeshKit.C(GroundColor(i));
                }
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int iz = 0; iz < nz - 1; iz++)
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = T.Idx(ix, iz), b = a + 1, c = a + nx, d = c + 1;
                    // Unity: prednja strana prema gore
                    tris[k++] = a; tris[k++] = c; tris[k++] = d;
                    tris[k++] = a; tris[k++] = d; tris[k++] = b;
                }
            var mesh = new Mesh { name = "Teren", indexFormat = verts.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = verts;
            mesh.colors32 = cols;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Teren");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mats.Lit;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        // ---------- voda ----------
        void BuildWater()
        {
            var T = Terrain;
            // pjena: blizina obale (more) i ivice jezera
            var sea = TerrainBuilder.Distance(T.Nx, T.Nz, i => T.Land[i] && !T.Lake[i], T.Cell);
            var lake = TerrainBuilder.Distance(T.Nx, T.Nz, i => !T.Lake[i], T.Cell);
            var tex = new Texture2D(T.Nx, T.Nz, TextureFormat.RGBA32, false) { name = "Obala", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[T.Nx * T.Nz];
            for (int i = 0; i < px.Length; i++)
            {
                float d = T.Lake[i] ? lake[i] : sea[i];
                byte v = (byte)(255 * Mathf.Clamp01(1 - d / 2.6f));
                px[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(px);
            tex.Apply();
            var mat = new Material(Mats.Water) { name = "Voda" };
            mat.SetTexture("_ShoreTex", tex);
            mat.SetVector("_ShoreRect", new Vector4((float)T.X0, (float)T.Z0, (float)((T.Nx - 1) * T.Cell), (float)((T.Nz - 1) * T.Cell)));

            // mreža vode malo veća od terena (do horizonta se vidi more)
            var b = Bounds;
            float pad = 90, size = Mathf.Max(b.size.x, b.size.z) + pad * 2;
            int n = 140;
            var verts = new List<Vector3>(n * n);
            var tris = new List<int>();
            float step = size / (n - 1);
            var origin = new Vector3(b.center.x - size / 2, (float)T.SeaLevel, b.center.z - size / 2);
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                    verts.Add(origin + new Vector3(x * step, 0, z * step));
            for (int z = 0; z < n - 1; z++)
                for (int x = 0; x < n - 1; x++)
                {
                    int a = z * n + x, c = a + n;
                    tris.Add(a); tris.Add(c); tris.Add(c + 1);
                    tris.Add(a); tris.Add(c + 1); tris.Add(a + 1);
                }
            var mesh = new Mesh { name = "Voda", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(b.center, new Vector3(size, 2, size));
            var go = new GameObject("Voda");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- drveće, kamenje, kuće ----------
        void BuildScenery(int seed)
        {
            var T = Terrain;
            var rng = new Rng(seed + 4);
            // dijelimo u 4×4 komada, da kamera ne crta ono što ne vidi
            const int G = 4;
            var kits = new MeshKit[G * G];
            for (int i = 0; i < kits.Length; i++) kits[i] = new MeshKit();
            MeshKit KitAt(Vec3d p)
            {
                int gx = Mathf.Clamp((int)((p.X - T.X0) / ((T.Nx - 1) * T.Cell) * G), 0, G - 1);
                int gz = Mathf.Clamp((int)((p.Z - T.Z0) / ((T.Nz - 1) * T.Cell) * G), 0, G - 1);
                return kits[gz * G + gx];
            }

            var trunk = MeshKit.C("#7a5236");
            foreach (var t in T.Trees)
            {
                var k = KitAt(t.Pos);
                var s = (float)t.Scale * 0.42f;
                var shadeK = 0.85f + (float)rng.Next() * 0.3f;
                var leaf = Color.Lerp(Atmo.Grass * 0.62f, Atmo.Grass * 0.9f, (float)rng.Next()) * shadeK;
                if (Atmo.Id == "snijeg") leaf = Color.Lerp(leaf, Color.white, 0.25f);
                k.Push(t.Pos.U(), Quaternion.Euler(0, (float)t.Rot, 0), Vector3.one * s);
                switch (t.Variant)
                {
                    case 0: // bor: tri kupe
                        k.Cylinder(Vector3.zero, 0.09f, 0.07f, 0.35f, 6, trunk, false);
                        k.Cone(new Vector3(0, 0.25f, 0), 0.5f, 0.6f, 8, MeshKit.C(leaf));
                        k.Cone(new Vector3(0, 0.55f, 0), 0.4f, 0.55f, 8, MeshKit.C(leaf * 1.06f));
                        k.Cone(new Vector3(0, 0.85f, 0), 0.28f, 0.5f, 8, MeshKit.C(leaf * 1.12f));
                        if (Atmo.Id == "snijeg") k.Cone(new Vector3(0, 1.08f, 0), 0.14f, 0.28f, 8, MeshKit.C(Atmo.Snow));
                        break;
                    case 1: // listopadno: okrugla krošnja
                        k.Cylinder(Vector3.zero, 0.08f, 0.06f, 0.5f, 6, trunk, false);
                        k.Sphere(new Vector3(0, 0.75f, 0), new Vector3(0.45f, 0.42f, 0.45f), MeshKit.C(leaf * 1.1f), 6, 9);
                        k.Sphere(new Vector3(0.18f, 0.95f, 0.05f), new Vector3(0.3f, 0.28f, 0.3f), MeshKit.C(leaf * 1.2f), 5, 8);
                        break;
                    default: // čempres (primorje)
                        k.Cylinder(Vector3.zero, 0.06f, 0.05f, 0.2f, 6, trunk, false);
                        k.Sphere(new Vector3(0, 0.75f, 0), new Vector3(0.2f, 0.65f, 0.2f), MeshKit.C(leaf * 0.85f), 6, 8);
                        break;
                }
                k.Pop();
            }

            foreach (var r in T.Rocks)
            {
                var k = KitAt(r.Pos);
                var s = (float)r.Scale;
                k.Push(r.Pos.U(), Quaternion.Euler((float)rng.Range(-10, 10), (float)r.Rot, 0));
                k.Sphere(Vector3.zero, new Vector3(s, s * 0.6f, s * 0.8f), MeshKit.C(Atmo.Rock * (0.9f + (float)rng.Next() * 0.2f)), 4, 6);
                k.Pop();
            }

            string[] walls = { "#fbf6ec", "#f4e3c6", "#ffffff", "#efe6da" };
            string[] roofs = { "#d0573d", "#c4472f", "#b8553f", "#d86a3e" };
            foreach (var hse in T.Houses)
            {
                var k = KitAt(hse.Pos);
                var s = (float)hse.Scale * 0.34f;
                k.Push(hse.Pos.U(), Quaternion.Euler(0, (float)hse.Rot, 0), Vector3.one * s);
                var wall = MeshKit.C(walls[hse.Variant % walls.Length]);
                var roof = MeshKit.C(roofs[hse.Variant % roofs.Length]);
                bool tall = hse.Variant == 3;
                float wh = tall ? 0.95f : 0.6f;
                k.Box(new Vector3(0, wh / 2, 0), new Vector3(0.75f, wh, 0.6f), wall);
                // krov na dvije vode
                k.Push(new Vector3(0, wh, 0));
                k.Quad(new Vector3(0.42f, 0, -0.36f), new Vector3(-0.42f, 0, -0.36f), new Vector3(-0.42f, 0.32f, 0), new Vector3(0.42f, 0.32f, 0), roof);
                k.Quad(new Vector3(-0.42f, 0, 0.36f), new Vector3(0.42f, 0, 0.36f), new Vector3(0.42f, 0.32f, 0), new Vector3(-0.42f, 0.32f, 0), roof);
                k.Triangle(new Vector3(0.38f, 0, 0.3f), new Vector3(0.38f, 0, -0.3f), new Vector3(0.38f, 0.3f, 0), wall);
                k.Triangle(new Vector3(-0.38f, 0, -0.3f), new Vector3(-0.38f, 0, 0.3f), new Vector3(-0.38f, 0.3f, 0), wall);
                k.Pop();
                // prozori: svijetle noću
                var win = MeshKit.C("#4a5a6a", 1f);
                k.Box(new Vector3(-0.18f, wh * 0.55f, 0.305f), new Vector3(0.16f, 0.16f, 0.02f), win);
                k.Box(new Vector3(0.18f, wh * 0.55f, 0.305f), new Vector3(0.16f, 0.16f, 0.02f), win);
                k.Box(new Vector3(0.18f, wh * 0.55f, -0.305f), new Vector3(0.16f, 0.16f, 0.02f), win);
                k.Box(new Vector3(0, 0.15f, 0.305f), new Vector3(0.14f, 0.3f, 0.02f), MeshKit.C("#7a4a2a"));
                k.Pop();
            }

            for (int i = 0; i < kits.Length; i++)
                if (kits[i].VertexCount > 0) kits[i].ToObject("Priroda " + i, transform);
        }

        // ---------- mostovi tamo gdje put ide preko vode ----------
        void BuildBridges()
        {
            var T = Terrain;
            var kit = new MeshKit();
            var pillar = MeshKit.C("#e9e1d0");
            float y = RoadY - 0.02f;
            int last = -10;
            for (int i = 0; i < Track.Road.Count; i++)
            {
                var p = Track.Road[i];
                if (T.SampleHeight(p.X, p.Z) > T.SeaLevel + 0.05) continue;
                if (i - last < 6) continue;
                last = i;
                var bottom = new Vector3((float)p.X, (float)T.SeaLevel - 0.6f, (float)p.Z);
                kit.Cylinder(bottom, 0.16f, 0.12f, y - bottom.y, 8, pillar);
            }
            if (kit.VertexCount > 0) kit.ToObject("Mostovi", transform);
        }

        // ---------- oblaci ----------
        void BuildClouds(int seed)
        {
            var rng = new Rng(seed + 77);
            var b = Bounds;
            int count = Atmo.Id == "kisa" ? 26 : Atmo.Id == "noc" ? 8 : 16;
            var col = Atmo.Id == "kisa" ? Col.Hex("#c9d1d9") : Atmo.Id == "noc" ? Col.Hex("#3a4870") : Atmo.Id == "zalazak" ? Col.Hex("#ffe1cf") : Color.white;
            for (int i = 0; i < count; i++)
            {
                var kit = new MeshKit();
                int puffs = rng.Range(3, 6);
                for (int j = 0; j < puffs; j++)
                {
                    var r = (float)rng.Range(0.8, 1.6);
                    kit.Sphere(new Vector3(j * 1.0f - puffs * 0.5f, (float)rng.Range(-0.1, 0.4), (float)rng.Range(-0.4, 0.4)), new Vector3(r, r * 0.7f, r), MeshKit.C(col), 6, 9);
                }
                var go = kit.ToObject("Oblak", transform, null, Atmo.Id != "noc");
                go.transform.position = new Vector3(
                    (float)rng.Range(b.min.x - 10, b.max.x + 10), (float)rng.Range(7, 11), (float)rng.Range(b.min.z - 10, b.max.z + 10));
                clouds.Add(go.transform);
                cloudSpeed.Add((float)rng.Range(0.25, 0.7));
            }
        }

        // ---------- kiša i snijeg (prate kameru) ----------
        void BuildWeather()
        {
            if (Atmo.Weather == null) return;
            var go = new GameObject("Padavine");
            go.transform.SetParent(transform, false);
            weatherFollow = go.transform;
            weather = go.AddComponent<ParticleSystem>();
            var main = weather.main;
            bool rain = Atmo.Weather == "kisa";
            main.loop = true;
            main.startLifetime = rain ? 0.9f : 4f;
            main.startSpeed = rain ? 16f : 1.2f;
            main.startSize = rain ? 0.05f : 0.12f;
            main.startColor = rain ? new Color(0.8f, 0.88f, 1f, 0.55f) : new Color(1, 1, 1, 0.9f);
            main.maxParticles = rain ? 1600 : 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = rain ? 0.6f : 0.04f;
            var em = weather.emission;
            em.rateOverTime = rain ? 900 : 260;
            var sh = weather.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(40, 0.5f, 40);
            sh.rotation = new Vector3(90, 0, 0);
            if (!rain)
            {
                var noise = weather.noise;
                noise.enabled = true;
                noise.strength = 0.6f;
                noise.frequency = 0.4f;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(false);
            r.renderMode = rain ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (rain) { r.velocityScale = 0.04f; r.lengthScale = 1.2f; }
            weather.Play();
        }

        void Update()
        {
            var b = Bounds;
            for (int i = 0; i < clouds.Count; i++)
            {
                var c = clouds[i];
                c.position += Vector3.right * cloudSpeed[i] * Time.deltaTime;
                if (c.position.x > b.max.x + 14) c.position = new Vector3(b.min.x - 14, c.position.y, c.position.z);
            }
            if (weatherFollow != null && Camera.main != null)
            {
                var cam = Camera.main.transform.position;
                weatherFollow.position = new Vector3(cam.x, cam.y + 9, cam.z) + Camera.main.transform.forward.Flat() * 6;
            }
        }

        // Visina tla u tački (za postavljanje modela).
        public float GroundAt(Vector3 p) => (float)Terrain.SampleHeight(p.x, p.z);
    }
}
