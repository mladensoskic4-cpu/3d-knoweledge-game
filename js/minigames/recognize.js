// Mini igre „Prepoznaj”: instrumenti i životinje, naizmjenično po ZVUKU i po pravoj FOTOGRAFIJI.
// Zvuk instrumenata: FluidR3 GM (CC BY 3.0). Zvuk životinja: snimci iz Wikipedia članka o životinji (Wikimedia Commons). Fotografije: Wikipedia.
// Ako fotografija ne može da se učita, koristi se ilustracija (Microsoft Fluent Emoji, MIT).
import { h } from '../ui.js';
import { shuffle } from '../util.js';
import { playChoiceRounds, simulate, pickOptions } from './kit.js';
import { wikiImage, preloadImage, wikiSound, playInstrument, playUrl, stopSounds } from '../media.js';
import { loadSlike, photoFrame, preload, optionsFor, imeStavke } from '../slike.js';

// [naziv, folder sa zvukom, Wikipedia naslov, ključ ilustracije]
export const INSTRUMENTS = [
  ['Klavir', 'acoustic_grand_piano', 'Piano', 'Klavijature'], ['Violina', 'violin', 'Violin', 'Violina'], ['Violončelo', 'cello', 'Cello'],
  ['Kontrabas', 'contrabass', 'Double_bass'], ['Truba', 'trumpet', 'Trumpet', 'Truba'], ['Trombon', 'trombone', 'Trombone'],
  ['Horna', 'french_horn', 'French_horn', 'Rog'], ['Tuba', 'tuba', 'Tuba'], ['Flauta', 'flute', 'Western_concert_flute', 'Frula'],
  ['Panova frula', 'pan_flute', 'Pan_flute'], ['Klarinet', 'clarinet', 'Clarinet'], ['Oboa', 'oboe', 'Oboe'],
  ['Saksofon', 'alto_sax', 'Saxophone', 'Saksofon'], ['Klasična gitara', 'acoustic_guitar_nylon', 'Classical_guitar'],
  ['Električna gitara', 'electric_guitar_clean', 'Electric_guitar', 'Gitara'], ['Bendžo', 'banjo', 'Banjo', 'Bendžo'],
  ['Sitar', 'sitar', 'Sitar'], ['Harmonika', 'accordion', 'Accordion', 'Harmonika'], ['Usna harmonika', 'harmonica', 'Harmonica'],
  ['Gajde', 'bagpipe', 'Bagpipes'], ['Orgulje', 'church_organ', 'Pipe_organ'], ['Harfa', 'orchestral_harp', 'Harp'],
  ['Ksilofon', 'xylophone', 'Xylophone'], ['Marimba', 'marimba', 'Marimba'], ['Timpani', 'timpani', 'Timpani'],
  ['Čelični bubanj', 'steel_drums', 'Steelpan']
].map(([name, folder, wiki, ilu]) => ({ name, folder, wiki, ilu }));

