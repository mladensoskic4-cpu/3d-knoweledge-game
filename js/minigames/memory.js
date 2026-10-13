// Mini igra: Memorija sa zastavama. Okreni kartice i pronađi parove (zastava + naziv države).
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { FLAG_COUNTRIES, flagHtml } from '../flags.js';
import { timerBar } from './kit.js';

const game = {
  id: 'memorija', name: 'Memorija: zastava i država', duration: 25,
  intro: 'Okreni kartice i spoji svaku zastavu sa nazivom njene države. Ima 6 parova i 25 sekundi. Na početku sve kartice vidiš na trenutak.',
  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000;
    return new Promise(async resolve => {
      const { fill, status } = timerBar(host, 'Zapamti!');
      const picks = shuffle([...FLAG_COUNTRIES], rng).slice(0, 6);
      const cards = shuffle(picks.flatMap(c => [{ c, kind: 'zastava' }, { c, kind: 'ime' }]), rng);
      const grid = h('div', { class: 'mg-grid mem4' });
      host.append(grid);
      let open = [], found = 0, busy = true, done = false;
      const els = cards.map((card, i) => {
        const front = card.kind === 'zastava'
          ? h('span', { class: 'mg-face mg-front flagf', html: flagHtml(card.c.code) })
          : h('span', { class: 'mg-face mg-front namef' }, card.c.name);
        const b = h('button', { type: 'button', class: 'mg-card flipped', id: `mem-${i}`, 'aria-label': 'Skrivena kartica' }, h('span', { class: 'mg-face mg-back' }), front);
        b.dataset.k = card.c.code;
        b.addEventListener('click', () => flip(b));
        grid.append(b);
        return b;
      });
      await sleep(1800);
      els.forEach(b => b.classList.remove('flipped'));
      busy = false;
      const t0 = performance.now();
      let raf;
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${found}/6 · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) return end(0);
        raf = requestAnimationFrame(tick);
      };
      raf = requestAnimationFrame(tick);
      async function flip(b) {
        if (busy || done || b.classList.contains('flipped')) return;
        b.classList.add('flipped'); open.push(b);
        if (open.length < 2) return;
        const [a, c] = open; open = [];
        if (a.dataset.k === c.dataset.k) {
          a.classList.add('matched'); c.classList.add('matched'); found++;
          if (found === 6) end(Math.max(0, dur - (performance.now() - t0)) / dur);
        } else {
          busy = true; await sleep(650);
          a.classList.remove('flipped'); c.classList.remove('flipped'); busy = false;
        }
      }
      function end(timeFrac) {
        if (done) return;
        done = true; cancelAnimationFrame(raf);
        els.forEach(b => (b.disabled = true));
        status.textContent = `${found}/6 parova`;
        resolve({ score: found === 6 ? 0.85 + 0.15 * timeFrac : (found / 6) * 0.85, label: `${found}/6 parova` });
      }
    });
  },
  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${Math.min(6, Math.round((score / 0.85) * 6))}/6 parova` };
  }
};
export default game;
