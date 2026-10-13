// Glavni tok igre: učitavanje → (prvi put: kratki let iznad mape i probna trka) → meni → trka → cilj.
// Pravi cijelu scenu u kodu (kamera, sunce, post-processing, UI, zvuk), pa radi u bilo kojoj sceni.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrkaZnanja.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrkaZnanja.Game
{
    public sealed class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public static bool AutoPlay;            // za automatske testove: igra sama sebe
        public static bool TestMode;            // za testove: učitaj sve, ali ne pokreći meni
        public static int? ForcedSeed;           // za testove: ista trka svaki put

        public CameraRig Camera { get; private set; }
        public Profile Profile { get; private set; }
        public Race CurrentRace { get; private set; }
        public bool InRace { get; private set; }
        public int RacesFinished { get; private set; }
        Light sun;
        PostFx fx;
        UIRoot ui;
        WorldView world;
        TrackView track;
        WorldLabels labels;
        Hud hud;
        readonly List<RacerView> views = new List<RacerView>();
        Rng rng;
        string worldKey;
        Transform worldRoot;

        public static App Boot()
        {
            if (I != null) return I;
            var go = new GameObject("Trka znanja");
            DontDestroyOnLoad(go);
            return go.AddComponent<App>();
        }

        void Awake()
        {
            I = this;
            rng = ForcedSeed.HasValue ? new Rng(ForcedSeed.Value) : new Rng();
            ProfileStore.Test = AutoPlay || TestMode;    // testovi ne diraju pravi profil
            Profile = ProfileStore.Load();
            var S = Profile.Podesavanja;
            Lang.Set(string.IsNullOrEmpty(S.Jezik) ? Lang.FromSystemLanguage(Application.systemLanguage.ToString()) : S.Jezik);
            PostFx.ApplyQuality(S.Kvalitet);
            Haptics.Enabled = S.Vibracija;

            worldRoot = new GameObject("3D").transform;
            worldRoot.SetParent(transform, false);
            Camera = CameraRig.Create(transform);
            var sunGo = new GameObject("Sunce");
            sunGo.transform.SetParent(transform, false);
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;
            RenderSettings.sun = sun;
            fx = PostFx.Create(transform, Camera.Cam);
            ui = UIRoot.Create(transform);
            var audio = AudioKit.Create(transform);
            audio.SetSfx(S.Efekti);
        }

        IEnumerator Start()
        {
            var loading = UIKit.Label(ui.Screen, "Učitavam…", 48, Color.white, TextAnchor.MiddleCenter, true);
            loading.rectTransform.Stretch();
            loading.Outlined(new Color(0, 0, 0, 0.5f), 3);
            Camera.Cam.backgroundColor = Col.Hex("#3d8ee8");
            yield return null;
            GameData.LoadTranslations();
            var count = GameData.Questions.Count;
            Debug.Log($"Trka znanja: {count} pitanja učitano.");
            if (Profile.Podesavanja.Muzika && !AutoPlay) AudioKit.I.SetMusic(true);
            ShowWorld(Profile.Podesavanja.Mapa, null);
            Destroy(loading.gameObject);
            if (TestMode) yield break;
            if (!AutoPlay) StartCoroutine(NetStart());   // testovi ne prave naloge na serveru

            if (!Profile.Podesavanja.UputstvoGotovo && !AutoPlay)
            {
                yield return Intro();
                yield return RunRace(true);
            }
            ShowMenu();
        }

        IEnumerator NetStart()
        {
            bool ok = false;
            yield return Net.Init(r => ok = r);
            if (!ok) yield break;
            yield return Net.ApprovedQuestions(list => GameData.AddQuestions(list));
            yield return Net.SendPendingResults(Profile, Save);
            yield return Net.PushProfile(Profile);
        }

        public void Save() => ProfileStore.Save(Profile);

        // ---------- svijet ----------
        void ShowWorld(string mapId, string atmosphere)
        {
            var map = Catalog.MapOf(mapId);
            if (!Profile.MapUnlocked(map)) map = Catalog.Maps[0];
            var tiles = Catalog.LengthOf(Profile.Podesavanja.Duzina).Tiles;
            atmosphere ??= map.Atmosfera;
            var key = $"{map.Id}|{tiles}|{atmosphere}";
            if (key == worldKey && world != null) return;
            worldKey = key;
            if (world != null) Destroy(world.gameObject);
            labels?.Clear();
            var seed = rng.Range(1, 100000);
            world = WorldView.Build(worldRoot, map, tiles, atmosphere, seed);
            track = TrackView.Build(world);
            world.Atmo.Apply(sun);
            fx.Apply(world.Atmo);
            Camera.SetWorld(world.Bounds);
            labels = WorldLabels.Create(ui.Labels, Camera.Cam);
            foreach (var l in track.Labels) labels.Add(l.Text, l.Pos, l.Kind);
            foreach (var (name, pos) in Landmarks.Placed) labels.Add(name, pos, "znamenitost");
        }

        string PickAtmosphere(RaceMap map)
        {
            var r = rng.Next();
            if (r < 0.35) return "dan";
            if (r < 0.6) return map.Atmosfera;
            if (r < 0.75) return "zalazak";
            if (r < 0.85) return "noc";
            if (r < 0.95) return "kisa";
            return map.Id == "cg" || map.Id == "balkan" ? "snijeg" : "dan";
        }

        // ---------- prvi put: kratki let iznad mape ----------
        IEnumerator Intro()
        {
            Camera.MapOrbit();
            var title = UIKit.Label(ui.Screen, "Trka znanja", 140, Color.white, TextAnchor.MiddleCenter, true);
            title.Outlined(Col.Hex("#1b4f8a"), 6);
            title.rectTransform.Stretch(0, 0, 0, 200);
            var sub = UIKit.Label(ui.Screen, "Proba: 3 pitanja da vidiš kako se igra.", 40, Color.white, TextAnchor.MiddleCenter);
            sub.Outlined(new Color(0, 0, 0, 0.5f), 3);
            sub.rectTransform.Stretch(0, 300, 0, 0);
            var skip = UIKit.Button(ui.Screen, "Preskoči", Theme.InkSoft, () => { Profile.Podesavanja.UputstvoGotovo = true; }, 30);
            ((RectTransform)skip.transform).Anchor(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(260, 90));
            var cg = title.gameObject.AddComponent<CanvasGroup>();
            yield return Tw.Run(1.2f, k => { cg.alpha = k; title.rectTransform.localScale = Vector3.one * (0.8f + 0.2f * Tw.EaseOutBack(k)); }, true);
            float t = 0;
            while (t < 3.5f && !Profile.Podesavanja.UputstvoGotovo) { t += Time.unscaledDeltaTime; yield return null; }
            ui.ClearScreen();
        }

        // ---------- meni ----------
        void ShowMenu()
        {
            InRace = false;
            Time.timeScale = 1;
            ui.ClearScreen();
            UIKit.Clear(ui.Hud);
            ShowWorld(Profile.Podesavanja.Mapa, null);
            Camera.MapOrbit();
            fx.BlurBackground(false);
            var counts = new Dictionary<string, int>();
            foreach (var q in GameData.Questions) counts[q.Oblast] = counts.TryGetValue(q.Oblast, out var c) ? c + 1 : 1;
            MenuScreen.Build(ui.Screen, Profile, counts, OnMenu);
            if (AutoPlay) StartCoroutine(AutoStart());
        }

        IEnumerator AutoStart()
        {
            yield return null;
            StartCoroutine(RunRace(false));
        }

        void OnMenu(MenuChoice c)
        {
            Save();
            if (c.MapChanged != null) { ShowWorld(c.MapChanged, null); return; }
            if (c.Start) { StartCoroutine(RunRace(false)); return; }
            if (c.Settings) { SettingsScreen.Build(ui.Screen, Profile, OnSetting, () => { Save(); ShowMenu(); }); return; }
            if (c.Garage) { GarageScreen.Open(ui.Screen, worldRoot, Profile, Save, ShowMenu); return; }
            if (c.Leaderboard) ui.Toast("Rang-lista sa drugim igračima stiže sa multiplayerom.", Theme.Gold);
        }

        void OnSetting(string what)
        {
            var S = Profile.Podesavanja;
            switch (what)
            {
                case "jezik":
                    Lang.Set(string.IsNullOrEmpty(S.Jezik) ? Lang.FromSystemLanguage(Application.systemLanguage.ToString()) : S.Jezik);
                    SettingsScreen.Build(ui.Screen, Profile, OnSetting, () => { Save(); ShowMenu(); });
                    worldKey = null;
                    break;
                case "kvalitet": PostFx.ApplyQuality(S.Kvalitet); if (world != null) fx.Apply(world.Atmo); break;
                case "zvuk": AudioKit.I.SetMusic(S.Muzika); AudioKit.I.SetSfx(S.Efekti); break;
                case "vibracija": Haptics.Enabled = S.Vibracija; break;
            }
            Save();
        }

        // ---------- trka ----------
        IEnumerator RunRace(bool tutorial)
        {
            InRace = true;
            ui.ClearScreen();
            var S = Profile.Podesavanja;
            var map = Catalog.MapOf(S.Mapa);
            if (!Profile.MapUnlocked(map)) map = Catalog.Maps[0];
            var age = Catalog.AgeOf(S.Uzrast);
            var tiles = tutorial ? 15 : Catalog.LengthOf(S.Duzina).Tiles;
            worldKey = null;
            var atmo = tutorial ? "dan" : PickAtmosphere(map);
            var oldLen = S.Duzina;
            ShowWorldExact(map, tiles, atmo);

            var human = new Racer { Id = "ti", Name = string.IsNullOrEmpty(Profile.Ime) ? Lang.L("Ti") : Profile.Ime, Color = "#e0312f", Human = true, Look = Profile.Izgled };
            var bots = Bots.Create(tutorial ? "lako" : S.Nivo, rng, 3, Profile.Izgled.Zivotinja);
            var race = new Race(human, bots, tiles, rng);
            CurrentRace = race;
            var areas = S.Miks || S.Oblasti == null || S.Oblasti.Count == 0 ? Catalog.Areas.Select(a => a.Id).ToList() : S.Oblasti;
            var deck = new Deck(GameData.Questions, areas, rng, tutorial ? 1 : age.MaxTezina);

            foreach (var v in views) if (v != null) Destroy(v.gameObject);
            views.Clear();
            foreach (var p in race.Players) views.Add(RacerView.Create(p, track, world.transform));
            yield return Relayout(race);
            hud = Hud.Create(ui.Hud, race, Catalog.CheckpointEvery);
            hud.Refresh(1);
            var me = views.First(v => v.Racer.Human);
            Camera.Follow(me.transform);
            Camera.Snap();
            ui.Toast(tutorial ? "Odgovori na pitanje. Brz i tačan odgovor = više polja." : $"Trka počinje: {map.Route[0].name}. Srećno!", Col.Hex(human.Color), 3);
            yield return Tw.Wait(AutoPlay ? 0.1f : 1.2f, true);

            while (true)
            {
                int round = race.NextRound();
                hud.Refresh(round);
                var q = deck.Next().Localized(Lang.Mode);
                if (age.BonusSeconds > 0) { q = q.Clone(); q.Vrijeme = QuizTypes.TimeLimitOf(q) + age.BonusSeconds; }

                // pitanje
                Camera.QuestionOrbit(me.transform);
                fx.BlurBackground(true);
                var pus = Catalog.Powerups.Select(pu => (pu, Profile.Powerupi.TryGetValue(pu.Id, out var n) ? n : 0)).Where(x => x.Item2 > 0).ToList();
                var qs = QuestionScreen.Open(ui.Screen, q, round, rng, tutorial ? new List<(Powerup, int)>() : pus);
                if (tutorial && round == 2) ui.Toast("Zlatna polja su stanice sa mini igrom.", Theme.Gold, 4);
                if (tutorial && round == 3) ui.Toast("Prvi na cilju pobjeđuje!", Theme.Go, 4);
                qs.OnUsePowerup = id => { Profile.UsePowerup(id); Save(); return true; };
                if (AutoPlay) { yield return Tw.Wait(0.2f, true); qs.AutoAnswer(rng); }
                yield return qs.WaitAnswer();
                var entries = race.ResolveRound(q, qs.Result);
                yield return qs.ShowBoard(entries);
                qs.Close();
                fx.BlurBackground(false);

                // vožnja
                var moves = race.MovesFor(entries);
                yield return Drive(race, moves);
                race.Apply(moves);
                yield return Relayout(race);
                hud.Refresh(round);

                if (tutorial && round >= 3)
                {
                    Profile.Podesavanja.UputstvoGotovo = true;
                    Save();
                    ui.Toast("Spreman si! Sad prava trka.", Theme.Go, 3);
                    yield return Tw.Wait(1.5f, true);
                    S.Duzina = oldLen;
                    ShowMenu();
                    yield break;
                }

                var station = race.AtStation(moves);
                if (station.Count > 0) yield return Station(race, station);
                if (race.IsBonusRound) yield return Bonus(race);

                if (race.CheckFinish())
                {
                    yield return Finish(race, map);
                    yield break;
                }
                yield return Tw.Wait(0.2f, true);
            }
        }

        void ShowWorldExact(RaceMap map, int tiles, string atmosphere)
        {
            var key = $"{map.Id}|{tiles}|{atmosphere}";
            if (key == worldKey && world != null) return;
            worldKey = key;
            if (world != null) Destroy(world.gameObject);
            labels?.Clear();
            world = WorldView.Build(worldRoot, map, tiles, atmosphere, rng.Range(1, 100000));
            track = TrackView.Build(world);
            world.Atmo.Apply(sun);
            fx.Apply(world.Atmo);
            Camera.SetWorld(world.Bounds);
            labels = WorldLabels.Create(ui.Labels, Camera.Cam);
            foreach (var l in track.Labels) labels.Add(l.Text, l.Pos, l.Kind);
            foreach (var (name, pos) in Landmarks.Placed) labels.Add(name, pos, "znamenitost");
        }

        RacerView ViewOf(Racer r) => views.First(v => v.Racer == r);

        IEnumerator Drive(Race race, List<Move> moves)
        {
            var me = views.First(v => v.Racer.Human);
            Camera.Follow(me.transform);
            // usporen snimak: neko u ovoj vožnji prolazi kroz cilj
            var finisher = moves.Where(m => m.Target >= race.Last && m.Target > m.From).OrderBy(m => m.Racer.Human ? 0 : 1).FirstOrDefault();
            if (finisher != null && !AutoPlay)
            {
                Camera.Finale(ViewOf(finisher.Racer).transform);
                Time.timeScale = 0.4f;
                AudioKit.I?.Play("fanfara", 0.9f);
            }
            var running = new List<Coroutine>();
            foreach (var m in moves)
                if (m.Target > m.From) running.Add(StartCoroutine(ViewOf(m.Racer).DriveTo(m.Target, AutoPlay ? 6 : 1)));
            foreach (var c in running) yield return c;
            if (finisher != null) { Time.timeScale = 1; if (!AutoPlay) yield return Tw.Wait(0.6f, true); }
            foreach (var m in moves.Where(x => x.Target > x.From && x.Racer.Human))
                Effects.Sparkle(me.Position + Vector3.up * 0.4f, world.transform, Theme.Gold);
        }

        // Više igrača na istom polju stoje jedan pored drugog.
        IEnumerator Relayout(Race race)
        {
            var running = new List<Coroutine>();
            foreach (var g in race.Players.GroupBy(p => p.Pos))
            {
                var list = g.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var off = track.SlotPosition(g.Key, i, list.Count) - track.SlotPosition(g.Key, 0, 1);
                    running.Add(StartCoroutine(ViewOf(list[i]).SlideTo(off)));
                }
            }
            foreach (var c in running) yield return c;
        }

        IEnumerator Station(Race race, List<Racer> atStation)
        {
            var mg = Minigame.Pick(rng);
            var me = atStation.FirstOrDefault(p => p.Human);
            var outcomes = new List<StationOutcome>();
            if (me != null)
            {
                fx.BlurBackground(true);
                var tile = track.Tiles[me.Pos];
                yield return MinigameScreen.Run(ui.Screen, mg, mg.Duration, $"Stanica · {tile.Near}", Theme.Gold,
                    Lang.L("Nagrada: novčići i skok do 3 polja naprijed."), rng, outcomes, (score, label) =>
                    {
                        var list = new List<StationOutcome> { race.Station(me, score, label) };
                        foreach (var b in atStation.Where(p => !p.Human)) { var s = mg.SimulateBot(Bots.MinigameSkill(b), rng); list.Add(race.Station(b, s.score, s.label)); }
                        return list;
                    });
                fx.BlurBackground(false);
                if (outcomes.Count > 0 && outcomes[0].Coins > 0) AudioKit.I?.Play("novcic");
            }
            else
            {
                foreach (var b in atStation)
                {
                    var s = mg.SimulateBot(Bots.MinigameSkill(b), rng);
                    var o = race.Station(b, s.score, s.label);
                    outcomes.Add(o);
                    ui.Toast($"{b.Name} na stanici: {Lang.L(o.Label)} » {(o.Jump > 0 ? $"+{o.Jump} polja" : "bez skoka")}", Col.Hex(b.Color));
                }
                yield return Tw.Wait(0.8f, true);
            }
            var jumps = race.Jumps(outcomes);
            yield return Drive(race, jumps);
            race.Apply(jumps);
            yield return Relayout(race);
            hud.Refresh(race.Round);
        }

        IEnumerator Bonus(Race race)
        {
            var mg = Minigame.Pick(rng);
            int seconds = Mathf.RoundToInt(mg.Duration * (float)Catalog.BonusTime / 5f) * 5;
            var outcomes = new List<StationOutcome>();
            fx.BlurBackground(true);
            yield return MinigameScreen.Run(ui.Screen, mg, seconds, "Bonus runda · svi igraju", Theme.Purple,
                Lang.L("Sva četiri igrača igraju istu mini igru. Pobjednik ide 3 polja naprijed i dobija 50 novčića, drugi ide 1 polje."), rng, outcomes,
                (score, label) =>
                {
                    var results = race.Players.Select(p =>
                    {
                        if (p.Human) return (p, score, label);
                        var s = mg.SimulateBot(Bots.MinigameSkill(p), rng);
                        return (p, System.Math.Min(1, s.score * 1.0), s.label);
                    });
                    return race.Bonus(results);
                });
            fx.BlurBackground(false);
            var jumps = race.Jumps(outcomes);
            yield return Drive(race, jumps);
            race.Apply(jumps);
            yield return Relayout(race);
            hud.Refresh(race.Round);
        }

        // ---------- cilj: usporen snimak, postolje, vatromet i konfete ----------
        IEnumerator Finish(Race race, RaceMap map)
        {
            var ranked = race.Ranking();
            var winner = ViewOf(ranked[0]);
            var finishPos = track.TileCenter(race.Last);
            Camera.Finale(winner.transform);

            // postolje 1-2-3 pored cilja
            var fwd = track.Tiles[race.Last].Dir.U(0);
            var side = Vector3.Cross(Vector3.up, fwd);
            var podiumCenter = finishPos + fwd * 1.6f;
            var k = new MeshKit();
            float[] heights = { 0.55f, 0.38f, 0.26f };
            Vector3[] offsets = { Vector3.zero, -side * 0.75f, side * 0.75f };
            string[] colors = { "#f6bd2a", "#c9d3dc", "#d38b4a" };
            for (int i = 0; i < 3; i++)
            {
                k.Push(podiumCenter + offsets[i] + Vector3.down * 0.05f, Quaternion.LookRotation(-fwd));
                k.RoundBox(new Vector3(0, heights[i] / 2, 0), new Vector3(0.7f, heights[i], 0.7f), 0.05f, MeshKit.C(colors[i]));
                k.Pop();
            }
            var podium = k.ToObject("Postolje", world.transform);
            for (int i = 0; i < 3 && i < ranked.Count; i++)
            {
                var v = ViewOf(ranked[i]);
                v.StopAllCoroutines();
                v.transform.position = podiumCenter + offsets[i] + Vector3.up * (heights[i] - 0.05f);
                v.transform.rotation = Quaternion.LookRotation(-fwd);
                v.SetDancing(i == 0);
            }
            if (ranked.Count > 3) ViewOf(ranked[3]).transform.position = podiumCenter + fwd * 1.0f;
            Camera.Fixed(podiumCenter - fwd * 3.0f + Vector3.up * 1.4f, podiumCenter + Vector3.up * 0.45f);
            Effects.Confetti(podiumCenter + Vector3.up * 2.2f, world.transform);
            StartCoroutine(Effects.FireworkShow(podiumCenter + fwd * 1.5f, world.transform, 6));
            AudioKit.I?.Play("fanfara", 0.8f);

            var me = race.Human;
            var award = Profile.AwardRace(ranked.IndexOf(me), me.Correct, me.Answered, me.Coins, map.Id);
            Save();
            RacesFinished++;
            if (!AutoPlay)
            {
                StartCoroutine(Net.SendPendingResults(Profile, Save));
                StartCoroutine(Net.PushProfile(Profile));
            }
            yield return Tw.Wait(AutoPlay ? 0.1f : 1.0f, true);
            UIKit.Clear(ui.Hud);
            bool again = false, menu = false;
            ResultsScreen.Build(ui.Screen, race, ranked, award, map, Profile, () => again = true, () => menu = true);
            if (award.RankUp != null) ui.Toast($"Novi rang: {award.RankUp.Label}!", Col.Hex(award.RankUp.Color), 4);
            float w = 0;
            while (!again && !menu)
            {
                w += Time.unscaledDeltaTime;
                if (AutoPlay && w > 0.5f) menu = true;
                yield return null;
            }
            Destroy(podium);
            foreach (var v in views) v.SetDancing(false);
            InRace = false;
            if (again) StartCoroutine(RunRace(false));
            else if (!AutoPlay) ShowMenu();
        }
    }
}
