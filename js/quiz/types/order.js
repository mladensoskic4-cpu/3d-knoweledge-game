// Tip pitanja: poredaj stavke. Tapni stavke redom: prva tapnuta ide na mjesto 1, sljedeća na 2…
// Mjesta su numerisana, a krajevi su jasno označeni (npr. „1 · najstariji” … „4 · najnoviji”).
// Tap na popunjeno mjesto vraća stavku nazad. Bodovi = udio parova u tačnom redoslijedu.
import { h } from '../../ui.js';
import { shuffle, clamp, gaussian } from '../../util.js';
import { getLang } from '../../i18n.js';

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

// Natpisi za prvo i posljednje mjesto, izvedeni iz teksta pitanja.
const ENDS = [
  [/najmanj|smallest|least|fewest/i, ['najmanji', 'najveći'], ['smallest', 'largest']],
  [/najveć|largest|biggest|most\b/i, ['najveći', 'najmanji'], ['largest', 'smallest']],
  [/najranij|earliest/i, ['najraniji', 'najkasniji'], ['earliest', 'latest']],
  [/najstarij|hronolo|godini|redu izla|premijer|oldest|chronolog|release|year/i, ['najstariji', 'najnoviji'], ['oldest', 'newest']],
  [/najbliž|closest|nearest/i, ['najbliži', 'najdalji'], ['closest', 'farthest']],
  [/najviš|highest|tallest/i, ['najviši', 'najniži'], ['highest', 'lowest']],
  [/najduž|longest/i, ['najduži', 'najkraći'], ['longest', 'shortest']],
  [/najkrać|shortest/i, ['najkraći', 'najduži'], ['shortest', 'longest']],
  [/najbrž|fastest/i, ['najbrži', 'najsporiji'], ['fastest', 'slowest']],
  [/najšir|widest/i, ['najširi', 'najuži'], ['widest', 'narrowest']],
  [/najljuć|hottest|spiciest/i, ['najljući', 'najblaži'], ['hottest', 'mildest']],
  [/sjevera ka jugu|north to south/i, ['sjever', 'jug'], ['north', 'south']],
  [/zapada ka istoku|west to east/i, ['zapad', 'istok'], ['west', 'east']],
  [/uzlazno|ascending|lowest/i, ['najniži', 'najviši'], ['lowest', 'highest']],
  [/tla naviše|ground up/i, ['dno', 'vrh'], ['bottom', 'top']],
  [/od sunca|from the sun/i, ['najbliža Suncu', 'najdalja'], ['closest to the Sun', 'farthest']]
];
export function endLabels(text) {
  const en = getLang() === 'en';
  // pravilo čija se riječ prva pojavljuje u tekstu („od najvećeg do najmanjeg” → najveći)
  let best = null, at = Infinity;
  for (const r of ENDS) { const m = text.match(r[0]); if (m && m.index < at) { at = m.index; best = r; } }
  if (best) return en ? best[2] : best[1];
  return en ? ['first', 'last'] : ['prvo', 'posljednje'];
}

