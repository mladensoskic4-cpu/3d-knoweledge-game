// Mini igra: Mapa svijeta. Tapni državu, grad ili rijeku. Prave granice (Natural Earth), Kosovo u sastavu Srbije.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { loadWorld, inCountry, distToCountry, distToLine, CITIES, RIVERS } from '../world.js';
import { haversine } from '../geo.js';
import { timerBar } from './kit.js';
import { L } from '../i18n.js';

const B = { minLon: -170, maxLon: 190, minLat: -58, maxLat: 80 };

const game = {
  id: 'mapa-svijeta', name: 'Pronađi na mapi svijeta', duration: 25, ready: false, w: null,
  intro: 'Na mapi svijeta tapni traženu državu, grad ili rijeku. Što bliže, to više bodova. Imaš 25 sekundi.',
  async init() { this.w = await loadWorld(); this.ready = true; },

  play(host, { rng = Math.random } = {}) {
    const w = this.w, dur = this.duration * 1000;
    return new Promise(resolve => {
      const { fill, status } = timerBar(host);
      const prompt = h('div', { class: 'fly-prompt' });
      const cv = h('canvas', { class: 'wm-cv', 'aria-label': 'Mapa svijeta' });
      const wrap = h('div', { class: 'map-wrap wm-wrap' }, cv);
      const note = h('p', { class: 'map-hint wm-note' }, 'Tapni na mapu.');
      host.append(prompt, wrap, note);

      const big = w.drzave.filter(d => d.area > 60000);
      const tasks = shuffle([
        ...shuffle(big, rng).slice(0, 14).map(d => ({ kind: 'država', label: d.name, d })),
        ...shuffle([...CITIES], rng).slice(0, 8).map(c => ({ kind: 'grad', label: c.name, c })),
        ...shuffle([...RIVERS], rng).slice(0, 5).map(r => ({ kind: 'rijeka', label: r.name, r }))
      ], rng);
      let ti = 0, score = 0, count = 0, done = false, busy = false, mark = null, W = 0, H = 0;
      const ctx = cv.getContext('2d');
      const X = lon => ((lon < B.minLon ? lon + 360 : lon) - B.minLon) / (B.maxLon - B.minLon) * W;
      const Y = lat => (B.maxLat - lat) / (B.maxLat - B.minLat) * H;
      const toLL = (x, y) => { let lon = B.minLon + x / W * (B.maxLon - B.minLon); if (lon > 180) lon -= 360; return [lon, B.maxLat - y / H * (B.maxLat - B.minLat)]; };

      function path(ring) { let prev = null; ring.forEach(([x, y], i) => { const px = X(x), py = Y(y); (i && Math.abs(px - prev) < W / 3) ? ctx.lineTo(px, py) : ctx.moveTo(px, py); prev = px; }); ctx.closePath(); }
      function draw() {
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        W = wrap.clientWidth; H = Math.round(W * 0.5);
        cv.width = W * dpr; cv.height = H * dpr; cv.style.height = H + 'px';
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctx.fillStyle = '#bfe3f2'; ctx.fillRect(0, 0, W, H);
        ctx.fillStyle = '#e3eedb'; ctx.strokeStyle = '#a9c49a'; ctx.lineWidth = 0.6;
        w.kopno.forEach(p => { ctx.beginPath(); p.forEach(path); ctx.fill('evenodd'); });
        w.drzave.forEach(d => d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); ctx.stroke(); }));
        if (mark) {
          const t = mark.t;
          if (t.d) { ctx.fillStyle = mark.ok ? 'rgba(29,138,78,.75)' : 'rgba(29,138,78,.55)'; t.d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); }); }
          if (t.r) { ctx.strokeStyle = '#1f5fd1'; ctx.lineWidth = 3; ctx.beginPath(); t.r.pts.forEach(([x, y], i) => (i ? ctx.lineTo(X(x), Y(y)) : ctx.moveTo(X(x), Y(y)))); ctx.stroke(); }
          if (t.c) { ctx.fillStyle = '#1d8a4e'; ctx.beginPath(); ctx.arc(X(t.c.lon), Y(t.c.lat), 6, 0, 7); ctx.fill(); }
          ctx.fillStyle = '#c8102e'; ctx.strokeStyle = '#fff'; ctx.lineWidth = 2;
          ctx.beginPath(); ctx.arc(X(mark.lon), Y(mark.lat), 5, 0, 7); ctx.fill(); ctx.stroke();
        }
      }
      function show() {
        const t = tasks[ti % tasks.length];
        prompt.replaceChildren(h('span', {}, `Tapni ${t.kind === 'država' ? 'državu' : t.kind === 'grad' ? 'grad' : 'rijeku'}:`), h('b', {}, t.label));
        mark = null; draw();
      }
      cv.addEventListener('pointerdown', async e => {
        if (done || busy) return;
        const r = cv.getBoundingClientRect();
        const [lon, lat] = toLL(e.clientX - r.left, e.clientY - r.top);
        const t = tasks[ti % tasks.length];
        let acc, km;
        if (t.d) { km = distToCountry(lon, lat, t.d); acc = km === 0 ? 1 : clamp(1 - km / 900, 0, 0.8); }
        else if (t.c) { km = haversine(lon, lat, t.c.lon, t.c.lat); acc = km < 150 ? 1 : clamp(1 - (km - 150) / 1500, 0, 1); }
        else { km = distToLine(lon, lat, t.r.pts); acc = km < 120 ? 1 : clamp(1 - (km - 120) / 1200, 0, 1); }
        score += acc; count++;
        busy = true; mark = { lon, lat, t, ok: acc >= 0.99 };
        note.textContent = acc >= 0.99 ? 'Tačno!' : `Promašaj: ${Math.round(km)} km`;
        draw();
        await sleep(acc >= 0.99 ? 600 : 1200);
        busy = false; ti++; note.textContent = 'Tapni na mapu.';
        if (!done) show();
      });
      const onResize = () => draw();
      window.addEventListener('resize', onResize);
      show();
      const t0 = performance.now();
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${score.toFixed(1)} bodova · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true; window.removeEventListener('resize', onResize);
          return resolve({ score: clamp(score / 6, 0, 1), label: `${score.toFixed(1)} bodova` });
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  },
  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.95 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${(score * 6).toFixed(1)} bodova` };
  }
};
export default game;
