// Glavna petlja igre: meni (profil, mapa, oblasti, uzrast, dužina, jezik, garaža) → runde → cilj i nagrade.
import { createWorld, scatterScenery, setProjectionForRoute } from './scene.js';
import { createTrack } from './track.js';
import { createDeck, runQuestion, getType, tilesFor, timeLimitOf } from './quiz/engine.js';
import { preloadBorders } from './quiz/types/mapTap.js';
import { pickMinigame, rewardFor, initMinigames } from './minigames/index.js';
import { createBots, botAnswer, botMinigameSkill, LEVELS, ROSTER, strongWeak } from './bots.js';
import { h, AREAS, openCard, clearLayer, toast, renderHud, hideHud, waitButton, fmtSec } from './ui.js';
import { sleep } from './util.js';
import { MAPS_RACE, TRACK_LENGTHS, loadLand } from './maps.js';
import { profile, save, RANKS, rankOf, rankProgress, rankIndex, AGES, awardRace, POWERUPS, usePowerup } from './profile.js';
import { randomBotLook } from './models.js';
import { openGarage } from './garage.js';
import { loadFlags } from './flags.js';
import { LANGS, setLang, loadCountryNames } from './i18n.js';
import { initNet, isOnline, pushProfile, submitResult, submitSuggestion, approvedQuestions } from './net.js';
import { openLeaderboard, openSuggest } from './social.js';
import { musicEnabled, startMusic, setMusic, isPlaying, duck } from './music.js';

// Muzika: kreće na prvi klik (pregledači ne dozvoljavaju zvuk prije toga); dugme u uglu je pali i gasi.
const musicBtn = h('button', { type: 'button', id: 'muzika', class: 'music-btn', 'aria-label': 'Muzika' });
const syncMusicBtn = () => { musicBtn.classList.toggle('off', !isPlaying()); musicBtn.title = isPlaying() ? 'Isključi muziku' : 'Uključi muziku'; };
musicBtn.addEventListener('click', e => { e.stopPropagation(); setMusic(!isPlaying()); syncMusicBtn(); });
document.getElementById('app').append(musicBtn);
syncMusicBtn();
window.addEventListener('pointerdown', () => { if (musicEnabled() && !isPlaying()) { startMusic(); syncMusicBtn(); } }, { once: true });
// tiša muzika dok se sluša melodija ili zvuk
new MutationObserver(() => duck(!!document.querySelector('.mel-stage'))).observe(document.getElementById('layer'), { childList: true, subtree: true });

const rng = Math.random;
const world = createWorld(document.getElementById('scene'));
const P = profile();
setLang(P.podesavanja.jezik);

let QUESTIONS = [];
let players = [], round = 0, track = null, currentMap = null, currentLand = null;
const PLAYER_COLOR = '#e0312f';

// Mapa u pozadini menija.
async function prepareMap(map, tiles) {
  setProjectionForRoute(map.route, tiles);
  currentLand = await loadLand(map);
  world.buildMap(currentLand, map.route);
  track?.dispose();
  track = createTrack(world, map, tiles);
  scatterScenery(world, currentLand, track.tiles.map(t => t.pos), map.id);
  currentMap = map;
}

function availableAreas() {
  const count = {};
  QUESTIONS.forEach(q => { if (getType(q.tip)) count[q.oblast] = (count[q.oblast] || 0) + 1; });
  return count;
}
const mapUnlocked = m => rankIndex(rankOf(P.bodovi).id) >= rankIndex(m.rank);

// ---------------- meni ----------------
function seg(id, label, options, current, onPick) {
  const el = h('div', { class: 'seg', role: 'radiogroup', 'aria-label': label },
    options.map(([k, text]) => {
      const b = h('button', { type: 'button', role: 'radio', id: `${id}-${k}`, 'aria-checked': String(current === k) }, text);
      b.addEventListener('click', () => { el.querySelectorAll('button').forEach(x => x.setAttribute('aria-checked', String(x === b))); onPick(k); });
      return b;
    }));
  return el;
}

