// Mini igra: Mapa svijeta. Tapni državu, grad ili rijeku. Prave granice (Natural Earth), Kosovo u sastavu Srbije.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { loadWorld, inCountry, distToCountry, distToLine, CITIES, RIVERS } from '../world.js';
import { haversine } from '../geo.js';
import { timerBar } from './kit.js';
import { L } from '../i18n.js';
import { correct as sfxOk, wrong as sfxBad } from '../sfx.js';

const B = { minLon: -170, maxLon: 190, minLat: -58, maxLat: 80 };

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
      const cv = h('canvas', { class: 'wm-cv', 'aria-label': 'Mapa svijeta' });
      const zoomBtn = (txt, label, fn) => { const b = h('button', { type: 'button', class: 'wm-zbtn', 'aria-label': label, title: label }, txt); b.addEventListener('click', e => { e.stopPropagation(); fn(); }); return b; };
      const tools = h('div', { class: 'wm-tools' },
        zoomBtn('+', 'Uvećaj', () => zoomAt(1.6, Wv / 2, Hv / 2)),
        zoomBtn('−', 'Umanji', () => zoomAt(1 / 1.6, Wv / 2, Hv / 2)),
        zoomBtn('⤢', 'Cijela mapa', () => { k = kMin; ox = (W - Wv / k) / 2; oy = (H - Hv / k) / 2; clampView(); draw(); }));
      const wrap = h('div', { class: 'map-wrap wm-wrap' }, cv, tools);
      const note = h('p', { class: 'map-hint wm-note' }, 'Tapni na mapu. Prevuci za pomjeranje.');
      host.append(prompt, wrap, note);

      const big = w.drzave.filter(d => d.area > 60000);
      const tasks = shuffle([
        ...shuffle(big, rng).slice(0, 14).map(d => ({ kind: 'država', label: d.name, d })),
        ...shuffle([...CITIES], rng).slice(0, 8).map(c => ({ kind: 'grad', label: c.name, c })),
        ...shuffle([...RIVERS], rng).slice(0, 5).map(r => ({ kind: 'rijeka', label: r.name, r }))
      ], rng);
      let ti = 0, score = 0, done = false, busy = false, mark = null;
      // W×H = cijeli svijet pri uvećanju 1; Wv×Hv = vidljivi dio (platno). Pogled: uvećanje k, gornji lijevi ugao (ox, oy).
      let W = 0, H = 0, Wv = 0, Hv = 0, k = 1, kMin = 1, ox = 0, oy = 0, first = true;
      const ctx = cv.getContext('2d');
      const X = lon => ((lon < B.minLon ? lon + 360 : lon) - B.minLon) / (B.maxLon - B.minLon) * W;
      const Y = lat => (B.maxLat - lat) / (B.maxLat - B.minLat) * H;
      const toLL = (sx, sy) => {
        const x = sx / k + ox, y = sy / k + oy;
        let lon = B.minLon + x / W * (B.maxLon - B.minLon); if (lon > 180) lon -= 360;
        return [lon, B.maxLat - y / H * (B.maxLat - B.minLat)];
      };
      function clampView() {
        k = clamp(k, kMin, 10);
        ox = clamp(ox, 0, Math.max(0, W - Wv / k));
        oy = clamp(oy, 0, Math.max(0, H - Hv / k));
      }
      function zoomAt(f, sx, sy) {
        const bx = sx / k + ox, by = sy / k + oy;
        k = clamp(k * f, kMin, 10);
        ox = bx - sx / k; oy = by - sy / k;
        clampView(); draw();
      }
      function layout() {
        Wv = wrap.clientWidth;
        Hv = Math.round(clamp(window.innerHeight * 0.5, Wv * 0.5, Wv * 0.85));
        W = Wv; H = W * 0.5;
        kMin = Hv / H;
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        cv.width = Wv * dpr; cv.height = Hv * dpr; cv.style.height = Hv + 'px';
        if (first) { // početni pogled: cijela visina svijeta, centrirano na Evropu i Afriku
          first = false; k = kMin;
          ox = X(15) - Wv / k / 2; oy = 0;
        }
        clampView();
      }
      function path(ring) { let prev = null; ring.forEach(([x, y], i) => { const px = X(x), py = Y(y); (i && Math.abs(px - prev) < W / 3) ? ctx.lineTo(px, py) : ctx.moveTo(px, py); prev = px; }); ctx.closePath(); }
      let pending = 0;
      function draw() { if (!pending) pending = requestAnimationFrame(paint); }
      function paint() {
        pending = 0;
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctx.fillStyle = '#bfe3f2'; ctx.fillRect(0, 0, Wv, Hv);
        ctx.setTransform(dpr * k, 0, 0, dpr * k, -ox * k * dpr, -oy * k * dpr);
        ctx.fillStyle = '#e3eedb'; ctx.strokeStyle = '#94b384'; ctx.lineWidth = 0.7 / k;
        w.kopno.forEach(p => { ctx.beginPath(); p.forEach(path); ctx.fill('evenodd'); });
        w.drzave.forEach(d => d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); ctx.stroke(); }));
        if (mark) {
          const t = mark.t;
          if (t.d) { ctx.fillStyle = mark.ok ? 'rgba(29,138,78,.75)' : 'rgba(29,138,78,.55)'; t.d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); }); }
          if (t.r) { ctx.strokeStyle = '#1f5fd1'; ctx.lineWidth = 3 / k; ctx.beginPath(); t.r.pts.forEach(([x, y], i) => (i ? ctx.lineTo(X(x), Y(y)) : ctx.moveTo(X(x), Y(y)))); ctx.stroke(); }
          if (t.c) { ctx.fillStyle = '#1d8a4e'; ctx.beginPath(); ctx.arc(X(t.c.lon), Y(t.c.lat), 6 / k, 0, 7); ctx.fill(); }
          ctx.fillStyle = '#c8102e'; ctx.strokeStyle = '#fff'; ctx.lineWidth = 2 / k;
          ctx.beginPath(); ctx.arc(X(mark.lon), Y(mark.lat), 5 / k, 0, 7); ctx.fill(); ctx.stroke();
        }
      }
      function show() {
        const t = tasks[ti % tasks.length];
        prompt.replaceChildren(h('span', {}, `Tapni ${t.kind === 'država' ? 'državu' : t.kind === 'grad' ? 'grad' : 'rijeku'}:`), h('b', {}, t.label));
        mark = null; draw();
      }
      async function answer(sx, sy) {
        if (done || busy) return;
        const [lon, lat] = toLL(sx, sy);
        const t = tasks[ti % tasks.length];
        let acc, km;
        if (t.d) { km = distToCountry(lon, lat, t.d); acc = km === 0 ? 1 : clamp(1 - km / 900, 0, 0.8); }
        else if (t.c) { km = haversine(lon, lat, t.c.lon, t.c.lat); acc = km < 150 ? 1 : clamp(1 - (km - 150) / 1500, 0, 1); }
        else { km = distToLine(lon, lat, t.r.pts); acc = km < 120 ? 1 : clamp(1 - (km - 120) / 1200, 0, 1); }
        score += acc;
        (acc >= 0.99 ? sfxOk : sfxBad)();
        busy = true; mark = { lon, lat, t, ok: acc >= 0.99 };
        note.textContent = acc >= 0.99 ? 'Tačno!' : `Promašaj: ${Math.round(km)} km`;
        draw();
        await sleep(acc >= 0.99 ? 600 : 1300);
        busy = false; ti++; note.textContent = 'Tapni na mapu. Prevuci za pomjeranje.';
        if (!done) show();
      }

      // Dodir i miš: jedan prst = pomjeranje ili tap, dva prsta = uvećanje, točkić = uvećanje.
      const pts = new Map();
      let drag = null, pinch = null;
      const local = e => { const r = cv.getBoundingClientRect(); return [e.clientX - r.left, e.clientY - r.top]; };
      cv.addEventListener('pointerdown', e => {
        cv.setPointerCapture(e.pointerId);
        pts.set(e.pointerId, local(e));
        if (pts.size === 1) drag = { start: local(e), last: local(e), moved: false };
        if (pts.size === 2) {
          const [a, b] = [...pts.values()];
          pinch = { d: Math.hypot(a[0] - b[0], a[1] - b[1]) || 1 };
          if (drag) drag.moved = true;
        }
      });
      cv.addEventListener('pointermove', e => {
        if (!pts.has(e.pointerId)) return;
        const p = local(e); pts.set(e.pointerId, p);
        if (pts.size === 2 && pinch) {
          const [a, b] = [...pts.values()];
          const d = Math.hypot(a[0] - b[0], a[1] - b[1]) || 1;
          zoomAt(d / pinch.d, (a[0] + b[0]) / 2, (a[1] + b[1]) / 2); pinch.d = d;
        } else if (drag) {
          if (Math.hypot(p[0] - drag.start[0], p[1] - drag.start[1]) > 7) drag.moved = true;
          if (drag.moved) { ox -= (p[0] - drag.last[0]) / k; oy -= (p[1] - drag.last[1]) / k; clampView(); draw(); }
          drag.last = p;
        }
      });
      const up = e => {
        if (!pts.has(e.pointerId)) return;
        pts.delete(e.pointerId);
        if (pts.size < 2) pinch = null;
        if (!pts.size) {
          if (drag && !drag.moved && e.type === 'pointerup') answer(...drag.start);
          drag = null;
        }
      };
      cv.addEventListener('pointerup', up);
      cv.addEventListener('pointercancel', up);
      cv.addEventListener('wheel', e => { e.preventDefault(); const [sx, sy] = local(e); zoomAt(e.deltaY < 0 ? 1.25 : 0.8, sx, sy); }, { passive: false });

      const onResize = () => { layout(); draw(); };
      window.addEventListener('resize', onResize);
      layout(); show();
      const t0 = performance.now();
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${score.toFixed(1)} bodova · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true; window.removeEventListener('resize', onResize);
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
