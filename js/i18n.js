// Jezici: latinica (izvorno), ćirilica (automatska transliteracija) i engleski (interfejs).
// Tekst se prevodi kad se pojavi na ekranu (MutationObserver), pa moduli pišu samo latinicu.
let mode = 'lat';
export const LANGS = { lat: 'Latinica', cyr: 'Ћирилица', en: 'English' };
export const getLang = () => mode;

const DI = { Lj: 'Љ', LJ: 'Љ', lj: 'љ', Nj: 'Њ', NJ: 'Њ', nj: 'њ', 'Dž': 'Џ', 'DŽ': 'Џ', 'dž': 'џ' };
const SI = {
  a: 'а', b: 'б', c: 'ц', č: 'ч', ć: 'ћ', d: 'д', đ: 'ђ', e: 'е', f: 'ф', g: 'г', h: 'х', i: 'и', j: 'ј', k: 'к', l: 'л',
  m: 'м', n: 'н', o: 'о', p: 'п', r: 'р', s: 'с', š: 'ш', t: 'т', u: 'у', v: 'в', z: 'з', ž: 'ж'
};
Object.keys(SI).forEach(k => { SI[k.toUpperCase()] = SI[k].toUpperCase(); });
// Strane riječi i oznake koje ostaju latinicom.
const KEEP = new Set(['EXIT', 'MVP', 'GTA', 'Minecraft', 'Sea', 'Dance', 'Lovefest', 'Abbey', 'Road', 'Underground', 'Dream', 'Arizona',
  'Bros', 'Super', 'Mario', 'Film', 'Festival', 'Wiegenlied', 'AC', 'DC', 'U2', 'WWW', 'WWF', 'kcal', 'English', 'Latinica']);
const WORD = /[A-Za-zČčĆćĐđŠšŽžÅåÄäÖöÜüÉé₀-₉⁰-⁹¹²³⁻π]+/g;

function trWord(w) {
  if (KEEP.has(w)) return w;
  if (/[qwxyQWXYÅåÄäÖöÜüÉé]/.test(w)) return w;          // strana riječ
  if (/[₀-₉⁰-⁹¹²³⁻π]/.test(w)) return w;                    // formule (H₂O, a³)
  if (/^[IVXLCDM]+$/.test(w) && w.length <= 6) return w;   // rimski broj
  let out = '';
  for (let i = 0; i < w.length; i++) {
    const two = w.slice(i, i + 2);
    if (DI[two]) { out += DI[two]; i++; continue; }
    out += SI[w[i]] ?? w[i];
  }
  return out;
}
export function toCyr(text) {
  return text.replace(WORD, trWord);
}

