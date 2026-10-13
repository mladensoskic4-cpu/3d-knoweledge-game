// Zajednički dio za mini igre sa tajmerom: traka vremena, brojač, 4 ponuđena odgovora, tasteri 1–4.
import { h } from '../ui.js';
import { clamp, gaussian, sleep } from '../util.js';
import { correct as sfxOk, wrong as sfxBad } from '../sfx.js';

export function timerBar(host, label = '') {
  const fill = h('i');
  const status = h('span', { class: 'mg-status' }, label);
  host.append(h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, fill), status));
  return { fill, status };
}

// next() vraća { stage: Node, options: [string], answer: string, onShow?, onDone? }
export function playChoiceRounds(host, { duration = 20, goal = 8, next, unit = 'tačnih' }) {
  const dur = duration * 1000;
  return new Promise(resolve => {
    const { fill, status } = timerBar(host);
    const stageWrap = h('div', { class: 'kit-stage' });
    const grid = h('div', { class: 'cap-grid' });
    host.append(stageWrap, grid);
    let hits = 0, misses = 0, done = false, busy = false, cur = null;
    async function round() {
      cur?.onDone?.();
      cur = await next();
      if (done) return;
      stageWrap.replaceChildren(cur.stage);
      cur.onShow?.();
      grid.replaceChildren(...cur.options.map((o, i) => {
        const b = h('button', { type: 'button', class: 'mc-opt', id: `kit-${i}`, 'data-ok': String(o === cur.answer) }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
        b.addEventListener('click', async () => {
          if (done || busy) return;
          busy = true;
          const ok = o === cur.answer;
          (ok ? sfxOk : sfxBad)();
          if (ok) { hits++; b.classList.add('is-right'); }
          else { misses++; b.classList.add('is-wrong'); grid.querySelectorAll('.mc-opt[data-ok="true"]').forEach(x => x.classList.add('is-right')); }
          cur.reveal?.(ok);
          await sleep(ok ? 350 : 900);
          busy = false;
          if (!done) round();
        });
        return b;
      }));
    }
    const onKey = e => { const n = Number(e.key); if (n >= 1 && n <= 4) grid.children[n - 1]?.click(); };
    window.addEventListener('keydown', onKey);
    round();
    const t0 = performance.now();
    const tick = now => {
      const left = Math.max(0, dur - (now - t0));
      fill.style.transform = `scaleX(${left / dur})`;
      status.textContent = `${hits} ${unit} · ${Math.ceil(left / 1000)} s`;
      if (left <= 0) {
        done = true;
        cur?.onDone?.();
        window.removeEventListener('keydown', onKey);
        grid.querySelectorAll('button').forEach(b => (b.disabled = true));
        return resolve({ score: clamp((hits - misses * 0.5) / goal, 0, 1), label: `${hits} ${unit}` });
      }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
}

export function simulate(goal, unit) {
  return (skill, rng = Math.random) => {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${Math.round(score * goal)} ${unit}` };
  };
}

export function pickOptions(rng, correct, pool, n = 4) {
  const others = pool.filter(x => x !== correct);
  const out = new Set([correct]);
  while (out.size < Math.min(n, pool.length)) out.add(others[Math.floor(rng() * others.length)]);
  return [...out].sort(() => rng() - 0.5);
}
