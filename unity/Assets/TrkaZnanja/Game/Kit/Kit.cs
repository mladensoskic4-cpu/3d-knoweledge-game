// Sitni pomoćnici za Unity dio: boje iz "#rrggbb", pretvaranje Core vektora, tween korutine i materijali.
using System;
using System.Collections;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public static class Col
    {
        static readonly Dictionary<string, Color> cache = new Dictionary<string, Color>();

        public static Color Hex(string hex, float alpha = 1f)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            if (!cache.TryGetValue(hex, out var c))
            {
                if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
                cache[hex] = c;
            }
            c.a = alpha;
            return c;
        }

        public static Color WithA(this Color c, float a) { c.a = a; return c; }
        public static Color Mul(this Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);
        public static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c).ToLowerInvariant();
    }

    public static class V
    {
        public static Vector3 U(this Vec3d v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        public static Vector3 U(this Vec3d v, float y) => new Vector3((float)v.X, y, (float)v.Z);
        public static Vector2 U(this Vec2d v) => new Vector2((float)v.X, (float)v.Y);
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0, v.z);
    }

    public static class Tw
    {
        public static float EaseInOut(float k) => k < 0.5f ? 2 * k * k : 1 - Mathf.Pow(-2 * k + 2, 2) / 2;
        public static float EaseOut(float k) => 1 - (1 - k) * (1 - k) * (1 - k);
        public static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2);
        }

        // Korutina koja zove f(0..1) tokom `seconds` (ne zavisi od Time.timeScale ako je unscaled).
        public static IEnumerator Run(float seconds, Action<float> f, bool unscaled = false)
        {
            if (seconds <= 0) { f(1); yield break; }
            float t = 0;
            while (t < seconds)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                f(Mathf.Clamp01(t / seconds));
                yield return null;
            }
            f(1);
        }

        public static IEnumerator Wait(float seconds, bool unscaled = false)
        {
            float t = 0;
            while (t < seconds) { t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }

        // Čeka da se ispuni uslov.
        public static IEnumerator Until(Func<bool> cond) { while (!cond()) yield return null; }
    }

    // Materijali: svi modeli dijele isti stilizovani šejder; ako URP nije podešen, pada na ugrađeni.
    public static class Mats
    {
        static Material lit, water, sky, particleAlpha, particleAdd, uiBlur;

        static Shader Find(string name, string fallback)
        {
            var s = Shader.Find(name);
            if (s == null || !s.isSupported) s = Shader.Find(fallback);
            return s;
        }

        public static Material Lit => lit ??= new Material(Find("TrkaZnanja/Lit", "Standard")) { name = "Trka Lit", enableInstancing = true };

        public static Material NewLit(Color tint)
        {
            var m = new Material(Lit) { name = "Trka Lit (boja)" };
            m.SetColor("_BaseColor", tint);
            return m;
        }

        public static Material Water => water ??= new Material(Find("TrkaZnanja/Water", "Standard")) { name = "Trka voda" };
        public static Material Sky => sky ??= new Material(Find("TrkaZnanja/Sky", "Skybox/Procedural")) { name = "Trka nebo" };
        public static Material UIBlur => uiBlur ??= new Material(Find("TrkaZnanja/UIBlur", "UI/Default")) { name = "Trka UI blur" };

        public static Material Particle(bool additive)
        {
            if (additive && particleAdd != null) return particleAdd;
            if (!additive && particleAlpha != null) return particleAlpha;
            var m = new Material(Find("TrkaZnanja/Particle", "Sprites/Default")) { name = additive ? "Čestice (sjaj)" : "Čestice" };
            m.SetTexture("_MainTex", Tex.SoftDot);
            m.SetFloat("_SrcBlend", additive ? 5 : 5);     // SrcAlpha
            m.SetFloat("_DstBlend", additive ? 1 : 10);    // One / OneMinusSrcAlpha
            m.SetFloat("_Boost", additive ? 2.2f : 1f);
            if (additive) particleAdd = m; else particleAlpha = m;
            return m;
        }
    }

    // Teksture koje se prave u kodu (meka tačka za čestice, zaobljeni pravougaonici za UI…).
    public static class Tex
    {
        static Texture2D softDot, square;

        public static Texture2D SoftDot => softDot ??= Radial(64, 0.0f);
        public static Texture2D Square
        {
            get
            {
                if (square != null) return square;
                square = new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = "Kvadrat", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[64];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                square.SetPixels32(px);
                square.Apply();
                return square;
            }
        }

        public static Texture2D Radial(int size, float hard)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Meka tačka", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - Mathf.SmoothStep(hard, 1, d));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