function profileBar() {
  const rank = rankOf(P.bodovi), prog = rankProgress(P.bodovi);
  return h('div', { class: 'profile-bar' },
    h('span', { class: 'rank-badge', style: { '--rc': rank.color } }, rank.label),
    h('div', { class: 'rank-prog' },
      h('div', { class: 'rank-track' }, h('b', { style: { width: `${Math.round(prog.pct * 100)}%`, background: rank.color } })),
      h('small', {}, prog.next ? `${P.bodovi} bodova · još ${prog.need} do ranga ${prog.next.label}` : `${P.bodovi} bodova · najviši rang`)),
    h('span', { class: 'coin-pill big', title: 'Novčići' }, h('i', { class: 'coin' }), h('b', { translate: 'no' }, String(P.novcici))));
}

async function showMenu() {
  world.cam.orbit();
  hideHud();
  const S = P.podesavanja;
  const counts = availableAreas();
  const avail = Object.keys(AREAS).filter(a => counts[a]);
  let picked = new Set((S.oblasti || []).filter(a => avail.includes(a)));
  let mix = S.miks || !picked.size;
  if (!MAPS_RACE.find(m => m.id === S.mapa && mapUnlocked(m))) S.mapa = 'cg';

  const card = openCard('menu');
  const startBtn = h('button', { class: 'btn btn-primary btn-big', type: 'button', id: 'start' }, 'Kreni na start');
  const garageBtn = h('button', { class: 'btn btn-ghost', type: 'button', id: 'garage' }, 'Garaža');
  const areaHint = h('p', { class: 'hint' });
  const chips = h('div', { class: 'area-grid' });
  const mixChip = h('button', { class: 'area-chip mix', type: 'button', id: 'area-mix', 'aria-pressed': 'false' }, h('b', {}, 'Miks'), h('small', {}, 'sve dostupne oblasti'));
  const roster = h('div', { class: 'roster' });

  function refresh() {
    mixChip.setAttribute('aria-pressed', String(mix));
    chips.querySelectorAll('[data-area]').forEach(b => b.setAttribute('aria-pressed', String(!mix && picked.has(b.dataset.area))));
    startBtn.disabled = !(mix || picked.size === Math.min(3, avail.length));
    areaHint.textContent = mix ? `Pitanja iz svih oblasti (${avail.length}).` : `Izabrano ${picked.size} od 3.`;
    const areas = mix ? avail : (picked.size ? [...picked] : avail);
    roster.replaceChildren(...ROSTER.map(b => {
      const sw = strongWeak(b, areas);
      return h('div', { class: 'bot-card' }, h('i', { class: 'dot', style: { background: b.color } }),
        h('div', {}, h('b', {}, b.name), h('small', {}, b.tagline),
          h('small', { class: 'sw' }, `Jak: ${AREAS[sw.strong].label.toLowerCase()} · Slab: ${AREAS[sw.weak].label.toLowerCase()}`)));
    }));
  }
  mixChip.addEventListener('click', () => { mix = true; picked.clear(); refresh(); });
  chips.append(mixChip);
  Object.entries(AREAS).forEach(([key, a]) => {
    const on = !!counts[key];
    const b = h('button', { class: 'area-chip', type: 'button', 'data-area': key, id: `area-${key}`, disabled: !on, 'aria-pressed': 'false', style: { '--chip': a.color } },
      h('b', {}, a.label), h('small', {}, on ? `${counts[key]} pitanja` : 'uskoro'));
    b.addEventListener('click', () => { mix = false; picked.has(key) ? picked.delete(key) : picked.size < 3 && picked.add(key); refresh(); });
    chips.append(b);
  });

  const maps = h('div', { class: 'map-grid' }, MAPS_RACE.map(m => {
    const open = mapUnlocked(m), rank = RANKS.find(r => r.id === m.rank);
    const b = h('button', { type: 'button', class: 'map-card', id: `map-${m.id}`, 'aria-pressed': String(S.mapa === m.id), disabled: !open, style: { '--rc': rank.color } },
      h('b', {}, m.name), h('small', {}, open ? `${m.route[0][0]} → ${m.finish}` : `Otključava se na rangu ${rank.label}`));
    b.addEventListener('click', async () => {
      S.mapa = m.id; save();
      maps.querySelectorAll('.map-card').forEach(x => x.setAttribute('aria-pressed', String(x === b)));
      await prepareMap(m, TRACK_LENGTHS[S.duzina].tiles);
    });
    return b;
  }));

  const nameIn = h('input', { id: 'ime', type: 'text', maxlength: 12, placeholder: 'Ti', value: P.ime, autocomplete: 'off' });
  nameIn.addEventListener('change', () => { P.ime = nameIn.value.trim(); save(); });
  const cityIn = h('input', { id: 'grad', class: 'txt-in', type: 'text', maxlength: 40, placeholder: 'npr. Podgorica', value: P.grad || '', autocomplete: 'off' });
  const schoolIn = h('input', { id: 'skola', class: 'txt-in', type: 'text', maxlength: 60, placeholder: 'npr. Gimnazija „Slobodan Škerović”', value: P.skola || '', autocomplete: 'off' });
  cityIn.addEventListener('change', () => { P.grad = cityIn.value.trim(); save(); });
  schoolIn.addEventListener('change', () => { P.skola = schoolIn.value.trim(); save(); });
  const lbBtn = h('button', { class: 'btn btn-ghost', type: 'button', id: 'rang-lista' }, 'Rang-lista');
  const sgBtn = h('button', { class: 'btn btn-ghost', type: 'button', id: 'predlozi' }, 'Predloži pitanje');

  card.append(
    h('div', { class: 'menu-hero' },
      h('div', { class: 'menu-top' }, h('p', { class: 'eyebrow' }, 'Kviz-trka'),
        seg('lang', 'Jezik', Object.entries(LANGS), S.jezik, k => { S.jezik = k; save(); setLang(k); showMenu(); })),
      h('h1', {}, 'Trka do Durmitora'),
      h('p', { class: 'lede' }, 'Odgovaraj brzo i tačno da pređeš više polja. Na zlatnim stanicama te čeka mini igra. Pobjede donose bodove za rang, a viši rang otključava veće mape.'),
      profileBar()),
    h('div', { class: 'menu-sec' }, h('h3', {}, 'Mapa'), maps),
    h('div', { class: 'menu-sec' }, h('h3', {}, 'Oblasti'), chips, areaHint),
    h('div', { class: 'menu-row' },
      h('div', { class: 'menu-sec' }, h('h3', {}, 'Uzrast'), seg('age', 'Uzrast', Object.entries(AGES).map(([k, a]) => [k, a.label]), S.uzrast, k => { S.uzrast = k; save(); ageNote.textContent = AGES[k].note; }),
        h('p', { class: 'hint', id: 'age-note' }, AGES[S.uzrast].note)),
      h('div', { class: 'menu-sec' }, h('h3', {}, 'Dužina staze'), seg('len', 'Dužina staze', Object.entries(TRACK_LENGTHS).map(([k, t]) => [k, t.label]), S.duzina, async k => {
        S.duzina = k; save(); lenNote.textContent = TRACK_LENGTHS[k].note; await prepareMap(currentMap, TRACK_LENGTHS[k].tiles);
      }), h('p', { class: 'hint', id: 'len-note' }, TRACK_LENGTHS[S.duzina].note))),
    h('div', { class: 'menu-row' },
      h('div', { class: 'menu-sec' }, h('h3', {}, h('label', { for: 'ime' }, 'Tvoje ime')), nameIn),
      h('div', { class: 'menu-sec' }, h('h3', {}, 'Botovi'), seg('lvl', 'Težina botova', Object.entries(LEVELS).map(([k, l]) => [k, l.label]), S.nivo, k => { S.nivo = k; save(); }))),
    h('div', { class: 'menu-row' },
      h('div', { class: 'menu-sec' }, h('h3', {}, h('label', { for: 'grad' }, 'Grad (za rang-listu)')), cityIn),
      h('div', { class: 'menu-sec' }, h('h3', {}, h('label', { for: 'skola' }, 'Škola (opciono)')), schoolIn)),
    h('div', { class: 'menu-sec' }, h('h3', {}, 'Protivnici'), roster),
    S.jezik === 'en' ? h('p', { class: 'hint' }, 'Pitanja su na našem jeziku, prevod na engleski stiže uskoro.') : null,
    h('div', { class: 'menu-go' }, lbBtn, sgBtn, garageBtn, startBtn),
    h('p', { class: 'credits', translate: 'no' }, 'Zastave: flag-icons (MIT) · Mape: Natural Earth · Zvuci instrumenata: FluidR3 GM, Frank Wen (CC BY 3.0) · Fotografije i zvuci životinja: Wikipedia / Wikimedia Commons · Ilustracije: Microsoft Fluent Emoji (MIT)')
  );
  const ageNote = card.querySelector('#age-note'), lenNote = card.querySelector('#len-note');
  refresh();
  garageBtn.addEventListener('click', () => openGarage(showMenu));
  lbBtn.addEventListener('click', () => openLeaderboard(showMenu));
  sgBtn.addEventListener('click', () => openSuggest(showMenu));
  startBtn.addEventListener('click', () => {
    P.ime = nameIn.value.trim();
    Object.assign(S, { miks: mix, oblasti: mix ? null : [...picked] });
    save();
    startRace(mix ? avail : [...picked]);
  });
  const want = MAPS_RACE.find(m => m.id === S.mapa);
  if (!track || currentMap !== want) await prepareMap(want, TRACK_LENGTHS[S.duzina].tiles);
}

