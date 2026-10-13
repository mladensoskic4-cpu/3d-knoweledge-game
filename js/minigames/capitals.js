// Mini igra: Glavni gradovi. Za svaku državu izaberi glavni grad; što više tačnih za 20 sekundi.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { CAPITAL_COUNTRIES as COUNTRIES, flagHtml } from '../flags.js';

const GOAL = 9;

export default {
  id: 'gradovi',
  name: 'Glavni gradovi',
  intro: 'Pojavljuje se država, a ti biraš njen glavni grad. Odgovori na što više za 20 sekundi. Greška oduzima pola boda.',
  duration: 20,

  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000;
    return new Promise(resolve => {
      const fill = h('i');
      const status = h('span', { class: 'mg-status' }, '');
      const country = h('div', { class: 'cap-country' });
      const grid = h('div', { class: 'cap-grid' });
      host.append(h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, fill), status), country, grid);

      let bag = shuffle([...COUNTRIES], rng), hits = 0, misses = 0, done = false, busy = false;
      function next() {
        if (!bag.length) bag = shuffle([...COUNTRIES], rng);
        const c = bag.pop();
        // pogrešni odgovori: prvo veći gradovi iz iste zemlje, pa glavni gradovi sa istog kontinenta
        const own = shuffle([...(c.cities || [])], rng).slice(0, 2);
        const same = shuffle(COUNTRIES.filter(x => x !== c && x.continent === c.continent), rng).map(x => x.capital);
        const rest = shuffle(COUNTRIES.filter(x => x !== c && x.continent !== c.continent), rng).map(x => x.capital);
        const wrong = [...new Set([...own, ...same, ...rest])].filter(x => x !== c.capital).slice(0, 3);
        const opts = shuffle([c.capital, ...wrong], rng);
        country.replaceChildren(...[
          c.code ? h('span', { class: 'cap-flag', html: flagHtml(c.code) }) : null,
          h('b', {}, c.name)].filter(Boolean));
        grid.replaceChildren(...opts.map((o, i) => {
          const b = h('button', { type: 'button', class: 'mc-opt', id: `cap-${i}`, 'data-ok': String(o === c.capital) }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
          b.addEventListener('click', async () => {
            if (done || busy) return;
            busy = true;
            if (o === c.capital) { hits++; b.classList.add('is-right'); }
            else {
              misses++; b.classList.add('is-wrong');
              grid.querySelectorAll('.mc-opt[data-ok="true"]').forEach(x => x.classList.add('is-right'));
            }
            await sleep(o === c.capital ? 220 : 650);
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
      let raf;
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${hits} tačnih · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true;
          window.removeEventListener('keydown', onKey);
          grid.querySelectorAll('button').forEach(b => (b.disabled = true));
          const score = clamp((hits - misses * 0.5) / GOAL, 0, 1);
          return resolve({ score, label: `${hits} gradova` });
        }
        raf = requestAnimationFrame(tick);
      };
      raf = requestAnimationFrame(tick);
    });
  },

  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${Math.round(score * GOAL)} gradova` };
  }
};
