// Staza u 3D: širok asfaltni put kroz gradove, polja kao tanke crte, posebna polja na asfaltu (prečica, blato,
// dupla polja, kutija sa napadom), start i ciljna kapija.
// Daje i položaj na putu za glatku vožnju (po dužini puta, ne skokovima).
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class TrackView : MonoBehaviour
    {
        public sealed class Label { public string Text; public Vector3 Pos; public string Kind; }

        WorldView world;
        Vector3[] road;
        float[] arc;
        float[] tileArc;
        readonly List<Transform> floaters = new List<Transform>();
        public readonly List<Label> Labels = new List<Label>();
        public int Last => world.Track.Last;
        public float Y => world.RoadY;
        public IList<Tile> Tiles => world.Track.Tiles;

        public static TrackView Build(WorldView w)
        {
            var go = new GameObject("Staza");
            go.transform.SetParent(w.transform, false);
            var t = go.AddComponent<TrackView>();
            t.world = w;
            t.Create();
            return t;
        }

        void Create()
        {
            var tr = world.Track;
            road = new Vector3[tr.Road.Count];
            arc = new float[road.Length];
            for (int i = 0; i < road.Length; i++)
            {
                road[i] = tr.Road[i].U(Y);
                if (i > 0) arc[i] = arc[i - 1] + Vector3.Distance(road[i], road[i - 1]);
            }
            tileArc = new float[tr.Tiles.Count];
            for (int t = 0; t < tileArc.Length; t++)
            {
                var p = tr.Tiles[t].Pos.U(Y);
                int best = 0; float bd = float.MaxValue;
                int from = t == 0 ? 0 : IndexOfArc(tileArc[t - 1]);
                for (int i = from; i < road.Length; i++)
                {
                    var d = (road[i] - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = i; }
                }
                tileArc[t] = arc[best];
            }

            var kit = new MeshKit();
            // put: široka bankina, asfalt sa bijelim ivicama, isprekidana žuta linija po sredini
            kit.Ribbon(road, 1.22f, -0.035f, MeshKit.C(world.Atmo.Sand * 0.92f));
            kit.Ribbon(road, 1.0f, -0.02f, MeshKit.C("#5d6472"), MeshKit.C("#f4f4f0"), 0.05f);
            for (int i = 0; i + 1 < road.Length; i += 4)
            {
                var a = road[i]; var b = road[Mathf.Min(i + 2, road.Length - 1)];
                var dir = (b - a).Flat().normalized;
                var side = new Vector3(-dir.z, 0, dir.x) * 0.018f;
                var up = Vector3.up * -0.016f;
                kit.Quad(a - side + up, a + side + up, b + side + up, b - side + up, MeshKit.C("#ffd34d"));
            }

            // polja su samo tanke poprečne crte na putu; posebna polja su nacrtana na asfaltu
            for (int i = 0; i < tr.Tiles.Count; i++)
            {
                var tile = tr.Tiles[i];
                PoseAt(tileArc[i], out var pos, out var fwd);
                var left = new Vector3(-fwd.z, 0, fwd.x);
                var y = Vector3.up * -0.012f;
                if (i > 0 && i < tr.Last)
                    Decal(kit, pos + y, fwd, left, 0.025f, 0.4f, MeshKit.C("#7c8494"));
                switch (tile.Type)
                {
                    case TileKinds.Start:
                    case TileKinds.Finish:
                        Checkers(kit, pos + y * 0.5f, fwd, left);
                        break;
                    case TileKinds.Shortcut:
                        for (int k = -1; k <= 1; k++) Chevron(kit, pos + y * 0.5f + fwd * (k * 0.2f), fwd, left, MeshKit.C("#2fbf6b"));
                        break;
                    case TileKinds.Mud:
                        Puddle(kit, pos + y * 0.5f, fwd, left, i);
                        break;
                    case TileKinds.Double:
                        Decal(kit, pos + y * 0.6f, fwd, left, 0.22f, 0.42f, MeshKit.C("#f6bd2a"));
                        Decal(kit, pos + y * 0.4f, fwd, left, 0.16f, 0.36f, MeshKit.C("#ffe27a"));
                        floaters.Add(Floater("Dupla polja " + i, pos, fwd, true));
                        break;
                    case TileKinds.Box:
                        floaters.Add(Floater("Kutija " + i, pos, fwd, false));
                        break;
                }
                if (tile.City != null)
                {
                    var side = new Vector3(-tile.Dir.Z.F(), 0, tile.Dir.X.F()) * 1.2f;
                    Labels.Add(new Label { Text = tile.City, Pos = pos + side + Vector3.up * 0.8f, Kind = "grad" });
                }
            }

            // start: luk sa balonima
            var t0 = tr.Tiles[0];
            Gate(kit, t0.Pos.U(Y), t0.Dir.U(0), false);
            var tl = tr.Tiles[tr.Last];
            Gate(kit, tl.Pos.U(Y), tl.Dir.U(0), true);
            Labels.Add(new Label { Text = "CILJ · " + world.Map.Finish, Pos = tl.Pos.U(Y) + Vector3.up * 2.0f, Kind = "cilj" });

            kit.ToObject("Put", transform);
        }

        // pravougaonik na putu: hf = pola dužine (uz put), hw = pola širine (preko puta)
        static void Decal(MeshKit k, Vector3 c, Vector3 f, Vector3 l, float hf, float hw, Color32 col)
        {
            k.Quad(c - f * hf - l * hw, c - f * hf + l * hw, c + f * hf + l * hw, c + f * hf - l * hw, col);
        }

        static void Checkers(MeshKit k, Vector3 c, Vector3 f, Vector3 l)
        {
            const int n = 8; const float cell = 0.125f;
            for (int x = 0; x < n; x++)
                for (int z = 0; z < 2; z++)
                {
                    var cc = c + l * ((x - (n - 1) / 2f) * cell) + f * ((z - 0.5f) * cell);
                    Decal(k, cc, f, l, cell / 2, cell / 2, MeshKit.C((x + z) % 2 == 0 ? "#14232e" : "#ffffff"));
                }
        }

        // strelica „naprijed” (prečica)
        static void Chevron(MeshKit k, Vector3 c, Vector3 f, Vector3 l, Color32 col)
        {
            var tip = c + f * 0.09f;
            var lo = c - f * 0.05f;
            const float w = 0.26f, t = 0.06f;
            k.Quad(tip, tip - f * t, lo + l * w - f * t, lo + l * w, col);
            k.Quad(lo - l * w, lo - l * w - f * t, tip - f * t, tip, col);
        }

        // nepravilna barica blata
        static void Puddle(MeshKit k, Vector3 c, Vector3 f, Vector3 l, int seed)
        {
            var rng = new Rng(seed * 97 + 13);
            const int n = 14;
            var ring = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n, r = 1 + (float)(rng.Next() - 0.5) * 0.35f;
                ring[i] = c + l * (Mathf.Cos(a) * 0.42f * r) + f * (Mathf.Sin(a) * 0.26f * r);
            }
            var mud = MeshKit.C("#6b4a2f"); var dark = MeshKit.C("#4e3420");
            for (int i = 0; i < n; i++) k.Triangle(c, ring[i], ring[(i + 1) % n], mud);
            for (int i = 0; i < 3; i++)
            {
                var p = c + l * ((float)(rng.Next() - 0.5) * 0.4f) + f * ((float)(rng.Next() - 0.5) * 0.2f) + Vector3.up * 0.002f;
                Decal(k, p, f, l, 0.04f, 0.06f, dark);
            }
        }

        // lebdeći predmet iznad puta: kutija sa napadom ili dva zlatna novčića (dupla polja)
        Transform Floater(string name, Vector3 pos, Vector3 fwd, bool coins)
        {
            var k = new MeshKit();
            if (coins)
            {
                foreach (var x in new[] { -0.13f, 0.13f })
                {
                    k.Push(new Vector3(x, 0, 0), Quaternion.Euler(90, 0, 0));
                    k.Cylinder(new Vector3(0, -0.025f, 0), 0.13f, 0.13f, 0.05f, 16, MeshKit.C("#ffcf33", 0.6f), true, MeshKit.C("#ffe27a", 0.6f));
                    k.Pop();
                }
            }
            else
            {
                k.RoundBox(Vector3.zero, Vector3.one * 0.3f, 0.05f, MeshKit.C("#8e5bff", 0.55f));
                k.RoundBox(Vector3.zero, new Vector3(0.32f, 0.08f, 0.32f), 0.02f, MeshKit.C("#ffd34d", 0.55f));
                k.RoundBox(Vector3.zero, new Vector3(0.08f, 0.32f, 0.32f), 0.02f, MeshKit.C("#ffd34d", 0.55f));
            }
            var g = k.ToObject(name, transform);
            g.transform.position = pos + Vector3.up * 0.55f;
            g.transform.rotation = Quaternion.LookRotation(fwd);
            return g.transform;
        }

        static void Gate(MeshKit k, Vector3 pos, Vector3 dir, bool finish)
        {
            var rot = Quaternion.LookRotation(dir);
            k.Push(pos, rot);
            var pole = MeshKit.C(finish ? "#f4f8fa" : "#2fbf6b");
            k.Cylinder(new Vector3(-0.75f, 0, 0), 0.06f, 0.05f, 1.4f, 8, pole);
            k.Cylinder(new Vector3(0.75f, 0, 0), 0.06f, 0.05f, 1.4f, 8, pole);
            if (finish)
            {
                // šahovska traka
                for (int x = 0; x < 10; x++)
                    for (int y = 0; y < 2; y++)
                    {
                        var col = MeshKit.C((x + y) % 2 == 0 ? "#14232e" : "#ffffff");
                        float x0 = -0.75f + x * 0.15f, y0 = 1.15f + y * 0.15f;
                        k.Box(new Vector3(x0 + 0.075f, y0 + 0.075f, 0), new Vector3(0.15f, 0.15f, 0.04f), col);
                    }
            }
            else
            {
                k.Box(new Vector3(0, 1.3f, 0), new Vector3(1.6f, 0.22f, 0.06f), MeshKit.C("#2fbf6b"));
                k.Sphere(new Vector3(-0.75f, 1.55f, 0), 0.16f, MeshKit.C("#e2283e"), 6, 8);
                k.Sphere(new Vector3(0.75f, 1.55f, 0), 0.16f, MeshKit.C("#2b7de9"), 6, 8);
            }
            k.Pop();
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < floaters.Count; i++)
            {
                var g = floaters[i];
                if (g == null) continue;
                g.rotation = Quaternion.Euler(0, t * 90 + i * 40, 0);
                var p = g.position;
                p.y = Y + 0.55f + Mathf.Sin(t * 2.5f + i) * 0.06f;
                g.position = p;
            }
        }

        // ---- položaj na putu ----
        public float ArcOfTile(int i) => tileArc[Mathf.Clamp(i, 0, tileArc.Length - 1)];
        public float TotalArc => arc[arc.Length - 1];

        int IndexOfArc(float s)
        {
            int lo = 0, hi = arc.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (arc[mid] < s) lo = mid + 1; else hi = mid;
            }
            return Mathf.Clamp(lo, 0, arc.Length - 1);
        }

        // Tačka i pravac na putu za datu dužinu od starta.
        public void PoseAt(float s, out Vector3 pos, out Vector3 forward)
        {
            s = Mathf.Clamp(s, 0, TotalArc);
            int i = Mathf.Max(1, IndexOfArc(s));
            float seg = arc[i] - arc[i - 1];
            float f = seg < 1e-5f ? 0 : (s - arc[i - 1]) / seg;
            pos = Vector3.Lerp(road[i - 1], road[i], f);
            forward = (road[Mathf.Min(i + 1, road.Length - 1)] - road[Mathf.Max(i - 2, 0)]).Flat().normalized;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
        }

        // Položaj figure na polju: više figura na istom polju stoji jedna pored druge.
        public Vector3 SlotPosition(int tile, int slot, int count)
        {
            PoseAt(ArcOfTile(tile), out var p, out var fwd);
            var side = new Vector3(-fwd.z, 0, fwd.x);
            return p + side * ((slot - (count - 1) / 2f) * 0.32f);
        }

        public Vector3 TileCenter(int tile) => Tiles[Mathf.Clamp(tile, 0, Last)].Pos.U(Y);
    }

    static class DoubleExt
    {
        public static float F(this double d) => (float)d;
    }
}
