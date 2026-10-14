// Zamjena za Unity biblioteke SAMO za provjeru da se kod kompajlira bez Unity editora (i za izvoz modela u OBJ).
// Potpisi su prepisani iz Unity 6 API-ja; matematika (vektori, kvaternioni, matrice) radi stvarno.
// Ovaj folder se NE koristi u Unity projektu.
#pragma warning disable CS0067, CS0414, CS0649, CS1998, CS0169, CS0108
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized { get { var m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator *(float k, Vector2 a) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator /(Vector2 a, float k) => new Vector2(a.x / k, a.y / k);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object o) => o is Vector2 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { var m = magnitude; return m > 1e-5f ? this / m : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float k) => new Vector3(a.x * k, a.y * k, a.z * k);
        public static Vector3 operator *(float k, Vector3 a) => a * k;
        public static Vector3 operator /(Vector3 a, float k) => new Vector3(a.x / k, a.y / k, a.z / k);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object o) => o is Vector3 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Angle(Vector3 a, Vector3 b)
        {
            var d = Mathf.Clamp(Dot(a.normalized, b.normalized), -1, 1);
            return (float)(Math.Acos(d) * 180 / Math.PI);
        }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            var a = Angle(from, to);
            var s = Mathf.Sign(Dot(axis, Cross(from, to)));
            return a * s;
        }
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);

        // Unity: Euler(x, y, z) = rotacija oko Z, pa X, pa Y (primijenjeno na vektor: Y * X * Z)
        public static Quaternion Euler(float x, float y, float z)
        {
            return AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward);
        }
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);

        public static Quaternion AngleAxis(float deg, Vector3 axis)
        {
            axis = axis.normalized;
            var h = deg * Mathf.Deg2Rad * 0.5f;
            var s = (float)Math.Sin(h);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, (float)Math.Cos(h));
        }

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var u = new Vector3(q.x, q.y, q.z);
            var t = Vector3.Cross(u, v) * 2;
            return v + t * q.w + Vector3.Cross(u, t);
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from = from.normalized; to = to.normalized;
            var d = Vector3.Dot(from, to);
            if (d > 0.999999f) return identity;
            if (d < -0.999999f)
            {
                var axis = Vector3.Cross(Vector3.right, from);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(Vector3.up, from);
                return AngleAxis(180, axis);
            }
            var c = Vector3.Cross(from, to);
            var q = new Quaternion(c.x, c.y, c.z, 1 + d);
            var m = (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }

        public static Quaternion LookRotation(Vector3 forward) => LookRotation(forward, Vector3.up);
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            var f = forward.normalized;
            var r = Vector3.Cross(up, f).normalized;
            if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
            var u = Vector3.Cross(f, r);
            // matrica kolona (r, u, f) → kvaternion
            float m00 = r.x, m01 = u.x, m02 = f.x, m10 = r.y, m11 = u.y, m12 = f.y, m20 = r.z, m21 = u.z, m22 = f.z;
            float tr = m00 + m11 + m22;
            if (tr > 0)
            {
                var s = (float)Math.Sqrt(tr + 1) * 2;
                return new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s);
            }
            if (m00 > m11 && m00 > m22)
            {
                var s = (float)Math.Sqrt(1 + m00 - m11 - m22) * 2;
                return new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            if (m11 > m22)
            {
                var s = (float)Math.Sqrt(1 + m11 - m00 - m22) * 2;
                return new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            {
                var s = (float)Math.Sqrt(1 + m22 - m00 - m11) * 2;
                return new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s);
            }
        }
        public Vector3 eulerAngles => Vector3.zero;
    }

    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
        public static Matrix4x4 identity => new Matrix4x4 { m00 = 1, m11 = 1, m22 = 1, m33 = 1 };
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s)
        {
            var x = q * new Vector3(s.x, 0, 0); var y = q * new Vector3(0, s.y, 0); var z = q * new Vector3(0, 0, s.z);
            return new Matrix4x4
            {
                m00 = x.x, m10 = x.y, m20 = x.z, m01 = y.x, m11 = y.y, m21 = y.z, m02 = z.x, m12 = z.y, m22 = z.z,
                m03 = p.x, m13 = p.y, m23 = p.z, m33 = 1
            };
        }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var r = new Matrix4x4();
            float[,] A = a.ToArr(), B = b.ToArr(), R = new float[4, 4];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) { float s = 0; for (int k = 0; k < 4; k++) s += A[i, k] * B[k, j]; R[i, j] = s; }
            return FromArr(R);
        }
        float[,] ToArr() => new float[,] { { m00, m01, m02, m03 }, { m10, m11, m12, m13 }, { m20, m21, m22, m23 }, { m30, m31, m32, m33 } };
        static Matrix4x4 FromArr(float[,] R) => new Matrix4x4
        {
            m00 = R[0, 0], m01 = R[0, 1], m02 = R[0, 2], m03 = R[0, 3], m10 = R[1, 0], m11 = R[1, 1], m12 = R[1, 2], m13 = R[1, 3],
            m20 = R[2, 0], m21 = R[2, 1], m22 = R[2, 2], m23 = R[2, 3], m30 = R[3, 0], m31 = R[3, 1], m32 = R[3, 2], m33 = R[3, 3]
        };
        public Vector3 MultiplyPoint3x4(Vector3 v) => new Vector3(m00 * v.x + m01 * v.y + m02 * v.z + m03, m10 * v.x + m11 * v.y + m12 * v.z + m13, m20 * v.x + m21 * v.y + m22 * v.z + m23);
        public Vector3 MultiplyVector(Vector3 v) => new Vector3(m00 * v.x + m01 * v.y + m02 * v.z, m10 * v.x + m11 * v.y + m12 * v.z, m20 * v.x + m21 * v.y + m22 * v.z);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color magenta => new Color(1, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color operator *(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a * k);
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static implicit operator Color32(Color c) => new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.a * 255), 0, 255));
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static implicit operator Vector4(Color c) => new Vector4(c.r, c.g, c.b, c.a);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            c = Color.magenta;
            if (s == null || !s.StartsWith("#") || (s.Length != 7 && s.Length != 9)) return false;
            int v(int i) => Convert.ToInt32(s.Substring(i, 2), 16);
            c = new Color(v(1) / 255f, v(3) / 255f, v(5) / 255f, s.Length == 9 ? v(7) / 255f : 1);
            return true;
        }
        public static string ToHtmlStringRGB(Color c) => $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public static float Tan(float f) => (float)System.Math.Tan(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int f) => Math.Abs(f);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Exp(float a) => (float)Math.Exp(a);
        public static float Sign(float f) => f >= 0 ? 1 : -1;
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Round(float v) => (float)Math.Round(v);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float SmoothStep(float from, float to, float t) { t = Clamp01(t); t = -2f * t * t * t + 3f * t * t; return to * t + from * (1f - t); }
        public static float MoveTowards(float c, float t, float d) => Math.Abs(t - c) <= d ? t : c + Math.Sign(t - c) * d;
        public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.ToEven);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public static Rect MinMaxRect(float x0, float y0, float x1, float y1) => new Rect(x0, y0, x1 - x0, y1 - y0);
        public float xMin => x; public float yMin => y; public float xMax => x + width; public float yMax => y + height;
        public Vector2 size => new Vector2(width, height);
        public Vector2 center => new Vector2(x + width / 2, y + height / 2);
        public static bool operator ==(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public override bool Equals(object o) => o is Rect r && r == this;
        public override int GetHashCode() => 0;
    }

    public struct Bounds
    {
        public Vector3 center, size;
        public Bounds(Vector3 c, Vector3 s) { center = c; size = s; }
        public Vector3 min => center - size * 0.5f;
        public Vector3 max => center + size * 0.5f;
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
        public Vector3 extents => size * 0.5f;
        public void Encapsulate(Bounds b) { }
    }

    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object o) { if (o is GameObject g) { g.destroyed = true; g.transform.parent = null; } }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T o) where T : Object => o;
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public static implicit operator bool(Object o) => o is object;
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => 0;
    }

    public enum HideFlags { None }
    public enum PrimitiveType { Cube }
    public enum SendMessageOptions { RequireReceiver }

    public class Component : Object
    {
        internal GameObject go;
        public GameObject gameObject => go;
        Transform tr;
        public Transform transform => this is Transform t ? t : go != null ? go.transform : (tr ??= new Transform());
        public T GetComponent<T>() => go != null ? go.GetComponent<T>() : default;
        public T[] GetComponents<T>() => go != null ? go.GetComponents<T>() : new T[0];
        public T[] GetComponentsInChildren<T>() => go != null ? go.GetComponentsInChildren<T>() : new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) => GetComponentsInChildren<T>();
        public T GetComponentInParent<T>() => default;
        public T[] GetComponentsInParent<T>(bool includeInactive) => new T[0];
        public bool TryGetComponent<T>(out T c) { c = GetComponent<T>(); return c != null; }
        public string tag { get; set; }
    }

    public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => true; }

    public class Coroutine { }
    public class YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } public override bool keepWaiting => false; }
    public abstract class CustomYieldInstruction : IEnumerator { public abstract bool keepWaiting { get; } public object Current => null; public bool MoveNext() => keepWaiting; public void Reset() { } }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator e) => null;
        public void StopAllCoroutines() { }
        public void StopCoroutine(Coroutine c) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }

    public sealed class GameObject : Object
    {
        readonly List<Component> comps = new List<Component>();
        public GameObject() : this("GameObject") { }
        public GameObject(string name) { this.name = name; transform = new Transform(); transform.go = this; }
        public GameObject(string name, params Type[] components) : this(name) { }
        public Transform transform { get; }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get; private set; } = true;
        public void SetActive(bool v) { activeSelf = v; }
        public T AddComponent<T>() where T : Component
        {
            var c = (T)Activator.CreateInstance(typeof(T));
            c.go = this;
            comps.Add(c);
            if (c is MeshFilter mf) MeshFilter.All.Add(mf);
            if (c is ParticleSystem) { var r = new ParticleSystemRenderer(); r.go = this; comps.Add(r); }
            return c;
        }
        public Component AddComponent(Type t) => null;
        public T GetComponent<T>() { foreach (var c in comps) if (c is T t) return t; return default; }
        public T[] GetComponents<T>() { var l = new List<T>(); foreach (var c in comps) if (c is T t) l.Add(t); return l.ToArray(); }
        public T[] GetComponentsInChildren<T>()
        {
            var l = new List<T>(GetComponents<T>());
            foreach (var ch in transform.children) l.AddRange(ch.gameObject.GetComponentsInChildren<T>());
            return l.ToArray();
        }
        public bool TryGetComponent<T>(out T c) { c = GetComponent<T>(); return c != null; }
        internal bool destroyed;
    }

    public class Transform : Component, IEnumerable
    {
        internal readonly List<Transform> children = new List<Transform>();
        Transform par;
        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Vector3 localScale { get; set; } = Vector3.one;
        public Matrix4x4 World => (par != null ? par.World : Matrix4x4.identity) * Matrix4x4.TRS(localPosition, localRotation, localScale);
        public Vector3 position
        {
            get => World.MultiplyPoint3x4(Vector3.zero);
            set => localPosition = par == null ? value : value - par.position;   // pregled: roditelji bez rotacije
        }
        public Quaternion rotation { get => (par != null ? par.rotation : Quaternion.identity) * localRotation; set => localRotation = value; }
        public Vector3 forward { get => rotation * Vector3.forward; set => rotation = Quaternion.LookRotation(value); }
        public Vector3 right => rotation * Vector3.right;
        public Vector3 up => rotation * Vector3.up;
        public Transform parent { get => par; set => SetParent(value); }
        public int childCount => children.Count;
        public Transform GetChild(int i) => children[i];
        public void SetParent(Transform p, bool worldPositionStays) { par?.children.Remove(this); par = p; p?.children.Add(this); }
        public void SetParent(Transform p) => SetParent(p, true);
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
        public void LookAt(Vector3 p) { }
        public Vector3 TransformPoint(Vector3 p) => World.MultiplyPoint3x4(p);
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public IEnumerator GetEnumerator() => children.GetEnumerator();
        public Transform Find(string n) => children.Find(c => c.go != null && c.go.name == n);
        public bool IsChildOf(Transform p) { for (var x = this; x != null; x = x.par) if (x == p) return true; return false; }
    }

    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => new Rect();
        public void GetWorldCorners(Vector3[] c) { }
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint) { localPoint = Vector2.zero; return true; }
    }

    public static class Time
    {
        public static float deltaTime => 0.016f;
        public static float realtimeSinceStartup => 0;
        public static float unscaledDeltaTime => 0.016f;
        public static float time => 0;
        public static float unscaledTime => 0;
        public static float timeScale { get; set; }
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
        public static Rect safeArea => new Rect(0, 0, 1920, 1080);
    }

    public enum SystemLanguage { Serbian, English, Unknown }
    public enum RuntimePlatform { Android }

    public static class Application
    {
        public static bool isMobilePlatform => false;
        public static bool isBatchMode => false;
        public static SystemLanguage systemLanguage => SystemLanguage.English;
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath => "/tmp";
        public static string dataPath => "/tmp/Assets";
        public static bool isPlaying => true;
    }

    public static class SystemInfo { public static int systemMemorySize => 4096; }

    public static class PlayerPrefs
    {
        public static string GetString(string k, string d = "") => d;
        public static void SetString(string k, string v) { }
        public static void Save() { }
    }

    public static class Debug
    {
        public static void Log(object o) { }
        public static void LogWarning(object o) { }
        public static void LogError(object o) { }
    }

    public static class Random
    {
        static readonly System.Random r = new System.Random(1);
        public static float value => (float)r.NextDouble();
        public static float Range(float a, float b) => a + (float)r.NextDouble() * (b - a);
        public static int Range(int a, int b) => r.Next(a, b);
    }

    public enum KeyCode { None = 0, Return = 13, Space = 32, Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Keypad0 = 256, Keypad1, KeypadEnter = 271 }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static Vector3 mousePosition => Vector3.zero;
        public static int touchCount => 0;
        public static Touch GetTouch(int i) => default;
    }
    public struct Touch { public Vector2 position { get; set; } }

    public static class Handheld { public static void Vibrate() { } }

    public sealed class AndroidJavaClass : AndroidJavaObject { public AndroidJavaClass(string n) : base(n) { } }
    public class AndroidJavaObject : IDisposable
    {
        public AndroidJavaObject(string n, params object[] a) { }
        public T GetStatic<T>(string f) => default;
        public T Call<T>(string m, params object[] a) => default;
        public void Call(string m, params object[] a) { }
        public void Dispose() { }
    }

    public enum TextureFormat { RGB24 = 3, RGBA32 = 4, R8 = 63 }
    public enum RenderTextureFormat { ARGB32 }
    public sealed class RenderTexture : Texture
    {
        public RenderTexture(int w, int h, int depth, RenderTextureFormat f) { }
        public static RenderTexture active { get; set; }
        public void Release() { }
    }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public class Texture : Object
    {
        public int width { get; }
        public int height { get; }
        public TextureWrapMode wrapMode { get; set; }
        public TextureWrapMode wrapModeU { get; set; }
        public TextureWrapMode wrapModeV { get; set; }
        public FilterMode filterMode { get; set; }
        public int anisoLevel { get; set; }
        public int mipmapCount => 1;
    }
    public sealed class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixels32(Color32[] px) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public void LoadRawTextureData(byte[] data) { }
        public void ReadPixels(Rect r, int x, int y) { }
        public byte[] EncodeToPNG() => new byte[0];
    }

    public enum SpriteMeshType { FullRect, Tight }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot) => new Sprite();
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType mt, Vector4 border) => new Sprite();
    }

    public sealed class Shader : Object
    {
        public static Shader Find(string n) => new Shader();
        public bool isSupported => true;
        public static void SetGlobalFloat(string n, float v) { }
        public static void SetGlobalColor(string n, Color c) { }
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Material(Material m) { }
        public bool enableInstancing { get; set; }
        public Texture mainTexture { get; set; }
        public void SetColor(string n, Color c) { }
        public void SetFloat(string n, float v) { }
        public void SetVector(string n, Vector4 v) { }
        public void SetTexture(string n, Texture t) { }
    }

    public sealed class Mesh : Object
    {
        public Rendering.IndexFormat indexFormat { get; set; }
        public Vector3[] vertices { get; set; } = new Vector3[0];
        public Color32[] colors32 { get; set; }
        public int[] triangles { get; set; } = new int[0];
        public int vertexCount => vertices.Length;
        public Bounds bounds { get; set; }
        public void SetVertices(List<Vector3> v) => vertices = v.ToArray();
        public void SetNormals(List<Vector3> n) { }
        public void SetColors(List<Color32> c) => colors32 = c.ToArray();
        public void SetTriangles(List<int> t, int sub) => triangles = t.ToArray();
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
    }

    public sealed class MeshFilter : Component { public static readonly List<MeshFilter> All = new List<MeshFilter>(); public Mesh sharedMesh { get; set; } }
    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Bounds bounds => new Bounds();
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }
    public sealed class MeshRenderer : Renderer { }

    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public float shadowBias { get; set; }
        public float shadowNormalBias { get; set; }
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth, Nothing }
    public sealed class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float aspect { get; set; }
        public RenderTexture targetTexture { get; set; }
        public void Render() { }
        public float fieldOfView { get; set; }
        public void ResetAspect() { }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public Vector3 WorldToViewportPoint(Vector3 p) => p;
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }
    public sealed class AudioListener : Behaviour { }

    public enum FogMode { Linear = 1, Exponential, ExponentialSquared }
    public static class RenderSettings
    {
        public static Material skybox { get; set; }
        public static Light sun { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static Color fogColor { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static Rendering.SphericalHarmonicsL2 ambientProbe { get; set; }
    }

    public static class QualitySettings
    {
        public static Rendering.RenderPipelineAsset renderPipeline { get; set; }
        public static int vSyncCount { get; set; }
        public static string[] names => new string[0];
        public static int GetQualityLevel() => 0;
        public static void SetQualityLevel(int i, bool applyExpensive) { }
    }

    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => new AudioClip();
        public bool SetData(float[] data, int offset) => true;
    }
    public sealed class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public float volume { get; set; }
        public bool isPlaying => false;
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip c, float v) { }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object
        {
            if (typeof(T) != typeof(TextAsset)) return null;
            var root = Environment.GetEnvironmentVariable("TRKA_RES") ?? "unity/Assets/TrkaZnanja/Resources";
            foreach (var ext in new[] { ".json", ".txt" })
            {
                var f = System.IO.Path.Combine(root, path + ext);
                if (System.IO.File.Exists(f)) return (T)(Object)new TextAsset(System.IO.File.ReadAllText(f));
            }
            return null;
        }
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }

    public sealed class TextAsset : Object { public TextAsset(string t) { text = t; } public string text { get; } }

    public sealed class Font : Object { public string[] fontNames { get; set; } }

    public sealed class Keyframe { }
    public sealed class AnimationCurve
    {
        public static AnimationCurve Linear(float t0, float v0, float t1, float v1) => new AnimationCurve();
        public static AnimationCurve EaseInOut(float t0, float v0, float t1, float v1) => new AnimationCurve();
    }

    public struct GradientColorKey { public GradientColorKey(Color c, float t) { } }
    public struct GradientAlphaKey { public GradientAlphaKey(float a, float t) { } }
    public sealed class Gradient { public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) { } }

    public enum ParticleSystemSimulationSpace { Local, World }
    public enum ParticleSystemShapeType { Sphere = 0, Cone = 4, Box = 5 }
    public enum ParticleSystemRenderMode { Billboard = 0, Stretch = 1 }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemGradientMode { Color, Gradient, TwoColors, TwoGradients, RandomColor }

    public sealed class ParticleSystem : Component
    {
        public struct MinMaxCurve
        {
            public MinMaxCurve(float c) { }
            public MinMaxCurve(float min, float max) { }
            public MinMaxCurve(float multiplier, AnimationCurve curve) { }
            public static implicit operator MinMaxCurve(float c) => new MinMaxCurve(c);
        }
        public struct MinMaxGradient
        {
            public MinMaxGradient(Color c) { mode = ParticleSystemGradientMode.Color; }
            public MinMaxGradient(Color a, Color b) { mode = ParticleSystemGradientMode.TwoColors; }
            public MinMaxGradient(Gradient g) { mode = ParticleSystemGradientMode.Gradient; }
            public ParticleSystemGradientMode mode { get; set; }
            public static implicit operator MinMaxGradient(Color c) => new MinMaxGradient(c);
            public static implicit operator MinMaxGradient(Gradient g) => new MinMaxGradient(g);
        }
        public struct Burst { public Burst(float time, short count) { } }
        public struct MainModule
        {
            public bool loop { get; set; }
            public bool playOnAwake { get; set; }
            public float duration { get; set; }
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxCurve startRotation { get; set; }
            public MinMaxGradient startColor { get; set; }
            public int maxParticles { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
            public MinMaxCurve gravityModifier { get; set; }
        }
        public struct EmissionModule
        {
            public MinMaxCurve rateOverTime { get; set; }
            public void SetBursts(Burst[] b) { }
        }
        public struct ShapeModule
        {
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
            public float angle { get; set; }
            public Vector3 scale { get; set; }
            public Vector3 rotation { get; set; }
        }
        public struct NoiseModule { public bool enabled { get; set; } public MinMaxCurve strength { get; set; } public float frequency { get; set; } }
        public struct SizeOverLifetimeModule { public bool enabled { get; set; } public MinMaxCurve size { get; set; } }
        public struct ColorOverLifetimeModule { public bool enabled { get; set; } public MinMaxGradient color { get; set; } }
        public struct RotationOverLifetimeModule { public bool enabled { get; set; } public MinMaxCurve z { get; set; } }
        public struct TrailModule { public bool enabled { get; set; } public MinMaxCurve lifetime { get; set; } public MinMaxCurve widthOverTrail { get; set; } }
        public MainModule main => default;
        public EmissionModule emission => default;
        public ShapeModule shape => default;
        public NoiseModule noise => default;
        public SizeOverLifetimeModule sizeOverLifetime => default;
        public struct VelocityOverLifetimeModule { public bool enabled { get; set; } public ParticleSystemSimulationSpace space { get; set; } public MinMaxCurve x { get; set; } public MinMaxCurve y { get; set; } public MinMaxCurve z { get; set; } }
        public VelocityOverLifetimeModule velocityOverLifetime => default;
        public ColorOverLifetimeModule colorOverLifetime => default;
        public RotationOverLifetimeModule rotationOverLifetime => default;
        public TrailModule trails => default;
        public void Play() { }
        public void Stop(bool withChildren, ParticleSystemStopBehavior b) { }
    }
    public sealed class ParticleSystemRenderer : Renderer
    {
        public ParticleSystemRenderMode renderMode { get; set; }
        public float velocityScale { get; set; }
        public float lengthScale { get; set; }
        public Material trailMaterial { get; set; }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public enum ColorSpace { Gamma, Linear }
}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
    public enum ShadowCastingMode { Off, On }
    public enum AmbientMode { Skybox, Trilight, Flat = 3 }
    public enum BlendMode { Zero, One }
    public struct SphericalHarmonicsL2
    {
        public void AddAmbientLight(Color c) { }
        public void AddDirectionalLight(Vector3 dir, Color c, float intensity) { }
    }
    public abstract class RenderPipelineAsset : ScriptableObject { }
    public static class GraphicsSettings
    {
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset currentRenderPipeline => null;
    }
    public class VolumeParameter<T> { public T value; public void Override(T v) { } }
    public sealed class ClampedFloatParameter : VolumeParameter<float> { }
    public sealed class MinFloatParameter : VolumeParameter<float> { }
    public sealed class FloatParameter : VolumeParameter<float> { }
    public sealed class ColorParameter : VolumeParameter<Color> { }
    public class VolumeComponent : ScriptableObject { public bool active; }
    public sealed class VolumeProfile : ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent, new() => new T(); }
    public sealed class Volume : MonoBehaviour { public bool isGlobal { get; set; } public float priority { get; set; } public VolumeProfile sharedProfile { get; set; } }
}

