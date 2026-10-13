// Tip pitanja: mutna slika. Zastava se postepeno izoštrava; raniji tačan odgovor donosi više poena.
import { h } from '../../ui.js';
import { shuffle } from '../../util.js';
import { flagHtml } from '../../flags.js';

export default {
  id: 'mutna',
  label: 'Mutna slika',
  timeLimit: 15,

  mount(q, body, api) {
    const limit = q.vrijeme || this.timeLimit;
    const frame = h('div', { class: 'blur-frame', html: flagHtml(q.zastava, 'blur-img') });
    const img = frame.firstElementChild;
    const opts = shuffle([...q.opcije], api.rng);
    let locked = false, raf = 0;
    const grid = h('div', { class: 'mc-grid' });
    const btns = opts.map((o, i) => {
      const b = h('button', { class: 'mc-opt', type: 'button', id: `bl-${i}` }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
      b.addEventListener('click', () => { if (!locked) api.submit(o === q.tacno ? 1 : 0, { chosen: o }); });
      grid.append(b);
      return b;
    });
    body.append(frame, grid);
    const onKey = e => { const n = Number(e.key); if (n >= 1 && n <= btns.length) btns[n - 1].click(); };
    window.addEventListener('keydown', onKey);

    const t0 = performance.now();
    const tick = now => {
      const k = Math.min(1, (now - t0) / 1000 / (limit * 0.85));
      img.style.filter = `blur(${(1 - k) * 16}px) saturate(${0.3 + 0.7 * k})`;
      img.style.transform = `scale(${1.25 - 0.25 * k})`;
      if (k < 1 && !locked) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);

    return {
      lock() { locked = true; cancelAnimationFrame(raf); btns.forEach(b => (b.disabled = true)); window.removeEventListener('keydown', onKey); },
      fifty() {
        const wrong = btns.filter((b, i) => opts[i] !== q.tacno && !b.disabled);
        wrong.sort(() => Math.random() - 0.5).slice(0, 2).forEach(b => { b.disabled = true; b.classList.add('gone'); });
      },
      timeout: () => ({ accuracy: 0, detail: { chosen: null } }),
      reveal(res) {
        img.style.filter = 'none'; img.style.transform = 'none';
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
