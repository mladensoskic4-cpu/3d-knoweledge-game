// Posebne mini igre: Memorija (zastava i država), Mat u jednom potezu, Pronađi na mapi svijeta
// i Gdje je ova fotografija? (fotografija + mapa svijeta). Prebačeno iz web verzije (js/minigames).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrkaZnanja.Game
{
    // ---------- Memorija ----------
    public sealed class FlagMemory : Minigame
    {
        const int Pairs = 6;
        public override string Id => "memorija";
        public override string Name => "Memorija: zastava i država";
        public override int Duration => 25;
        public override string Intro(int s) => $"Okreni kartice i spoji svaku zastavu sa nazivom njene države. Ima {Pairs} parova i {s} sekundi. Na početku sve kartice vidiš na trenutak.";
        public override bool Ready => GameData.Countries.Count(c => GameData.Flag(c.Code) != null) >= Pairs;

        public override (double score, string label) SimulateBot(double skill, Rng rng)
        {
            var score = Mathx.Clamp(skill * 0.95 + rng.Gaussian() * 0.15, 0, 1);
            return (score, $"{System.Math.Min(Pairs, (int)System.Math.Round(score / 0.85 * Pairs))}/{Pairs} parova");
        }

        sealed class Card
        {
            public Country C; public bool IsFlag, Open, Matched;
            public RectTransform Rt; public GameObject Front, Back; public Image Face;
            public float Anim = 1; public bool Target;   // Anim 0→1 okretanje; Target = otvorena strana
        }

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var picks = rng.Shuffle(GameData.Countries.Where(c => GameData.Flag(c.Code) != null).ToList()).Take(Pairs).ToList();
            var cards = rng.Shuffle(picks.SelectMany(c => new[] { new Card { C = c, IsFlag = true }, new Card { C = c, IsFlag = false } }).ToList());

            var col = UIKit.VStack(host, 14, "Memorija", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var timer = MgTimer.Create(col.transform);
            var holder = UIKit.Rect(col.transform, "Tabla");
            holder.Size(-1, -1, 1, 1);
            var grid = UIKit.Grid(holder, new Vector2(262, 150), new Vector2(18, 16), 4, "Kartice");
            grid.childAlignment = TextAnchor.MiddleCenter;
            ((RectTransform)grid.transform).Stretch();

            var open = new List<Card>();
            int found = 0;
            bool busy = true, done = false;
            float t = 0, waitUntil = 0;
            Card pendA = null, pendB = null;

            foreach (var c in cards)
            {
                var card = c;
                var cell = UIKit.Rect(grid.transform, "Kartica");
                var inner = UIKit.Rect(cell, "Lice").Stretch();
                card.Rt = inner;
                var back = UIKit.Box(inner, Theme.Blue, UIKit.Round, "Poleđina", 0.8f);
                back.rectTransform.Stretch();
                var q = UIKit.Label(back.transform, "?", 64, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter, true);
                q.rectTransform.Stretch();
                card.Back = back.gameObject;
                var front = UIKit.Box(inner, Theme.Paper, UIKit.Round, "Prednja", 0.8f);
                front.rectTransform.Stretch();
                card.Face = front;
                front.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.12f);
                if (card.IsFlag)
                {
                    var f = UIKit.Raw(front.transform, GameData.Flag(card.C.Code), "Zastava");
                    f.rectTransform.Stretch(16, 16, 16, 16);
                    var arf = f.gameObject.AddComponent<AspectRatioFitter>();
                    arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    arf.aspectRatio = f.texture.width / (float)f.texture.height;
                    f.raycastTarget = false;
                }
                else
                {
                    var n = UIKit.Label(front.transform, card.C.Name, 30, Theme.Ink, TextAnchor.MiddleCenter, true);
                    n.rectTransform.Stretch(12, 8, 12, 8);
                    n.resizeTextForBestFit = true; n.resizeTextMinSize = 16; n.resizeTextMaxSize = 30;
                }
                card.Front = front.gameObject;
                card.Target = true; card.Open = true;   // na početku sve otvorene
                SetFace(card, true);
                cell.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);     // prima tap
                cell.gameObject.AddComponent<PointerDownHandler>().Down = () => Flip(card);
            }

            void SetFace(Card c, bool openSide) { c.Front.SetActive(openSide); c.Back.SetActive(!openSide); }
            void Turn(Card c, bool openSide) { c.Open = openSide; c.Target = openSide; c.Anim = 0; }

            void Flip(Card c)
            {
                if (busy || done || c.Open || c.Matched) return;
                AudioKit.I?.Click();
                Turn(c, true);
                open.Add(c);
                if (open.Count < 2) return;
                var a = open[0]; var b = open[1];
                open.Clear();
                if (a.C == b.C)
                {
                    a.Matched = b.Matched = true;
                    a.Face.color = b.Face.color = Col.Hex("#dff5e6");
                    found++;
                    AudioKit.I?.Correct();
                    if (found == Pairs) done = true;
                }
                else
                {
                    busy = true; pendA = a; pendB = b; waitUntil = t + 0.75f;
                    AudioKit.I?.Wrong();
                }
            }

            // zapamti: 1,8 s sve otvoreno
            float look = 0;
            while (look < 1.8f)
            {
                look += Time.unscaledDeltaTime;
                timer.Set(1, "Zapamti!");
                yield return null;
            }
            foreach (var c in cards) Turn(c, false);
            busy = false;

            float botWait = 0;
            Card botFirst = null;
            while (!done && t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                timer.Set(1 - t / seconds, $"{found}/{Pairs} · {Mathf.CeilToInt(Mathf.Max(0, seconds - t))} s");
                if (busy && t >= waitUntil) { Turn(pendA, false); Turn(pendB, false); busy = false; }
                foreach (var c in cards)
                {
                    if (c.Anim >= 1) continue;
                    c.Anim = Mathf.Min(1, c.Anim + dt / 0.22f);
                    float sx = c.Anim < 0.5f ? 1 - c.Anim * 2 : c.Anim * 2 - 1;
                    SetFace(c, c.Anim < 0.5f ? !c.Target : c.Target);
                    c.Rt.localScale = new Vector3(Mathf.Max(0.02f, sx), 1, 1);
                }
                if (Auto && !busy)
                {
                    botWait += dt;
                    if (botWait > 0.45f)
                    {
                        botWait = 0;
                        if (botFirst == null) { botFirst = cards.First(c => !c.Matched && !c.Open); Flip(botFirst); }
                        else { var pair = rng.Chance(0.75) ? cards.First(c => c != botFirst && c.C == botFirst.C) : cards.First(c => !c.Matched && !c.Open); botFirst = null; Flip(pair); }
                    }
                }
                yield return null;
            }
            foreach (var c in cards) c.Rt.localScale = Vector3.one;
            float timeFrac = Mathf.Clamp01(1 - t / seconds);
            timer.Set(timeFrac, $"{found}/{Pairs} parova");
            Score = found == Pairs ? 0.85 + 0.15 * timeFrac : found / (double)Pairs * 0.85;
            ResultLabel = $"{found}/{Pairs} parova";
            yield return Wait(0.6f);
        }
    }

    // ---------- Mat u jednom potezu ----------
    public sealed class MateInOne : Minigame
    {
        public override string Id => "sah";
        public override string Name => "Mat u jednom potezu";
        public override int Duration => 30;
        public override string Intro(int s) => $"Bijeli je na potezu i može da matira u jednom potezu. Tapni bijelu figuru, pa polje na koje ide. Imaš {s} sekundi za što više problema.";
        public override bool Ready => MinigameData.Chess.Count >= 5;

        public override (double score, string label) SimulateBot(double skill, Rng rng)
        {
            var score = Mathx.Clamp(skill * 0.85 + rng.Gaussian() * 0.2, 0, 1);
            return (score, $"{System.Math.Round(score * 3)} matova");
        }

        // figure se crtaju jednom (ChessArt) i čuvaju kao teksture
        static readonly Dictionary<char, Texture2D> pieceTex = new Dictionary<char, Texture2D>();
        static Texture2D PieceTex(char piece)
        {
            if (pieceTex.TryGetValue(piece, out var t) && t != null) return t;
            t = MinigameData.ToTexture(ChessArt.Draw(piece, 128), "Figura " + piece);
            pieceTex[piece] = t;
            return t;
        }

        // potez našim slovima (Dh7#), a na engleskom originalni zapis (Qh7#)
        static string San(ChessPuzzle p) => Lang.Mode == Lang.En ? p.San : ChessPuzzle.LocalSan(p.San);

        static readonly Color Light = Col.Hex("#f0d9b5"), Dark = Col.Hex("#b58863"), Sel = Col.Hex("#f6e05e"),
            Ok = Col.Hex("#5fd38d"), Bad = Col.Hex("#f2727f"), Hint = Col.Hex("#7fb4f5");

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var list = MinigameData.Chess;
            var bag = new List<ChessPuzzle>();
            var col = UIKit.VStack(host, 12, "Šah", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var timer = MgTimer.Create(col.transform);
            var row = UIKit.HStack(col.transform, 30, "Tabla i opis", TextAnchor.UpperCenter);
            row.Size(-1, -1, 1, 1);
            row.childForceExpandWidth = false; row.childControlWidth = true;

            const float Sq = 62;
            var boardBox = UIKit.Box(row.transform, Col.Hex("#6b4a2f"), UIKit.RoundSmall, "Tabla", 1f);
            boardBox.Size(Sq * 8 + 24, Sq * 8 + 24);
            var board = UIKit.Rect(boardBox.transform, "Polja").Stretch(12, 12, 12, 12);

            var side = UIKit.VStack(row.transform, 16, "Opis", TextAnchor.UpperLeft);
            side.Size(-1, -1, 1, 1);
            side.childForceExpandHeight = false;
            var info = UIKit.Label(side.transform, "Bijeli na potezu: mat u 1", 36, Theme.Ink, TextAnchor.UpperLeft, true);
            info.Size(-1, 110);
            var legend = UIKit.Label(side.transform, "Oznake poteza: K kralj · D dama · T top · L lovac · S skakač", 24, Theme.InkSoft);
            legend.FitHeight();

            var squares = new Dictionary<string, Image>();
            var pieces = new Dictionary<string, GameObject>();
            Dictionary<string, char> pos = null;
            ChessPuzzle cur = null;
            string sel = null;
            int solved = 0, misses = 0;
            float t = 0, waitUntil = -1;
            bool busy = false;

            for (int r = 8; r >= 1; r--)
                for (int f = 0; f < 8; f++)
                {
                    string sq = "" + ChessPuzzle.Files[f] + r;
                    var img = UIKit.Box(board, (f + r) % 2 == 1 ? Light : Dark, null, "Polje " + sq);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0, 0);
                    img.rectTransform.pivot = Vector2.zero;
                    img.rectTransform.anchoredPosition = new Vector2(f * Sq, (r - 1) * Sq);
                    img.rectTransform.sizeDelta = new Vector2(Sq, Sq);
                    img.raycastTarget = true;
                    var s = sq;
                    img.gameObject.AddComponent<PointerDownHandler>().Down = () => Click(s);
                    squares[sq] = img;
                    if (f == 0) { var l = UIKit.Label(img.transform, r.ToString(), 14, Theme.InkSoft, TextAnchor.UpperLeft, true); l.rectTransform.Stretch(3, 1, 0, 0); }
                    if (r == 1) { var l = UIKit.Label(img.transform, ChessPuzzle.Files[f].ToString(), 14, Theme.InkSoft, TextAnchor.LowerRight, true); l.rectTransform.Stretch(0, 0, 3, 1); }
                }

            void Paint(Dictionary<string, Color> hl)
            {
                foreach (var kv in squares)
                {
                    int f = kv.Key[0] - 'a', r = kv.Key[1] - '0';
                    var c = (f + r) % 2 == 1 ? Light : Dark;
                    if (kv.Key == sel) c = Sel;
                    if (hl != null && hl.TryGetValue(kv.Key, out var h)) c = h;
                    kv.Value.color = c;
                }
                foreach (var p in pieces.Values) Object.Destroy(p);
                pieces.Clear();
                foreach (var kv in pos)
                {
                    var img = UIKit.Raw(squares[kv.Key].transform, PieceTex(kv.Value), "Figura " + kv.Value);
                    img.rectTransform.Stretch(2, 2, 2, 2);
                    img.raycastTarget = false;
                    pieces[kv.Key] = img.gameObject;
                }
            }

            void Next()
            {
                if (bag.Count == 0) bag.AddRange(rng.Shuffle(list.ToList()));
                cur = bag[bag.Count - 1];
                bag.RemoveAt(bag.Count - 1);
                pos = ChessPuzzle.ParseFen(cur.Fen);
                sel = null;
                info.text = Lang.L("Bijeli na potezu: mat u 1");
                Paint(null);
            }

            void Click(string sq)
            {
                if (busy || t >= seconds || cur == null) return;
                bool whiteHere = pos.TryGetValue(sq, out var p) && ChessPuzzle.IsWhite(p);
                if (sel == null || whiteHere) { if (whiteHere) { sel = sq; AudioKit.I?.Click(); Paint(null); } return; }
                string from = sel;
                bool ok = from == cur.From && sq == cur.To;
                busy = true;
                sel = null;
                if (ok)
                {
                    solved++;
                    pos[sq] = cur.Promo ? 'Q' : pos[from];
                    pos.Remove(from);
                    info.text = Lang.L("Mat!") + " " + San(cur);
                    AudioKit.I?.Correct();
                    Paint(new Dictionary<string, Color> { [sq] = Ok });
                    waitUntil = t + 0.8f;
                }
                else
                {
                    misses++;
                    info.text = Lang.L("Nije mat. Tačno je bilo:") + " " + San(cur);
                    AudioKit.I?.Wrong();
                    Haptics.Tap(40);
                    Paint(new Dictionary<string, Color> { [from] = Bad, [cur.From] = Hint, [cur.To] = Hint });
                    waitUntil = t + 1.4f;
                }
            }

            yield return null;
            Next();
            float botWait = 0;
            while (t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                timer.Set(1 - t / seconds, $"{solved} matova · {Mathf.CeilToInt(Mathf.Max(0, seconds - t))} s");
                if (busy && t >= waitUntil) { busy = false; if (t < seconds) Next(); }
                if (Auto && !busy)
                {
                    botWait += dt;
                    if (botWait > 1.2f) { botWait = 0; Click(cur.From); Click(rng.Chance(0.7) ? cur.To : cur.From == "a1" ? "h8" : "a1"); }
                }
                yield return null;
            }
            Score = Mathx.Clamp((solved - misses * 0.3) / 3, 0, 1);
            ResultLabel = $"{solved} matova";
        }
    }

    // ---------- Pronađi na mapi svijeta ----------
    public sealed class WorldMapGame : Minigame
    {
        public override string Id => "mapa-svijeta";
        public override string Name => "Pronađi na mapi svijeta";
        public override int Duration => 35;
        public override string Intro(int s) => $"Na mapi svijeta tapni traženu državu, grad ili rijeku. Mapu možeš uvećati (+, točkić ili dva prsta) i pomjerati prevlačenjem. Što bliže, to više bodova. Imaš {s} sekundi.";
        public override bool Ready => MinigameData.World.Count > 20;
        public override void Prepare() => MinigameData.WarmWorldMap();

        public override (double score, string label) SimulateBot(double skill, Rng rng)
        {
            var score = Mathx.Clamp(skill * 0.95 + rng.Gaussian() * 0.15, 0, 1);
            return (score, $"{Fmt.Num(score * 7, 1)} bodova");
        }

        sealed class MapTask { public string Kind, Label; public WorldCountry D; public WorldPlace C; public WorldRiver R; }

        static string Show(string name, string en) => Lang.Mode == Lang.En && !string.IsNullOrEmpty(en) ? en : name;

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var big = MinigameData.World.Where(d => d.Area > 60000).ToList();
            var tasks = rng.Shuffle(
                rng.Shuffle(big.ToList()).Take(14).Select(d => new MapTask { Kind = "državu", Label = Show(d.Name, d.En), D = d })
                .Concat(rng.Shuffle(MinigameData.Cities.ToList()).Take(8).Select(c => new MapTask { Kind = "grad", Label = Show(c.Name, c.En), C = c }))
                .Concat(rng.Shuffle(MinigameData.Rivers.ToList()).Take(5).Select(r => new MapTask { Kind = "rijeku", Label = Show(r.Name, r.En), R = r }))
                .ToList());

            var col = UIKit.VStack(host, 12, "Mapa", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var timer = MgTimer.Create(col.transform);
            var prompt = Prompt(col.transform, 70);
            int ti = 0;
            double score = 0;
            float t = 0, waitUntil = -1;
            bool busy = false;
            Text note = null;
            WorldMapView map = null;

            void ShowTask()
            {
                var task = tasks[ti % tasks.Count];
                prompt.text = Lang.L($"Tapni {task.Kind}:") + "  " + Lang.L(task.Label);
                note.text = Lang.L("Tapni na mapu. Prevuci za pomjeranje.");
                map.ClearMarks();
            }

            void Answer(double lon, double lat)
            {
                if (busy || t >= seconds) return;
                var task = tasks[ti % tasks.Count];
                double km, acc;
                if (task.D != null) { km = WorldGeo.DistToCountry(lon, lat, task.D); acc = WorldGeo.ScoreCountry(km); }
                else if (task.C != null) { km = Geo.Haversine(lon, lat, task.C.Lon, task.C.Lat); acc = WorldGeo.ScoreCity(km); }
                else { km = WorldGeo.DistToLine(lon, lat, task.R.Points); acc = WorldGeo.ScoreRiver(km); }
                score += acc;
                bool ok = acc >= 0.99;
                if (ok) AudioKit.I?.Correct(); else { AudioKit.I?.Wrong(); Haptics.Tap(40); }
                if (task.D != null) map.HighlightCountry(task.D, Rgba.Hex("#1d8a4e", (byte)(ok ? 200 : 150)));
                if (task.R != null) map.DrawLine(task.R.Points, 4, Rgba.Hex("#1f5fd1"));
                map.ApplyMarks();
                if (task.C != null) map.Pin(task.C.Lon, task.C.Lat, Theme.Right, 30);
                map.Pin(lon, lat, Theme.Red, 24);
                note.text = ok ? Lang.L("Tačno!") : Lang.L($"Promašaj: {Fmt.Num(System.Math.Round(km))} km");
                busy = true;
                waitUntil = t + (ok ? 0.7f : 1.4f);
            }

            // mapa se crta u pozadini dok igrač čita uputstvo; ovdje sačekamo ako još nije gotova
            while (!MinigameData.WorldMapReady) { MinigameData.WarmWorldMap(); yield return null; }
            map = WorldMapView.Create(col.transform, Answer);
            map.Frame.Size(-1, -1, 1, 1);
            note = UIKit.Label(col.transform, "", 26, Theme.InkSoft, TextAnchor.MiddleCenter);
            note.Size(-1, 36);
            yield return null;
            ShowTask();
            float botWait = 0;
            while (t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                timer.Set(1 - t / seconds, $"{Fmt.Num(score, 1)} bodova · {Mathf.CeilToInt(Mathf.Max(0, seconds - t))} s");
                if (busy && t >= waitUntil) { busy = false; ti++; if (t < seconds) ShowTask(); }
                if (Auto && !busy)
                {
                    botWait += dt;
                    if (botWait > 1f)
                    {
                        botWait = 0;
                        var task = tasks[ti % tasks.Count];
                        var p = task.D != null ? task.D.Center : task.C != null ? new Vec2d(task.C.Lon, task.C.Lat) : task.R.Points[task.R.Points.Count / 2];
                        Answer(p.X + rng.Gaussian() * 2, p.Y + rng.Gaussian() * 2);
                    }
                }
                yield return null;
            }
            Score = Mathx.Clamp(score / 7 * Duration / seconds, 0, 1);
            ResultLabel = $"{Fmt.Num(score, 1)} bodova";
        }
    }

    // ---------- Gdje je ova fotografija? ----------
    public sealed class WorldPhotoMap : Minigame
    {
        const int Goal = 5;
        public override string Id => "foto-mapa";
        public override string Name => "Gdje je ova fotografija?";
        public override int Duration => 40;
        public override string Intro(int s) => $"Vidiš fotografiju poznatog mjesta. Tapni na mapi svijeta gdje se nalazi. Što bliže, to više bodova (do 100 km je pun bod). Imaš {s} sekundi.";
        public override bool Ready => MinigameData.Photos("mjesta").Count(p => p.HasPlace) >= 6;
        public override void Prepare() => MinigameData.WarmWorldMap();

        public override (double score, string label) SimulateBot(double skill, Rng rng)
        {
            var score = Mathx.Clamp(skill * 0.9 + rng.Gaussian() * 0.15, 0, 1);
            return (score, $"{Fmt.Num(score * Goal, 1)} bodova");
        }

        public override IEnumerator Play(RectTransform host, Rng rng, int seconds)
        {
            var list = MinigameData.Photos("mjesta").Where(p => p.HasPlace).ToList();
            var bag = new List<PhotoItem>();
            var col = UIKit.VStack(host, 12, "Foto mapa", TextAnchor.UpperCenter);
            ((RectTransform)col.transform).Stretch();
            col.childForceExpandHeight = false;
            var timer = MgTimer.Create(col.transform);
            var row = UIKit.HStack(col.transform, 18, "Slika i mapa", TextAnchor.MiddleCenter);
            row.Size(-1, -1, 1, 1);
            row.childForceExpandHeight = true; row.childControlHeight = true;
            var photo = UIKit.Rect(row.transform, "Fotografija");
            photo.Size(400, -1, 0, 1);
            var note = UIKit.Label(col.transform, "", 26, Theme.InkSoft, TextAnchor.MiddleCenter);

            PhotoItem cur = null;
            double score = 0;
            float t = 0, waitUntil = -1;
            bool busy = false;
            WorldMapView map = null;

            void Next()
            {
                for (int k = 0; k < 5; k++)
                {
                    if (bag.Count == 0) bag.AddRange(rng.Shuffle(list.ToList()));
                    var it = bag[bag.Count - 1];
                    bag.RemoveAt(bag.Count - 1);
                    var tex = MinigameData.Photo(it);
                    if (tex == null) continue;
                    cur = it;
                    UIKit.Clear(photo);
                    PhotoFrame(photo, it, tex);
                    break;
                }
                map.ClearMarks();
                map.ResetView();
                note.text = Lang.L("Tapni na mapu gdje je ovo mjesto.");
            }

            void Answer(double lon, double lat)
            {
                if (busy || cur == null || t >= seconds) return;
                var it = cur;
                double km = Geo.Haversine(lon, lat, it.Lon, it.Lat), acc = WorldGeo.ScoreForKm(km);
                score += acc;
                if (acc >= 0.8) AudioKit.I?.Correct(); else AudioKit.I?.Wrong();
                map.DrawLine(new List<Vec2d> { new Vec2d(lon, lat), new Vec2d(it.Lon, it.Lat) }, 3, Rgba.Hex("#14232e", 200));
                map.ApplyMarks();
                map.Pin(it.Lon, it.Lat, Theme.Right, 32);
                map.Pin(lon, lat, Theme.Red, 24);
                map.Focus(new List<Vec2d> { new Vec2d(lon, lat), new Vec2d(it.Lon, it.Lat) });
                note.text = $"<b>{Lang.L(it.Display)}{(string.IsNullOrEmpty(it.Country) ? "" : ", " + Lang.L(it.Country))}</b> · {Fmt.Num(System.Math.Round(km))} km · +{Fmt.Num(acc, 1)}";
                busy = true;
                waitUntil = t + 1.8f;
            }

            while (!MinigameData.WorldMapReady) { MinigameData.WarmWorldMap(); yield return null; }
            map = WorldMapView.Create(row.transform, Answer);
            map.Frame.Size(-1, -1, 1, 1);
            note.Size(-1, 36);
            note.transform.SetAsLastSibling();
            yield return null;
            Next();
            float botWait = 0;
            while (t < seconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                timer.Set(1 - t / seconds, $"{Fmt.Num(score, 1)} bodova · {Mathf.CeilToInt(Mathf.Max(0, seconds - t))} s");
                if (busy && t >= waitUntil) { busy = false; if (t < seconds) Next(); }
                if (Auto && !busy && cur != null)
                {
                    botWait += dt;
                    if (botWait > 1.2f) { botWait = 0; Answer(cur.Lon + rng.Gaussian() * 4, cur.Lat + rng.Gaussian() * 3); }
                }
                yield return null;
            }
            Score = Mathx.Clamp(score / Goal * Duration / seconds, 0, 1);
            ResultLabel = $"{Fmt.Num(score, 1)} bodova";
        }
    }
}
