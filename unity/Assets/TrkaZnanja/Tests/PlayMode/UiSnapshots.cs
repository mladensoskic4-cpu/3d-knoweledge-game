// Provjera preglednosti na telefonu: platno se postavi na veličinu pravog telefona/tableta (sa notch-om),
// otvori se svaki ekran (meni, pitanja svih tipova, tabla, mini igre, cilj…), snimi se slika
// u unity/Logs/UiSnimci/<uređaj>/ i provjeri da se dugmad ne preklapaju i ne izlaze sa ekrana ili iz kartice.
// Izvještaj: unity/Logs/UiSnimci/izvjestaj.txt. Test pada ako nađe preklapanje dugmadi ili dugme van ekrana.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TrkaZnanja.Core;
using TrkaZnanja.Game;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TrkaZnanja.Tests
{
    public class UiSnapshotTests
    {
        sealed class Device { public string Id; public Vector2 Px; public Rect Safe; public bool Full; }

        static readonly Device[] Devices =
        {
            // tipičan Android telefon 20:9, kamera (notch) sa lijeve strane kad je polegnut
            new Device { Id = "telefon-20x9", Px = new Vector2(2400, 1080), Safe = new Rect(110, 0, 2290, 1080), Full = true },
            new Device { Id = "telefon-16x9", Px = new Vector2(1920, 1080), Safe = new Rect(0, 0, 1920, 1080) },
            new Device { Id = "tablet-4x3", Px = new Vector2(2048, 1536), Safe = new Rect(0, 0, 2048, 1536) },
        };

        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "UiSnimci"));
        Device dev;
        readonly StringBuilder report = new StringBuilder();
        int hardProblems;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            App.AutoPlay = false;
            App.TestMode = false;
            Minigame.TestAuto = false;
            RealtimeChannel.Loopback = false;
            MpSession.Current?.Leave();
            if (App.I != null) Object.Destroy(App.I.gameObject);
            yield return null;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator PhoneLayoutsDoNotOverlap()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            Directory.CreateDirectory(Root);
            foreach (var d in Devices)
            {
                dev = d;
                Directory.CreateDirectory(Path.Combine(Root, d.Id));
                report.AppendLine($"===== {d.Id} ({d.Px.x}x{d.Px.y}, sigurna zona {d.Safe}) · platno {UIRoot.CanvasSizeFor(d.Px)} =====");
                yield return AllScreens();
            }
            File.WriteAllText(Path.Combine(Root, "izvjestaj.txt"), report.ToString());
            Debug.Log("Trka znanja · UI snimci: " + Root + "\n" + report);
            Assert.AreEqual(0, hardProblems, "dugmad se preklapaju ili su van ekrana — vidi Logs/UiSnimci/izvjestaj.txt");
        }

        IEnumerator AllScreens()
        {
            if (App.I != null) Object.Destroy(App.I.gameObject);
            yield return null;
            App.AutoPlay = false;
            App.TestMode = true;
            App.ForcedSeed = 42;
            App.Boot();
            yield return null;
            yield return null;
            var ui = UIRoot.I;
            ui.Emulate(dev.Px, dev.Safe);
            var p = App.I.Profile;
            p.Ime = "Aleksandra";
            p.Novcici = 1250;
            var rng = new Rng(5);

            // meni: lik na postolju u sredini, izbor oblasti, prijatelji
            App.I.ShowHero();
            MenuScreen.Build(ui.Screen, p, App.AreaCounts(), _ => { });
            yield return Snap("01-meni");
            ClickLabel(ui, Lang.L("Miks · sve oblasti").Split(' ')[0]);
            yield return Snap("01b-oblasti");
            MenuScreen.Build(ui.Screen, p, App.AreaCounts(), _ => { });
            ClickLabel(ui, Lang.L("Prijatelji"));
            yield return Snap("01c-prijatelji");
            p.Ime = "";
            MenuScreen.Build(ui.Screen, p, App.AreaCounts(), _ => { });
            yield return Snap("01d-dobrodoslica");
            p.Ime = "Aleksandra";
            SettingsScreen.Build(ui.Screen, p, _ => { }, () => { });
            yield return Snap("02-podesavanja");
            ui.ClearScreen();

            // garaža: okretanje, šare, nalijepnice, trag
            p.Bodovi = 1200;
            App.I.ShowHero(true);
            GarageScreen.Open(ui.Screen, App.I.Hero, App.I.World, p, () => { }, () => { });
            yield return Snap("03-garaza");
            foreach (var (slotId, itemId, shot) in new[] { ("vozilo", "v-kamion", "03b-garaza-vozilo"), ("sara", "s-plamen", "03c-garaza-sara"), ("nalijepnica", "l-zvijezda", "03d-garaza-nalijepnica"), ("trag", "t-duga", "03e-garaza-trag") })
            {
                ClickTag(ui, slotId);
                yield return null;
                ClickTag(ui, itemId);
                for (int i = 0; i < 20; i++) yield return null;
                yield return Snap(shot);
            }
            ui.ClearScreen();

            // sa drugima: izbor i soba (lokalna veza, bez interneta)
            RealtimeChannel.Loopback = true;
            MultiplayerScreen.Open(ui.Screen, p, App.I, s => null, (s, setup) => { }, () => { });
            yield return Snap("04-sa-drugima");
            var create = ui.Screen.GetComponentsInChildren<GameButton>().FirstOrDefault(b => b.Label != null && b.Label.text == Lang.L("Napravi sobu"));
            if (create != null)
            {
                create.Click();
                yield return null;
            }
            if (MpSession.Current == null) MultiplayerScreenTestRoom(ui, p);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Snap("05-soba");
            MpSession.Current?.Leave();
            RealtimeChannel.Loopback = false;
            ui.ClearScreen();

            // trka: HUD sa predmetom, pitanja svih tipova sa svim dodacima (najduža pitanja)
            var setup = new RaceSetup
            {
                Seed = 7, Map = Catalog.Maps[0].Id, Tiles = 40, Atmosphere = "dan", Level = "srednje",
                Areas = Catalog.Areas.Select(a => a.Id).ToList(), MaxTezina = 3, BonusSeconds = 30,
                Humans = new List<MpPlayer> { new MpPlayer { Id = "ti", Name = "Aleksandra", Look = p.Izgled.Clone() } },
            };
            var shared = new Rng(setup.Seed);
            var race = new Race(setup.BuildPlayers("ti", shared), setup.Tiles, shared, TileKinds.Generate(setup.Tiles, setup.Seed));
            race.NextRound();
            race.Human.Item = Items.Back;
            for (int i = 0; i < race.Players.Count; i++) race.Players[i].Pos = 3 + i * 4;
            var hud = Hud.Create(ui.Hud, race);
            hud.Refresh(7);
            ui.Toast("DUPLA RUNDA! Sva polja ×2", Theme.Gold, 1.5f);
            yield return Snap("06-trka-hud");

            var pus = Catalog.Powerups.Select(x => (x, 2)).ToList();
            int n = 7;
            foreach (var tip in new[] { "izbor", "mutna", "melodija", "procjena", "poredaj", "spoji", "mapa" })
            {
                var q = GameData.Questions.Where(x => x.Tip == tip).OrderByDescending(x => (x.Pitanje ?? "").Length + (x.Opcije?.Sum(o => o.Length) ?? 0)).First();
                var qs = QuestionScreen.Open(ui.Screen, q, 12, rng, pus, new QuestionScreen.Extras { Item = Items.Back, Confused = true, DoubleRound = true });
                yield return Snap($"{n++:00}-pitanje-{tip}");
                qs.AutoAnswer(rng);
                var entries = race.Players.Select((r, i) => new RoundEntry { Racer = r, Accuracy = 0.8, Time = 4.2 + i, Tiles = 3 - i % 3, Text = i == 0 ? "Tačno" : "Skoro tačno" }).ToList();
                qs.StartCoroutine(qs.ShowBoard(entries));
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Snap($"{n++:00}-tabla-{tip}");
                qs.Close();
                yield return null;
            }

            // mini igre: najava, igra u toku, rezultati
            Minigame.TestAuto = true;
            bool first = true;
            foreach (var mg in Minigame.All)
            {
                if (!dev.Full && !first) break;
                App.AutoPlay = false;
                var outcomes = new List<StationOutcome>();
                var run = MinigameScreen.Run(ui.Screen, mg, 8, "Mini igra · svi igraju", Theme.Purple,
                    Lang.L("Svi igraju istu mini igru. 1. mjesto ide 3 polja naprijed, 2. mjesto 2 polja, 3. mjesto 1 polje, uz novčiće."), rng, outcomes,
                    (score, label) => race.Players.Select((r, i) => new StationOutcome { Racer = r, Score = score, Label = label, Coins = 50 - i * 15, Jump = 3 - i }).ToList());
                var co = App.I.StartCoroutine(run);
                if (first) yield return Snap("30-mini-najava");
                App.AutoPlay = true;
                yield return new WaitForSecondsRealtime(1.3f);
                yield return Snap($"31-mini-{mg.Id}");
                float t = 0;
                while (t < 30 && !ui.Screen.GetComponentsInChildren<GameButton>().Any(b => b.Label != null && b.Label.text == Lang.L("Nastavi")))
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (first) { App.AutoPlay = false; yield return Snap("32-mini-rezultati"); App.AutoPlay = true; }
                t = 0;
                while (t < 3 && ui.Screen.childCount > 0) { t += Time.unscaledDeltaTime; yield return null; }
                App.I.StopCoroutine(co);
                ui.ClearScreen();
                AudioKit.I?.StopNotes();
                first = false;
            }
            Minigame.TestAuto = false;
            App.AutoPlay = false;

            // cilj
            var ranked = race.Ranking();
            var award = new Profile.Award { Pts = 140, Coins = 85, RankUp = Catalog.Ranks[1] };
            award.NewMaps.Add(Catalog.Maps.Last());
            ResultsScreen.Build(ui.Screen, race, ranked, award, Catalog.Maps[0], p, () => { }, () => { });
            yield return Snap("40-cilj");
            ui.ClearScreen();
            UIKit.Clear(ui.Hud);
        }

        static void ClickLabel(UIRoot ui, string startsWith)
        {
            var b = ui.Screen.GetComponentsInChildren<GameButton>().FirstOrDefault(x => x.Label != null && x.Label.text.StartsWith(startsWith));
            if (b != null) b.Click(); else Debug.Log("Trka znanja · snimci: nema dugmeta " + startsWith);
        }

        static void ClickTag(UIRoot ui, string tag)
        {
            var b = ui.Screen.GetComponentsInChildren<GameButton>().FirstOrDefault(x => x.Tag == tag);
            if (b != null) b.Click(); else Debug.Log("Trka znanja · snimci: nema dugmeta " + tag);
        }

        static void MultiplayerScreenTestRoom(UIRoot ui, Profile p)
        {
            // rezerva ako dugme nije nađeno: obična soba sa jednim gostom
            MpSession.Join("TESTA", p, App.I.transform, "domacin");
        }

        // ---------- snimak + provjera ----------
        IEnumerator Snap(string name)
        {
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var ui = UIRoot.I;
            var crt = (RectTransform)ui.transform;
            var size = crt.rect.size;
            int w = Mathf.RoundToInt(dev.Px.x * 0.6f), h = Mathf.RoundToInt(dev.Px.y * 0.6f);
            var rtex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var go = new GameObject("Snimač UI-ja");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size.y / 2;
            cam.aspect = size.x / size.y;
            cam.transform.position = crt.position + new Vector3(0, 0, -100);
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 1;
            cam.farClipPlane = 300;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Col.Hex("#7fb2d9");
            // 3D svijet i UI zajedno: platno se za trenutak postavi ispred glavne kamere (kao ekran),
            // pa jedan snimak pokaže i providnost kartica i lik iza njih
            var main = App.I != null && App.I.Camera != null ? App.I.Camera.Cam : null;
            if (main != null)
            {
                App.I.Camera.Snap();
                main.targetTexture = rtex;
                main.aspect = w / (float)h;
                var ct = ui.transform;
                Vector3 p0 = ct.position, s0 = ct.localScale; Quaternion r0 = ct.rotation;
                const float d = 1f;
                float viewH = 2 * d * Mathf.Tan(main.fieldOfView * 0.5f * Mathf.Deg2Rad);
                ct.position = main.transform.position + main.transform.forward * d;
                ct.rotation = main.transform.rotation;
                ct.localScale = Vector3.one * (viewH / size.y);
                main.Render();
                ct.position = p0; ct.rotation = r0; ct.localScale = s0;
                main.targetTexture = null;
                main.ResetAspect();
            }
            else
            {
                cam.targetTexture = rtex;
                cam.Render();
            }
            var prev = RenderTexture.active;
            RenderTexture.active = rtex;
            var img = new Texture2D(w, h, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            img.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(Root, dev.Id, name + ".png"), img.EncodeToPNG());
            Object.Destroy(img);
            Object.Destroy(go);
            rtex.Release();
            Object.Destroy(rtex);
            Audit(name);
        }

        static Rect RectOf(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var o = UIRoot.EmulationOrigin;
            float x0 = Mathf.Min(c[0].x, c[2].x) - o.x, x1 = Mathf.Max(c[0].x, c[2].x) - o.x;
            float y0 = Mathf.Min(c[0].y, c[2].y) - o.y, y1 = Mathf.Max(c[0].y, c[2].y) - o.y;
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static float OverlapArea(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0 && h > 0 ? w * h : 0;
        }

        static bool Inside(Rect outer, Rect r, float tol) =>
            r.xMin >= outer.xMin - tol && r.xMax <= outer.xMax + tol && r.yMin >= outer.yMin - tol && r.yMax <= outer.yMax + tol;

        static bool Clipped(Transform t) => t.GetComponentsInParent<ScrollRect>(true).Length > 0 || t.GetComponentsInParent<RectMask2D>(true).Length > 0 || t.GetComponentsInParent<Mask>(true).Length > 0;

        static Rect Visible(Transform t, Rect r)
        {
            foreach (var m in t.GetComponentsInParent<RectMask2D>(true))
            {
                var c = RectOf((RectTransform)m.transform);
                float x0 = Mathf.Max(r.xMin, c.xMin), y0 = Mathf.Max(r.yMin, c.yMin);
                float x1 = Mathf.Min(r.xMax, c.xMax), y1 = Mathf.Min(r.yMax, c.yMax);
                r = x1 > x0 && y1 > y0 ? Rect.MinMaxRect(x0, y0, x1, y1) : new Rect(0, 0, 0, 0);
            }
            return r;
        }

        static RectTransform CardOf(Transform t)
        {
            for (var x = t.parent; x != null; x = x.parent)
                if (IsCard(x)) return (RectTransform)x;
            return null;
        }

        // lice kartice (UIKit.Card) ima sjenku pored sebe; lice dugmeta nema
        static bool IsCard(Transform x) => x.name == "Lice" && x.parent != null && x.parent.Find("Sjenka") != null;

        static string Who(Component c)
        {
            var b = c as GameButton;
            if (b != null && b.Label != null) return "dugme „" + b.Label.text.Replace("\n", " ") + "”";
            var t = c as Text;
            if (t != null) return "tekst „" + (t.text.Length > 40 ? t.text.Substring(0, 40) + "…" : t.text).Replace("\n", " ") + "”";
            return c.name;
        }

        void Problem(string screen, string what, bool hard)
        {
            if (hard) hardProblems++;
            report.AppendLine($"  [{(hard ? "GREŠKA" : "pazi")}] {screen}: {what}");
        }

        void Audit(string screen)
        {
            var ui = UIRoot.I;
            var safe = RectOf(ui.Safe);
            int before = report.Length;
            // ako je otvoren prozor preko ekrana (zatamnjenje), provjerava se samo taj prozor
            var dim = ui.Screen.GetComponentsInChildren<Image>().LastOrDefault(x => x.name == "Zatamnjenje" && x.isActiveAndEnabled);
            Component scope = dim != null ? (Component)dim.transform.parent : ui;
            var buttons = scope.GetComponentsInChildren<GameButton>().Where(b => b.isActiveAndEnabled).ToList();
            // vidljivi dio dugmeta: ono što je u listi sa klizanjem se odsijeca na rubu liste
            var rects = buttons.Select(b => Visible(b.transform, RectOf((RectTransform)b.transform))).ToList();
            var keep = rects.Select(r => r.width > 1 && r.height > 1).ToList();
            buttons = buttons.Where((b, i) => keep[i]).ToList();
            rects = rects.Where((r, i) => keep[i]).ToList();
            for (int i = 0; i < buttons.Count; i++)
            {
                if (!Clipped(buttons[i].transform) && !Inside(safe, rects[i], 2))
                    Problem(screen, Who(buttons[i]) + " je van ekrana " + rects[i], true);
                var card = CardOf(buttons[i].transform);
                if (card != null && !Clipped(buttons[i].transform) && !Inside(RectOf(card), rects[i], 3))
                    Problem(screen, Who(buttons[i]) + " izlazi iz kartice", true);
                for (int j = i + 1; j < buttons.Count; j++)
                {
                    if (buttons[i].transform.IsChildOf(buttons[j].transform) || buttons[j].transform.IsChildOf(buttons[i].transform)) continue;
                    float a = OverlapArea(rects[i], rects[j]);
                    float small = Mathf.Min(rects[i].width * rects[i].height, rects[j].width * rects[j].height);
                    if (a > 0.02f * small)
                        Problem(screen, $"{Who(buttons[i])} i {Who(buttons[j])} se preklapaju", true);
                }
            }
            // tekst koji ne staje u svoj prostor ili izlazi iz kartice
            foreach (var t in scope.GetComponentsInChildren<Text>().Where(x => x.isActiveAndEnabled && !string.IsNullOrEmpty(x.text)))
            {
                if (t.resizeTextForBestFit || t.GetComponentInParent<WorldLabels>() != null || t.GetComponent<ContentSizeFitter>() != null) continue;
                if (t.GetComponentInParent<InputField>() != null) continue;
                var r = RectOf(t.rectTransform);
                if (t.horizontalOverflow == HorizontalWrapMode.Wrap && t.preferredHeight > r.height + 6)
                    Problem(screen, $"{Who(t)} ne staje ({t.preferredHeight:0} > {r.height:0})", false);
                else if (t.horizontalOverflow == HorizontalWrapMode.Overflow && t.preferredWidth > r.width + 6)
                    Problem(screen, $"{Who(t)} je preširok ({t.preferredWidth:0} > {r.width:0})", false);
                var card = CardOf(t.transform);
                if (card != null && !Clipped(t.transform) && !Inside(RectOf(card), r, 3))
                    Problem(screen, Who(t) + " izlazi iz kartice", false);
                if (t.fontSize < 24 && t.GetComponentInParent<Hud>() == null)
                    Problem(screen, $"{Who(t)} ima sitna slova ({t.fontSize})", false);
            }
            // kartice koje se preklapaju (npr. pitanje preko poretka)
            var cards = scope.GetComponentsInChildren<Image>().Where(x => IsCard(x.transform) && x.isActiveAndEnabled && !Clipped(x.transform.parent)).ToList();
            for (int i = 0; i < cards.Count; i++)
                for (int j = i + 1; j < cards.Count; j++)
                {
                    if (cards[i].transform.IsChildOf(cards[j].transform) || cards[j].transform.IsChildOf(cards[i].transform)) continue;
                    if (OverlapArea(RectOf(cards[i].rectTransform), RectOf(cards[j].rectTransform)) > 400)
                        Problem(screen, $"kartice {cards[i].transform.parent.name} i {cards[j].transform.parent.name} se preklapaju", false);
                }
            foreach (var c in cards)
                if (!Inside(safe, RectOf(c.rectTransform), 2))
                    Problem(screen, $"kartica {c.transform.parent.name} je van ekrana {RectOf(c.rectTransform)}", true);
            if (report.Length == before) report.AppendLine($"  [ok] {screen}");
        }
    }
}