// ---------------- trka ----------------
const human = () => players.find(p => p.human);
function followMe() { world.cam.follow(track.tokenPosition(human())); }

async function startRace(areas) {
  const S = P.podesavanja;
  await prepareMap(MAPS_RACE.find(m => m.id === S.mapa), TRACK_LENGTHS[S.duzina].tiles);
  track.clearTokens();
  players = [
    { id: 'ti', name: P.ime || 'Ti', color: PLAYER_COLOR, human: true, look: P.izgled },
    ...createBots(S.nivo).map(b => ({ ...b, look: randomBotLook(rng, b.color) }))
  ].map(p => ({ ...p, pos: 0, coins: 0, lastCp: 0, correct: 0, answered: 0, finishedRound: null }));
  players.forEach(p => track.addToken(p));
  await track.relayout(players);
  round = 0;
  clearLayer();
  followMe();
  renderHud(players, track.last, 1);
  toast(`Trka počinje: ${currentMap.route[0][0]}. Srećno!`, PLAYER_COLOR, 2000);
  await sleep(900);
  const age = AGES[S.uzrast];
  raceLoop(createDeck(QUESTIONS, areas, rng, { maxTezina: age.maxTezina }), age.bonus);
}

function advanceTarget(p, steps) {
  let target = Math.min(track.last, p.pos + steps), cp = false;
  for (let i = p.pos + 1; i <= target; i++) {
    if (track.isCheckpoint(i) && i > p.lastCp) { target = i; cp = true; break; }
  }
  return { target, cp };
}

