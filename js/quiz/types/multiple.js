// Tip pitanja: višestruki izbor (4 ponuđena odgovora, tasteri 1–4).
import { h } from '../../ui.js';
import { shuffle } from '../../util.js';

export default {
  id: 'izbor',
  label: 'Višestruki izbor',
  timeLimit: 15,

  mount(q, body, api) {
    const opts = shuffle([...q.opcije], api.rng);
    let locked = false;
    const grid = h('div', { class: 'mc-grid' });
    const btns = opts.map((o, i) => {
      const b = h('button', { class: 'mc-opt', type: 'button', id: `mc-${i}` },
        h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
      b.addEventListener('click', () => { if (!locked) api.submit(o === q.tacno ? 1 : 0, { chosen: o }); });
      grid.append(b);
      return b;
    });
    body.append(grid);
    const onKey = e => { const n = Number(e.key); if (n >= 1 && n <= btns.length) btns[n - 1].click(); };
    window.addEventListener('keydown', onKey);

    return {
      lock() { locked = true; btns.forEach(b => (b.disabled = true)); window.removeEventListener('keydown', onKey); },
      fifty() {
        const wrong = btns.filter((b, i) => opts[i] !== q.tacno && !b.disabled);
        wrong.sort(() => Math.random() - 0.5).slice(0, 2).forEach(b => { b.disabled = true; b.classList.add('gone'); });
      },
      timeout: () => ({ accuracy: 0, detail: { chosen: null } }),
      reveal(res) {
        btns.forEach((b, i) => {
          if (opts[i] === q.tacno) b.classList.add('is-right');
          else if (opts[i] === res.detail.chosen) b.classList.add('is-wrong');
        });
      }
    };
  },

  simulateBot: (q, p, rng) => (rng() < p ? 1 : 0),
  summarize: r => (r.accuracy >= 1 ? 'tačno' : 'netačno')
};
