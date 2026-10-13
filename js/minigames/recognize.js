// Mini igre „Prepoznaj”: pokaže se ilustracija (instrument ili životinja), biraš naziv. Što više tačnih za 20 sekundi.
// Ilustracije: Microsoft Fluent Emoji (MIT licenca), spakovane u data/ilustracije.json.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';

let DATA = null;
async function loadData() {
  if (DATA) return DATA;
  const r = await fetch('data/ilustracije.json');
  DATA = r.ok ? await r.json() : { instrumenti: {}, zivotinje: {} };
  [...Object.values(DATA.instrumenti || {}), ...Object.values(DATA.zivotinje || {})].forEach(src => { const i = new Image(); i.src = src; });
  return DATA;
}

function makeGame({ id, name, intro, group, goal, exclude = [] }) {
  return {
    id, name, intro, duration: 20, ready: false, items: [],
    async init() {
      const d = await loadData();
      this.items = Object.entries(d[group] || {}).filter(([n]) => !exclude.includes(n)).map(([label, src]) => ({ label, src }));
      this.ready = this.items.length >= 6;
    },
    play(host, { rng = Math.random } = {}) {
      const dur = this.duration * 1000, items = this.items;
      return new Promise(resolve => {
        const fill = h('i');
        const status = h('span', { class: 'mg-status' }, '');
        const img = h('img', { class: 'rec-img', alt: '', draggable: 'false' });
        const frame = h('div', { class: 'rec-frame' }, img);
        const grid = h('div', { class: 'cap-grid' });
        host.append(h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, fill), status), frame, grid);
        let bag = shuffle([...items], rng), hits = 0, misses = 0, done = false, busy = false;
        function next() {
          if (!bag.length) bag = shuffle([...items], rng);
          const it = bag.pop();
          const opts = shuffle([it, ...shuffle(items.filter(x => x !== it), rng).slice(0, 3)], rng);
          img.src = it.src;
          img.alt = 'Šta je na slici?';
          frame.classList.remove('pop'); void frame.offsetWidth; frame.classList.add('pop');
          grid.replaceChildren(...opts.map((o, i) => {
            const b = h('button', { type: 'button', class: 'mc-opt', id: `rec-${i}`, 'data-ok': String(o === it) }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o.label));
            b.addEventListener('click', async () => {
              if (done || busy) return;
              busy = true;
              if (o === it) { hits++; b.classList.add('is-right'); }
              else { misses++; b.classList.add('is-wrong'); grid.querySelectorAll('.mc-opt[data-ok="true"]').forEach(x => x.classList.add('is-right')); }
              await sleep(o === it ? 220 : 650);
              busy = false;
              if (!done) next();
            });
            return b;
          }));
        }
        const onKey = e => { const n = Number(e.key); if (n >= 1 && n <= 4) grid.children[n - 1]?.click(); };
        window.addEventListener('keydown', onKey);
        next();
        const t0 = performance.now();
        const tick = now => {
          const left = Math.max(0, dur - (now - t0));
          fill.style.transform = `scaleX(${left / dur})`;
          status.textContent = `${hits} tačnih · ${Math.ceil(left / 1000)} s`;
          if (left <= 0) {
            done = true;
            window.removeEventListener('keydown', onKey);
            grid.querySelectorAll('button').forEach(b => (b.disabled = true));
            return resolve({ score: clamp((hits - misses * 0.5) / goal, 0, 1), label: `${hits} tačnih` });
          }
          requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
      });
    },
    simulateBot(skill, rng = Math.random) {
      const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
      return { score, label: `${Math.round(score * goal)} tačnih` };
    }
  };
}

export const instrumenti = makeGame({
  id: 'instrumenti', name: 'Koji je ovo instrument?', group: 'instrumenti', goal: 9, exclude: ['Mikrofon'],
  intro: 'Pojavljuje se muzički instrument. Izaberi kako se zove. Što više tačnih za 20 sekundi, greška oduzima pola boda.'
});
export const zivotinje = makeGame({
  id: 'zivotinje', name: 'Koja je ovo životinja?', group: 'zivotinje', goal: 10,
  intro: 'Pojavljuje se životinja. Izaberi kako se zove. Što više tačnih za 20 sekundi, greška oduzima pola boda.'
});
