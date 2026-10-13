// Slike sa slobodnom licencom (Wikimedia Commons), skinute skriptom tools/skini_sve_slike.py.
// Svaka kategorija ima svoj folder u korijenu igre: <kategorija>/podaci.json + <kategorija>/slike/.
// Polje "slika" je putanja od korijena igre, pa se koristi direktno. Ništa se ne upisuje ručno u kod.
import { h } from './ui.js';
import { getLang } from './i18n.js';

export const KATEGORIJE = {
  mjesta: 'Poznata mjesta',
  zivotinje: 'Životinje',
  tijelo: 'Ljudsko tijelo',
  instrumenti: 'Muzički instrumenti'
};

let job = null;
// Vraća { mjesta: [...], zivotinje: [...], tijelo: [...], instrumenti: [...] }; kategorija bez fajla je prazna lista.
export function loadSlike() {
  job ||= Promise.all(Object.keys(KATEGORIJE).map(k =>
    fetch(`${k}/podaci.json`).then(r => (r.ok ? r.json() : [])).catch(() => [])))
    .then(all => Object.fromEntries(Object.keys(KATEGORIJE).map((k, i) =>
      [k, (Array.isArray(all[i]) ? all[i] : []).filter(x => x && x.slika && x.naziv && !x.iskljuceno)])));
  return job;
}

// Naziv za prikaz: na engleskom koristi naslov članka sa engleske Wikipedije (bez zagrada).
export function imeStavke(x) {
  if (getLang() === 'en' && x.wikipedia) return x.wikipedia.replace(/\s*\([^)]*\)\s*$/, '');
  return x.naziv;
}

// Kratka oznaka autora preko slike. Bez linka, da adresa fajla ne oda odgovor; puni podaci su na ekranu „Zasluge”.
export function creditText(x) {
  const autor = (x.autor || '').replace(/\s+/g, ' ').trim();
  return `Foto: ${autor.length > 40 ? autor.slice(0, 38) + '…' : autor || 'nepoznat autor'} · ${x.licenca || ''}`;
}

export function preload(src) {
  return new Promise(res => { const i = new Image(); i.onload = () => res(true); i.onerror = () => res(false); i.src = src; });
}

// Okvir sa slikom: slika zadržava proporcije i staje u okvir (bez sječenja).
export function photoFrame(x, { alt = 'Šta je na slici?', small = false } = {}) {
  return h('figure', { class: `pic-frame${small ? ' small' : ''}` },
    h('img', { src: x.slika, alt, decoding: 'async', draggable: 'false' }),
    h('figcaption', { class: 'pic-credit', translate: 'no' }, creditText(x)));
}

// Ponuđeni odgovori: tačan + 3 slična (isti kontinent / grupa / sistem / vrsta), da ne bude prelako.
export function optionsFor(rng, item, list, key) {
  const same = list.filter(x => x !== item && key && x[key] && x[key] === item[key]);
  const other = list.filter(x => x !== item && !same.includes(x));
  const pick = (arr, n) => { const a = arr.slice(), out = []; while (a.length && out.length < n) out.push(a.splice(Math.floor(rng() * a.length), 1)[0]); return out; };
  const d = pick(same, 2);
  d.push(...pick(other.concat(same.filter(x => !d.includes(x))), 3 - d.length));
  const names = [item, ...d].map(imeStavke);
  for (let i = names.length - 1; i > 0; i--) { const j = Math.floor(rng() * (i + 1)); [names[i], names[j]] = [names[j], names[i]]; }
  return names;
}