async function moveAll(list) {
  followMe(); // kamera uvijek prati tvoje vozilo
  await Promise.all(list.map(async ({ p, target }) => {
    if (target > p.pos) { const from = p.pos; await track.move(p, from, target); p.pos = target; }
  }));
  await track.relayout(players);
}

async function raceLoop(deck, timeBonus) {
  while (true) {
    round++;
    renderHud(players, track.last, round);
    const base = deck.next();
    const q = timeBonus ? { ...base, vrijeme: timeLimitOf(base) + timeBonus } : base;
    const type = getType(q.tip);
    const limit = timeLimitOf(q);
    const card = openCard('q-card');
    const powerups = POWERUPS.map(pu => ({ id: pu.id, kind: pu.kind, label: pu.label, count: P.powerupi[pu.id] || 0 })).filter(pu => pu.count > 0);
    const res = await runQuestion(q, card, { round, rng, powerups, onUse: usePowerup });
    const usedShortcut = res.used.includes('precica'), usedSlow = res.used.includes('uspori');

    const results = players.map(p => {
      if (p.human) return { p, acc: res.accuracy, time: res.timeUsed, tiles: res.tiles, text: res.timedOut && res.accuracy < 0.2 ? 'isteklo vrijeme' : type.summarize(res) };
      const b = botAnswer(p, q, type, limit, rng);
      return { p, acc: b.accuracy, time: b.timeUsed, tiles: tilesFor(b.accuracy, b.timeUsed, limit), text: b.timedOut ? 'isteklo vrijeme' : type.summarize({ accuracy: b.accuracy, q }) };
    });
    results.forEach(r => { r.p.answered++; if (r.acc >= 0.75) r.p.correct++; r.p.lastScore = r.tiles * 100 - r.time; });
    results.forEach(r => {
      if (r.p.human && usedShortcut) { r.tiles += 2; r.text += ' · prečica +2'; }
      if (!r.p.human && usedSlow && r.tiles > 0) { r.tiles -= 1; r.text += ' · usporen'; }
    });

    const go = h('button', { class: 'btn btn-primary', type: 'button', id: 'go' }, 'Kreni');
    res.foot.append(
      h('ol', { class: 'sum-list' }, [...results].sort((a, b) => b.tiles - a.tiles || a.time - b.time).map(r =>
        h('li', { class: r.p.human ? 'me' : '' },
          h('i', { class: 'dot', style: { background: r.p.color } }),
          h('span', { class: 'sum-name' }, r.p.name),
          h('span', { class: 'sum-text' }, r.text),
          h('span', { class: 'sum-time' }, fmtSec(r.time)),
          h('b', { class: `sum-tiles t${r.tiles}` }, r.tiles ? `+${r.tiles}` : '0')))),
      h('div', { class: 'row-bar end' }, go));
    res.foot.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    await waitButton(go);
    clearLayer();

    const moves = results.map(r => ({ p: r.p, ...advanceTarget(r.p, r.tiles) }));
    await moveAll(moves);
    moves.forEach(m => { if (m.cp) m.p.lastCp = m.target; });
    renderHud(players, track.last, round);

    const atStation = moves.filter(m => m.cp && m.p.pos < track.last).map(m => m.p);
    if (atStation.length) await handleStations(atStation);
    if (BONUS_ROUNDS.includes(round) && players.every(p => p.pos < track.last)) await bonusRound();

    const finishers = players.filter(p => p.pos >= track.last);
    if (finishers.length) {
      finishers.forEach(p => { if (p.finishedRound == null) p.finishedRound = round; });
      return showResults();
    }
    await sleep(250);
  }
}

