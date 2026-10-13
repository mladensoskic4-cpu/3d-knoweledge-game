// Mini igra: Gdje je ovo? Prikazuje fotografiju mjesta ili spomenika; biraš gdje se nalazi.
// Fotografije i podaci su u data/mjesta.json i folderu slike/. Igra se uključuje tek kad ima bar 6 fotografija.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';

const GOAL = 6;

export default {
  id: 'mjesta',
  name: 'Gdje je ovo?',
  intro: 'Pojavljuje se fotografija mjesta ili spomenika. Izaberi gdje se nalazi. Što više tačnih za 20 sekundi, greška oduzima pola boda.',
  duration: 20,
  ready: false,
  entries: [],

  async init() {
    try {
      const r = await fetch('data/mjesta.json');
      if (!r.ok) return;
      const d = await r.json();
      this.entries = (d.mjesta || []).filter(e => e.slika && e.odgovor);
      this.ready = this.entries.length >= 6;
      // unaprijed učitaj prve slike
      if (this.ready) this.entries.slice(0, 4).forEach(e => { const i = new Image(); i.src = e.slika; });
    } catch { this.ready = false; }
  },

  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000;
    const entries = this.entries;
    return new Promise(resolve => {
      const fill = h('i');
      const status = h('span', { class: 'mg-status' }, '');
      const img = h('img', { alt: 'Fotografija mjesta koje treba prepoznati', decoding: 'async' });
      const credit = h('span', { class: 'photo-credit' });
      const frame = h('div', { class: 'photo-frame' }, img, credit);
      const grid = h('div', { class: 'cap-grid' });
      host.append(h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, fill), status), frame, grid);

      let bag = shuffle([...entries], rng), hits = 0, misses = 0, done = false, busy = false;
      function next() {
        if (!bag.length) bag = shuffle([...entries], rng);
        const e = bag.pop();
        if (bag.length) { const pre = new Image(); pre.src = bag[bag.length - 1].slika; }
        const others = [...new Set(entries.filter(x => x.odgovor !== e.odgovor).map(x => x.odgovor))];
        const opts = shuffle([e.odgovor, ...shuffle(others, rng).slice(0, 3)], rng);
        img.src = e.slika;
        credit.textContent = [e.autor, e.licenca].filter(Boolean).join(' · ');
        credit.hidden = !credit.textContent;
        grid.replaceChildren(...opts.map((o, i) => {
          const b = h('button', { type: 'button', class: 'mc-opt', id: `ph-${i}` }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
          b.addEventListener('click', async () => {
            if (done || busy) return;
            busy = true;
            const ok = o === e.odgovor;
            if (ok) { hits++; b.classList.add('is-right'); }
            else {
              misses++; b.classList.add('is-wrong');
              grid.querySelectorAll('.mc-opt').forEach(x => { if (x.textContent.slice(1) === e.odgovor) x.classList.add('is-right'); });
            }
            await sleep(ok ? 300 : 800);
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
          return resolve({ score: clamp((hits - misses * 0.5) / GOAL, 0, 1), label: `${hits} mjesta` });
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  },

  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${Math.round(score * GOAL)} mjesta` };
  }
};
