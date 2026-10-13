// Mini igra: Mapa svijeta. Tapni državu, grad ili rijeku. Prave granice (Natural Earth), Kosovo u sastavu Srbije.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { loadWorld, distToCountry, distToLine, CITIES, RIVERS } from '../world.js';
import { haversine } from '../geo.js';
import { timerBar } from './kit.js';
import { createWorldMap } from './mapwidget.js';
import { correct as sfxOk, wrong as sfxBad } from '../sfx.js';

const game = {
  id: 'mapa-svijeta', name: 'Pronađi na mapi svijeta', duration: 35, ready: false, w: null,
  intro: 'Na mapi svijeta tapni traženu državu, grad ili rijeku. Mapu možeš uvećati (＋, točkić ili dva prsta) i pomjerati prevlačenjem. Što bliže, to više bodova. Imaš 35 sekundi.',
  async init() { this.w = await loadWorld(); this.ready = true; },

  play(host, { rng = Math.random } = {}) {
    const w = this.w, dur = this.duration * 1000;
    const card = host.closest('.card');
    card?.classList.add('wide-map');
    return new Promise(resolve => {
      const { fill, status } = timerBar(host);
      const prompt = h('div', { class: 'fly-prompt' });
      const note = h('p', { class: 'map-hint wm-note' }, 'Tapni na mapu. Prevuci za pomjeranje.');
      const big = w.drzave.filter(d => d.area > 60000);
      const tasks = shuffle([
        ...shuffle(big, rng).slice(0, 14).map(d => ({ kind: 'država', label: d.name, d })),
        ...shuffle([...CITIES], rng).slice(0, 8).map(c => ({ kind: 'grad', label: c.name, c })),
        ...shuffle([...RIVERS], rng).slice(0, 5).map(r => ({ kind: 'rijeka', label: r.name, r }))
      ], rng);
      let ti = 0, score = 0, done = false, busy = false;

      const map = createWorldMap(w, { onTap: answer });
      host.append(prompt, map.el, note);

      function show() {
        const t = tasks[ti % tasks.length];
        prompt.replaceChildren(h('span', {}, `Tapni ${t.kind === 'država' ? 'državu' : t.kind === 'grad' ? 'grad' : 'rijeku'}:`), h('b', {}, t.label));
        map.setOverlay(null);
      }
      async function answer(lon, lat) {
        if (done || busy) return;
        const t = tasks[ti % tasks.length];
        let acc, km;
        if (t.d) { km = distToCountry(lon, lat, t.d); acc = km === 0 ? 1 : clamp(1 - km / 900, 0, 0.8); }
        else if (t.c) { km = haversine(lon, lat, t.c.lon, t.c.lat); acc = km < 150 ? 1 : clamp(1 - (km - 150) / 1500, 0, 1); }
        else { km = distToLine(lon, lat, t.r.pts); acc = km < 120 ? 1 : clamp(1 - (km - 120) / 1200, 0, 1); }
        score += acc;
        (acc >= 0.99 ? sfxOk : sfxBad)();
        busy = true;
        const ok = acc >= 0.99;
        map.setOverlay(({ ctx, X, Y, k, path }) => {
          if (t.d) { ctx.fillStyle = ok ? 'rgba(29,138,78,.75)' : 'rgba(29,138,78,.55)'; t.d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); }); }
          if (t.r) { ctx.strokeStyle = '#1f5fd1'; ctx.lineWidth = 3 / k; ctx.beginPath(); t.r.pts.forEach(([x, y], i) => (i ? ctx.lineTo(X(x), Y(y)) : ctx.moveTo(X(x), Y(y)))); ctx.stroke(); }
          if (t.c) { ctx.fillStyle = '#1d8a4e'; ctx.beginPath(); ctx.arc(X(t.c.lon), Y(t.c.lat), 6 / k, 0, 7); ctx.fill(); }
          ctx.fillStyle = '#c8102e'; ctx.strokeStyle = '#fff'; ctx.lineWidth = 2 / k;
          ctx.beginPath(); ctx.arc(X(lon), Y(lat), 5 / k, 0, 7); ctx.fill(); ctx.stroke();
        });
        note.textContent = ok ? 'Tačno!' : `Promašaj: ${Math.round(km)} km`;
        await sleep(ok ? 600 : 1300);
        busy = false; ti++; note.textContent = 'Tapni na mapu. Prevuci za pomjeranje.';
        if (!done) show();
      }

      map.start(); show();
      const t0 = performance.now();
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${score.toFixed(1)} bodova · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true; map.destroy();
          card?.classList.remove('wide-map');
          return resolve({ score: clamp(score / 7, 0, 1), label: `${score.toFixed(1)} bodova` });
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  },
  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${(score * 7).toFixed(1)} bodova` };
  }
};
export default game;
