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
        Rng raceRng;                              // zajedničko seme trke (isto na svim uređajima u multiplayeru)
        public MpSession Multi { get; private set; }
        string worldKey;
        Transform worldRoot;
        Showcase hero;                            // tvoj lik na postolju (meni i garaža)
        public Showcase Hero => hero;
        public WorldView World => world;

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
            hero = null;
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
            ShowHero();
            fx.BlurBackground(false);
            MenuScreen.Build(ui.Screen, Profile, AreaCounts(), OnMenu);
            if (AutoPlay) StartCoroutine(AutoStart());
        }

        public static Dictionary<string, int> AreaCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var q in GameData.Questions) counts[q.Oblast] = counts.TryGetValue(q.Oblast, out var c) ? c + 1 : 1;
            return counts;
        }

        // Lik na postolju na početku staze; kamera ga snima sa staze prije starta (iza njega se vidi put kroz svijet).
        public void ShowHero(bool garage = false)
        {
            if (world == null || track == null) return;
            if (hero == null)
            {
                // malo dalje od starta, da kapija starta ostane iza kamere
                track.PoseAt(Mathf.Min(4.2f, track.TotalArc * 0.25f), out var pos, out var fwd);
                hero = Showcase.Create(world.transform, pos, fwd);
            }
            hero.SetLook(Profile.Izgled);
            ui.Labels.gameObject.SetActive(false);    // nazivi gradova bi se preklapali sa menijem
            if (garage) return;
            hero.AutoSpin = false;
            hero.Yaw = -32;
            Camera.FixedRate = 2;
            hero.CameraAt(0, 10, 2.7f, -0.07f, world, out var cp, out var cl, 0.04f);
            Camera.Fixed(cp, cl);
        }

        void HideHero()
        {
            if (hero != null) Destroy(hero.gameObject);
            hero = null;
            ui.Labels.gameObject.SetActive(true);
            Camera.FixedRate = 2;
        }

        IEnumerator AutoStart()
        {
            yield return null;
            StartCoroutine(RunRace(false));
        }

        void OnMenu(MenuChoice c)
        {
            Save();
            if (c.LookChanged) { hero?.SetLook(Profile.Izgled); return; }
            if (c.MapChanged != null) { ShowWorld(c.MapChanged, null); ShowHero(); return; }
            if (c.Start) { StartCoroutine(RunRace(false)); return; }
            if (c.Settings) { SettingsScreen.Build(ui.Screen, Profile, OnSetting, () => { Save(); ShowMenu(); }); return; }
            if (c.Garage) { ShowHero(true); GarageScreen.Open(ui.Screen, hero, world, Profile, Save, ShowMenu); return; }
            if (c.Multiplayer) { OpenMultiplayer(c.Friends); return; }
            if (c.Leaderboard) ui.Toast("Rang-liste (prijatelji, grad, škola, cijela država) stižu uskoro.", Theme.Gold);
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

        // ---------- igra sa drugima ----------
        void OpenMultiplayer(string start = null)
        {
            ui.ClearScreen();
            UIKit.Clear(ui.Hud);
            MultiplayerScreen.Open(ui.Screen, Profile, this, MultiSetup, (mp, setup) => StartCoroutine(RunRace(false, setup, mp)), ShowMenu, start);
        }

        // Domaćin pravi postavu: njegova mapa, dužina i oblasti; ljudi po redu (domaćin prvi), ostalo botovi.
        RaceSetup MultiSetup(MpSession mp)
        {
            var setup = SoloSetup(false, mp.LocalId);
            var order = mp.Members.Select(m => m.Id).ToList();
            if (mp.Group != null) order = mp.Group.Where(mp.Present).Concat(order.Where(id => !mp.Group.Contains(id))).ToList();
            order.Remove(mp.LocalId);
            order.Insert(0, mp.LocalId);
            setup.Humans = order.Take(RaceSetup.Seats).Select(id =>
            {
                if (id == mp.LocalId) return new MpPlayer { Id = id, Name = string.IsNullOrEmpty(Profile.Ime) ? Lang.L("Igrač") : Profile.Ime, Look = Profile.Izgled.Clone() };
                var m = mp.Members.First(x => x.Id == id);
                return new MpPlayer { Id = id, Name = m.Name, Look = m.Look };
            }).ToList();
            return setup;
        }

        // za testove: pokreni trku sa već spojenom sobom
        public void StartMultiplayerRace(MpSession mp)
        {
            if (!mp.IsHost) return;
            mp.OnStart += setup => StartCoroutine(RunRace(false, setup, mp));
            mp.StartRace(MultiSetup(mp));
        }

        void MpProblem(string msg) => ui.Toast(msg, Theme.Red, 4);

        // ---------- trka ----------
        // Postava trke za igru protiv botova: isto kao u multiplayeru, samo sa jednim čovjekom.
        RaceSetup SoloSetup(bool tutorial, string localId)
        {
            var S = Profile.Podesavanja;
            var map = Catalog.MapOf(S.Mapa);
            if (!Profile.MapUnlocked(map)) map = Catalog.Maps[0];
            var age = Catalog.AgeOf(S.Uzrast);
            return new RaceSetup
            {
                Seed = rng.Range(1, int.MaxValue),
                Map = map.Id,
                Tiles = tutorial ? 15 : Catalog.LengthOf(S.Duzina).Tiles,
                Atmosphere = tutorial ? "dan" : PickAtmosphere(map),
                Level = tutorial ? "lako" : S.Nivo,
                Areas = S.Miks || S.Oblasti == null || S.Oblasti.Count == 0 ? Catalog.Areas.Select(a => a.Id).ToList() : S.Oblasti.ToList(),
                MaxTezina = tutorial ? 1 : age.MaxTezina,
                BonusSeconds = age.BonusSeconds,
                Humans = new List<MpPlayer> { new MpPlayer { Id = localId, Name = string.IsNullOrEmpty(Profile.Ime) ? Lang.L("Ti") : Profile.Ime, Look = Profile.Izgled.Clone() } },
            };
        }

        IEnumerator RunRace(bool tutorial) => RunRace(tutorial, null, null);

        IEnumerator RunRace(bool tutorial, RaceSetup setup, MpSession mp)
        {
            InRace = true;
            ui.ClearScreen();
            HideHero();
            var S = Profile.Podesavanja;
            var localId = mp != null ? mp.LocalId : "ti";
            setup ??= SoloSetup(tutorial, localId);
            var map = Catalog.MapOf(setup.Map);
            var tiles = setup.Tiles;
            worldKey = null;
            var oldLen = S.Duzina;
            var kinds = TileKinds.Generate(tiles, setup.Seed);
            ShowWorldExact(map, tiles, setup.Atmosphere, kinds);

            // sve što mora biti isto na svim uređajima ide iz zajedničkog semena: botovi, pitanja, mini igre
            var shared = new Rng(setup.Seed);
            raceRng = shared;
            Minigame.ResetQueue();
            var race = new Race(setup.BuildPlayers(localId, shared), tiles, shared, kinds);
            var human = race.Human;
            CurrentRace = race;
            Multi = mp;
            var questions = mp != null ? GameData.Questions.Take(GameData.BaseQuestionCount).ToList() : GameData.Questions;
            // u igri protiv botova preskačemo pitanja viđena u ranijim trkama (u multiplayeru špil mora biti isti za sve)
            var deck = new Deck(questions, setup.Areas, shared, setup.MaxTezina, mp == null ? new HashSet<string>(Profile.Vidjena) : null);

            foreach (var v in views) if (v != null) Destroy(v.gameObject);
            views.Clear();
            foreach (var p in race.Players) views.Add(RacerView.Create(p, track, world.transform));
            yield return Relayout(race);
            hud = Hud.Create(ui.Hud, race);
            hud.Refresh(1);
            var me = views.First(v => v.Racer.Human);
            Camera.Follow(me.transform);
            Camera.Snap();
            ui.Toast(tutorial ? "Odgovori na pitanje. Brz i tačan odgovor = više polja." : $"Trka počinje: {map.Route[0].name}. Srećno!", Col.Hex(human.Color), 3);
            if (mp != null) mp.OnProblem += MpProblem;
            yield return Tw.Wait(AutoPlay ? 0.1f : 1.2f, true);

            while (true)
            {
                int round = race.NextRound();
                hud.Refresh(round);
                if (race.IsDoubleRound)
                {
                    ui.Toast("DUPLA RUNDA! Sva polja ×2", Theme.Gold, 2.5f);
                    AudioKit.I?.Play("fanfara", 0.6f);
                    if (!AutoPlay) yield return Tw.Wait(1.0f, true);
                }
                var q = deck.Next().Localized(Lang.Mode);
                if (setup.BonusSeconds > 0) { q = q.Clone(); q.Vrijeme = QuizTypes.TimeLimitOf(q) + setup.BonusSeconds; }

                // pitanje
                Camera.QuestionOrbit(me.transform);
                fx.BlurBackground(true);
                var pus = Catalog.Powerups.Select(pu => (pu, Profile.Powerupi.TryGetValue(pu.Id, out var n) ? n : 0)).Where(x => x.Item2 > 0).ToList();
                var extras = new QuestionScreen.Extras { Item = human.Item, Confused = human.Confused, DoubleRound = race.IsDoubleRound, DoubleNext = human.DoubleNext };
                var qs = QuestionScreen.Open(ui.Screen, q, round, rng, tutorial ? new List<(Powerup, int)>() : pus, extras);
                if (human.Confused) ui.Toast("Zbunjen si: manje vremena za ovo pitanje!", Theme.Red, 2.5f);
                Profile.MarkSeen(q.Id);
                if (tutorial && round == 2) ui.Toast("Kutije na putu daju napade, zelene strelice su prečice, a blato vraća nazad.", Theme.Gold, 5);
                if (tutorial && round == 3) ui.Toast("Prvi na cilju pobjeđuje!", Theme.Go, 4);
                qs.OnUsePowerup = id => { Profile.UsePowerup(id); Save(); return true; };
                if (AutoPlay) { yield return Tw.Wait(0.2f, true); qs.AutoAnswer(rng); }
                yield return qs.WaitAnswer();
                List<RoundEntry> entries = null;
                if (mp == null) entries = race.ResolveRound(q, qs.Result);
                else
                {
                    // pošalji svoj odgovor i sačekaj odluku domaćina (svi primjenjuju isto)
                    var r = qs.Result;
                    var text = r.TimedOut && r.Accuracy < 0.2 ? "isteklo vrijeme" : QuizTypes.Get(q.Tip).Summarize(q, r);
                    var key = "q:" + round;
                    mp.Contribute(key, MpAnswer.From(r, text).ToJson());
                    var persons = race.Players.Where(p => p.Person).Select(p => p.Id).ToList();
                    var limit = QuizTypes.TimeLimitOf(q);
                    float waited = 0;
                    Dictionary<string, Dictionary<string, object>> res = null;
                    StartCoroutine(mp.Await(key, persons, (float)limit + 12, id => MpAnswer.Timeout(limit).ToJson(), d => res = d));
                    while (res == null)
                    {
                        waited += Time.unscaledDeltaTime;
                        if (waited > 1.2f && waited - Time.unscaledDeltaTime <= 1.2f) ui.Toast("Čekam ostale igrače…", Theme.InkSoft, 2);
                        yield return null;
                    }
                    entries = race.ResolveRound(q, res.ToDictionary(kv => kv.Key, kv => MpAnswer.FromJson(kv.Value)));
                }
                var attacks = race.Events.ToList();
                yield return qs.ShowBoard(entries);
                qs.Close();
                fx.BlurBackground(false);
                foreach (var ev in attacks) ui.Toast(ev.Text, ev.Kind == "blokirano" ? Theme.Blue : Theme.Red, 3);

                // vožnja (i nazad, ako je neko napadnut)
                var moves = race.MovesFor(entries);
                yield return Drive(race, moves);
                race.Apply(moves);
                // polja na kojima su stali: prečica, blato, dupla polja, kutija
                race.Events.Clear();
                var landings = race.Landings(moves);
                foreach (var ev in race.Events)
                {
                    var c = ev.Kind == "blato" ? Col.Hex("#8a5a33") : ev.Kind == "precica" ? Theme.Go : ev.Kind == "kutija" ? Theme.Purple : Theme.Gold;
                    if (ev.Who.Human || ev.Kind == "kutija") ui.Toast(ev.Text, c, 2.5f);
                    if (ev.Who.Human && ev.Kind == "kutija") AudioKit.I?.Play("novcic", 0.8f);
                }
                if (landings.Count > 0) { yield return Drive(race, landings); race.Apply(landings); }
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

                if (race.IsMinigameRound) yield return Bonus(race);

                if (race.CheckFinish())
                {
                    yield return Finish(race, map);
                    yield break;
                }
                yield return Tw.Wait(0.2f, true);
            }
        }

        void ShowWorldExact(RaceMap map, int tiles, string atmosphere, IList<string> kinds = null)
        {
            var key = $"{map.Id}|{tiles}|{atmosphere}|{(kinds == null ? "" : string.Join(",", kinds))}";
            if (key == worldKey && world != null) return;
            worldKey = key;
            if (world != null) Destroy(world.gameObject);
            labels?.Clear();
            world = WorldView.Build(worldRoot, map, tiles, atmosphere, rng.Range(1, 100000), kinds);
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
                if (m.Target != m.From) running.Add(StartCoroutine(ViewOf(m.Racer).DriveTo(m.Target, AutoPlay ? 6 : 1)));
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

        IEnumerator Bonus(Race race)
        {
            var mg = Minigame.Pick(raceRng);
            int seconds = mg.Duration;
            var outcomes = new List<StationOutcome>();
            var key = "bo:" + race.Round;
            var persons = race.Players.Where(p => p.Person).Select(p => p.Id).ToList();
            Dictionary<string, Dictionary<string, object>> scores = null;
            IEnumerator Exchange(double score, string label)
            {
                if (Multi == null) yield break;
                Multi.Contribute(key, new MpScore { Score = score, Label = label }.ToJson());
                yield return Multi.Await(key, persons, seconds + 25, id => new MpScore { Label = "—" }.ToJson(), d => scores = d);
            }
            fx.BlurBackground(true);
            yield return MinigameScreen.Run(ui.Screen, mg, seconds, "Mini igra · svi igraju", Theme.Purple,
                Lang.L("Svi igraju istu mini igru. 1. mjesto ide 3 polja naprijed, 2. mjesto 2 polja, 3. mjesto 1 polje, uz novčiće."), rng, outcomes,
                (score, label) =>
                {
                    var results = race.Players.Select(p =>
                    {
                        if (p.Human && Multi == null) return (p, score, label);
                        if (p.Person)
                        {
                            var sc = MpScore.FromJson(scores != null && scores.TryGetValue(p.Id, out var v) ? v : null) ?? new MpScore { Label = "—" };
                            return (p, sc.Score, sc.Label);
                        }
                        var s = mg.SimulateBot(Bots.MinigameSkill(p), raceRng);
                        return (p, System.Math.Min(1, s.score * 1.0), s.label);
                    }).ToList();
                    return race.Bonus(results);
                }, Multi != null ? Exchange : (System.Func<double, string, IEnumerator>)null);
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
            if (Multi != null)
            {
                Multi.OnProblem -= MpProblem;
                Multi.Leave();
                Multi = null;
                if (again) OpenMultiplayer(); else ShowMenu();
                yield break;
            }
            if (again) StartCoroutine(RunRace(false));
            else if (!AutoPlay) ShowMenu();
        }
    }
}
