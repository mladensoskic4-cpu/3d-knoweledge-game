// Izvoz modela (vozila sa životinjama, znamenitosti, priroda) u OBJ sa bojama vrhova, za pregled bez Unity-ja.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using TrkaZnanja.Core;
using TrkaZnanja.Game;
using UnityEngine;

namespace TrkaZnanja.LocalTools
{
    public static class MeshExport
    {
        static List<T> Field<T>(MeshKit k, string name) => (List<T>)typeof(MeshKit).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(k);

        public static void Obj(MeshKit k, string path)
        {
            var v = Field<Vector3>(k, "v"); var c = Field<Color32>(k, "c"); var t = Field<int>(k, "t");
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            for (int i = 0; i < v.Count; i++)
                sb.AppendLine(string.Format(ci, "v {0} {1} {2} {3} {4} {5} {6}", v[i].x, v[i].y, v[i].z, c[i].r / 255f, c[i].g / 255f, c[i].b / 255f, c[i].a / 255f));
            for (int i = 0; i < t.Count; i += 3) sb.AppendLine($"f {t[i] + 1} {t[i + 1] + 1} {t[i + 2] + 1}");
            File.WriteAllText(path, sb.ToString());
        }

        public static int Run(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var vehicles = typeof(Avatars).Assembly.GetType("TrkaZnanja.Game.Vehicles");
            var animals = typeof(Avatars).Assembly.GetType("TrkaZnanja.Game.Animals");
            var draw = vehicles.GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);
            var drawAnimal = animals.GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);
            int idx = 0;
            foreach (var veh in Catalog.Vehicles)
            {
                var animal = Catalog.Animals[idx++ % Catalog.Animals.Length];
                var look = new Look { Zivotinja = animal.Id, Vozilo = veh.Id, Boja = veh.DefaultPaint, Kapa = idx % 3 == 0 ? "kapa" : idx % 3 == 1 ? "kacket" : "nema", Naocare = idx % 4 == 0 };
                var kit = new MeshKit();
                var wheels = new List<(Vector3, float, float)>();
                var args = new object[] { kit, veh.Id, Col.Hex(veh.DefaultPaint), wheels, null, null, new AvatarRig() };
                draw.Invoke(null, args);
                var seat = (Vector3)args[4]; var scale = (float)args[5];
                kit.Push(seat, Quaternion.identity, Vector3.one * scale);
                drawAnimal.Invoke(null, new object[] { kit, animal, look });
                kit.Pop();
                foreach (var (pos, r, w) in wheels) { kit.Push(pos); kit.Wheel(Vector3.zero, r, w, MeshKit.C("#23262c"), MeshKit.C("#d9dde2")); kit.Pop(); }
                Obj(kit, Path.Combine(outDir, $"vozilo-{veh.Id}-{animal.Id}.obj"));
            }
            // sve životinje u autu
            foreach (var a in Catalog.Animals)
            {
                var kit = new MeshKit();
                drawAnimal.Invoke(null, new object[] { kit, a, new Look { Zivotinja = a.Id } });
                Obj(kit, Path.Combine(outDir, $"zivotinja-{a.Id}.obj"));
            }
            var lm = typeof(Avatars).Assembly.GetType("TrkaZnanja.Game.Landmarks");
            var atmo = AtmospherePreset.Day();
            foreach (var name in new[] { "SvetiStefan", "Gospa", "Ostrog", "Cetinje", "TaraBridge", "BlackLake", "Millennium", "ClockTower" })
            {
                var kit = new MeshKit();
                if (name == "SvetiStefan" || name == "Gospa") lm.GetMethod("Islet", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { kit, atmo });
                lm.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { kit, atmo });
                Obj(kit, Path.Combine(outDir, $"znamenitost-{name}.obj"));
            }
            Console.WriteLine("Izvezeno u " + outDir);
            return 0;
        }

        // Cijela scena (teren, voda, staza, priroda, znamenitosti, 4 igrača) u jedan OBJ, za pregled kompozicije.
        public static int Scene(string path, string mapId, string atmo, int tiles = 32)
        {
            MeshFilter.All.Clear();
            var root = new GameObject("Korijen").transform;
            var w = WorldView.Build(root, Catalog.MapOf(mapId), tiles, atmo, 7);
            var track = TrackView.Build(w);
            var rng = new Rng(1);
            var human = new Racer { Id = "ti", Name = "Ti", Color = "#e0312f", Human = true, Look = new Look { Zivotinja = "lisica", Vozilo = "auto", Boja = "#e0312f", Kapa = "kacket" } };
            var racers = new List<Racer> { human };
            racers.AddRange(Bots.Create("srednje", rng));
            int[] pos = { 4, 4, 6, 9 };
            for (int i = 0; i < racers.Count; i++)
            {
                racers[i].Pos = pos[i];
                var v = RacerView.Create(racers[i], track, w.transform);
                int same = pos.Count(p => p == pos[i]);
                int slot = pos.Take(i).Count(p => p == pos[i]);
                v.transform.position = track.SlotPosition(pos[i], slot, same);
            }
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            int offset = 1, tris = 0;
            foreach (var mf in MeshFilter.All)
            {
                if (mf.gameObject == null || mf.gameObject.destroyed || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh;
                var M = mf.transform.World;
                bool water = mf.gameObject.name == "Voda";
                for (int i = 0; i < m.vertices.Length; i++)
                {
                    var p = M.MultiplyPoint3x4(m.vertices[i]);
                    Color32 c = m.colors32 != null && i < m.colors32.Length ? m.colors32[i] : water ? new Color32(40, 140, 200, 255) : new Color32(200, 200, 200, 255);
                    sb.AppendLine(string.Format(ci, "v {0} {1} {2} {3} {4} {5}", p.x, p.y, p.z, c.r / 255f, c.g / 255f, c.b / 255f));
                }
                for (int i = 0; i + 2 < m.triangles.Length; i += 3)
                {
                    sb.AppendLine($"f {m.triangles[i] + offset} {m.triangles[i + 1] + offset} {m.triangles[i + 2] + offset}");
                    tris++;
                }
                offset += m.vertices.Length;
            }
            File.WriteAllText(path, sb.ToString());
            var me = racers[0];
            var t0 = track.SlotPosition(me.Pos, 0, 2);
            Console.WriteLine($"scena: {tris} trouglova; igrač na {t0.x:0.00} {t0.y:0.00} {t0.z:0.00}; pravac {track.Tiles[me.Pos].Dir.X:0.00} {track.Tiles[me.Pos].Dir.Z:0.00}");
            return 0;
        }
    }
}