// Bonus runda: svi igrači, bez obzira gdje su na stazi, igraju istu mini igru. Pobjednik dobija nagradu.
const BONUS_ROUNDS = [4, 10, 16];
async function bonusRound() {
  const mg = pickMinigame(rng);
  const card = openCard('mg-card-wrap bonus');
  const startBtn = h('button', { class: 'btn btn-primary', type: 'button', id: 'bonus-start' }, 'Počni');
  card.append(
    h('header', { class: 'q-head' }, h('span', { class: 'chip', style: { '--chip': '#9b4bd6' } }, 'Bonus runda · svi igraju'), h('span', { class: 'chip ghost' }, `${mg.duration} s`)),
    h('h2', { class: 'q-text' }, mg.name),
    h('p', { class: 'lede small' }, mg.intro),
    h('p', { class: 'hint' }, 'Sva četiri igrača igraju istu mini igru. Pobjednik ide 3 polja naprijed i dobija 50 novčića, drugi ide 1 polje.'),
    h('div', { class: 'row-bar end' }, startBtn));
  await waitButton(startBtn);
  card.replaceChildren(h('header', { class: 'q-head' }, h('span', { class: 'chip', style: { '--chip': '#9b4bd6' } }, `Bonus runda · ${mg.name}`)));
  const host = h('div', { class: 'mg-host' });
  card.append(host);
  const me = human();
  const mine = await mg.play(host, { rng });
  const results = players.map(p => ({ p, ...(p.human ? mine : mg.simulateBot(botMinigameSkill(p), rng)) }))
    .sort((a, b) => b.score - a.score);
  const prize = [{ jump: 3, coins: 50 }, { jump: 1, coins: 20 }, { jump: 0, coins: 10 }, { jump: 0, coins: 0 }];
  results.forEach((r, i) => { r.jump = prize[i].jump; r.coins = prize[i].coins; r.p.coins += r.coins; });
  const cont = h('button', { class: 'btn btn-primary', type: 'button', id: 'bonus-cont' }, 'Nastavi');
  card.append(
    h('p', { class: 'rank-up', style: { '--rc': results[0].p.color } }, results[0].p.human ? 'Pobijedio si u bonus rundi!' : `${results[0].p.name} je pobijedio u bonus rundi`),
    h('ol', { class: 'sum-list' }, results.map((r, i) => h('li', { class: r.p.human ? 'me' : '' },
      h('i', { class: 'dot', style: { background: r.p.color } }),
      h('span', { class: 'sum-name' }, `${i + 1}. ${r.p.name}`),
      h('span', { class: 'sum-text' }, `${r.label} · ${r.coins} novčića`),
      h('span', { class: 'sum-time' }, ''),
      h('b', { class: `sum-tiles t${r.jump}` }, r.jump ? `+${r.jump}` : '0')))),
    h('div', { class: 'row-bar end' }, cont));
  await waitButton(cont);
  clearLayer();
  await moveAll(results.map(r => ({ p: r.p, target: Math.min(track.last, r.p.pos + r.jump) })));
  renderHud(players, track.last, round);
  void me;
}

