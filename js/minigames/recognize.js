// Mini igre „Prepoznaj”: instrumenti i životinje, naizmjenično po ZVUKU i po pravoj FOTOGRAFIJI.
// Zvuk instrumenata: FluidR3 GM (CC BY 3.0). Zvuk životinja: Wikimedia Commons. Fotografije: Wikipedia.
// Ako fotografija ne može da se učita, koristi se ilustracija (Microsoft Fluent Emoji, MIT).
import { h } from '../ui.js';
import { shuffle } from '../util.js';
import { playChoiceRounds, simulate, pickOptions } from './kit.js';
import { wikiImage, preloadImage, commonsSound, playInstrument, playUrl, stopSounds } from '../media.js';

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

// [naziv, Wikipedia naslov, pretraga zvuka (ili null ako je samo slika)]
export const ANIMALS = [
  ['Lav', 'Lion', 'lion roar'], ['Vuk', 'Wolf', 'wolf howl'], ['Pas', 'Dog', 'dog barking'], ['Mačka', 'Cat', 'cat meow'],
  ['Krava', 'Cattle', 'cow moo'], ['Konj', 'Horse', 'horse neigh'], ['Ovca', 'Sheep', 'sheep bleat'], ['Koza', 'Goat', 'goat bleat'],
  ['Svinja', 'Pig', 'pig grunt'], ['Pijetao', 'Chicken', 'rooster crowing'], ['Patka', 'Duck', 'duck quack'], ['Magarac', 'Donkey', 'donkey bray'],
  ['Slon', 'Elephant', 'elephant trumpeting'], ['Sova', 'Tawny_owl', 'tawny owl call'], ['Žaba', 'Common_frog', 'frog croaking'],
  ['Kukavica', 'Common_cuckoo', 'cuckoo call'], ['Delfin', 'Common_bottlenose_dolphin', 'dolphin whistle'], ['Gavran', 'Common_raven', 'raven call'],
  ['Pčela', 'Western_honey_bee', 'bee buzzing'], ['Cvrčak', 'Field_cricket', 'cricket chirping'], ['Galeb', 'European_herring_gull', 'herring gull call'],
  ['Tigar', 'Tiger', 'tiger growl'], ['Kit grbavac', 'Humpback_whale', 'humpback whale song'], ['Guska', 'Goose', 'goose honking'],
  ['Ćurka', 'Wild_turkey', 'turkey gobble'], ['Žirafa', 'Giraffe', null], ['Zebra', 'Zebra', null], ['Kengur', 'Kangaroo', null],
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

function makeGame({ id, name, intro, list, iluGroup, playSound, canSound }) {
  return {
    id, name, intro, duration: 25,
    play(host, { rng = Math.random } = {}) {
      let bag = shuffle([...list], rng), turn = 0;
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
  id: 'instrumenti', name: 'Koji je ovo instrument?', list: INSTRUMENTS, iluGroup: 'instrumenti',
  intro: 'Naizmjenično čuješ zvuk instrumenta ili vidiš njegovu fotografiju. Izaberi koji je instrument. Što više tačnih za 25 sekundi.',
  canSound: () => true,
  playSound: async it => () => playInstrument(it.folder)
});

export const zivotinje = makeGame({
  id: 'zivotinje', name: 'Koja je ovo životinja?', list: ANIMALS, iluGroup: 'zivotinje',
  intro: 'Naizmjenično čuješ glas životinje ili vidiš njenu fotografiju. Izaberi koja je životinja. Što više tačnih za 25 sekundi.',
  canSound: it => !!it.sound,
  playSound: async it => {
    const s = await commonsSound(it.sound);
    return s ? () => playUrl(s.src) : null;
  }
});