namespace UnityEngine.Rendering.Universal
{
    public enum TonemappingMode { None, Neutral, ACES }
    public enum DepthOfFieldMode { Off, Gaussian, Bokeh }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing }
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new TonemappingModeParameter(); }
    public sealed class Bloom : VolumeComponent { public MinFloatParameter threshold = new MinFloatParameter(), intensity = new MinFloatParameter(); public ClampedFloatParameter scatter = new ClampedFloatParameter(); public ColorParameter tint = new ColorParameter(); }
    public sealed class ColorAdjustments : VolumeComponent { public FloatParameter postExposure = new FloatParameter(); public ClampedFloatParameter contrast = new ClampedFloatParameter(), saturation = new ClampedFloatParameter(); }
    public sealed class Vignette : VolumeComponent { public ClampedFloatParameter intensity = new ClampedFloatParameter(), smoothness = new ClampedFloatParameter(); }
    public sealed class WhiteBalance : VolumeComponent { public ClampedFloatParameter temperature = new ClampedFloatParameter(); }
    public sealed class DepthOfField : VolumeComponent
    {
        public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter();
        public MinFloatParameter gaussianStart = new MinFloatParameter(), gaussianEnd = new MinFloatParameter();
        public ClampedFloatParameter gaussianMaxRadius = new ClampedFloatParameter();
    }
    public sealed class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
        public bool requiresDepthTexture { get; set; }
    }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => null; }
    public class ScriptableRendererData : ScriptableObject { }
    public sealed class PostProcessData : ScriptableObject { }
    public sealed class UniversalRendererData : ScriptableRendererData { public PostProcessData postProcessData; }
    public sealed class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public static UniversalRenderPipelineAsset Create(ScriptableRendererData d = null) => new UniversalRenderPipelineAsset();
        public float renderScale { get; set; }
        public int msaaSampleCount { get; set; }
        public float shadowDistance { get; set; }
        public int shadowCascadeCount { get; set; }
        public bool supportsHDR { get; set; }
        public bool supportsCameraDepthTexture { get; set; }
    }
}

namespace UnityEngine.Networking
{
    public class DownloadHandler { public string text => ""; }
    public sealed class DownloadHandlerBuffer : DownloadHandler { }
    public class UploadHandler { public string contentType { get; set; } }
    public sealed class UploadHandlerRaw : UploadHandler { public UploadHandlerRaw(byte[] d) { } }
    public sealed class UnityWebRequestAsyncOperation : YieldInstruction { }
    public sealed class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
        public UnityWebRequest(string url, string method) { }
        public DownloadHandler downloadHandler { get; set; }
        public UploadHandler uploadHandler { get; set; }
        public int timeout { get; set; }
        public Result result => Result.Success;
        public long responseCode => 200;
        public void SetRequestHeader(string k, string v) { }
        public UnityWebRequestAsyncOperation SendWebRequest() => null;
        public void Dispose() { }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public static class SceneManager { public static void MoveGameObjectToScene(GameObject g, Scene s) { } }
}