export default {
  id: 'poredaj',
  label: 'Poredaj',
  timeLimit: 25,

  mount(q, body, api) {
    const correct = q.tacno, n = correct.length;
    let start = shuffle([...correct], api.rng);
    for (let k = 0; k < 5 && start.every((v, i) => v === correct[i]); k++) start = shuffle([...correct], api.rng);
    const [top, bottom] = endLabels(q.pitanje || '');
    const placed = Array(n).fill(null);
    let locked = false;

    const slots = h('ol', { class: 'ord-slots' });
    const pool = h('div', { class: 'ord-pool' });
    const chips = new Map();
    const tagOf = i => (i === 0 ? `▲ ${top}` : i === n - 1 ? `▼ ${bottom}` : '');
    const slotEls = correct.map((_, i) => {
      const el = h('li', { class: `ord-slot${tagOf(i) ? ' end' : ''}`, 'data-i': String(i) },
        h('span', { class: 'ord-num' }, String(i + 1)),
        h('span', { class: 'ord-fill' }),
        h('span', { class: 'ord-badge' }));
      el.addEventListener('click', () => {
        if (locked || placed[i] == null) return;
        const v = placed[i]; placed[i] = null;
        chips.get(v).hidden = false;
        render();
      });
      slots.append(el);
      return el;
    });
    start.forEach(v => {
      const chip = h('button', { type: 'button', class: 'ord-chip', 'data-v': v }, v);
      chip.addEventListener('click', () => {
        if (locked) return;
        const i = placed.indexOf(null);
        if (i < 0) return;
        placed[i] = v; chip.hidden = true;
        // posljednja stavka sama ide na posljednje slobodno mjesto
        const left = [...chips.values()].filter(c => !c.hidden);
        if (left.length === 1) { placed[placed.indexOf(null)] = left[0].dataset.v; left[0].hidden = true; }
        render();
      });
      chips.set(v, chip); pool.append(chip);
    });

    const clear = h('button', { class: 'btn btn-ghost', type: 'button', id: 'ord-clear' }, 'Poništi');
    const ok = h('button', { class: 'btn btn-primary', type: 'button', id: 'ord-ok' }, 'Potvrdi');
    const note = h('span', { class: 'map-hint' }, 'Tapni stavke redom: prva ide na mjesto 1.');
    function render() {
      slotEls.forEach((el, i) => {
        const v = placed[i];
        el.classList.toggle('full', v != null);
        el.querySelector('.ord-fill').replaceChildren(
          v != null ? h('span', { class: 'ord-txt' }, v) : h('span', { class: 'ord-ph' }, tagOf(i) || '…'),
          v != null && tagOf(i) ? h('small', { class: 'ord-end' }, tagOf(i)) : '');
      });
      const next = placed.indexOf(null);
      slotEls.forEach((el, i) => el.classList.toggle('next', i === next));
      ok.disabled = next >= 0;
      pool.hidden = next < 0;
    }
    clear.addEventListener('click', () => {
      if (locked) return;
      placed.fill(null); chips.forEach(c => (c.hidden = false)); render();
    });
    ok.addEventListener('click', () => { if (!locked && !placed.includes(null)) api.submit(score(placed, correct), { order: [...placed] }); });
    body.append(h('div', { class: 'ord-wrap' }, slots, pool), h('div', { class: 'row-bar' }, note, clear, ok));
    render();

    const current = () => [...placed.filter(v => v != null), ...[...chips.values()].filter(c => !c.hidden).map(c => c.dataset.v)];
    return {
      lock() { locked = true; ok.disabled = true; clear.disabled = true; chips.forEach(c => (c.disabled = true)); slots.classList.add('locked'); },
      timeout: () => (placed.some(v => v != null) ? { accuracy: score(current(), correct), detail: { order: current() } } : { accuracy: 0, detail: {} }),
      reveal() {
        const order = current();
        order.forEach((v, i) => { placed[i] = v; });
        chips.forEach(c => (c.hidden = true));
        render();
        slotEls.forEach((el, i) => {
          const ci = correct.indexOf(placed[i]);
          el.classList.add(ci === i ? 'is-right' : 'is-wrong');
          el.querySelector('.ord-badge').textContent = ci === i
            ? (q.oznake ? `✓ ${q.oznake[ci]}` : '✓')
            : `→ ${ci + 1}.${q.oznake ? ` · ${q.oznake[ci]}` : ''}`;
        });
        ok.hidden = true; clear.hidden = true;
        note.textContent = 'Strelica s brojem pokazuje tačno mjesto stavke.';
      }
    };
  },

  simulateBot: (q, p, rng) => clamp(p * 1.05 + gaussian(rng) * 0.2, 0, 1),
  summarize: r => (r.accuracy >= 1 ? 'sve tačno' : `${Math.round(r.accuracy * 100)}%`)
};
