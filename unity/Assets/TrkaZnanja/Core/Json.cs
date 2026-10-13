// Mali JSON čitač i pisač bez zavisnosti (radi i u Unity-ju i u običnom .NET-u, pa se može testirati bilo gdje).
// Objekti postaju Dictionary<string, object>, nizovi List<object>, brojevi double, ostalo string / bool / null.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TrkaZnanja.Core
{
    public static class Json
    {
        public static object Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var p = new Parser(text);
            p.SkipWs();
            var v = p.Value();
            p.SkipWs();
            if (!p.End) throw p.Error("višak teksta poslije JSON vrijednosti");
            return v;
        }

        public static string Write(object value, bool pretty = false)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        // ---- pomoćnici za čitanje ----
        public static Dictionary<string, object> Obj(object o) => o as Dictionary<string, object>;
        public static List<object> Arr(object o) => o as List<object>;

        public static object Get(Dictionary<string, object> d, string key) =>
            d != null && d.TryGetValue(key, out var v) ? v : null;

        public static string Str(Dictionary<string, object> d, string key, string def = null)
        {
            var v = Get(d, key);
            if (v is string s) return s;
            if (v is double n) return n.ToString(CultureInfo.InvariantCulture);
            if (v is bool b) return b ? "true" : "false";
            return def;
        }

        public static double Num(Dictionary<string, object> d, string key, double def = 0)
        {
            var v = Get(d, key);
            if (v is double n) return n;
            if (v is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var r)) return r;
            if (v is bool b) return b ? 1 : 0;
            return def;
        }

        public static int Int(Dictionary<string, object> d, string key, int def = 0) => (int)Math.Round(Num(d, key, def));

        public static bool Bool(Dictionary<string, object> d, string key, bool def = false)
        {
            var v = Get(d, key);
            if (v is bool b) return b;
            if (v is double n) return n != 0;
            return def;
        }

        public static List<string> StrList(object o)
        {
            var list = new List<string>();
            if (o is List<object> a)
                foreach (var x in a) list.Add(x is string s ? s : x is double n ? n.ToString(CultureInfo.InvariantCulture) : x?.ToString());
            return list;
        }

        // ---- čitač ----
        class Parser
        {
            readonly string s;
            int i;
            public Parser(string s) { this.s = s; }
            public bool End => i >= s.Length;

            public FormatException Error(string msg) => new FormatException($"JSON greška na poziciji {i}: {msg}");

            public void SkipWs()
            {
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '﻿') i++;
                    else break;
                }
            }

            public object Value()
            {
                if (End) throw Error("neočekivan kraj");
                char c = s[i];
                switch (c)
                {
                    case '{': return Object();
                    case '[': return Array();
                    case '"': return String();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return Number();
                        throw Error($"neočekivan znak '{c}'");
                }
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error($"očekivano '{word}'");
                i += word.Length;
            }

            Dictionary<string, object> Object()
            {
                var d = new Dictionary<string, object>();
                i++; SkipWs();
                if (!End && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || s[i] != '"') throw Error("očekivan ključ");
                    var key = String();
                    SkipWs();
                    if (End || s[i] != ':') throw Error("očekivano ':'");
                    i++; SkipWs();
                    d[key] = Value();
                    SkipWs();
                    if (End) throw Error("nezatvoren objekat");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw Error("očekivano ',' ili '}'");
                }
            }

            List<object> Array()
            {
                var a = new List<object>();
                i++; SkipWs();
                if (!End && s[i] == ']') { i++; return a; }
                while (true)
                {
                    SkipWs();
                    a.Add(Value());
                    SkipWs();
                    if (End) throw Error("nezatvoren niz");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return a; }
                    throw Error("očekivano ',' ili ']'");
                }
            }

            string String()
            {
                i++; // "
                var sb = new StringBuilder();
                while (true)
                {
                    if (End) throw Error("nezatvoren string");
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (End) throw Error("loš escape");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw Error("loš \\u escape");
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw Error($"nepoznat escape '\\{e}'");
                    }
                }
            }

            double Number()
            {
                int start = i;
                if (s[i] == '-') i++;
                while (i < s.Length && "0123456789.eE+-".IndexOf(s[i]) >= 0) i++;
                var t = s.Substring(start, i - start);
                if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) throw Error($"loš broj '{t}'");
                return v;
            }
        }

        // ---- pisač ----
        static void WriteValue(StringBuilder sb, object v, bool pretty, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: WriteNumber(sb, d); break;
                case float f: WriteNumber(sb, f); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> d: WriteObject(sb, d, pretty, depth); break;
                case IDictionary d2:
                    var tmp = new Dictionary<string, object>();
                    foreach (DictionaryEntry e in d2) tmp[e.Key.ToString()] = e.Value;
                    WriteObject(sb, tmp, pretty, depth);
                    break;
                case IEnumerable list: WriteArray(sb, list, pretty, depth); break;
                default: WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture)); break;
            }
        }

        static void WriteNumber(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
            if (Math.Abs(d % 1) < 1e-12 && Math.Abs(d) < 1e15) sb.Append(((long)d).ToString(CultureInfo.InvariantCulture));
            else sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        static void Indent(StringBuilder sb, bool pretty, int depth)
        {
            if (!pretty) return;
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }

        static void WriteObject(StringBuilder sb, IDictionary<string, object> d, bool pretty, int depth)
        {
            sb.Append('{');
            bool first = true;
            foreach (var kv in d)
            {
                if (!first) sb.Append(',');
                first = false;
                Indent(sb, pretty, depth + 1);
                WriteString(sb, kv.Key);
                sb.Append(pretty ? ": " : ":");
                WriteValue(sb, kv.Value, pretty, depth + 1);
            }
            if (!first) Indent(sb, pretty, depth);
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable list, bool pretty, int depth)
        {
            sb.Append('[');
            bool first = true;
            foreach (var x in list)
            {
                if (!first) sb.Append(',');
                first = false;
                Indent(sb, pretty, depth + 1);
                WriteValue(sb, x, pretty, depth + 1);
            }
            if (!first) Indent(sb, pretty, depth);
            sb.Append(']');
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
