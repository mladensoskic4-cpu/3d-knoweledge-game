// Mini igra: Zastave u letu. Čim se pojavi naziv države, njena zastava kreće preko ekrana zajedno sa drugim zastavama.
// Ako je ne tapneš dok ne izađe, ne vraća se: računa se kao promašaj i dolazi sljedeća država. 20 sekundi.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle } from '../util.js';
import { flagHtml, FLAG_COUNTRIES } from '../flags.js';

const GOAL = 7; // pogodaka za maksimalan rezultat

export default {
  id: 'zastave',
  name: 'Zastave u letu',
  intro: 'Čim se pojavi naziv države, njena zastava poleti preko ekrana. Tapni je prije nego što izađe, jer se ne vraća. Pogrešna zastava oduzima pola boda.',
  duration: 20,

  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000;
    return new Promise(resolve => {
      const fill = h('i');
      const status = h('span', { class: 'mg-status' }, '');
      const prompt = h('div', { class: 'fly-prompt' });
      const field = h('div', { class: 'fly-field' });
      host.append(h('div', { class: 'timer' }, h('div', { class: 'timer-track' }, fill), status), prompt, field);

      const lanes = 4;
      let flyers = [], hits = 0, misses = 0, target = null, targetFlyer = null, done = false, lastDecoy = 0;
      let bag = shuffle([...FLAG_COUNTRIES], rng);

      function spawn(country, lane, speedK = 1) {
        const W = field.clientWidth, H = field.clientHeight;
        const dir = rng() < 0.5 ? 1 : -1;
        const el = h('button', { type: 'button', class: 'fly-flag', 'aria-label': country.name, html: flagHtml(country.code) });
        field.append(el);
        const fw = el.offsetWidth || 68;
        const speed = ((W + fw) / (3600 + rng() * 1400)) * speedK; // px/ms
        const f = { el, country, x: dir > 0 ? -fw : W, y: (lane + 0.5) * (H / lanes) - 25, dir, speed, fw };
        el.addEventListener('pointerdown', e => { e.preventDefault(); tap(f); });
        flyers.push(f);
        return f;
      }
      function newTarget() {
        if (!bag.length) bag = shuffle([...FLAG_COUNTRIES], rng);
        target = bag.pop();
        prompt.replaceChildren(h('span', {}, 'Tapni:'), h('b', {}, target.name));
        // zastava cilja kreće odmah, u nasumičnoj traci
        const lane = Math.floor(rng() * lanes);
        targetFlyer = spawn(target, lane, 1);
        // i dvije druge zastave u isto vrijeme, u drugim trakama
        const others = shuffle([...Array(lanes).keys()].filter(l => l !== lane), rng).slice(0, 2);
        others.forEach(l => spawn(FLAG_COUNTRIES[Math.floor(rng() * FLAG_COUNTRIES.length)], l, 0.9 + rng() * 0.3));
      }
      function tap(f) {
        if (done || f.gone) return;
        if (f.country === target && f === targetFlyer) {
          hits++; f.gone = true; f.el.classList.add('hit');
          setTimeout(() => f.el.remove(), 250);
          newTarget();
        } else {
          misses++; f.el.classList.add('miss');
          setTimeout(() => f.el.classList.remove('miss'), 300);
        }
      }

      newTarget();
      const t0 = performance.now();
      let raf, prev = t0;
      const tick = now => {
        const dt = Math.min(50, now - prev); prev = now;
        const el = now - t0, left = Math.max(0, dur - el);
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${hits} pogodaka · ${Math.ceil(left / 1000)} s`;
        if (el - lastDecoy > 900) {
          lastDecoy = el;
          spawn(FLAG_COUNTRIES[Math.floor(rng() * FLAG_COUNTRIES.length)], Math.floor(rng() * lanes), 0.9 + rng() * 0.3);
        }
        const W = field.clientWidth;
        let targetLeft = false;
        flyers = flyers.filter(f => {
          if (f.gone) return false;
          f.x += f.dir * f.speed * dt;
          f.el.style.transform = `translate(${f.x}px, ${f.y}px)`;
          if ((f.dir > 0 && f.x > W) || (f.dir < 0 && f.x < -f.fw)) {
            f.el.remove();
            if (target && f === targetFlyer) targetLeft = true;
            return false;
          }
          return true;
        });
        if (targetLeft) { misses++; prompt.classList.add('missed'); setTimeout(() => prompt.classList.remove('missed'), 400); newTarget(); }
        if (left <= 0) return end();
        raf = requestAnimationFrame(tick);
      };
      raf = requestAnimationFrame(tick);

      function end() {
        done = true;
        cancelAnimationFrame(raf);
        flyers.forEach(f => f.el.remove());
        prompt.replaceChildren(h('b', {}, `${hits} pogodaka, ${misses} promašaja`));
        resolve({ score: clamp((hits - misses * 0.5) / GOAL, 0, 1), label: `${hits} zastava` });
      }
    });
  },

  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${Math.round(score * GOAL)} zastava` };
  }
};