// [naziv, Wikipedia naslov (za fotografiju), članci u kojima tražimo snimak, ime u nazivu fajla (regex)]
// Bez zvuka (null) = samo fotografija.
const S = (articles, names) => ({ articles, rx: new RegExp(`\\b(${names})`, 'i') });
export const ANIMALS = [
  ['Lav', 'Lion', S(['Lion'], 'lions?\\b|panthera leo')], ['Vuk', 'Wolf', S(['Wolf', 'Howling'], 'wol(f|ves)\\b|canis lupus')],
  ['Pas', 'Dog', S(['Dog', 'Bark (sound)'], 'dogs?\\b|canis familiaris|puppy')], ['Mačka', 'Cat', S(['Cat', 'Meow', 'Purr'], 'cats?\\b|felis catus|kitten')],
  ['Krava', 'Cattle', S(['Cattle', 'Moo'], 'cows?\\b|cattle|bos taurus')], ['Konj', 'Horse', S(['Horse', 'Neigh'], 'horses?\\b|equus ferus|neigh')],
  ['Ovca', 'Sheep', S(['Sheep'], 'sheep|ovis aries|lambs?\\b')], ['Koza', 'Goat', S(['Goat'], 'goats?\\b|capra hircus')],
  ['Svinja', 'Pig', S(['Pig', 'Domestic pig'], 'pigs?\\b|sus scrofa|piglets?\\b')], ['Pijetao', 'Chicken', S(['Chicken', 'Rooster', 'Crowing'], 'roosters?\\b|cockerel|chickens?\\b|gallus gallus|cock crow')],
  ['Patka', 'Duck', S(['Duck', 'Mallard', 'Quack'], 'ducks?\\b|mallard|anas platyrhynchos')], ['Magarac', 'Donkey', S(['Donkey', 'Bray (sound)'], 'donkeys?\\b|equus asinus|burro')],
  ['Slon', 'Elephant', S(['Elephant', 'African bush elephant', 'Asian elephant'], 'elephants?\\b|loxodonta|elephas')], ['Sova', 'Tawny_owl', S(['Tawny owl'], 'strix aluco|tawny owl')],
  ['Žaba', 'Common_frog', S(['Common frog', 'Frog'], 'frogs?\\b|rana temporaria|rana\\b')], ['Kukavica', 'Common_cuckoo', S(['Common cuckoo'], 'cuculus canorus|cuckoo')],
  ['Delfin', 'Common_bottlenose_dolphin', S(['Common bottlenose dolphin', 'Dolphin'], 'dolphins?\\b|tursiops')], ['Gavran', 'Common_raven', S(['Common raven'], 'corvus corax|raven')],
  ['Pčela', 'Western_honey_bee', S(['Western honey bee', 'Honey bee'], 'bees?\\b|apis mellifera|honeybee')], ['Cvrčak', 'Field_cricket', S(['Field cricket', 'Cricket (insect)'], 'crickets?\\b|gryllus')],
  ['Galeb', 'European_herring_gull', S(['European herring gull', 'Gull'], 'gulls?\\b|larus')], ['Tigar', 'Tiger', S(['Tiger'], 'tigers?\\b|panthera tigris')],
  ['Kit grbavac', 'Humpback_whale', S(['Humpback whale', 'Whale vocalization'], 'humpback|megaptera')], ['Guska', 'Goose', S(['Goose', 'Greylag goose'], 'goose|geese|anser\\b')],
  ['Ćurka', 'Wild_turkey', S(['Wild turkey', 'Domestic turkey'], 'turkeys?\\b|meleagris')],
  ['Žirafa', 'Giraffe', null], ['Zebra', 'Zebra', null], ['Kengur', 'Kangaroo', null],
  ['Koala', 'Koala', null], ['Panda', 'Giant_panda', null], ['Pingvin', 'Emperor_penguin', null], ['Orao', 'Golden_eagle', null],
  ['Flamingo', 'Flamingo', null], ['Paun', 'Indian_peafowl', null], ['Ajkula', 'Great_white_shark', null], ['Hobotnica', 'Octopus', null],
  ['Krokodil', 'Nile_crocodile', null], ['Kornjača', 'Turtle', null], ['Jež', 'European_hedgehog', null], ['Lisica', 'Red_fox', null],
  ['Jelen', 'Red_deer', null], ['Nosorog', 'Rhinoceros', null], ['Nilski konj', 'Hippopotamus', null], ['Gorila', 'Gorilla', null],
  ['Lijenčina', 'Sloth', null], ['Dabar', 'Beaver', null], ['Lama', 'Llama', null], ['Medvjed', 'Brown_bear', null],
  ['Polarni medvjed', 'Polar_bear', null], ['Leptir', 'Butterfly', null], ['Bubamara', 'Coccinella_septempunctata', null],
  ['Foka', 'Harbor_seal', null], ['Kamila', 'Bactrian_camel', null], ['Ris', 'Eurasian_lynx', null], ['Divokoza', 'Chamois', null]
].map(([name, wiki, sound]) => ({ name, wiki, sound }));

let ILU = null;
async function illustrations() {
  if (ILU) return ILU;
  try { ILU = await (await fetch('data/ilustracije.json')).json(); } catch { ILU = { instrumenti: {}, zivotinje: {} }; }
  return ILU;
}

