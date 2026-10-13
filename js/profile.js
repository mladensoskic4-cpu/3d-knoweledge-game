// Profil igrača (čuva se na uređaju, kao gost): ime, novčići, rang-bodovi, kupljena oprema, izgled i podešavanja.
// Kad se poveže nalog (Supabase), isti objekat se samo sinhronizuje.
import { DEFAULT_LOOK } from './models.js';

const KEY = 'trka-durmitor-profil-v1';

export const RANKS = [
  { id: 'bronza', label: 'Bronza', min: 0, color: '#b0703c' },
  { id: 'srebro', label: 'Srebro', min: 250, color: '#8f9ba6' },
  { id: 'zlato', label: 'Zlato', min: 700, color: '#d9a21b' },
  { id: 'platina', label: 'Platina', min: 1400, color: '#3aa7a7' },
  { id: 'dijamant', label: 'Dijamant', min: 2500, color: '#5b7cfa' }
];
export const rankIndex = id => RANKS.findIndex(r => r.id === id);
export function rankOf(points) {
  let r = RANKS[0];
  for (const x of RANKS) if (points >= x.min) r = x;
  return r;
}
export function rankProgress(points) {
  const i = rankIndex(rankOf(points).id), next = RANKS[i + 1];
  if (!next) return { pct: 1, next: null, need: 0 };
  const cur = RANKS[i];
  return { pct: (points - cur.min) / (next.min - cur.min), next, need: next.min - points };
}

export const AGES = {
  djeca: { label: 'Djeca', note: 'do 12 godina · lakša pitanja, više vremena', maxTezina: 1, bonus: 6 },
  tinejdzeri: { label: 'Tinejdžeri', note: '13–17 · bez najtežih pitanja', maxTezina: 2, bonus: 2 },
  odrasli: { label: 'Odrasli', note: 'sva pitanja', maxTezina: 3, bonus: 0 }
};

// Prodavnica. slot = gdje se predmet stavlja; rank = minimalni rang; za = samo za određeno vozilo.
export const SHOP = [
  { id: 'v-auto', slot: 'vozilo', value: 'auto', label: 'Auto', price: 0 },
  { id: 'v-konj', slot: 'vozilo', value: 'konj', label: 'Konj', price: 300 },
  { id: 'v-kamion', slot: 'vozilo', value: 'kamion', label: 'Kamion', price: 500, rank: 'srebro' },
  ...[['#e0312f', 'Crvena', 0], ['#2b7de9', 'Plava', 0], ['#16a36a', 'Zelena', 40], ['#f08a1c', 'Narandžasta', 40],
    ['#9b4bd6', 'Ljubičasta', 80], ['#14232e', 'Crna', 100], ['#f4f4f4', 'Bijela', 100], ['#8b5a2b', 'Smeđa', 60],
    ['#e3b23c', 'Zlatna', 400, 'zlato'], ['#5b7cfa', 'Dijamantska', 800, 'dijamant']]
    .map(([v, l, p, r]) => ({ id: `b-${v}`, slot: 'boja', value: v, label: l, price: p, rank: r, swatch: v })),
  ...[['#e8b48f', 'Svijetla'], ['#f1c7a5', 'Vrlo svijetla'], ['#c98e66', 'Tamnija'], ['#8d5a3b', 'Tamna']]
    .map(([v, l]) => ({ id: `k-${v}`, slot: 'koza', value: v, label: l, price: 0, swatch: v })),
  ...[['#2b7de9', 'Plava', 0], ['#e0312f', 'Crvena', 0], ['#16a36a', 'Zelena', 30], ['#f2b630', 'Žuta', 30], ['#14232e', 'Crna', 50], ['#ffffff', 'Bijela', 50]]
    .map(([v, l, p]) => ({ id: `m-${v}`, slot: 'majica', value: v, label: `Majica: ${l}`, price: p, swatch: v })),
  { id: 'h-nema', slot: 'kapa', value: 'nema', label: 'Bez kape', price: 0 },
  { id: 'h-kacket', slot: 'kapa', value: 'kacket', label: 'Kačket', price: 60 },
  { id: 'h-sesir', slot: 'kapa', value: 'sesir', label: 'Šešir', price: 120 },
  { id: 'h-kapa', slot: 'kapa', value: 'kapa', label: 'Tradicionalna kapa', price: 150 },
  { id: 'h-kaciga', slot: 'kapa', value: 'kaciga', label: 'Kaciga', price: 200, rank: 'srebro' },
  { id: 'h-kruna', slot: 'kapa', value: 'kruna', label: 'Kruna', price: 1000, rank: 'dijamant' },
  { id: 'n-ne', slot: 'naocare', value: false, label: 'Bez naočara', price: 0 },
  { id: 'n-da', slot: 'naocare', value: true, label: 'Naočare za sunce', price: 80 },
  { id: 'd-zastavica', slot: 'dodatak', value: 'zastavica', label: 'Zastavica', price: 150 },
  { id: 'd-turbo', slot: 'dodatak', value: 'turbo', label: 'Turbo plamen', price: 250, rank: 'srebro' },
  { id: 'd-spojler', slot: 'dodatak', value: 'spojler', label: 'Spojler', price: 200, za: 'auto' },
  { id: 'd-sedlo', slot: 'dodatak', value: 'sedlo', label: 'Sedlo', price: 120, za: 'konj' },
  { id: 'd-sanduci', slot: 'dodatak', value: 'sanduci', label: 'Teret', price: 150, za: 'kamion' }
];