async function handleStations(list) {
  const me = list.find(p => p.human);
  const bots = list.filter(p => !p.human);
  const mg = pickMinigame(rng);
  const outcomes = [];

  if (me) {
    const tile = track.tiles[me.pos];
    const card = openCard('mg-card-wrap');
    const startBtn = h('button', { class: 'btn btn-primary', type: 'button', id: 'mg-start' }, 'Počni');
    card.append(
      h('header', { class: 'q-head' }, h('span', { class: 'chip', style: { '--chip': '#d99a12' } }, `Stanica · ${tile.near}`), h('span', { class: 'chip ghost' }, `Mini igra · ${mg.duration} s`)),
      h('h2', { class: 'q-text' }, mg.name),
      h('p', { class: 'lede small' }, mg.intro),
      h('p', { class: 'hint' }, 'Nagrada: novčići i skok do 3 polja naprijed.'),
      h('div', { class: 'row-bar end' }, startBtn));
    await waitButton(startBtn);
    card.replaceChildren(h('header', { class: 'q-head' }, h('span', { class: 'chip', style: { '--chip': '#d99a12' } }, mg.name)));
    const host = h('div', { class: 'mg-host' });
    card.append(host);
    const r = await mg.play(host, { rng });
    outcomes.push({ p: me, ...r, ...rewardFor(r.score) });
  }
  bots.forEach(b => { const r = mg.simulateBot(botMinigameSkill(b), rng); outcomes.push({ p: b, ...r, ...rewardFor(r.score) }); });
  outcomes.forEach(o => { o.p.coins += o.coins; });

  if (me) {
    const card = document.querySelector('.mg-card-wrap');
    const cont = h('button', { class: 'btn btn-primary', type: 'button', id: 'mg-cont' }, 'Nastavi');
    card.append(
      h('ol', { class: 'sum-list' }, outcomes.map(o => h('li', { class: o.p.human ? 'me' : '' },
        h('i', { class: 'dot', style: { background: o.p.color } }),
        h('span', { class: 'sum-name' }, o.p.name),
        h('span', { class: 'sum-text' }, `${o.label} · ${o.coins} novčića`),
        h('span', { class: 'sum-time' }, ''),
        h('b', { class: `sum-tiles t${o.jump}` }, o.jump ? `+${o.jump}` : '0')))),
      h('div', { class: 'row-bar end' }, cont));
    await waitButton(cont);
    clearLayer();
  } else {
    outcomes.forEach(o => toast(`<b>${o.p.name}</b> na stanici: ${o.label} → ${o.jump ? `+${o.jump} polja` : 'bez skoka'}`, o.p.color));
    await sleep(700);
  }
  await moveAll(outcomes.map(o => ({ p: o.p, target: Math.min(track.last, o.p.pos + o.jump) })));
  renderHud(players, track.last, round);
}

