// Kviz-engine: registar tipova pitanja, špil (≤40% višestrukog izbora), tajmer i bodovanje u polja.
//
// Ugovor za tip pitanja (svaki tip je zaseban modul u ./types):
//   id, label, timeLimit
//   mount(q, body, { submit(accuracy, detail), rng }) -> { lock(), timeout() -> {accuracy, detail}, reveal(result) }
//   simulateBot(q, p, rng) -> accuracy 0..1      (p = vjerovatnoća/umijeće bota)
//   summarize(result) -> kratak opis rezultata za tabelu runde
import izbor from './types/multiple.js';
import mapa from './types/mapTap.js';
import procjena from './types/estimate.js';
import poredaj from './types/order.js';
import spoji from './types/match.js';
import mutna from './types/blur.js';
import melodija from './types/melody.js';
import { h, AREAS } from '../ui.js';
import { shuffle, clamp } from '../util.js';
import { getLang } from '../i18n.js';
import { correct as sfxOk, wrong as sfxBad } from '../sfx.js';

const REGISTRY = new Map();
export function registerType(mod) { REGISTRY.set(mod.id, mod); }
export const getType = id => REGISTRY.get(id);
[izbor, mapa, procjena, poredaj, spoji, mutna, melodija].forEach(registerType);

export const MC_ID = 'izbor';
export const MC_SHARE_MAX = 0.4;
export const MAX_TILES = 4;

export function timeLimitOf(q) { return q.vrijeme || getType(q.tip).timeLimit; }

// Brz i tačan odgovor = do 4 polja, spor tačan = 2, netačan = 0.
export function tilesFor(accuracy, timeUsed, limit) {
  if (accuracy < 0.2) return 0;
  const speed = 1 - clamp(timeUsed / limit, 0, 1);
  const s = accuracy * (0.55 + 0.45 * speed);
  return clamp(Math.round(s * MAX_TILES), 1, MAX_TILES);
}

// Špil bira tip sa najvećim "dugom" prema ciljnom udjelu; višestruki izbor nikad ne prelazi 40%.
export function createDeck(questions, areas, rng = Math.random, { maxTezina = 3 } = {}) {
  let pool = questions.filter(q => areas.includes(q.oblast) && REGISTRY.has(q.tip) && (q.tezina || 1) <= maxTezina);
  if (pool.length < 20) pool = questions.filter(q => areas.includes(q.oblast) && REGISTRY.has(q.tip));
  const originals = {};
  pool.forEach(q => (originals[q.tip] ||= []).push(q));
  const left = {};
  const refill = t => { left[t] = shuffle([...originals[t]], rng); };
  Object.keys(originals).forEach(refill);
  const types = Object.keys(originals);
  const others = types.filter(t => t !== MC_ID);
  const w = {};
  if (types.includes(MC_ID)) w[MC_ID] = others.length ? MC_SHARE_MAX : 1;
  // tipovi sa malo pitanja dobijaju manju težinu, da se ne ponavljaju
  const raw = Object.fromEntries(others.map(t => [t, Math.min(1, originals[t].length / 15)]));
  const rawSum = others.reduce((s, t) => s + raw[t], 0) || 1;
  others.forEach(t => { w[t] = ((1 - (w[MC_ID] || 0)) * raw[t]) / rawSum; });
  const used = {}, usedArea = {};
  let n = 0;

  return {
    size: pool.length,
    next() {
      let best = null, bestScore = -Infinity;
      for (const t of types) {
        if (t === MC_ID && others.length && (used[t] || 0) + 1 > MC_SHARE_MAX * (n + 1) + 1e-9) continue;
        const score = w[t] * (n + 1) - (used[t] || 0) + rng() * 0.01;
        if (score > bestScore) { bestScore = score; best = t; }
      }
      if (!left[best].length) refill(best);
      // unutar tipa: oblast koja je najmanje korištena
      const list = left[best];
      let idx = 0, min = Infinity;
      list.forEach((q, i) => { const u = usedArea[q.oblast] || 0; if (u < min) { min = u; idx = i; } });
      const q = list.splice(idx, 1)[0];
      used[best] = (used[best] || 0) + 1;
      usedArea[q.oblast] = (usedArea[q.oblast] || 0) + 1;
      n++;
      return q;
    }
  };
}

function verdictText(r) {
  if (r.timedOut && r.accuracy < 0.2) return 'Isteklo vrijeme';
  if (r.accuracy >= 0.999) return 'Tačno!';
  if (r.accuracy >= 0.75) return 'Odlično, blizu!';
  if (r.accuracy >= 0.2) return 'Djelimično';
  return 'Netačno';
}

