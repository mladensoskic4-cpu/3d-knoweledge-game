// Pravi mediji tokom igre:
// - fotografije: glavna slika članka sa Wikipedije (REST API), uz link na izvor;
// - zvuci životinja: snimci sa Wikimedia Commonsa (pretraga zvučnih fajlova);
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

export async function commonsSound(query) {
  const key = `snd:${query}`;
  if (cache.has(key)) return cache.get(key);
  const job = (async () => {
    try {
      const url = 'https://commons.wikimedia.org/w/api.php?action=query&format=json&origin=*&generator=search&gsrnamespace=6&gsrlimit=10'
        + `&gsrsearch=${encodeURIComponent(query + ' filetype:audio')}&prop=imageinfo&iiprop=url|mime|size`;
      const r = await withTimeout(fetch(url), 6000);
      if (!r.ok) return null;
      const d = await r.json();
      const files = Object.values(d.query?.pages || {})
        .map(p => ({ title: p.title, ...(p.imageinfo?.[0] || {}) }))
        .filter(f => f.url && /^audio\//.test(f.mime || '') && (f.size || 0) < 3e6)
        .sort((a, b) => (a.size || 0) - (b.size || 0));
      for (const f of files) {
        let src = f.url;
        if (/ogg|opus|webm/.test(f.mime) && !canOgg) {
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
