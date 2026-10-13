// Pravljenje 3D modela u kodu: kocke, valjci, kupe, kugle… sa bojom u svakom vrhu.
// Sve se slaže u jednu mrežu (mesh), pa i složen model (životinja u vozilu) košta samo jedan poziv crtanja.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrkaZnanja.Game
{
    public sealed class MeshKit
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Color32> c = new List<Color32>();
        readonly List<int> t = new List<int>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        Matrix4x4 m = Matrix4x4.identity;

        public int VertexCount => v.Count;

        // ---- transformacije (kao u crtanju: Push → pomjeri/okreni → oblici → Pop) ----
        public MeshKit Push(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            stack.Push(m);
            m = m * Matrix4x4.TRS(pos, rot, scale);
            return this;
        }
        public MeshKit Push(Vector3 pos, Quaternion rot) => Push(pos, rot, Vector3.one);
        public MeshKit Push(Vector3 pos) => Push(pos, Quaternion.identity, Vector3.one);
        public MeshKit Pop() { m = stack.Pop(); return this; }

        int Vert(Vector3 p, Vector3 normal, Color32 col)
        {
            v.Add(m.MultiplyPoint3x4(p));
            n.Add(m.MultiplyVector(normal).normalized);
            c.Add(col);
            return v.Count - 1;
        }

        void Tri(int a, int b, int d) { t.Add(a); t.Add(b); t.Add(d); }

        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color32 col)
        {
            var normal = Vector3.Cross(b - a, d - a).normalized;
            int i0 = Vert(a, normal, col), i1 = Vert(b, normal, col), i2 = Vert(cc, normal, col), i3 = Vert(d, normal, col);
            Tri(i0, i1, i2); Tri(i0, i2, i3);   // Unity: prednja strana kad je cross(p1-p0, p2-p0) prema posmatraču
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 d, Color32 col)
        {
            var normal = Vector3.Cross(b - a, d - a).normalized;
            Tri(Vert(a, normal, col), Vert(b, normal, col), Vert(d, normal, col));
        }

        // Kocka (centar, veličina), oštre ivice.
        public void Box(Vector3 center, Vector3 size, Color32 col)
        {
            var h = size * 0.5f;
            Vector3 p(float x, float y, float z) => center + new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(p(-1, -1, 1), p(1, -1, 1), p(1, 1, 1), p(-1, 1, 1), col);     // naprijed (+z)
            Quad(p(1, -1, -1), p(-1, -1, -1), p(-1, 1, -1), p(1, 1, -1), col); // nazad
            Quad(p(1, -1, 1), p(1, -1, -1), p(1, 1, -1), p(1, 1, 1), col);     // desno
            Quad(p(-1, -1, -1), p(-1, -1, 1), p(-1, 1, 1), p(-1, 1, -1), col); // lijevo
            Quad(p(-1, 1, 1), p(1, 1, 1), p(1, 1, -1), p(-1, 1, -1), col);     // gore
            Quad(p(-1, -1, -1), p(1, -1, -1), p(1, -1, 1), p(-1, -1, 1), col); // dolje
        }

        // Zaobljena kocka (blago oborene ivice) – izgleda „mekše”, kao igračka.
        public void RoundBox(Vector3 center, Vector3 size, float bevel, Color32 col)
        {
            bevel = Mathf.Min(bevel, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.45f);
            Box(center, new Vector3(size.x - bevel * 2, size.y, size.z - bevel * 2), col);
            Box(center, new Vector3(size.x, size.y - bevel * 2, size.z - bevel * 2), col);
            Box(center, new Vector3(size.x - bevel * 2, size.y - bevel * 2, size.z), col);
            var h = size * 0.5f - Vector3.one * bevel;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Cylinder(center + new Vector3(sx * h.x, -h.y, sz * h.z), bevel, bevel, size.y - bevel * 2, 8, col, false);
        }

        // Valjak/kupa duž osi Y, od base nagore, poluprečnik dolje r0 i gore r1.
        public void Cylinder(Vector3 bottom, float r0, float r1, float height, int seg, Color32 col, bool caps = true, Color32? capCol = null)
        {
            seg = Mathf.Max(3, seg);
            var slope = (r0 - r1) / Mathf.Max(0.0001f, height);
            int start = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var nn = new Vector3(ca, slope, sa).normalized;
                Vert(bottom + new Vector3(ca * r0, 0, sa * r0), nn, col);
                Vert(bottom + new Vector3(ca * r1, height, sa * r1), nn, col);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 2, b = a + 1, d = a + 2, e = a + 3;
                Tri(a, b, e); Tri(a, e, d);
            }
            if (!caps) return;
            var cc = capCol ?? col;
            if (r1 > 0.0001f) Disc(bottom + Vector3.up * height, r1, seg, cc, true);
            if (r0 > 0.0001f) Disc(bottom, r0, seg, cc, false);
        }

        public void Disc(Vector3 center, float r, int seg, Color32 col, bool up)
        {
            var nn = up ? Vector3.up : Vector3.down;
            int ci = Vert(center, nn, col), start = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg;
                Vert(center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), nn, col);
            }
            for (int i = 0; i < seg; i++)
                if (up) Tri(ci, start + i + 1, start + i); else Tri(ci, start + i, start + i + 1);
        }

        public void Cone(Vector3 bottom, float r, float height, int seg, Color32 col) => Cylinder(bottom, r, 0.0001f, height, seg, col, true);

        // Elipsoid (kugla sa različitim poluprečnicima), glatko osjenčen.
        public void Sphere(Vector3 center, Vector3 radius, Color32 col, int lat = 8, int lon = 12)
        {
            int start = v.Count;
            for (int y = 0; y <= lat; y++)
            {
                float th = y * Mathf.PI / lat, st = Mathf.Sin(th), ct = Mathf.Cos(th);
                for (int x = 0; x <= lon; x++)
                {
                    float ph = x * Mathf.PI * 2 / lon;
                    var unit = new Vector3(st * Mathf.Cos(ph), ct, st * Mathf.Sin(ph));
                    var p = Vector3.Scale(unit, radius);
                    var nn = new Vector3(unit.x / Mathf.Max(radius.x, 1e-4f), unit.y / Mathf.Max(radius.y, 1e-4f), unit.z / Mathf.Max(radius.z, 1e-4f)).normalized;
                    Vert(center + p, nn, col);
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = start + y * (lon + 1) + x, b = a + lon + 1;
                    Tri(a, a + 1, b + 1); Tri(a, b + 1, b);
                }
        }
        public void Sphere(Vector3 center, float r, Color32 col, int lat = 8, int lon = 12) => Sphere(center, Vector3.one * r, col, lat, lon);

        // Točak: valjak položen na bok (osa X), guma + felna.
        public void Wheel(Vector3 center, float r, float width, Color32 tire, Color32 rim)
        {
            Push(center, Quaternion.Euler(0, 0, 90));
            Cylinder(new Vector3(0, -width / 2, 0), r, r, width, 14, tire, true, tire);
            Cylinder(new Vector3(0, -width / 2 - 0.005f, 0), r * 0.55f, r * 0.55f, width + 0.01f, 10, rim, true, rim);
            Pop();
        }

        // Traka duž niza tačaka (put, ograda): širina, visina iznad tačaka, boje lijevo/desno.
        public void Ribbon(IList<Vector3> pts, float width, float yOffset, Color32 col, Color32? edge = null, float edgeWidth = 0)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var dirA = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).Flat().normalized;
                var dirB = (pts[Mathf.Min(i + 2, pts.Count - 1)] - pts[i]).Flat().normalized;
                var sa = new Vector3(-dirA.z, 0, dirA.x) * (width / 2);
                var sb = new Vector3(-dirB.z, 0, dirB.x) * (width / 2);
                var up = Vector3.up * yOffset;
                Quad(a - sa + up, a + sa + up, b + sb + up, b - sb + up, col);
                if (edge.HasValue && edgeWidth > 0)
                {
                    var ea = sa.normalized * edgeWidth; var eb = sb.normalized * edgeWidth;
                    var up2 = up + Vector3.up * 0.003f;
                    Quad(a - sa + up2, a - sa + ea + up2, b - sb + eb + up2, b - sb + up2, edge.Value);
                    Quad(a + sa - ea + up2, a + sa + up2, b + sb + up2, b + sb - eb + up2, edge.Value);
                }
            }
        }

        // Dodaj gotovu mrežu (npr. iz drugog MeshKit-a) sa trenutnom transformacijom.
        public void Append(MeshKit other)
        {
            int o = v.Count;
            for (int i = 0; i < other.v.Count; i++) { v.Add(m.MultiplyPoint3x4(other.v[i])); n.Add(m.MultiplyVector(other.n[i]).normalized); c.Add(other.c[i]); }
            foreach (var idx in other.t) t.Add(idx + o);
        }

        public Mesh ToMesh(string name = "Model")
        {
            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Napravi objekat sa mrežom i zajedničkim materijalom.
        public GameObject ToObject(string name, Transform parent, Material mat = null, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ToMesh(name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat != null ? mat : Mats.Lit;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }

        public static Color32 C(string hex, float glow = 0)
        {
            var col = (Color32)Col.Hex(hex);
            col.a = (byte)Mathf.RoundToInt(255 * (1 - glow));
            return col;
        }
        public static Color32 C(Color color, float glow = 0)
        {
            var col = (Color32)color;
            col.a = (byte)Mathf.RoundToInt(255 * (1 - glow));
            return col;
        }
    }
}