// Power-upovi: potrošni, kupuju se po komadu; u jednoj rundi može se koristiti svaki po jednom.
export const POWERUPS = [
  { id: 'pola', kind: 'fifty', label: '50/50', price: 40, opis: 'Uklanja dva netačna odgovora (pitanja sa ponuđenim odgovorima).' },
  { id: 'zamrzni', kind: 'freeze', label: 'Zamrzni tajmer', price: 50, opis: 'Zaustavlja vrijeme na 5 sekundi.' },
  { id: 'precica', kind: 'shortcut', label: 'Prečica', price: 80, opis: 'U ovoj rundi ideš 2 polja više.' },
  { id: 'uspori', kind: 'slow', label: 'Uspori protivnike', price: 70, opis: 'U ovoj rundi svaki protivnik ide polje manje.' }
];
export function buyPowerup(id) {
  const pu = POWERUPS.find(x => x.id === id);
  if (!pu || P.novcici < pu.price) return false;
  P.novcici -= pu.price; P.powerupi[id] = (P.powerupi[id] || 0) + 1; save();
  return true;
}
export function usePowerup(id) { if (P.powerupi[id] > 0) { P.powerupi[id]--; save(); } }

const FREE = SHOP.filter(i => i.price === 0 && !i.rank).map(i => i.id);

function fresh() {
  return {
    ime: '', grad: '', skola: '', novcici: 150, bodovi: 0, kupljeno: [...FREE],
    powerupi: { pola: 1, zamrzni: 1, precica: 0, uspori: 0 }, istorija: [], predlozi: [],
    izgled: JSON.parse(JSON.stringify(DEFAULT_LOOK)),
    podesavanja: { jezik: 'lat', uzrast: 'odrasli', duzina: 'srednja', nivo: 'srednje', mapa: 'cg', oblasti: null, miks: true },
    statistika: { trke: 0, pobjede: 0, tacni: 0, odgovori: 0 }
  };
}

let P = null;
export function profile() {
  if (P) return P;
  P = fresh();
  try {
    const raw = localStorage.getItem(KEY);
    if (raw) {
      const s = JSON.parse(raw), f = fresh();
      P = { ...f, ...s, izgled: { ...f.izgled, ...s.izgled, lik: { ...f.izgled.lik, ...(s.izgled?.lik || {}) } },
        podesavanja: { ...f.podesavanja, ...s.podesavanja }, statistika: { ...f.statistika, ...s.statistika },
        powerupi: { ...f.powerupi, ...(s.powerupi || {}) } };
      P.kupljeno = [...new Set([...FREE, ...(s.kupljeno || [])])];
    }
  } catch { /* pregledač ne dozvoljava čuvanje; igra radi bez pamćenja */ }
  return P;
}
export function save() {
  try { localStorage.setItem(KEY, JSON.stringify(P)); } catch { /* čuvanje nije dostupno */ }
}

export const owns = id => profile().kupljeno.includes(id);
export function rankAllows(item) {
  return !item.rank || rankIndex(rankOf(profile().bodovi).id) >= rankIndex(item.rank);
}
export function isEquipped(item) {
  const l = profile().izgled;
  if (item.slot === 'vozilo') return l.vozilo === item.value;
  if (item.slot === 'boja') return l.boja === item.value;
  if (item.slot === 'koza') return l.lik.koza === item.value;
  if (item.slot === 'majica') return l.lik.majica === item.value;
  if (item.slot === 'kapa') return l.lik.kapa === item.value;
  if (item.slot === 'naocare') return l.lik.naocare === item.value;
  if (item.slot === 'dodatak') return (l.dodaci || []).includes(item.value);
  return false;
}
export function applyItem(look, item) {
  const l = JSON.parse(JSON.stringify(look));
  if (item.slot === 'vozilo') l.vozilo = item.value;
  else if (item.slot === 'boja') l.boja = item.value;
  else if (item.slot === 'koza') l.lik.koza = item.value;
  else if (item.slot === 'majica') l.lik.majica = item.value;
  else if (item.slot === 'kapa') l.lik.kapa = item.value;
  else if (item.slot === 'naocare') l.lik.naocare = item.value;
  else if (item.slot === 'dodatak') {
    const set = new Set(l.dodaci || []);
    set.has(item.value) ? set.delete(item.value) : set.add(item.value);
    l.dodaci = [...set];
  }
  return l;
}
export function equip(item) { P.izgled = applyItem(P.izgled, item); save(); }
export function buy(item) {
  if (owns(item.id)) return true;
  if (!rankAllows(item) || P.novcici < item.price) return false;
  P.novcici -= item.price; P.kupljeno.push(item.id); save();
  return true;
}

// Nagrade posle trke.
export function awardRace({ place, correct, answered, coins, won, mapa = '' }) {
  const before = rankOf(P.bodovi);
  const pts = [100, 60, 35, 15][place] + correct * 4;
  const placeCoins = [60, 40, 25, 10][place];
  P.bodovi += pts;
  P.novcici += coins + placeCoins;
  P.statistika.trke++; if (won) P.statistika.pobjede++;
  P.statistika.tacni += correct; P.statistika.odgovori += answered;
  P.istorija = [{ datum: new Date().toISOString(), mapa, mjesto: place + 1, bodovi: pts, tacni: correct, odgovori: answered }, ...(P.istorija || [])].slice(0, 50);
  save();
  const after = rankOf(P.bodovi);
  return { pts, coins: coins + placeCoins, rankUp: after.id !== before.id ? after : null };
}
