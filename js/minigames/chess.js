// Mini igra: Mat u jednom potezu. Bijeli je na potezu; tapni figuru pa polje na koje matira.
// Problemi su generisani i provjereni bibliotekom chess.js (data/sah.json): u svakom postoji tačno jedan mat.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { timerBar } from './kit.js';

const GLYPH = { K: '♔', Q: '♕', R: '♖', B: '♗', N: '♘', P: '♙', k: '♚', q: '♛', r: '♜', b: '♝', n: '♞', p: '♟' };
const FILES = 'abcdefgh';

function parseFen(fen) {
  const board = {};
  fen.split(' ')[0].split('/').forEach((row, ri) => {
    let f = 0;
    for (const ch of row) {
      if (/\d/.test(ch)) f += Number(ch);
      else { board[FILES[f] + (8 - ri)] = ch; f++; }
    }
  });
  return board;
}

const game = {
  id: 'sah', name: 'Mat u jednom potezu', duration: 30, ready: false, list: [],
  intro: 'Bijeli je na potezu i može da matira u jednom potezu. Tapni bijelu figuru, pa polje na koje ide. Imaš 30 sekundi za što više problema.',
  async init() {
    const r = await fetch('data/sah.json');
    this.list = r.ok ? await r.json() : [];
    this.ready = this.list.length >= 5;
  },
  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000;
    return new Promise(resolve => {
      const { fill, status } = timerBar(host);
      const info = h('p', { class: 'map-hint chess-info' }, 'Bijeli na potezu: mat u 1');
      const board = h('div', { class: 'chess-board', role: 'grid', 'aria-label': 'Šahovska tabla' });
      host.append(info, board);
      let bag = shuffle([...this.list], rng), cur = null, pos = {}, sel = null, solved = 0, misses = 0, done = false, busy = false;

      function render(hl = {}) {
        board.replaceChildren(...[8, 7, 6, 5, 4, 3, 2, 1].flatMap(rank => [...FILES].map((f, fi) => {
          const sq = f + rank, p = pos[sq];
          const cls = ['sq', (fi + rank) % 2 ? 'light' : 'dark', sel === sq ? 'sel' : '', hl[sq] || ''].join(' ');
          const b = h('button', { type: 'button', class: cls, 'data-sq': sq, 'aria-label': sq + (p ? ' ' + p : '') },
            p ? h('span', { class: `pc ${p === p.toUpperCase() ? 'w' : 'b'}` }, GLYPH[p]) : null,
            fi === 0 ? h('i', { class: 'rank' }, String(rank)) : null,
            rank === 1 ? h('i', { class: 'file' }, f) : null);
          b.addEventListener('click', () => clickSq(sq));
          return b;
        })));
      }
      function next() {
        if (!bag.length) bag = shuffle([...this.list], rng);
        cur = bag.pop(); pos = parseFen(cur.fen); sel = null;
        render();
      }
      const nextP = next.bind(this);
      async function clickSq(sq) {
        if (done || busy) return;
        const p = pos[sq];
        if (!sel) { if (p && p === p.toUpperCase()) { sel = sq; render(); } return; }
        if (p && p === p.toUpperCase()) { sel = sq; render(); return; }
        const ok = sel === cur.from && sq === cur.to;
        busy = true;
        const from = sel;
        if (ok) {
          solved++;
          pos[sq] = cur.promo ? 'Q' : pos[from]; delete pos[from]; sel = null;
          info.textContent = `Mat! ${cur.san}`;
          render({ [sq]: 'ok' });
          await sleep(700);
        } else {
          misses++; sel = null;
          info.textContent = `Nije mat. Tačno je bilo: ${cur.san}`;
          render({ [from]: 'bad', [cur.from]: 'hint', [cur.to]: 'hint' });
          await sleep(1300);
        }
        busy = false;
        info.textContent = 'Bijeli na potezu: mat u 1';
        if (!done) nextP();
      }
      nextP();
      const t0 = performance.now();
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${solved} matova · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true; board.querySelectorAll('button').forEach(b => (b.disabled = true));
          return resolve({ score: clamp((solved - misses * 0.3) / 3, 0, 1), label: `${solved} matova` });
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  },
  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.85 + gaussian(rng) * 0.2, 0, 1);
    return { score, label: `${Math.round(score * 3)} matova` };
  }
};
export default game;
