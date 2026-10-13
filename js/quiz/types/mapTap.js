// Tip pitanja: tapni na mapi. Poeni zavise od udaljenosti (km) od tačne lokacije.
import { h, fmtNum } from '../../ui.js';
import { MAPS, BALKAN_LAND, ITALY, CG_SHAPE, SRB_SHAPE, KOTOR_BAY, LAKES, COUNTRY_LABELS, CG_LABELS, SRB_LABELS, haversine, projector } from '../../geo.js';
import { clamp, gaussian } from '../../util.js';
import { L } from '../../i18n.js';

// Prave granice država (Natural Earth), učitavaju se jednom.
const BORDER_FILES = { cg: 'data/maps/granice-cg.json', srb: 'data/maps/granice-srb.json', balkan: 'data/maps/granice-balkan.json' };
const FOCUS_NAME = { cg: 'Crna Gora', srb: 'Srbija' };
const borders = {};
export function preloadBorders() {
  return Promise.all(Object.entries(BORDER_FILES).map(async ([k, url]) => {
    try { const r = await fetch(url); if (r.ok) borders[k] = await r.json(); } catch { /* koristi se rezervni obris */ }
  }));
}

const C = { sea: '#bfe3f2', seaGrid: 'rgba(31,134,194,0.14)', land: '#e3eedb', landEdge: '#a9c49a', focus: '#9fd07e', focusEdge: '#3f7a3a', lake: '#8fcbe8', text: '#3b5361', pick: '#c8102e', right: '#1d8a4e' };

function accuracyFor(km, cfg, q) {
  const full = q.tolerancija?.pun ?? cfg.full, zero = q.tolerancija?.nula ?? cfg.zero;
  return km <= full ? 1 : km >= zero ? 0 : 1 - (km - full) / (zero - full);
}

