// Pravi mediji tokom igre:
// - fotografije: glavna slika članka sa Wikipedije (REST API), uz link na izvor;
// - zvuci životinja: snimci ugrađeni u Wikipedia članak o toj životinji (Wikimedia Commons);
// - zvuci instrumenata: snimci iz FluidR3 GM soundfonta (CC BY 3.0) u data/zvuk/.
// Ako mreža ili izvor nisu dostupni, funkcije vraćaju null, a igra koristi rezervu (ilustraciju ili sliku).

const cache = new Map();
function withTimeout(p, ms) {
  return Promise.race([p, new Promise((_, rej) => setTimeout(() => rej(new Error('timeout')), ms))]);
}

export async function wikiImage(title, lang = 'en') {
  const key = `img:${lang}:${title}`;
  if (cache.has(key)) return cache.get(key);
  const job = (async () => {
    try {
      const r = await withTimeout(fetch(`https://${lang}.wikipedia.org/api/rest_v1/page/summary/${encodeURIComponent(title)}?redirect=true`), 5000);
      if (!r.ok) return null;
      const d = await r.json();
      let src = d.thumbnail?.source || d.originalimage?.source;
      if (!src) return null;
      src = src.replace(/\/(\d+)px-/, '/640px-');
      const page = d.content_urls?.desktop?.page || `https://${lang}.wikipedia.org/wiki/${encodeURIComponent(title)}`;
      return { src, page, credit: 'Foto: Wikipedia / Wikimedia Commons' };
    } catch { return null; }
  })();
  cache.set(key, job);
  return job;
}

export function preloadImage(src) {
  return new Promise(res => { const i = new Image(); i.onload = () => res(true); i.onerror = () => res(false); i.src = src; });
}

const canOgg = (() => { try { return !!new Audio().canPlayType('audio/ogg; codecs="vorbis"'); } catch { return false; } })();

// Zvuk životinje: SAMO snimci koji su ugrađeni u Wikipedia članak o toj životinji (urednici ih provjeravaju),
// i to samo fajlovi čiji naziv sadrži ime životinje (npr. „Dog barking.ogg”, „Strix aluco … .ogg”).
// Izgovori riječi (En-us-…, LL-…) i izgovoreni članci se preskaču. Nema pogađanja preko slobodne pretrage.
const AUDIO_RX = /\.(ogg|oga|opus|wav|mp3|flac|webm)$/i;
const BAD_RX = /spoken|pronunc|^File:[A-Za-z]{2}(-[A-Za-z]{2})?-|^File:LL-|wikipedia|anthem|song by|music/i;
export function soundFileOk(name, rx) { return AUDIO_RX.test(name) && !BAD_RX.test(name) && rx.test(name.replace(/_/g, ' ')); }
export async function wikiSound(titles, rx) {
  const key = `snd:${titles.join('|')}`;
  if (cache.has(key)) return cache.get(key);
  const job = (async () => {
    try {
      const u1 = 'https://en.wikipedia.org/w/api.php?action=query&format=json&origin=*&redirects=1&prop=images&imlimit=500&titles='
        + encodeURIComponent(titles.join('|'));
      const r1 = await withTimeout(fetch(u1), 6000);
      if (!r1.ok) return null;
      const d1 = await r1.json();
      const names = [...new Set(Object.values(d1.query?.pages || {}).flatMap(p => (p.images || []).map(i => i.title)))]
        .filter(n => soundFileOk(n, rx));
      if (!names.length) return null;
      const u2 = 'https://commons.wikimedia.org/w/api.php?action=query&format=json&origin=*&prop=imageinfo&iiprop=url|mime|size&titles='
        + encodeURIComponent(names.slice(0, 20).join('|'));
      const r2 = await withTimeout(fetch(u2), 6000);
      if (!r2.ok) return null;
      const d2 = await r2.json();
      const files = Object.values(d2.query?.pages || {})
        .map(p => ({ title: p.title, ...(p.imageinfo?.[0] || {}) }))
        .filter(f => f.url && /^(audio|video|application\/ogg)/.test(f.mime || '') && f.size > 15000 && f.size < 4e6)
        .sort((a, b) => a.size - b.size);
      for (const f of files) {
        let src = f.url;
        if (/ogg|opus|webm/.test(f.mime + src) && !canOgg) {
          // Safari/iOS: Commons pravi mp3 kopiju ogg snimaka
          const m = src.match(/\/commons\/(\w)\/(\w\w)\/(.+)$/);
          if (!m) continue;
          src = `https://upload.wikimedia.org/wikipedia/commons/transcoded/${m[1]}/${m[2]}/${m[3]}/${m[3]}.mp3`;
        }
        return { src, page: `https://commons.wikimedia.org/wiki/${encodeURIComponent(f.title)}` };
      }
      return null;
    } catch { return null; }
  })();
  cache.set(key, job);
  return job;
}

// Pušta kratku frazu instrumenta iz data/zvuk/<folder>/ (C4, E4, G4, C5).
let current = [];
export function stopSounds() { current.forEach(a => { try { a.pause(); } catch { /* već zaustavljen */ } }); current = []; }
export function playInstrument(folder) {
  stopSounds();
  const notes = ['C4', 'E4', 'G4', 'C5'];
  notes.forEach((n, i) => setTimeout(() => {
    const a = new Audio(`data/zvuk/${folder}/${n}.mp3`);
    a.volume = 0.9; current.push(a);
    a.play().catch(() => {});
  }, i * 480));
}
export async function playUrl(src, { volume = 0.9, maxMs = 6000 } = {}) {
  stopSounds();
  const a = new Audio(src);
  a.volume = volume; current.push(a);
  try {
    await withTimeout(a.play(), 5000);
    setTimeout(() => { try { a.pause(); } catch { /* */ } }, maxMs);
    return true;
  } catch { return false; }
}
