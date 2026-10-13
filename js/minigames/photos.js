// Mini igra: Gdje je ovo? Prava fotografija mjesta ili spomenika (Wikipedia), biraš gdje se nalazi.
// Tvoje fotografije možeš dodati u data/mjesta.json (polja slika, odgovor, autor, licenca); one imaju prednost.
import { h } from '../ui.js';
import { shuffle } from '../util.js';
import { playChoiceRounds, simulate, pickOptions } from './kit.js';
import { wikiImage, preloadImage } from '../media.js';

// [Wikipedia naslov (engleski), odgovor]
const PLACES = [
  ['Sveti_Stefan', 'Sveti Stefan, Crna Gora'], ['Our_Lady_of_the_Rocks', 'Perast, Crna Gora'], ['Kotor', 'Kotor, Crna Gora'],
  ['Đurđevića_Tara_Bridge', 'Most na Đurđevića Tari, Crna Gora'], ['Ostrog_Monastery', 'Manastir Ostrog, Crna Gora'],
  ['Njegoš_Mausoleum', 'Lovćen, Crna Gora'], ['Durmitor', 'Durmitor, Crna Gora'], ['Stari_Bar', 'Stari Bar, Crna Gora'],
  ['Belgrade_Fortress', 'Kalemegdan, Beograd'], ['Church_of_Saint_Sava', 'Hram Svetog Save, Beograd'],
  ['Petrovaradin_Fortress', 'Petrovaradin, Novi Sad'], ['Studenica_Monastery', 'Manastir Studenica, Srbija'],
  ['Stari_Most', 'Stari most, Mostar'], ['Baščaršija', 'Baščaršija, Sarajevo'], ['Dubrovnik', 'Dubrovnik, Hrvatska'],
  ['Plitvice_Lakes_National_Park', 'Plitvička jezera, Hrvatska'], ['Lake_Bled', 'Bled, Slovenija'], ['Lake_Ohrid', 'Ohridsko jezero'],
  ['Eiffel_Tower', 'Ajfelova kula, Pariz'], ['Colosseum', 'Koloseum, Rim'], ['Leaning_Tower_of_Pisa', 'Krivi toranj, Piza'],
  ['Acropolis_of_Athens', 'Akropolj, Atina'], ['Sagrada_Família', 'Sagrada Familija, Barselona'], ['Big_Ben', 'Big Ben, London'],
  ['Tower_Bridge', 'Tauer bridž, London'], ['Brandenburg_Gate', 'Brandenburška kapija, Berlin'], ['Neuschwanstein_Castle', 'Nojšvanštajn, Njemačka'],
  ['Saint_Basil\'s_Cathedral', 'Crkva Vasilija Blaženog, Moskva'], ['Hagia_Sophia', 'Aja Sofija, Istanbul'], ['Charles_Bridge', 'Karlov most, Prag'],
  ['Statue_of_Liberty', 'Kip slobode, Njujork'], ['Golden_Gate_Bridge', 'Golden Gejt, San Francisko'], ['Grand_Canyon', 'Veliki kanjon, SAD'],
  ['Niagara_Falls', 'Nijagarini vodopadi'], ['Christ_the_Redeemer_(statue)', 'Hrist Iskupitelj, Rio de Žaneiro'], ['Machu_Picchu', 'Maču Pikču, Peru'],
  ['Chichen_Itza', 'Čičen Ica, Meksiko'], ['Great_Pyramid_of_Giza', 'Piramide u Gizi, Egipat'], ['Taj_Mahal', 'Tadž Mahal, Indija'],
  ['Great_Wall_of_China', 'Kineski zid'], ['Mount_Fuji', 'Fudži, Japan'], ['Petra', 'Petra, Jordan'], ['Burj_Khalifa', 'Burdž Kalifa, Dubai'],
  ['Sydney_Opera_House', 'Opera u Sidneju'], ['Angkor_Wat', 'Angkor Vat, Kambodža'], ['Mount_Everest', 'Mont Everest'],
  ['Stonehenge', 'Stonhendž, Engleska'], ['Victoria_Falls', 'Viktorijini vodopadi'], ['Matterhorn', 'Materhorn, Alpi'], ['Uluru', 'Uluru, Australija']
].map(([wiki, answer]) => ({ wiki, answer }));

const game = {
  id: 'mjesta', name: 'Gdje je ovo?', duration: 25, ready: false, own: [],
  intro: 'Pojavljuje se fotografija mjesta ili spomenika. Izaberi gdje se nalazi. Što više tačnih za 25 sekundi.',
  async init() {
    try {
      const r = await fetch('data/mjesta.json');
      if (r.ok) this.own = ((await r.json()).mjesta || []).filter(e => e.slika && e.odgovor);
    } catch { /* bez sopstvenih fotografija */ }
    // provjeri da li Wikipedia odgovara (npr. bez interneta igra se preskače)
    this.ready = this.own.length >= 6;
    wikiImage(PLACES[0].wiki).then(x => { if (x) this.ready = true; });
  },
  play(host, { rng = Math.random } = {}) {
    const all = [...this.own.map(o => ({ own: o, answer: o.odgovor })), ...PLACES];
    const answers = all.map(x => x.answer);
    let bag = shuffle([...all], rng);
    return playChoiceRounds(host, {
      duration: this.duration, goal: 6,
      next: async () => {
        for (let k = 0; k < 6; k++) {
          if (!bag.length) bag = shuffle([...all], rng);
          const it = bag.pop();
          const img = it.own ? { src: it.own.slika, credit: [it.own.autor, it.own.licenca].filter(Boolean).join(' · ') } : await wikiImage(it.wiki);
          if (!img || !(await preloadImage(img.src))) continue;
          const stage = h('div', { class: 'photo-frame' }, h('img', { src: img.src, alt: 'Fotografija mjesta koje treba prepoznati' }),
            img.credit ? (img.page ? h('a', { class: 'photo-credit', href: img.page, target: '_blank', rel: 'noopener' }, img.credit) : h('span', { class: 'photo-credit' }, img.credit)) : null);
          return { stage, options: pickOptions(rng, it.answer, answers), answer: it.answer };
        }
        return { stage: h('p', { class: 'lede small' }, 'Fotografije trenutno nisu dostupne.'), options: pickOptions(rng, answers[0], answers), answer: answers[0] };
      }
    });
  },
  simulateBot: simulate(6, 'tačnih')
};
export default game;
