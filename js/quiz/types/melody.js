// Tip pitanja: prepoznaj melodiju. Melodija svira u pozadini dok ne odgovoriš.
// Note su u pitanju (polje "note"), a opciono "audio" može biti putanja do snimka na koji imaš prava.
import { h } from '../../ui.js';
import { shuffle } from '../../util.js';
import { playMelody, audioContext } from '../../audio.js';

export default {
  id: 'melodija',
  label: 'Prepoznaj melodiju',
  timeLimit: 20,

  mount(q, body, api) {
    let player = null, audioEl = null, locked = false;
    const opts = shuffle([...q.opcije], api.rng);
    const bars = h('div', { class: 'eq', 'aria-hidden': 'true' }, [0, 1, 2, 3, 4, 5, 6].map(() => h('i')));
    const replay = h('button', { type: 'button', class: 'btn btn-ghost', id: 'mel-play' }, 'Pusti ponovo');
    const hint = h('span', { class: 'map-hint' }, 'Melodija svira dok ne odgovoriš.');

    function start() {
      stop();
      if (q.audio) {
        audioEl = new Audio(q.audio); audioEl.loop = true;
        audioEl.play().catch(() => { hint.textContent = 'Tapni „Pusti ponovo” da čuješ melodiju.'; });
      } else {
        audioContext();
        player = playMelody(q.note, { tempo: q.tempo || 110 });
        if (!player.playing) hint.textContent = 'Ovaj pregledač ne podržava zvuk.';
      }
      bars.classList.add('on');
    }
    function stop() {
      player?.stop(); player = null;
      if (audioEl) { audioEl.pause(); audioEl = null; }
      bars.classList.remove('on');
    }
    replay.addEventListener('click', () => { if (!locked) start(); });

    const grid = h('div', { class: 'mc-grid' });
    const btns = opts.map((o, i) => {
      const b = h('button', { class: 'mc-opt', type: 'button', id: `mel-${i}` }, h('span', { class: 'mc-key' }, String(i + 1)), h('span', { class: 'mc-txt' }, o));
      b.addEventListener('click', () => { if (!locked) api.submit(o === q.tacno ? 1 : 0, { chosen: o }); });
      grid.append(b);
      return b;
    });
    body.append(h('div', { class: 'mel-stage' }, bars, h('div', { class: 'mel-ctrl' }, hint, replay)), grid);
    const onKey = e => { const n = Number(e.key); if (n >= 1 && n <= btns.length) btns[n - 1].click(); };
    window.addEventListener('keydown', onKey);
    start();

    return {
      lock() { locked = true; btns.forEach(b => (b.disabled = true)); replay.disabled = true; window.removeEventListener('keydown', onKey); setTimeout(stop, 1200); },
      fifty() {
        const wrong = btns.filter((b, i) => opts[i] !== q.tacno && !b.disabled);
        wrong.sort(() => Math.random() - 0.5).slice(0, 2).forEach(b => { b.disabled = true; b.classList.add('gone'); });
      },
      timeout: () => ({ accuracy: 0, detail: { chosen: null } }),
      reveal(res) {
        btns.forEach((b, i) => {
          if (opts[i] === q.tacno) b.classList.add('is-right');
          else if (opts[i] === res.detail.chosen) b.classList.add('is-wrong');
        });
        hint.textContent = q.djelo ? `${q.djelo}` : '';
      }
    };
  },

  simulateBot: (q, p, rng) => (rng() < p ? 1 : 0),
  summarize: r => (r.accuracy >= 1 ? 'tačno' : 'netačno')
};