export default {
  id: 'mapa',
  label: 'Tapni na mapi',
  timeLimit: 20,

  mount(q, body, api) {
    const cfg = MAPS[q.mapa || 'cg'];
    const cv = h('canvas', { class: 'map-cv', 'aria-label': `Mapa: ${cfg.label}. Tapni lokaciju.` });
    const wrap = h('div', { class: 'map-wrap' }, cv);
    const hint = h('span', { class: 'map-hint' }, 'Tapni na mapu, pa potvrdi.');
    const ok = h('button', { class: 'btn btn-primary', type: 'button', disabled: true, id: 'map-ok' }, 'Potvrdi');
    body.append(wrap, h('div', { class: 'row-bar' }, hint, ok));

    let pick = null, locked = false, result = null, proj, W = 0, H = 0;
    const ctx = cv.getContext('2d');

    function poly(pts, fill, stroke, lw = 1) {
      ctx.beginPath();
      pts.forEach(([lon, lat], i) => { const [x, y] = proj.toXY(lon, lat); i ? ctx.lineTo(x, y) : ctx.moveTo(x, y); });
      ctx.closePath();
      if (fill) { ctx.fillStyle = fill; ctx.fill(); }
      if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = lw; ctx.lineJoin = 'round'; ctx.stroke(); }
    }
    function pin(lon, lat, color) {
      const [x, y] = proj.toXY(lon, lat);
      ctx.fillStyle = color; ctx.strokeStyle = '#fff'; ctx.lineWidth = 2.5;
      ctx.beginPath(); ctx.moveTo(x, y); ctx.arc(x, y - 16, 8, Math.PI * 0.8, Math.PI * 2.2); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.beginPath(); ctx.arc(x, y - 16, 3, 0, 6.3); ctx.fillStyle = '#fff'; ctx.fill();
    }
    function draw() {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      W = wrap.clientWidth;
      const a = projector(cfg, 100, 100, 0).aspect;
      H = Math.round(clamp(W * a, 200, Math.min(window.innerHeight * 0.42, 440)));
      cv.width = W * dpr; cv.height = H * dpr; cv.style.height = H + 'px';
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      proj = projector(cfg, W, H, 10);

      ctx.fillStyle = C.sea; ctx.fillRect(0, 0, W, H);
      ctx.strokeStyle = C.seaGrid; ctx.lineWidth = 1;
      for (let lon = Math.ceil(cfg.minLon / cfg.grid) * cfg.grid; lon < cfg.maxLon; lon += cfg.grid) {
        const [x1, y1] = proj.toXY(lon, cfg.maxLat), [x2, y2] = proj.toXY(lon, cfg.minLat);
        ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();
      }
      for (let lat = Math.ceil(cfg.minLat / cfg.grid) * cfg.grid; lat < cfg.maxLat; lat += cfg.grid) {
        const [x1, y1] = proj.toXY(cfg.minLon, lat), [x2, y2] = proj.toXY(cfg.maxLon, lat);
        ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();
      }
      const key = q.mapa || 'cg', bd = borders[key];
      if (bd) {
        const focusName = FOCUS_NAME[key];
        bd.countries.forEach(c => c.polygons.forEach(rings => {
          ctx.beginPath();
          rings.forEach(r => r.forEach(([lon, lat], i) => { const [x, y] = proj.toXY(lon, lat); i ? ctx.lineTo(x, y) : ctx.moveTo(x, y); }));
          ctx.closePath();
          const focus = c.name === focusName || (!focusName && (c.name === 'Crna Gora' || c.name === 'Srbija'));
          ctx.fillStyle = focus ? C.focus : C.land; ctx.fill('evenodd');
          ctx.strokeStyle = focus ? C.focusEdge : C.landEdge; ctx.lineWidth = focus ? 1.8 : 1; ctx.lineJoin = 'round'; ctx.stroke();
        }));
      } else {
        poly(BALKAN_LAND, C.land, C.landEdge, 1.2);
        poly(ITALY, C.land, C.landEdge, 1.2);
        if (cfg.focus === 'cg') poly(CG_SHAPE, C.focus, C.focusEdge, 2);
        else if (cfg.focus === 'srb') poly(SRB_SHAPE, C.focus, C.focusEdge, 2);
      }
      Object.values(LAKES).forEach(l => poly(l, C.lake, C.landEdge, 1));

      ctx.fillStyle = C.text; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.font = `600 ${W < 420 ? 10 : 12}px Figtree, system-ui, sans-serif`;
      ({ balkan: COUNTRY_LABELS, cg: CG_LABELS, srb: SRB_LABELS })[cfg.labels].forEach(([t, lon, lat]) => {
        const [x, y] = proj.toXY(lon, lat);
        ctx.fillText(L(t).toUpperCase(), x, y);
      });

      if (result) {
        const [x1, y1] = proj.toXY(q.tacno.lon, q.tacno.lat);
        if (pick) {
          const [x2, y2] = proj.toXY(pick[0], pick[1]);
          ctx.setLineDash([6, 5]); ctx.strokeStyle = C.text; ctx.lineWidth = 2;
          ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke(); ctx.setLineDash([]);
          pin(pick[0], pick[1], C.pick);
        }
        pin(q.tacno.lon, q.tacno.lat, C.right);
        ctx.font = '700 13px Figtree, system-ui, sans-serif';
        const tag = L(q.tacno.naziv);
        const tw = ctx.measureText(tag).width + 14;
        const tx = clamp(x1, tw / 2 + 4, W - tw / 2 - 4), ty = y1 - 38 < 12 ? y1 + 16 : y1 - 38;
        ctx.fillStyle = C.right; ctx.beginPath(); ctx.roundRect ? ctx.roundRect(tx - tw / 2, ty - 11, tw, 22, 11) : ctx.rect(tx - tw / 2, ty - 11, tw, 22); ctx.fill();
        ctx.fillStyle = '#fff'; ctx.fillText(tag, tx, ty + 1);
      } else if (pick) pin(pick[0], pick[1], C.pick);
    }

    const evaluate = () => {
      const km = haversine(pick[0], pick[1], q.tacno.lon, q.tacno.lat);
      return { accuracy: accuracyFor(km, cfg, q), detail: { pick, km } };
    };
    cv.addEventListener('pointerdown', e => {
      if (locked) return;
      const r = cv.getBoundingClientRect();
      pick = proj.toLonLat(e.clientX - r.left, e.clientY - r.top);
      ok.disabled = false;
      hint.textContent = 'Možeš tapnuti ponovo da pomjeriš oznaku.';
      draw();
    });
    ok.addEventListener('click', () => { if (pick && !locked) { const r = evaluate(); api.submit(r.accuracy, r.detail); } });
    const onResize = () => draw();
    window.addEventListener('resize', onResize);
    requestAnimationFrame(draw);

    return {
      lock() { locked = true; ok.disabled = true; },
      timeout: () => (pick ? evaluate() : { accuracy: 0, detail: {} }),
      reveal(res) {
        result = res;
        hint.textContent = pick ? `Promašaj: ${fmtNum(Math.round(res.detail.km))} km` : 'Nisi označio lokaciju.';
        ok.hidden = true;
        draw();
      }
    };
  },

  simulateBot: (q, p, rng) => clamp(p * 1.05 + gaussian(rng) * 0.2, 0, 1),
  summarize: r => (r.detail?.km != null ? `${fmtNum(Math.round(r.detail.km))} km` : `${Math.round(r.accuracy * 100)}% tačno`)
};