function soundStage(replay) {
  const btn = h('button', { type: 'button', class: 'btn btn-ghost', id: 'rec-replay' }, 'Pusti ponovo');
  btn.addEventListener('click', replay);
  return h('div', { class: 'mel-stage rec-sound' }, h('div', { class: 'eq on', 'aria-hidden': 'true' }, [0, 1, 2, 3, 4, 5, 6].map(() => h('i'))),
    h('div', { class: 'mel-ctrl' }, h('span', { class: 'map-hint' }, 'Slušaj: šta je ovo?'), btn));
}
async function photoStage(item, iluGroup) {
  const img = await wikiImage(item.wiki);
  if (img && await preloadImage(img.src)) {
    return h('div', { class: 'photo-frame rec-photo' }, h('img', { src: img.src, alt: 'Šta je na fotografiji?' }),
      h('a', { class: 'photo-credit', href: img.page, target: '_blank', rel: 'noopener' }, img.credit));
  }
  const ilu = (await illustrations())[iluGroup]?.[item.ilu || item.name];
  if (ilu) return h('div', { class: 'rec-frame' }, h('img', { class: 'rec-img', src: ilu, alt: 'Šta je na slici?' }));
  return null;
}

function makeGame({ id, name, intro, list, iluGroup, playSound, canSound, category, key }) {
  return {
    id, name, intro, duration: 25, local: [],
    // fotografije iz <kategorija>/podaci.json imaju prednost nad slikama sa Wikipedije
    async init() { this.local = (await loadSlike())[category] || []; },
    play(host, { rng = Math.random } = {}) {
      const local = this.local.length >= 6 ? this.local : null;
      let bag = shuffle([...list], rng), lbag = [], turn = 0;
      async function localRound() {
        for (let k = 0; k < 5; k++) {
          if (!lbag.length) lbag = shuffle([...local], rng);
          const it = lbag.pop();
          if (await preload(it.slika)) return { stage: photoFrame(it), options: optionsFor(rng, it, local, key), answer: imeStavke(it) };
        }
        return null;
      }
      return playChoiceRounds(host, {
        duration: this.duration, goal: 7,
        next: async () => {
          for (let tries = 0; tries < 6; tries++) {
            if (!bag.length) bag = shuffle([...list], rng);
            const it = bag.pop();
            const wantSound = turn % 2 === 0 && canSound(it);
            if (wantSound) {
              const play = await playSound(it);
              if (play) {
                turn++;
                const pool = list.filter(canSound).map(x => x.name);
                return { stage: soundStage(play), options: pickOptions(rng, it.name, pool), answer: it.name, onShow: play, onDone: stopSounds };
              }
            }
            if (local) { const r = await localRound(); if (r) { turn++; return r; } }
            const stage = await photoStage(it, iluGroup);
            if (stage) { turn++; return { stage, options: pickOptions(rng, it.name, list.map(x => x.name)), answer: it.name }; }
          }
          const it = list[0];
          return { stage: h('p', { class: 'lede small' }, 'Slike trenutno nisu dostupne.'), options: pickOptions(rng, it.name, list.map(x => x.name)), answer: it.name };
        }
      });
    },
    simulateBot: simulate(7, 'tačnih')
  };
}

export const instrumenti = makeGame({
  id: 'instrumenti', name: 'Koji je ovo instrument?', list: INSTRUMENTS, iluGroup: 'instrumenti', category: 'instrumenti', key: 'vrsta',
  intro: 'Naizmjenično čuješ zvuk instrumenta ili vidiš njegovu fotografiju. Izaberi koji je instrument. Što više tačnih za 25 sekundi.',
  canSound: () => true,
  playSound: async it => () => playInstrument(it.folder)
});

export const zivotinje = makeGame({
  id: 'zivotinje', name: 'Koja je ovo životinja?', list: ANIMALS, iluGroup: 'zivotinje', category: 'zivotinje', key: 'grupa',
  intro: 'Naizmjenično čuješ glas životinje ili vidiš njenu fotografiju. Izaberi koja je životinja. Što više tačnih za 25 sekundi.',
  canSound: it => !!it.sound,
  playSound: async it => {
    const s = await wikiSound(it.sound.articles, it.sound.rx);
    return s ? () => playUrl(s.src) : null;
  }
});
