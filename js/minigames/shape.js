// Mini igra: Pogodi državu po obliku. Država je zelena, oko nje blijedo susjedi i more; biraš koja je.
import { h } from '../ui.js';
import { shuffle } from '../util.js';
import { loadWorld } from '../world.js';
import { playChoiceRounds, simulate } from './kit.js';

// Crtež: more (plavo), susjedne države blijedo, tražena država zeleno. Tako se vidi i obala i okruženje.
const VIEW_W = 340, VIEW_H = 250;
function drawShape(canvas, d, world) {
  const W = VIEW_W, H = VIEW_H, dpr = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = W * dpr; canvas.height = H * dpr;
  const ctx = canvas.getContext('2d');
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  // geografske dužine: prsten se „odmota” bez skokova od 360° i pomjeri blizu centra države
  // (Rusija, Fidži, Novi Zeland, Antarktik…)
  const cx = d.c?.[0] ?? (d.b[0] + d.b[2]) / 2;
  const cache = new Map();
  const U = r => {
    if (cache.has(r)) return cache.get(r);
    let prev = null, lo = Infinity, hi = -Infinity;
    const o = r.map(([x, y]) => {
      if (prev !== null) { while (x - prev > 180) x -= 360; while (x - prev < -180) x += 360; }
      prev = x; lo = Math.min(lo, x); hi = Math.max(hi, x);
      return [x, y];
    });
    const sh = Math.round((cx - (lo + hi) / 2) / 360) * 360;
    if (sh) o.forEach(p => { p[0] += sh; });
    cache.set(r, o);
    return o;
  };
  const bbox = ring => U(ring).reduce((b, [x, y]) => [Math.min(b[0], x), Math.min(b[1], y), Math.max(b[2], x), Math.max(b[3], y)], [Infinity, Infinity, -Infinity, -Infinity]);
  // glavni dio države + dijelovi u blizini (bez dalekih prekomorskih teritorija)
  const parts = d.poly.map(r => ({ b: bbox(r[0]) })).map(p => ({ ...p, size: (p.b[2] - p.b[0]) * (p.b[3] - p.b[1]) }));
  const main = parts.reduce((m, p) => (p.size > m.size ? p : m));
  const mx = (main.b[0] + main.b[2]) / 2, my = (main.b[1] + main.b[3]) / 2;
  const reach = Math.max(12, (main.b[2] - main.b[0]) * 1.2, (main.b[3] - main.b[1]) * 1.2);
  const near = parts.filter(p => Math.abs((p.b[0] + p.b[2]) / 2 - mx) < reach && Math.abs((p.b[1] + p.b[3]) / 2 - my) < reach);
  let [minX, minY, maxX, maxY] = near.reduce((b, p) => [Math.min(b[0], p.b[0]), Math.min(b[1], p.b[1]), Math.max(b[2], p.b[2]), Math.max(b[3], p.b[3])], [Infinity, Infinity, -Infinity, -Infinity]);
  const cos = Math.max(0.25, Math.cos(((minY + maxY) / 2) * Math.PI / 180));
  // okvir: 35% prostora oko države, najmanje ~5° da se vide susjedi i kod malih država
  let sx = Math.max((maxX - minX) * cos, 3.5 * cos), sy = Math.max(maxY - minY, 3.5);
  sx *= 1.7; sy *= 1.7;
  if (sx / sy > W / H) sy = sx * H / W; else sx = sy * W / H;
  const ccx = (minX + maxX) / 2, ccy = (minY + maxY) / 2;
  const x0 = ccx - sx / cos / 2, y1 = ccy + sy / 2;
  const s = W / sx;
  const P = (x, y) => [(x - x0) * cos * s, (y1 - y) * s];
  const inView = b => { const a = P(b[0], b[3]), c = P(b[2], b[1]); return a[0] < W && c[0] > 0 && a[1] < H && c[1] > 0; };
  const trace = rings => rings.forEach(r => U(r).forEach(([x, y], i) => { const [px, py] = P(x, y); i ? ctx.lineTo(px, py) : ctx.moveTo(px, py); }));

  ctx.fillStyle = '#9fd3ec'; ctx.fillRect(0, 0, W, H);
  ctx.fillStyle = '#eef1e6';
  world.kopno.forEach(p => { if (!inView(bbox(p[0]))) return; ctx.beginPath(); trace(p); ctx.fill('evenodd'); });
  ctx.strokeStyle = '#b4bfa6'; ctx.lineWidth = 1;
  world.drzave.forEach(o => {
    if (o === d) return;
    o.poly.forEach(rings => { if (!inView(bbox(rings[0]))) return; ctx.beginPath(); trace(rings); ctx.fill('evenodd'); ctx.stroke(); });
  });
  ctx.fillStyle = '#2f9e5b'; ctx.strokeStyle = '#17643a'; ctx.lineWidth = 1.6;
  d.poly.forEach(rings => { ctx.beginPath(); trace(rings); ctx.fill('evenodd'); ctx.stroke(); });
}

const game = {
  id: 'oblik', name: 'Pogodi državu po obliku', duration: 20, ready: false, list: [], world: null,
  intro: 'Pojavljuje se država (zeleno) sa susjedima i morem oko nje. Izaberi koja je to država. Što više tačnih za 20 sekundi, greška oduzima pola boda.',
  async init() {
    const w = await loadWorld();
    this.world = w;
    // bez sasvim sitnih država (obris im se ne vidi)
    this.list = w.drzave.filter(d => d.area > 8000);
    this.ready = this.list.length > 20;
  },
  play(host, { rng = Math.random } = {}) {
    let bag = shuffle([...this.list], rng);
    return playChoiceRounds(host, {
      duration: this.duration, goal: 7,
      next: () => {
        if (!bag.length) bag = shuffle([...this.list], rng);
        const d = bag.pop();
        const same = shuffle(this.list.filter(x => x !== d && x.continent === d.continent), rng);
        const rest = shuffle(this.list.filter(x => x !== d && x.continent !== d.continent), rng);
        const opts = shuffle([d, ...[...same, ...rest].slice(0, 3)], rng).map(x => x.name);
        const cv = h('canvas', { class: 'shape-cv', 'aria-label': 'Obris države' });
        drawShape(cv, d, this.world);
        return { stage: h('div', { class: 'shape-frame' }, cv), options: opts, answer: d.name };
      }
    });
  },
  simulateBot: simulate(7, 'tačnih')
};
export default game;
