// Tip pitanja: procijeni broj slajderom. Bliže tačnoj vrijednosti = više poena (linearno do tolerancije).
import { h, fmtNum } from '../../ui.js';
import { clamp, gaussian } from '../../util.js';

const fmt = (q, v) => {
  const decimals = (String(q.korak).split('.')[1] || '').length;
  const s = fmtNum(v, { decimals, group: q.format !== 'godina' });
  return q.format === 'godina' ? `${s}.` : s;
};
const unit = q => (q.format === 'godina' ? 'godina' : q.jedinica || '');

export default {
  id: 'procjena',
  label: 'Procijeni broj',
  timeLimit: 20,

  mount(q, body, api) {
    const step = q.korak || 1;
    const snap = v => Math.round(v / step) * step;
    let value = snap((q.min + q.max) / 2), moved = false, locked = false;

    const out = h('output', { class: 'est-value', for: 'est-range' }, fmt(q, value));
    const range = h('input', { type: 'range', id: 'est-range', class: 'est-range', min: q.min, max: q.max, step, value, 'aria-label': q.pitanje });
    const pos = v => `${((v - q.min) / (q.max - q.min)) * 100}%`;
    const marks = h('div', { class: 'est-marks' });
    const ok = h('button', { class: 'btn btn-primary', type: 'button', id: 'est-ok' }, 'Potvrdi');
    const nudge = d => h('button', { class: 'btn btn-ghost nudge', type: 'button', 'aria-label': d < 0 ? 'Smanji' : 'Povećaj', onclick: () => set(value + d * step) }, d < 0 ? '−' : '+');

    const decimals = (String(step).split('.')[1] || '').length;
    const num = h('input', { type: 'number', id: 'est-num', class: 'est-num', min: q.min, max: q.max, step, value, inputmode: decimals ? 'decimal' : 'numeric', 'aria-label': 'Upiši odgovor tastaturom' });
    function set(v, fromInput = false) {
      if (locked || !Number.isFinite(v)) return;
      value = clamp(snap(v), q.min, q.max);
      value = Number(value.toFixed(decimals));
      range.value = value; out.textContent = fmt(q, value); moved = true;
      if (!fromInput) num.value = value;
    }
    range.addEventListener('input', () => set(Number(range.value)));
    num.addEventListener('input', () => { const v = parseFloat(String(num.value).replace(',', '.')); if (Number.isFinite(v)) set(v, true); });
    num.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); ok.click(); } });
    setTimeout(() => { if (window.matchMedia('(pointer: fine)').matches) num.focus({ preventScroll: true }); }, 80);
    ok.addEventListener('click', () => { if (!locked) { const r = evaluate(); api.submit(r.accuracy, r.detail); } });
    const evaluate = () => ({ accuracy: Math.max(0, 1 - Math.abs(value - q.tacno) / q.tolerancija), detail: { value } });

    body.append(
      h('div', { class: 'est-readout' }, out, h('span', { class: 'est-unit' }, unit(q))),
      h('div', { class: 'est-track' }, range, marks),
      h('div', { class: 'est-scale' }, h('span', {}, fmt(q, q.min)), h('span', {}, fmt(q, q.max))),
      h('div', { class: 'row-bar' }, h('div', { class: 'nudges' }, nudge(-1), nudge(1), h('label', { class: 'est-type', for: 'est-num' }, 'ili upiši:'), num), ok)
    );

    return {
      lock() { locked = true; range.disabled = true; num.disabled = true; ok.disabled = true; body.querySelectorAll('.nudge').forEach(b => (b.disabled = true)); },
      timeout: () => (moved ? evaluate() : { accuracy: 0, detail: {} }),
      reveal(res) {
        marks.append(h('i', { class: 'mark right', style: { left: pos(q.tacno) } }, h('span', {}, fmt(q, q.tacno))));
        if (res.detail.value != null) marks.append(h('i', { class: 'mark mine', style: { left: pos(res.detail.value) } }));
        ok.hidden = true;
      }
    };
  },

  simulateBot(q, p, rng) {
    const err = gaussian(rng) * q.tolerancija * (1.1 - p) * 0.75;
    return Math.max(0, 1 - Math.abs(err) / q.tolerancija);
  },
  summarize(r) {
    if (r.detail?.value != null) return `${fmt(r.q, r.detail.value)} (${Math.round(r.accuracy * 100)}%)`;
    return `${Math.round(r.accuracy * 100)}% tačno`;
  }
};
