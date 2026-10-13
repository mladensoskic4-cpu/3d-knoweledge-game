// Mapa svijeta za mini igre: crtanje (Natural Earth, Kosovo u sastavu Srbije), uvećanje i pomjeranje.
// Jedan prst / miš = pomjeranje ili tap, dva prsta ili točkić = uvećanje, dugmad ＋ − ⤢.
import { h } from '../ui.js';
import { clamp } from '../util.js';

const B = { minLon: -170, maxLon: 190, minLat: -58, maxLat: 80 };

export function createWorldMap(world, { onTap, heightFactor = 0.5, startLon = 15 } = {}) {
  const cv = h('canvas', { class: 'wm-cv', 'aria-label': 'Mapa svijeta' });
  const zoomBtn = (txt, label, fn) => { const b = h('button', { type: 'button', class: 'wm-zbtn', 'aria-label': label, title: label }, txt); b.addEventListener('click', e => { e.stopPropagation(); fn(); }); return b; };
  const tools = h('div', { class: 'wm-tools' },
    zoomBtn('+', 'Uvećaj', () => zoomAt(1.6, Wv / 2, Hv / 2)),
    zoomBtn('−', 'Umanji', () => zoomAt(1 / 1.6, Wv / 2, Hv / 2)),
    zoomBtn('⤢', 'Cijela mapa', () => reset()));
  const el = h('div', { class: 'map-wrap wm-wrap' }, cv, tools);
  const ctx = cv.getContext('2d');
  // W×H = cijeli svijet pri uvećanju 1; Wv×Hv = vidljivi dio. Pogled: uvećanje k, gornji lijevi ugao (ox, oy).
  let W = 0, H = 0, Wv = 0, Hv = 0, k = 1, kMin = 1, ox = 0, oy = 0, first = true, overlay = null, pending = 0;
  const X = lon => ((lon < B.minLon ? lon + 360 : lon) - B.minLon) / (B.maxLon - B.minLon) * W;
  const Y = lat => (B.maxLat - lat) / (B.maxLat - B.minLat) * H;
  const toLL = (sx, sy) => {
    const x = sx / k + ox, y = sy / k + oy;
    let lon = B.minLon + x / W * (B.maxLon - B.minLon); if (lon > 180) lon -= 360;
    return [lon, B.maxLat - y / H * (B.maxLat - B.minLat)];
  };
  function clampView() { k = clamp(k, kMin, 12); ox = clamp(ox, 0, Math.max(0, W - Wv / k)); oy = clamp(oy, 0, Math.max(0, H - Hv / k)); }
  function zoomAt(f, sx, sy) { const bx = sx / k + ox, by = sy / k + oy; k = clamp(k * f, kMin, 12); ox = bx - sx / k; oy = by - sy / k; clampView(); draw(); }
  function reset() { k = kMin; ox = X(startLon) - Wv / k / 2; oy = 0; clampView(); draw(); }
  function layout() {
    Wv = el.clientWidth || 600;
    Hv = Math.round(clamp(window.innerHeight * heightFactor, Wv * 0.5, Wv * 0.85));
    W = Wv; H = W * 0.5; kMin = Hv / H;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    cv.width = Wv * dpr; cv.height = Hv * dpr; cv.style.height = Hv + 'px';
    if (first) { first = false; reset(); } else clampView();
  }
  function path(ring) { let prev = null; ring.forEach(([x, y], i) => { const px = X(x), py = Y(y); (i && Math.abs(px - prev) < W / 3) ? ctx.lineTo(px, py) : ctx.moveTo(px, py); prev = px; }); ctx.closePath(); }
  function draw() { if (!pending) pending = requestAnimationFrame(paint); }
  function paint() {
    pending = 0;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.fillStyle = '#bfe3f2'; ctx.fillRect(0, 0, Wv, Hv);
    ctx.setTransform(dpr * k, 0, 0, dpr * k, -ox * k * dpr, -oy * k * dpr);
    ctx.fillStyle = '#e3eedb'; ctx.strokeStyle = '#94b384'; ctx.lineWidth = 0.7 / k;
    world.kopno.forEach(p => { ctx.beginPath(); p.forEach(path); ctx.fill('evenodd'); });
    world.drzave.forEach(d => d.poly.forEach(rings => { ctx.beginPath(); rings.forEach(path); ctx.fill('evenodd'); ctx.stroke(); }));
    overlay?.({ ctx, X, Y, k, path });
  }
  // pokret
  const pts = new Map();
  let drag = null, pinch = null;
  const local = e => { const r = cv.getBoundingClientRect(); return [e.clientX - r.left, e.clientY - r.top]; };
  cv.addEventListener('pointerdown', e => {
    cv.setPointerCapture(e.pointerId);
    pts.set(e.pointerId, local(e));
    if (pts.size === 1) drag = { start: local(e), last: local(e), moved: false };
    if (pts.size === 2) { const [a, b] = [...pts.values()]; pinch = { d: Math.hypot(a[0] - b[0], a[1] - b[1]) || 1 }; if (drag) drag.moved = true; }
  });
  cv.addEventListener('pointermove', e => {
    if (!pts.has(e.pointerId)) return;
    const p = local(e); pts.set(e.pointerId, p);
    if (pts.size === 2 && pinch) {
      const [a, b] = [...pts.values()], d = Math.hypot(a[0] - b[0], a[1] - b[1]) || 1;
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
    if (!pts.size) { if (drag && !drag.moved && e.type === 'pointerup') onTap?.(...toLL(...drag.start)); drag = null; }
  };
  cv.addEventListener('pointerup', up);
  cv.addEventListener('pointercancel', up);
  cv.addEventListener('wheel', e => { e.preventDefault(); const [sx, sy] = local(e); zoomAt(e.deltaY < 0 ? 1.25 : 0.8, sx, sy); }, { passive: false });
  const onResize = () => { layout(); draw(); };
  window.addEventListener('resize', onResize);

  return {
    el,
    start() { layout(); draw(); },
    // overlay(ctx, X, Y, k, path): crta oznake preko mape (tačan odgovor, tap igrača…)
    setOverlay(fn) { overlay = fn; draw(); },
    // pomjeri pogled da se vide obje tačke (npr. tvoj tap i tačno mjesto)
    focus(points) {
      const xs = points.map(([lon]) => X(lon)), ys = points.map(([, lat]) => Y(lat));
      const cx = (Math.min(...xs) + Math.max(...xs)) / 2, cy = (Math.min(...ys) + Math.max(...ys)) / 2;
      const span = Math.max(Math.max(...xs) - Math.min(...xs), (Math.max(...ys) - Math.min(...ys)) * Wv / Hv, 1);
      k = clamp(Math.min(k, Wv / (span * 1.6)), kMin, 12);
      ox = cx - Wv / k / 2; oy = cy - Hv / k / 2; clampView(); draw();
    },
    destroy() { window.removeEventListener('resize', onResize); }
  };
}
