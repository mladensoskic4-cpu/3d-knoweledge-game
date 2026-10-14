// Jezici: latinica (izvorno), ćirilica (automatska transliteracija) i engleski (prevod interfejsa).
// Kod svuda piše latinicu, a L("...") vraća tekst na izabranom jeziku. Isto kao u web verziji.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TrkaZnanja.Core
{
    public static class Lang
    {
        public const string Lat = "lat", Cyr = "cyr", En = "en";
        public static readonly (string id, string label)[] All = { (Lat, "Latinica"), (Cyr, "Ћирилица"), (En, "English") };

        public static string Mode { get; private set; } = Lat;
        public static event Action Changed;

        public static void Set(string mode)
        {
            var m = mode == Cyr || mode == En ? mode : Lat;
            if (m == Mode) return;
            Mode = m;
            Changed?.Invoke();
        }

        // Jezik telefona → naš jezik ili engleski (systemLanguage dolazi iz Unity-ja kao tekst, npr. "Serbian").
        public static string FromSystemLanguage(string systemLanguage)
        {
            switch (systemLanguage)
            {
                case "Serbian":
                case "SerboCroatian":
                case "Croatian":
                case "Bosnian":
                case "Montenegrin":
                case "Slovenian":
                case "Macedonian":
                    return Lat;
                // Pitanja su zasad samo na našem jeziku, pa i engleski Windows/telefon dobija latinicu.
                // Engleski se bira ručno u podešavanjima (kad stignu prevodi pitanja, vraćamo En za strane jezike).
                default: return Lat;
            }
        }

        public static string L(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = Glyphs(text);
            if (Mode == Cyr) return Translit.ToCyr(text);
            if (Mode == En) return ToEn(text);
            return text;
        }

        // Znakovi kojih nema u fontu igre (Nunito) zamjenjuju se sličnim, da se ne pojave prazni kvadratići.
        static readonly (string from, string to)[] glyphs =
        {
            ("⇒", "=>"), ("→", "»"), ("←", "«"), ("∧", "i"), ("∨", "ili"), ("⁻", "-"), ("＋", "+"), ("✓", "tačno"), ("▲", "^"), ("▼", "v"),
        };
        public static string Glyphs(string text)
        {
            foreach (var (from, to) in glyphs)
                if (text.IndexOf(from, StringComparison.Ordinal) >= 0) text = text.Replace(from, to);
            return text;
        }

        // ---- engleski ----
        static readonly Dictionary<string, string> exact = new Dictionary<string, string>(StringComparer.Ordinal);
        static readonly List<(Regex rx, string rep)> rules = new List<(Regex, string)>();
        static readonly Dictionary<string, string> countries = new Dictionary<string, string>(StringComparer.Ordinal);
        static bool coded;

        // prevodi-en.json: { "exact": {...}, "rx": [[obrazac, zamjena], ...] } (isti prevodi kao u web verziji)
        public static void LoadEnglish(string json)
        {
            var d = Json.Obj(Json.Parse(json));
            foreach (var kv in Json.Obj(Json.Get(d, "exact")) ?? new Dictionary<string, object>())
                if (kv.Value is string s) exact[kv.Key] = s;
            foreach (var r in Json.Arr(Json.Get(d, "rx")) ?? new List<object>())
            {
                var pair = Json.StrList(r);
                if (pair.Count == 2) rules.Add((new Regex(pair[0], RegexOptions.CultureInvariant), pair[1]));
            }
            AddCoded();
        }

        public static void AddCountryNames(IDictionary<string, string> namesToEnglish)
        {
            foreach (var kv in namesToEnglish) countries[kv.Key] = kv.Value;
        }

        static void AddCoded()
        {
            if (coded) return;
            coded = true;
            // tekstovi koji postoje samo u Unity verziji
            var extra = new Dictionary<string, string>
            {
                ["Trka znanja"] = "Race of Knowledge", ["Igraj"] = "Play", ["Podešavanja"] = "Settings", ["Zatvori"] = "Close",
                ["Kvalitet grafike"] = "Graphics quality", ["Automatski"] = "Automatic", ["Nisko"] = "Low", ["Visoko"] = "High",
                ["Vibracija"] = "Vibration", ["Uključeno"] = "On", ["Isključeno"] = "Off", ["Zvučni efekti"] = "Sound effects",
                ["Životinja"] = "Animal", ["Boja vozila"] = "Paint", ["Naočare"] = "Glasses", ["Kupi"] = "Buy", ["Opremi"] = "Equip",
                ["brza trka"] = "quick race", ["standardna trka"] = "standard race", ["duga trka, više stanica"] = "long race, more stations",
                ["Kuče"] = "Puppy", ["Mače"] = "Kitten", ["Zec"] = "Hare", ["Soko"] = "Falcon", ["Bicikl"] = "Bicycle", ["Kolica"] = "Go-kart",
                ["Skuter"] = "Scooter", ["Mali retro auto"] = "Little retro car", ["Motor"] = "Motorbike", ["Traktor"] = "Tractor",
                ["Konjska zaprega"] = "Horse cart", ["Brod"] = "Boat", ["Balon"] = "Hot-air balloon", ["Avion"] = "Plane", ["Roze"] = "Pink",
                ["Poredak"] = "Standings", ["Uskoro"] = "Coming soon", ["Mini igre stižu u sljedećoj verziji."] = "Mini games arrive in the next version.",
                ["Proba: 3 pitanja da vidiš kako se igra."] = "Practice: 3 questions to learn how to play.", ["Preskoči"] = "Skip",
                ["Odgovori na pitanje. Brz i tačan odgovor = više polja."] = "Answer the question. Fast and correct = more tiles.",
                ["Zlatna polja su stanice sa mini igrom."] = "Golden tiles are stations with a mini game.",
                ["Prvi na cilju pobjeđuje!"] = "First to the finish wins!", ["Bez interneta: igraš protiv botova, rezultati se šalju kasnije."] = "Offline: you play against bots, results are sent later.",
                ["Atmosfera"] = "Weather", ["dan"] = "day", ["zalazak"] = "sunset", ["noć"] = "night", ["kiša"] = "rain", ["snijeg"] = "snow",
                ["Mat!"] = "Mate!", ["Nije mat. Tačno je bilo:"] = "Not mate. The answer was:",
                ["Oznake poteza: K kralj · D dama · T top · L lovac · S skakač"] = "Move notation: K king · Q queen · R rook · B bishop · N knight",
                ["Pojavljuje se fotografija životinje. Izaberi koja je. Što više tačnih za 25 sekundi, greška oduzima pola boda."] = "A photo of an animal appears. Pick which one it is. As many correct as you can in 25 seconds; a mistake costs half a point.",
                ["Sa drugima"] = "Multiplayer", ["Igraj sa drugima"] = "Play with others", ["Brza igra"] = "Quick match", ["Napravi sobu"] = "Create room",
                ["Imaš kod od prijatelja?"] = "Got a code from a friend?", ["Uđi"] = "Join", ["Kod sobe (5 znakova)"] = "Room code (5 characters)",
                ["Nazad"] = "Back", ["Otkaži"] = "Cancel", ["Izađi"] = "Leave", ["Kreni"] = "Go", ["domaćin"] = "host", ["ti"] = "you",
                ["slobodno mjesto (bot)"] = "free seat (bot)", ["Čekam ostale igrače…"] = "Waiting for other players…", ["Povezujem se…"] = "Connecting…",
                ["Ti si domaćin: pokreni trku kad su svi tu."] = "You are the host: start the race when everyone is here.",
                ["Čeka se da domaćin pokrene trku."] = "Waiting for the host to start the race.", ["Čekam da svi uđu…"] = "Waiting for everyone to join…",
                ["Soba za prijatelje"] = "Room for friends", ["Pošalji prijateljima ovaj kod da uđu u sobu."] = "Send this code to your friends so they can join.",
                ["Našli smo igrače! Trka kreće čim svi uđu."] = "Players found! The race starts as soon as everyone joins.",
                ["Kod ima 5 slova i brojeva, npr. K7RTX."] = "The code has 5 letters and digits, e.g. K7RTX.",
                ["Brza igra te spaja sa nasumičnim igračima tvog uzrasta. Sa prijateljima: napravi sobu i pošalji im kod. Prazna mjesta popunjavaju botovi."] = "Quick match pairs you with random players of your age group. With friends: create a room and send them the code. Empty seats are filled by bots.",
                ["Čim se skupe 4 igrača, trka kreće. Ako čekaš duže od 15 sekundi, krećemo i sa manje (ostalo su botovi)."] = "The race starts when 4 players gather. After 15 seconds we start with fewer (bots fill the rest).",
                ["Igra sa drugima radi u aplikaciji za telefon i računar, ne u web verziji."] = "Playing with others works in the phone and PC app, not in the web version.",
                ["Ne mogu da se povežem sa serverom. Provjeri internet."] = "Can't connect to the server. Check your internet.",
                ["Veza je prekinuta."] = "Connection lost.", ["Domaćin se ne javlja. Nastavljaš sam."] = "The host is not responding. You continue alone.",
                ["Nasumično"] = "Random", ["Slijedeća runda"] = "Next round", ["Runda"] = "Round", ["Ti"] = "You", ["Garaža stiže uskoro."] = "Garage is coming soon.",
            };
            foreach (var kv in extra) if (!exact.ContainsKey(kv.Key)) exact[kv.Key] = kv.Value;
            rules.Add((new Regex(@"^(.+) · (\d+) novčića$"), "\u0001coins"));
            rules.Add((new Regex(@"^([\d,]+) (zastava|gradova|mjesta|pogodaka|bodova|matova|tačnih)$"), "\u0001count"));
            rules.Add((new Regex(@"^([\d,]+) (zastava|gradova|pogodaka|bodova|matova|tačnih) · (\d+) s$"), "\u0001countS"));
            rules.Add((new Regex(@"^Tapni (državu|grad|rijeku):$"), "\u0001tap"));
            rules.Add((new Regex(@"^Bonus runda · (.+)$"), "\u0001bonus"));
            rules.Add((new Regex(@"^(\d+) min$"), "$1 min"));
            rules.Add((new Regex(@"^Tražim igrače… \((\d+) u čekaonici, (\d+) s\)$"), "Looking for players… ($1 waiting, $2 s)"));
            rules.Add((new Regex(@"^Tvoj rezultat: (.+)$"), "\u0001yours"));
            rules.Add((new Regex(@"^(\d+) do cilja$"), "$1 to go"));
            rules.Add((new Regex(@"^(\d+)\. mjesto$"), "Place $1"));
            rules.Add((new Regex(@"^Otključava se na rangu (.+)$"), "\u0001unlock"));
        }

        static string Unit(string w) => w == "zastava" ? "flags" : w == "gradova" ? "capitals" : w == "mjesta" ? "places" : w == "bodova" ? "points"
            : w == "matova" ? "mates" : w == "tačnih" ? "correct" : "hits";

        public static string ToEn(string text)
        {
            var t = text.Trim();
            if (t.Length == 0) return text;
            if (exact.TryGetValue(t, out var e)) return text.Replace(t, e);
            if (countries.TryGetValue(t, out var c)) return text.Replace(t, c);
            foreach (var (rx, rep) in rules)
            {
                var m = rx.Match(t);
                if (!m.Success) continue;
                string res;
                switch (rep)
                {
                    case "\u0001coins": res = ToEn(m.Groups[1].Value) + " · " + m.Groups[2].Value + " coins"; break;
                    case "\u0001count":
                    case "\u0001countS":
                        res = m.Groups[1].Value.Replace(',', '.') + " " + Unit(m.Groups[2].Value) + (rep == "\u0001countS" ? " · " + m.Groups[3].Value + " s" : "");
                        break;
                    case "\u0001tap":
                        var x = m.Groups[1].Value;
                        res = "Tap the " + (x == "državu" ? "country" : x == "grad" ? "city" : "river") + ":";
                        break;
                    case "\u0001yours": res = "Your result: " + ToEn(m.Groups[1].Value); break;
                    case "\u0001bonus": res = "Bonus round · " + ToEn(m.Groups[1].Value); break;
                    case "\u0001unlock": res = "Unlocks at rank " + ToEn(m.Groups[1].Value); break;
                    default: res = rx.Replace(t, rep); break;
                }
                return text.Replace(t, res);
            }
            return text;
        }
    }

    public static class Translit
    {
        static readonly Dictionary<string, string> Di = new Dictionary<string, string>
        {
            ["Lj"] = "Љ", ["LJ"] = "Љ", ["lj"] = "љ", ["Nj"] = "Њ", ["NJ"] = "Њ", ["nj"] = "њ", ["Dž"] = "Џ", ["DŽ"] = "Џ", ["dž"] = "џ",
        };
        static readonly Dictionary<char, string> Si = BuildSingles();
        static Dictionary<char, string> BuildSingles()
        {
            var lower = new Dictionary<char, string>
            {
                ['a'] = "а", ['b'] = "б", ['c'] = "ц", ['č'] = "ч", ['ć'] = "ћ", ['d'] = "д", ['đ'] = "ђ", ['e'] = "е", ['f'] = "ф", ['g'] = "г",
                ['h'] = "х", ['i'] = "и", ['j'] = "ј", ['k'] = "к", ['l'] = "л", ['m'] = "м", ['n'] = "н", ['o'] = "о", ['p'] = "п", ['r'] = "р",
                ['s'] = "с", ['š'] = "ш", ['t'] = "т", ['u'] = "у", ['v'] = "в", ['z'] = "з", ['ž'] = "ж",
            };
            var all = new Dictionary<char, string>(lower);
            foreach (var kv in lower) all[char.ToUpperInvariant(kv.Key)] = kv.Value.ToUpperInvariant();
            return all;
        }

        // Strane riječi i oznake koje ostaju latinicom.
        static readonly HashSet<string> Keep = new HashSet<string>
        {
            "EXIT", "MVP", "GTA", "Minecraft", "Sea", "Dance", "Lovefest", "Abbey", "Road", "Underground", "Dream", "Arizona", "Bros", "Super",
            "Mario", "Film", "Festival", "Wiegenlied", "AC", "DC", "U2", "WWW", "WWF", "kcal", "English", "Latinica", "Supabase", "Unity",
        };
        static readonly Regex Word = new Regex("[A-Za-zČčĆćĐđŠšŽžÅåÄäÖöÜüÉé₀-₉⁰-⁹¹²³⁻π]+");
        static readonly Regex Foreign = new Regex("[qwxyQWXYÅåÄäÖöÜüÉé]");
        static readonly Regex Formula = new Regex("[₀-₉⁰-⁹¹²³⁻π]");
        static readonly Regex Roman = new Regex("^[IVXLCDM]+$");

        static string TrWord(string w)
        {
            if (Keep.Contains(w)) return w;
            if (Foreign.IsMatch(w)) return w;
            if (Formula.IsMatch(w)) return w;
            if (Roman.IsMatch(w) && w.Length <= 6) return w;
            var sb = new StringBuilder(w.Length);
            for (int i = 0; i < w.Length; i++)
            {
                if (i + 1 < w.Length && Di.TryGetValue(w.Substring(i, 2), out var two)) { sb.Append(two); i++; continue; }
                sb.Append(Si.TryGetValue(w[i], out var one) ? one : w[i].ToString());
            }
            return sb.ToString();
        }

        public static string ToCyr(string text) => Word.Replace(text, m => TrWord(m.Value));
    }
}