// Engleski: prevod interfejsa. Pitanja su za sada na našem jeziku.
const EN_EXACT = {
  'Kreni na start': 'Start the race', 'Kreni': 'Go', 'Potvrdi': 'Confirm', 'Nastavi': 'Continue', 'Počni': 'Start',
  'Nova trka': 'New race', 'Promijeni podešavanja': 'Change settings', 'Pusti ponovo': 'Play again', 'Oblasti': 'Topics',
  'Botovi': 'Bots', 'Protivnici': 'Opponents', 'Tvoje ime': 'Your name', 'Uzrast': 'Age group', 'Dužina staze': 'Track length',
  'Mapa': 'Map', 'Jezik': 'Language', 'Garaža': 'Garage', 'Nazad': 'Back', 'Miks': 'Mix', 'sve dostupne oblasti': 'all topics',
  'Lako': 'Easy', 'Srednje': 'Medium', 'Teško': 'Hard', 'Kratka': 'Short', 'Srednja': 'Medium', 'Duga': 'Long',
  'Djeca': 'Kids', 'Tinejdžeri': 'Teens', 'Odrasli': 'Adults', 'Tačno!': 'Correct!', 'Netačno': 'Wrong', 'Djelimično': 'Partly right',
  'Odlično, blizu!': 'Great, close!', 'Isteklo vrijeme': 'Time is up', 'bez pomaka': 'no move', 'Višestruki izbor': 'Multiple choice',
  'Tapni na mapi': 'Tap the map', 'Procijeni broj': 'Estimate', 'Poredaj': 'Put in order', 'Spoji parove': 'Match pairs',
  'Mutna slika': 'Blurry picture', 'Prepoznaj melodiju': 'Name the tune', 'Pobjeda!': 'Victory!', 'Geografija': 'Geography',
  'Istorija': 'History', 'Sport': 'Sport', 'Muzika i pop kultura': 'Music & pop culture', 'Priroda i životinje': 'Nature & animals',
  'Hrana i običaji': 'Food & customs', 'Logika i matematika': 'Logic & math', 'Filmovi i serije': 'Movies & series', 'Koji je ovo instrument?': 'Which instrument is this?', 'Koja je ovo životinja?': 'Which animal is this?', 'Zastave u letu': 'Flying flags',
  'Glavni gradovi': 'Capitals', 'Gdje je ovo?': 'Where is this?', 'Lik': 'Character', 'Vozilo': 'Vehicle', 'Boja': 'Paint',
  'Kapa': 'Hat', 'Dodaci': 'Extras', 'Opremljeno': 'Equipped', 'Izaberi': 'Equip', 'Zaključano': 'Locked', 'Stanica': 'Station',
  'Tapni:': 'Tap:', 'Rang-lista': 'Leaderboard', 'Predloži pitanje': 'Suggest a question', 'Pošalji': 'Send', 'Danas': 'Today', 'Sedmica': 'Week', 'Ukupno': 'All time', 'Svi': 'Everyone', 'Moj grad': 'My city', 'Moja škola': 'My school', 'Prečica': 'Shortcut', 'Zamrzni tajmer': 'Freeze timer', 'Uspori protivnike': 'Slow rivals', 'Meni': 'Menu', 'Oblast': 'Topic', 'Pitanje': 'Question', 'Odgovori': 'Answers', 'Bronza': 'Bronze', 'Srebro': 'Silver', 'Zlato': 'Gold', 'Platina': 'Platinum', 'Dijamant': 'Diamond',
  'Crna Gora': 'Montenegro', 'Balkan': 'Balkans', 'Evropa': 'Europe', 'Svijet': 'World', 'Novčići': 'Coins', 'cilj': 'finish',
  'Melodija svira dok ne odgovoriš.': 'The tune plays until you answer.', 'Tapni na mapu, pa potvrdi.': 'Tap the map, then confirm.',
  'Prevuci stavke ili koristi strelice.': 'Drag the items or use the arrows.', 'Tapni pojam lijevo, pa njegov par desno.': 'Tap an item on the left, then its pair on the right.',
  'Pitanja su na našem jeziku, prevod na engleski stiže uskoro.': 'Questions are in Montenegrin/Serbian for now; English is coming soon.'
};
const EN_RX = [
  [/^Runda (\d+)$/, 'Round $1'], [/^\+(\d+) polja?$/, '+$1 tiles'], [/^(\d+) pitanja$/, '$1 questions'], [/^uskoro$/, 'coming soon'],
  [/^Izabrano (\d+) od 3\.$/, '$1 of 3 selected.'], [/^Kupi \((\d+)\)$/, 'Buy ($1)'], [/^Stanica · (.+)$/, 'Station · $1'],
  [/^(\d+) tačnih · (\d+) s$/, '$1 correct · $2 s'], [/^(\d+) pogodaka · (\d+) s$/, '$1 hits · $2 s'], [/^Mini igra · (\d+) s$/, 'Mini game · $1 s'],
  [/^Otključava se na rangu (.+)$/, 'Unlocks at rank $1']
];
export function toEn(text) {
  const t = text.trim();
  if (!t) return text;
  if (EN_EXACT[t]) return text.replace(t, EN_EXACT[t]);
  for (const [rx, rep] of EN_RX) if (rx.test(t)) return text.replace(t, t.replace(rx, rep));
  return text;
}

export function L(text) {
  if (text == null) return text;
  const s = String(text);
  return mode === 'cyr' ? toCyr(s) : mode === 'en' ? toEn(s) : s;
}

const SKIP = new Set(['SCRIPT', 'STYLE', 'TEXTAREA', 'INPUT']);
function translateNode(node) {
  if (mode === 'lat') return;
  if (node.nodeType === 3) {
    const p = node.parentElement;
    if (!p || SKIP.has(p.tagName) || p.closest('[translate="no"]')) return;
    const v = L(node.nodeValue);
    if (v !== node.nodeValue) node.nodeValue = v;
  } else if (node.nodeType === 1 && !SKIP.has(node.tagName)) {
    node.childNodes.forEach(translateNode);
  }
}
let observer = null;
export function setLang(m, root = document.getElementById('app')) {
  mode = LANGS[m] ? m : 'lat';
  document.documentElement.lang = mode === 'en' ? 'en' : mode === 'cyr' ? 'sr-Cyrl' : 'sr-Latn';
  observer?.disconnect();
  if (mode !== 'lat') {
    observer = new MutationObserver(list => list.forEach(r => {
      if (r.type === 'characterData') translateNode(r.target);
      else r.addedNodes.forEach(translateNode);
    }));
    observer.observe(root, { childList: true, subtree: true, characterData: true });
    translateNode(root);
  }
}
