// Mali zamjenik za NUnit, samo za pokretanje Core testova van Unity-ja (NuGet nije dostupan svuda).
// U Unity-ju se koristi pravi NUnit iz Test Framework paketa; ovaj fajl se tamo ne kompajlira.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestCaseAttribute : Attribute { public object[] Args; public TestCaseAttribute(params object[] args) { Args = args; } }

    public sealed class AssertionException : Exception { public AssertionException(string m) : base(m) { } }

    public interface IConstraint { bool Matches(object actual, out string description); }

    sealed class Fn : IConstraint
    {
        readonly Func<object, bool> f; readonly string d;
        public Fn(Func<object, bool> f, string d) { this.f = f; this.d = d; }
        public bool Matches(object a, out string description) { description = d; return f(a); }
    }

    public static class Is
    {
        static double D(object o) => Convert.ToDouble(o);
        public static IConstraint InRange(double a, double b) => new Fn(x => D(x) >= a && D(x) <= b, $"u opsegu [{a}, {b}]");
        public static IConstraint InRange(float a, float b) => InRange((double)a, (double)b);
        public static IConstraint LessThan(double a) => new Fn(x => D(x) < a, $"manje od {a}");
        public static IConstraint LessThanOrEqualTo(double a) => new Fn(x => D(x) <= a, $"najviše {a}");
        public static IConstraint GreaterThan(double a) => new Fn(x => D(x) > a, $"veće od {a}");
        public static IConstraint EqualTo(object e) => new Fn(x => Equals(x, e), $"jednako {e}");
    }

    public static class Assert
    {
        static void Fail(string msg, string user) => throw new AssertionException(string.IsNullOrEmpty(user) ? msg : msg + " — " + user);
        static double D(object o) => Convert.ToDouble(o);

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (expected is IConvertible && actual is IConvertible && !(expected is string) && !(actual is string))
            {
                if (Math.Abs(D(expected) - D(actual)) < 1e-12) return;
            }
            else if (Equals(expected, actual)) return;
            Fail($"očekivano <{expected}>, dobijeno <{actual}>", message);
        }
        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (Math.Abs(expected - actual) > delta) Fail($"očekivano {expected} ± {delta}, dobijeno {actual}", message);
        }
        public static void IsTrue(bool c, string message = null) { if (!c) Fail("očekivano true", message); }
        public static void IsFalse(bool c, string message = null) { if (c) Fail("očekivano false", message); }
        public static void IsNotNull(object o, string message = null) { if (o == null) Fail("očekivano da nije null", message); }
        public static void IsNull(object o, string message = null) { if (o != null) Fail("očekivano null", message); }
        public static void Greater(double a, double b, string message = null) { if (!(a > b)) Fail($"{a} nije veće od {b}", message); }
        public static void GreaterOrEqual(double a, double b, string message = null) { if (!(a >= b)) Fail($"{a} nije ≥ {b}", message); }
        public static void Less(double a, double b, string message = null) { if (!(a < b)) Fail($"{a} nije manje od {b}", message); }
        public static void LessOrEqual(double a, double b, string message = null) { if (!(a <= b)) Fail($"{a} nije ≤ {b}", message); }
        public static void That(object actual, IConstraint c, string message = null)
        {
            if (!c.Matches(actual, out var d)) Fail($"<{actual}> nije {d}", message);
        }
        public static void That(bool cond, string message = null) { if (!cond) Fail("uslov nije ispunjen", message); }
        public static T Throws<T>(Action a) where T : Exception
        {
            try { a(); }
            catch (T e) { return e; }
            catch (Exception e) { Fail($"očekivan {typeof(T).Name}, bačen {e.GetType().Name}", null); }
            Fail($"očekivan {typeof(T).Name}, ništa nije bačeno", null);
            return null;
        }
    }

    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable expected, IEnumerable actual, string message = null)
        {
            var a = expected.Cast<object>().ToList(); var b = actual?.Cast<object>().ToList() ?? new List<object>();
            if (a.Count != b.Count || a.Where((x, i) => !Equals(x, b[i])).Any())
                throw new AssertionException($"kolekcije se razlikuju: [{string.Join(", ", a)}] vs [{string.Join(", ", b)}] {message}");
        }
    }
}

