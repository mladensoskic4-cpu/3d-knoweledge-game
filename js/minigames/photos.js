// Mini igra: Gdje je ovo? Prava fotografija poznatog mjesta, biraš koje je mjesto.
// Podaci: mjesta/podaci.json (skinuto skriptom tools/skini_sve_slike.py). Ponuđeni odgovori su sa istog kontinenta.
import { playChoiceRounds, simulate } from './kit.js';
import { shuffle } from '../util.js';
import { loadSlike, photoFrame, preload, optionsFor, imeStavke } from '../slike.js';

export function photoChoiceGame({ id, name, intro, category, key, goal = 6, duration = 25, alt }) {
  return {
    id, name, intro, duration, ready: false, list: [],
    async init() { this.list = (await loadSlike())[category]; this.ready = this.list.length >= 6; },
    play(host, { rng = Math.random } = {}) {
      const list = this.list;
      let bag = shuffle([...list], rng);
      return playChoiceRounds(host, {
        duration: this.duration, goal,
        next: async () => {
          let it = null;
          for (let k = 0; k < 5 && !it; k++) {
            if (!bag.length) bag = shuffle([...list], rng);
            const c = bag.pop();
            if (await preload(c.slika)) it = c;
          }
          it ||= list[0];
          return { stage: photoFrame(it, { alt }), options: optionsFor(rng, it, list, key), answer: imeStavke(it) };
        }
      });
    },
    simulateBot: simulate(goal, 'tačnih')
  };
}

export default photoChoiceGame({
  id: 'mjesta', name: 'Gdje je ovo?', category: 'mjesta', key: 'kontinent',
  intro: 'Pojavljuje se fotografija poznatog mjesta ili spomenika. Izaberi koje je to mjesto. Što više tačnih za 25 sekundi.',
  alt: 'Fotografija mjesta koje treba prepoznati'
});

export const tijelo = photoChoiceGame({
  id: 'tijelo', name: 'Koji je ovo dio tijela?', category: 'tijelo', key: 'sistem', goal: 7,
  intro: 'Pojavljuje se slika dijela ljudskog tijela. Izaberi koji je. Što više tačnih za 25 sekundi.',
  alt: 'Slika dijela ljudskog tijela'
});