function showResults() {
  const ranked = [...players].sort((a, b) =>
    (a.finishedRound ?? 1e9) - (b.finishedRound ?? 1e9) || b.pos - a.pos || (b.lastScore || 0) - (a.lastScore || 0));
  const winner = ranked[0], me = human();
  const place = ranked.indexOf(me);
  const unlockedBefore = MAPS_RACE.filter(mapUnlocked).map(m => m.id);
  const award = awardRace({ place, correct: me.correct, answered: me.answered, coins: me.coins, won: place === 0, mapa: currentMap.id });
  const newMaps = MAPS_RACE.filter(m => mapUnlocked(m) && !unlockedBefore.includes(m.id));
  if (isOnline()) {
    submitResult({ mapa: currentMap.id, mjesto: place + 1, bodovi: award.pts, tacni: me.correct, odgovori: me.answered }).catch(() => {});
    pushProfile(P).catch(() => {});
  }
  world.cam.finale(track.tokenPosition(winner));
  renderHud(players, track.last, round);
  const card = openCard('results');
  const again = h('button', { class: 'btn btn-primary', type: 'button', id: 'again' }, 'Nova trka');
  const menu = h('button', { class: 'btn btn-ghost', type: 'button', id: 'to-menu' }, 'Meni');
  card.append(
    h('p', { class: 'eyebrow' }, `Cilj: ${currentMap.finish} · ${round} rundi`),
    h('h1', {}, winner.human ? 'Pobjeda!' : `${winner.name} je prvi stigao`),
    h('p', { class: 'lede' }, winner.human ? `Stigao si prvi do cilja.` : `Ti si ${place + 1}. na cilju. Probaj ponovo!`),
    h('div', { class: 'reward-row' },
      h('span', { class: 'reward' }, h('b', {}, `+${award.pts}`), h('small', {}, 'bodova za rang')),
      h('span', { class: 'reward' }, h('b', {}, `+${award.coins}`), h('small', {}, 'novčića'))),
    award.rankUp ? h('p', { class: 'rank-up', style: { '--rc': award.rankUp.color } }, `Novi rang: ${award.rankUp.label}!`) : null,
    ...newMaps.map(m => h('p', { class: 'rank-up', style: { '--rc': '#1d8a4e' } }, `Otključana mapa: ${m.name}`)),
    h('ol', { class: 'podium' }, ranked.map((p, i) => h('li', { class: p.human ? 'me' : '' },
      h('span', { class: 'place' }, `${i + 1}.`),
      h('i', { class: 'dot', style: { background: p.color } }),
      h('span', { class: 'sum-name' }, p.name),
      h('span', { class: 'sum-text' }, `${p.correct}/${p.answered} dobrih · ${p.coins} novčića`),
      h('span', { class: 'sum-time' }, p.pos >= track.last ? 'cilj' : `${track.last - p.pos} do cilja`)))),
    profileBar(),
    h('div', { class: 'row-bar end' }, menu, again));
  const S = P.podesavanja;
  again.addEventListener('click', () => startRace(S.miks || !S.oblasti ? Object.keys(availableAreas()) : S.oblasti));
  menu.addEventListener('click', showMenu);
}

// ---------------- pokretanje ----------------
Promise.all([
  fetch('data/questions.json').then(r => { if (!r.ok) throw new Error(r.status); return r.json(); }),
  loadFlags(), preloadBorders(), loadCountryNames()
])
  .then(async ([d]) => {
    QUESTIONS = d.pitanja;
    await initMinigames();
    showMenu();
    // server (ako je podešen): prijava kao gost, odobrena pitanja igrača, slanje sačuvanih predloga
    if (await initNet()) {
      QUESTIONS = QUESTIONS.concat(await approvedQuestions());
      pushProfile(P).catch(() => {});
      const queue = P.predlozi || [];
      P.predlozi = [];
      for (const s of queue) if (!(await submitSuggestion({ oblast: s.oblast, tip: s.tip, pitanje: s.pitanje, opcije: s.opcije, tacno: s.tacno }))) P.predlozi.push(s);
      save();
    }
  })
  .catch(() => {
    const c = openCard();
    c.append(h('h2', { class: 'q-text' }, 'Igra se nije učitala'), h('p', {}, 'Osvježi stranicu da pokušaš ponovo.'));
  });
