// Staza u 3D: asfaltni put kroz gradove, šestougaona polja, zlatne stanice sa dragim kamenom, start i ciljna kapija.
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
        readonly List<Transform> gems = new List<Transform>();
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
            // put: bankina, asfalt, bijele ivice
            kit.Ribbon(road, 0.78f, -0.035f, MeshKit.C(world.Atmo.Sand * 0.92f));
            kit.Ribbon(road, 0.6f, -0.02f, MeshKit.C("#5d6472"), MeshKit.C("#f4f4f0"), 0.04f);
            // isprekidana srednja linija
            for (int i = 0; i + 1 < road.Length; i += 4)
            {
                var a = road[i]; var b = road[Mathf.Min(i + 2, road.Length - 1)];
                var dir = (b - a).Flat().normalized;
                var side = new Vector3(-dir.z, 0, dir.x) * 0.018f;
                var up = Vector3.up * -0.016f;
                kit.Quad(a - side + up, a + side + up, b + side + up, b - side + up, MeshKit.C("#ffd34d"));
            }

            // polja
            for (int i = 0; i < tr.Tiles.Count; i++)
            {
                var tile = tr.Tiles[i];
                var pos = tile.Pos.U(Y);
                var rot = Quaternion.LookRotation(tile.Dir.U(0));
                Color32 top;
                float glow = 0;
                switch (tile.Type)
                {
                    case "start": top = MeshKit.C("#2fbf6b"); break;
                    case "cilj": top = MeshKit.C("#e2283e"); break;
                    case "stanica": top = MeshKit.C("#f6bd2a", 0.35f); glow = 1; break;
                    default: top = MeshKit.C(i % 2 == 0 ? "#ffffff" : "#d5ecfb"); break;
                }
                kit.Push(pos, rot);
                kit.Cylinder(new Vector3(0, -0.07f, 0), 0.47f, 0.43f, 0.1f, 6, MeshKit.C("#2b3442"), false);
                kit.Cylinder(new Vector3(0, 0.0f, 0), 0.43f, 0.4f, 0.05f, 6, top, true, top);
                if (glow > 0) kit.Cylinder(new Vector3(0, 0.05f, 0), 0.22f, 0.2f, 0.02f, 6, MeshKit.C("#fff2b0", 0.8f));
                kit.Pop();

                if (tile.Type == "stanica")
                {
                    var gem = new MeshKit();
                    gem.Sphere(Vector3.zero, new Vector3(0.2f, 0.28f, 0.2f), MeshKit.C("#ffd34d", 0.55f), 2, 4);
                    var g = gem.ToObject("Stanica " + i, transform);
                    g.transform.position = pos + Vector3.up * 0.95f;
                    gems.Add(g.transform);
                    Labels.Add(new Label { Text = "Stanica", Pos = pos + Vector3.up * 1.45f, Kind = "stanica" });
                }
                if (tile.City != null)
                {
                    var side = new Vector3(-tile.Dir.Z.F(), 0, tile.Dir.X.F()) * 1.0f;
                    Labels.Add(new Label { Text = tile.City, Pos = pos + side + Vector3.up * 0.8f, Kind = "grad" });
                }
            }

            // start: luk sa balonima
            var t0 = tr.Tiles[0];
            Gate(kit, t0.Pos.U(Y), t0.Dir.U(0), false);
            var tl = tr.Tiles[tr.Last];
            Gate(kit, tl.Pos.U(Y), tl.Dir.U(0), true);
            Labels.Add(new Label { Text = "CILJ · " + world.Map.Finish, Pos = tl.Pos.U(Y) + Vector3.up * 2.0f, Kind = "cilj" });

            kit.ToObject("Put i polja", transform);
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
            for (int i = 0; i < gems.Count; i++)
            {
                var g = gems[i];
                g.rotation = Quaternion.Euler(0, t * 90 + i * 40, 0);
                var p = g.position;
                p.y = Y + 0.95f + Mathf.Sin(t * 2.5f + i) * 0.08f;
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