// powerups: [{ id, label, count, kind: 'fifty' | 'freeze' | 'shortcut' | 'slow' }], onUse(id) javlja potrošnju.
export function runQuestion(q, card, { round, rng = Math.random, powerups = [], onUse = () => {} }) {
  if (getLang() === 'en' && q.en) q = { ...q, ...q.en };
  return new Promise(resolve => {
    const type = getType(q.tip);
    const limit = timeLimitOf(q);
    const area = AREAS[q.oblast] || { label: q.oblast, color: '#555' };
    const timerFill = h('i');
    const timerNum = h('span', { class: 'timer-num' }, String(limit));
    const body = h('div', { class: 'q-body' });
    const foot = h('div', { class: 'q-foot' });
    card.append(
      h('header', { class: 'q-head' },
        h('span', { class: 'chip', style: { '--chip': area.color } }, area.label),
        h('span', { class: 'chip ghost' }, type.label),
        h('span', { class: 'diff', title: `Težina ${q.tezina}/3`, 'aria-label': `Težina ${q.tezina} od 3` }, '●'.repeat(q.tezina) + '○'.repeat(3 - q.tezina)),
        h('span', { class: 'q-round' }, `Runda ${round}`)
      ),
      h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, timerFill), timerNum),
      h('h2', { class: 'q-text' }, q.pitanje),
      body, foot
    );

    let finished = false, raf = 0, frozenMs = 0, freezeUntil = 0;
    const t0 = performance.now();
    const used = new Set();
    const elapsed = now => (now - t0 - frozenMs - (freezeUntil > now ? now - (freezeUntil - 5000) : 0)) / 1000;
    const handle = type.mount(q, body, { rng, submit: (acc, detail) => finish(acc, detail, false) });

    // traka sa power-upovima
    if (powerups.length) {
      const bar = h('div', { class: 'pu-bar', 'aria-label': 'Power-upovi' });
      powerups.forEach(pu => {
        const ok = pu.kind !== 'fifty' || typeof handle.fifty === 'function';
        const b = h('button', { type: 'button', class: `pu pu-${pu.kind}`, id: `pu-${pu.id}`, disabled: !ok || pu.count < 1, title: pu.label },
          h('span', { class: 'pu-ic', 'aria-hidden': 'true' }), h('span', { class: 'pu-lb' }, pu.label), h('b', {}, `×${pu.count}`));
        b.addEventListener('click', () => {
          if (finished || used.has(pu.id) || pu.count < 1) return;
          if (pu.kind === 'fifty') handle.fifty();
          if (pu.kind === 'freeze') { freezeUntil = performance.now() + 5000; card.classList.add('frozen'); setTimeout(() => card.classList.remove('frozen'), 5000); }
          used.add(pu.id); pu.count--; onUse(pu.id);
          b.disabled = true; b.classList.add('used'); b.querySelector('b').textContent = `×${pu.count}`;
        });
        bar.append(b);
      });
      card.insertBefore(bar, body);
    }

    function finish(acc, detail, timedOut) {
      if (finished) return;
      finished = true;
      cancelAnimationFrame(raf);
      handle.lock?.();
      card.classList.add('answered');
      const timeUsed = timedOut ? limit : Math.min(limit, Math.max(0, elapsed(performance.now())));
      const res = { q, type, accuracy: clamp(acc, 0, 1), detail: detail || {}, timeUsed, limit, timedOut, used: [...used] };
      res.tiles = tilesFor(res.accuracy, timeUsed, limit);
      handle.reveal?.(res);
      const good = res.accuracy >= 0.75 ? 'good' : res.accuracy >= 0.2 ? 'mid' : 'bad';
      (good === 'bad' ? sfxBad : sfxOk)();
      foot.append(h('div', { class: `verdict ${good}` },
        h('strong', {}, verdictText(res)),
        h('span', {}, res.tiles ? `+${res.tiles} ${res.tiles === 1 ? 'polje' : 'polja'}` : 'bez pomaka')
      ));
      if (q.info) foot.append(h('p', { class: 'info' }, q.info));
      resolve({ ...res, card, foot });
    }

    function tick(now) {
      if (freezeUntil && now >= freezeUntil) { frozenMs += 5000; freezeUntil = 0; }
      const el = elapsed(now);
      const left = Math.max(0, limit - el);
      timerFill.style.transform = `scaleX(${left / limit})`;
      timerNum.textContent = String(Math.ceil(left));
      card.classList.toggle('hurry', left < 5);
      if (left <= 0) {
        const t = handle.timeout?.() || { accuracy: 0, detail: {} };
        finish(t.accuracy, t.detail, true);
        return;
      }
      raf = requestAnimationFrame(tick);
    }
    raf = requestAnimationFrame(tick);
  });
}
