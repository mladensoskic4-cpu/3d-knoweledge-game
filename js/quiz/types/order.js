// Tip pitanja: poredaj stavke (prevlačenje ili strelice). Bodovi = udio parova u tačnom redoslijedu.
import { h } from '../../ui.js';
import { shuffle, clamp, gaussian } from '../../util.js';

function score(order, correct) {
  const pos = new Map(correct.map((v, i) => [v, i]));
  let ok = 0, total = 0;
  for (let i = 0; i < order.length; i++) for (let j = i + 1; j < order.length; j++) {
    total++;
    if (pos.get(order[i]) < pos.get(order[j])) ok++;
  }
  const c = ok / total;
  return c >= 1 ? 1 : Math.max(0, (c - 0.5) * 2);
}

export default {
  id: 'poredaj',
  label: 'Poredaj',
  timeLimit: 25,

  mount(q, body, api) {
    const correct = q.tacno;
    let start = shuffle([...correct], api.rng);
    for (let k = 0; k < 5 && start.every((v, i) => v === correct[i]); k++) start = shuffle([...correct], api.rng);
    let locked = false, moved = false;
    const list = h('ol', { class: 'ord-list' });
    const read = () => [...list.children].map(li => li.dataset.v);

    function swap(li, dir) {
      if (locked) return;
      const sib = dir < 0 ? li.previousElementSibling : li.nextElementSibling;
      if (!sib) return;
      dir < 0 ? list.insertBefore(li, sib) : list.insertBefore(sib, li);
      moved = true;
    }
    start.forEach(v => {
      const li = h('li', { class: 'ord-item', 'data-v': v },
        h('span', { class: 'ord-grip', 'aria-hidden': 'true' }, '⋮⋮'),
        h('span', { class: 'ord-txt' }, v),
        h('span', { class: 'ord-badge' }),
        h('span', { class: 'ord-btns' },
          h('button', { type: 'button', class: 'ord-btn', 'aria-label': `Pomjeri gore: ${v}`, onclick: () => swap(li, -1) }, '↑'),
          h('button', { type: 'button', class: 'ord-btn', 'aria-label': `Pomjeri dolje: ${v}`, onclick: () => swap(li, 1) }, '↓')));
      let dragging = false;
      li.addEventListener('pointerdown', e => {
        if (locked || e.target.closest('button')) return;
        dragging = true; li.setPointerCapture(e.pointerId); li.classList.add('drag');
      });
      li.addEventListener('pointermove', e => {
        if (!dragging) return;
        const others = [...list.children].filter(s => s !== li);
        const idx = others.filter(s => { const r = s.getBoundingClientRect(); return e.clientY > r.top + r.height / 2; }).length;
        const ref = others[idx] || null;
        if (li.nextElementSibling !== ref) { list.insertBefore(li, ref); moved = true; }
      });
      const stop = () => { dragging = false; li.classList.remove('drag'); };
      li.addEventListener('pointerup', stop);
      li.addEventListener('pointercancel', stop);
      list.append(li);
    });

    const ok = h('button', { class: 'btn btn-primary', type: 'button', id: 'ord-ok' }, 'Potvrdi');
    const note = h('span', { class: 'map-hint' }, 'Prevuci stavke ili koristi strelice.');
    body.append(list, h('div', { class: 'row-bar' }, note, ok));
    ok.addEventListener('click', () => { if (!locked) { const o = read(); api.submit(score(o, correct), { order: o }); } });

    return {
      lock() { locked = true; ok.disabled = true; list.querySelectorAll('button').forEach(b => (b.disabled = true)); list.classList.add('locked'); },
      timeout: () => (moved ? { accuracy: score(read(), correct), detail: { order: read() } } : { accuracy: 0, detail: {} }),
      reveal() {
        [...list.children].forEach((li, i) => {
          const ci = correct.indexOf(li.dataset.v);
          li.classList.add(ci === i ? 'is-right' : 'is-wrong');
          li.querySelector('.ord-badge').textContent = q.oznake ? `${ci + 1}. · ${q.oznake[ci]}` : `${ci + 1}.`;
        });
        ok.hidden = true;
        note.textContent = 'Broj pokazuje tačno mjesto.';
      }
    };
  },

  simulateBot: (q, p, rng) => clamp(p * 1.05 + gaussian(rng) * 0.2, 0, 1),
  summarize: r => (r.accuracy >= 1 ? 'sve tačno' : `${Math.round(r.accuracy * 100)}%`)
};
