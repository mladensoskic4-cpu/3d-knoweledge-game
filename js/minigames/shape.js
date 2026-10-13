// Mini igra: Pogodi državu po obliku. Prikazuje se obris (silueta) države, biraš koja je.
import { h } from '../ui.js';
import { shuffle } from '../util.js';
import { loadWorld } from '../world.js';
import { playChoiceRounds, simulate } from './kit.js';

function drawShape(canvas, d) {
  const W = 300, H = 220, dpr = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = W * dpr; canvas.height = H * dpr;
  const ctx = canvas.getContext('2d');
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.clearRect(0, 0, W, H);
  // granice crteža (zanemari dio preko datumske granice)
  let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
  const lon = x => (d.b[0] > d.b[2] && x < 0 ? x + 360 : x); // Rusija, Fidži…
  d.poly.forEach(rings => rings[0].forEach(([x, y]) => { x = lon(x); minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y); }));
  const cos = Math.cos(((minY + maxY) / 2) * Math.PI / 180);
  const sx = (maxX - minX) * cos || 1, sy = maxY - minY || 1;
  const s = Math.min((W - 24) / sx, (H - 24) / sy);
  const ox = (W - sx * s) / 2, oy = (H - sy * s) / 2;
  ctx.fillStyle = '#14232e';
  ctx.beginPath();
  d.poly.forEach(rings => rings.forEach(r => r.forEach(([x, y], i) => {
    const px = ox + (lon(x) - minX) * cos * s, py = oy + (maxY - y) * s;
    i ? ctx.lineTo(px, py) : ctx.moveTo(px, py);
  })));
  ctx.fill('evenodd');
}

const game = {
  id: 'oblik', name: 'Pogodi državu po obliku', duration: 20, ready: false, list: [],
  intro: 'Pojavljuje se obris države. Izaberi koja je to država. Što više tačnih za 20 sekundi, greška oduzima pola boda.',
  async init() {
    const w = await loadWorld();
    // bez sasvim sitnih država (obris im se ne vidi)
    this.list = w.drzave.filter(d => d.area > 8000);
    this.ready = this.list.length > 20;
  },
  play(host, { rng = Math.random } = {}) {
    let bag = shuffle([...this.list], rng);
    return playChoiceRounds(host, {
      goal: 7,
      next: () => {
        if (!bag.length) bag = shuffle([...this.list], rng);
        const d = bag.pop();
        const same = shuffle(this.list.filter(x => x !== d && x.continent === d.continent), rng);
        const rest = shuffle(this.list.filter(x => x !== d && x.continent !== d.continent), rng);
        const opts = shuffle([d, ...[...same, ...rest].slice(0, 3)], rng).map(x => x.name);
        const cv = h('canvas', { class: 'shape-cv', 'aria-label': 'Obris države' });
        drawShape(cv, d);
        return { stage: h('div', { class: 'shape-frame' }, cv), options: opts, answer: d.name };
      }
    });
  },
  simulateBot: simulate(7, 'tačnih')
};
export default game;