namespace TrkaZnanja.LocalRunner
{
    using NUnit.Framework;

    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--teren") return DumpTerrain(args[1], args.Length > 2 ? args[2] : "teren.ppm");
#if UNITY_EDITOR_STUB
            if (args.Length >= 2 && args[0] == "--modeli") return TrkaZnanja.LocalTools.MeshExport.Run(args[1]);
            if (args.Length >= 4 && args[0] == "--scena") return TrkaZnanja.LocalTools.MeshExport.Scene(args[1], args[2], args[3]);
#endif
            var filter = args.Length > 0 ? args[0] : null;
            int pass = 0, fail = 0;
            var sw = Stopwatch.StartNew();
            foreach (var type in typeof(Program).Assembly.GetTypes().Where(t => t.Namespace == "TrkaZnanja.Tests" && t.IsClass && !t.IsAbstract).OrderBy(t => t.Name))
                foreach (var m in type.GetMethods().Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any() || m.GetCustomAttributes(typeof(TestCaseAttribute), false).Any()))
                {
                    var name = $"{type.Name}.{m.Name}";
                    if (filter != null && !name.Contains(filter)) continue;
                    var cases = m.GetCustomAttributes(typeof(TestCaseAttribute), false).Cast<TestCaseAttribute>().Select(c => c.Args).ToList();
                    if (cases.Count == 0) cases.Add(Array.Empty<object>());
                    foreach (var a in cases)
                    {
                        var t0 = sw.ElapsedMilliseconds;
                        try
                        {
                            m.Invoke(Activator.CreateInstance(type), a);
                            pass++;
                            Console.WriteLine($"  ✓ {name} ({sw.ElapsedMilliseconds - t0} ms)");
                        }
                        catch (TargetInvocationException e)
                        {
                            fail++;
                            Console.WriteLine($"  ✗ {name}: {e.InnerException?.Message}");
                            if (!(e.InnerException is AssertionException)) Console.WriteLine(e.InnerException?.StackTrace);
                        }
                    }
                }
            Console.WriteLine($"\n{pass} prošlo, {fail} palo ({sw.ElapsedMilliseconds} ms)");
            return fail == 0 ? 0 : 1;
        }

        // Slika terena odozgo (PPM), za brzu provjeru izgleda bez Unity-ja.
        static int DumpTerrain(string mapId, string outPath)
        {
            var map = TrkaZnanja.Core.Catalog.MapOf(mapId);
            var track = TrkaZnanja.Core.TrackLayout.Build(map, 58);
            var dir = Environment.GetEnvironmentVariable("TRKA_DATA") ?? "unity/Assets/TrkaZnanja/Resources/Data";
            var d = TrkaZnanja.Core.Json.Obj(TrkaZnanja.Core.Json.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "Mape", map.Land + ".json"))));
            var land = TrkaZnanja.Core.Geo.ParsePolygons(d["polygons"]);
            var lakes = TrkaZnanja.Core.GeoData.LakesFor(map.Id);
            var sw = Stopwatch.StartNew();
            var T = TrkaZnanja.Core.TerrainBuilder.Build(track, land, lakes, new TrkaZnanja.Core.TerrainParams(), map.Id);
            Console.WriteLine($"teren {T.Nx}×{T.Nz} za {sw.ElapsedMilliseconds} ms, drveća {T.Trees.Count}, kuća {T.Houses.Count}");
            using var f = new System.IO.StreamWriter(outPath);
            f.Write($"P3\n{T.Nx} {T.Nz}\n255\n");
            for (int iz = T.Nz - 1; iz >= 0; iz--)
                for (int ix = 0; ix < T.Nx; ix++)
                {
                    int i = T.Idx(ix, iz);
                    var h = T.Height[i];
                    int r, g, b;
                    switch (T.Kind[i])
                    {
                        case TrkaZnanja.Core.Ground.Sea: r = 31; g = 134; b = 194; break;
                        case TrkaZnanja.Core.Ground.Lake: r = 60; g = 160; b = 210; break;
                        case TrkaZnanja.Core.Ground.Sand: r = 233; g = 217; b = 166; break;
                        case TrkaZnanja.Core.Ground.Snow: r = 250; g = 250; b = 255; break;
                        case TrkaZnanja.Core.Ground.Rock: r = 140; g = 135; b = 125; break;
                        default: r = 110; g = 180; b = 80; break;
                    }
                    var k = 0.75 + Math.Min(0.4, h * 0.12);
                    f.Write($"{(int)Math.Min(255, r * k)} {(int)Math.Min(255, g * k)} {(int)Math.Min(255, b * k)} ");
                }
            foreach (var t in track.Tiles) Console.Write("");
            return 0;
        }
    }
}
