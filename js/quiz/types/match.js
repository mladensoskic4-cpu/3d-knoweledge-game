// Tip pitanja: spoji parove. Tapni pojam lijevo pa desno (ili povuci liniju). Bodovi = udio tačnih veza.
import { h } from '../../ui.js';
import { shuffle } from '../../util.js';

const LINE_COLORS = ['#1f86c2', '#9b4bd6', '#d9822b', '#2f9e5b', '#c8102e', '#3d5a80'];

export default {
  id: 'spoji',
  label: 'Spoji parove',
  timeLimit: 25,

  mount(q, body, api) {
    const pairs = q.tacno;
    const left = shuffle(pairs.map(p => p[0]), api.rng);
    const right = shuffle(pairs.map(p => p[1]), api.rng);
    const answer = new Map(pairs.map(([a, b]) => [a, b]));
    const links = new Map(); // indeks lijevo -> indeks desno
    let active = null, locked = false, result = null, drag = null;

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('class', 'match-lines');
    const mkBtn = (text, side, i) => h('button', { type: 'button', class: `match-item ${side}`, 'data-side': side, 'data-i': i, id: `m-${side}-${i}` }, text);
    const L = left.map((t, i) => mkBtn(t, 'l', i));
    const R = right.map((t, i) => mkBtn(t, 'r', i));
    const wrap = h('div', { class: 'match-wrap' }, h('div', { class: 'match-col' }, L), h('div', { class: 'match-col' }, R), svg);
    const ok = h('button', { class: 'btn btn-primary', type: 'button', id: 'match-ok', disabled: true }, 'Potvrdi');
    const note = h('span', { class: 'map-hint' }, 'Tapni pojam lijevo, pa njegov par desno.');
    body.append(wrap, h('div', { class: 'row-bar' }, note, ok));

    function connect(li, ri) {
      for (const [k, v] of links) if (v === ri) links.delete(k);
      links.set(li, ri);
      ok.disabled = links.size === 0;
    }
    function pick(side, i) {
      if (locked) return;
      if (!active || active.side === side) { active = { side, i }; }
      else {
        const li = side === 'l' ? i : active.i, ri = side === 'r' ? i : active.i;
        connect(li, ri); active = null;
      }
      draw();
    }
    [...L, ...R].forEach(b => {
      b.addEventListener('click', () => pick(b.dataset.side, +b.dataset.i));
      b.addEventListener('pointerdown', e => {
        if (locked) return;
        drag = { side: b.dataset.side, i: +b.dataset.i, x: e.clientX, y: e.clientY, moved: false };
      });
    });
    const onMove = e => {
      if (!drag) return;
      if (Math.hypot(e.clientX - drag.x, e.clientY - drag.y) > 8) drag.moved = true;
      drag.cx = e.clientX; drag.cy = e.clientY;
      if (drag.moved) draw();
    };
    const onUp = e => {
      if (!drag) return;
      const d = drag; drag = null;
      if (!d.moved) return draw();
      const target = document.elementFromPoint(e.clientX, e.clientY)?.closest('.match-item');
      if (target && target.dataset.side !== d.side) {
        const li = d.side === 'l' ? d.i : +target.dataset.i, ri = d.side === 'r' ? d.i : +target.dataset.i;
        connect(li, ri); active = null;
      }
      draw();
    };
    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);

    function anchor(btn, side) {
      const w = wrap.getBoundingClientRect(), r = btn.getBoundingClientRect();
      return [side === 'l' ? r.right - w.left : r.left - w.left, r.top - w.top + r.height / 2];
    }
    function line(a, b, color, dash) {
      const el = document.createElementNS('http://www.w3.org/2000/svg', 'line');
      el.setAttribute('x1', a[0]); el.setAttribute('y1', a[1]); el.setAttribute('x2', b[0]); el.setAttribute('y2', b[1]);
      el.setAttribute('stroke', color); el.setAttribute('stroke-width', '3.5'); el.setAttribute('stroke-linecap', 'round');
      if (dash) el.setAttribute('stroke-dasharray', '6 6');
      svg.append(el);
    }
    function draw() {
      const w = wrap.getBoundingClientRect();
      svg.setAttribute('viewBox', `0 0 ${w.width} ${w.height}`);
      svg.replaceChildren();
      [...L, ...R].forEach(b => { b.classList.remove('active', 'linked', 'is-right', 'is-wrong'); b.style.removeProperty('--lc'); });
      for (const [li, ri] of links) {
        const good = answer.get(left[li]) === right[ri];
        const color = result ? (good ? '#1d8a4e' : '#c8102e') : LINE_COLORS[li % LINE_COLORS.length];
        line(anchor(L[li], 'l'), anchor(R[ri], 'r'), color);
        [L[li], R[ri]].forEach(b => { b.classList.add('linked'); b.style.setProperty('--lc', color); if (result) b.classList.add(good ? 'is-right' : 'is-wrong'); });
      }
      if (result) {
        left.forEach((t, li) => {
          const ri = right.indexOf(answer.get(t));
          if (links.get(li) !== ri) line(anchor(L[li], 'l'), anchor(R[ri], 'r'), '#1d8a4e', true);
        });
      }
      if (active) (active.side === 'l' ? L : R)[active.i].classList.add('active');
      if (drag?.moved && drag.cx != null) {
        const btn = (drag.side === 'l' ? L : R)[drag.i];
        line(anchor(btn, drag.side), [drag.cx - w.left, drag.cy - w.top], '#14232e', true);
      }
    }
    const evaluate = () => {
      let good = 0;
      for (const [li, ri] of links) if (answer.get(left[li]) === right[ri]) good++;
      return { accuracy: good / pairs.length, detail: { good, total: pairs.length } };
    };
    ok.addEventListener('click', () => { if (!locked) { const r = evaluate(); api.submit(r.accuracy, r.detail); } });
    const onResize = () => draw();
    window.addEventListener('resize', onResize);
    requestAnimationFrame(draw);

    return {
      lock() {
        locked = true; ok.disabled = true; active = null; drag = null;
        window.removeEventListener('pointermove', onMove); window.removeEventListener('pointerup', onUp);
      },
      timeout: () => (links.size ? evaluate() : { accuracy: 0, detail: { good: 0, total: pairs.length } }),
      reveal(res) {
        result = res;
        ok.hidden = true;
        note.textContent = `Tačno spojeno: ${res.detail.good ?? 0} od ${pairs.length}. Isprekidano = tačan par.`;
        draw();
      }
    };
  },

  simulateBot(q, p, rng) {
    const n = q.tacno.length;
    let good = 0;
    for (let i = 0; i < n; i++) if (rng() < p) good++;
    return good / n;
  },
  summarize: r => (r.detail?.total ? `${r.detail.good}/${r.detail.total} parova` : `${Math.round(r.accuracy * r.q?.tacno?.length || 0)}/${r.q?.tacno?.length || 4} parova`)
};
